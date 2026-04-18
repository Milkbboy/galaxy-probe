#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DrillCorp.VFX;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// VFX 2D → 3D 일괄 전환 (Polygon Arsenal 기반).
    ///
    /// 포함 범위:
    ///   • Phase 2       — MachineGun (BulletVisual + HitVfx)
    ///   • Phase 3-1     — Bomb (Projectile Visual + Explosion + HitVfx)
    ///   • Phase 3-2     — Shotgun (MuzzleVfx)
    ///   • Phase 4 (라이트) — Laser 지면 스코치 (PA DamageAuraFire + LaserScorchDecay)
    ///   • Data SO 갱신  — Weapon_MachineGun / Weapon_Bomb / Weapon_Shotgun / Weapon_LaserBeam
    ///
    /// 제외 (수동/후속 작업):
    ///   • LockOnMarker / BombLandingMarker — 커스텀 SR 컴포넌트 의존 (별도 리팩터 필요)
    ///   • LaserBeam 본체 — 프로토 포팅 복잡도 높음 (기존 4겹 링 + LifeArc 그대로 유지)
    ///
    /// 산출물:
    ///   • Assets/_Game/Prefabs/Weapons/VFX_3D/   — PA Variant + VisualSocket 서브패턴
    ///   • Assets/_Game/Prefabs/Weapons/*_3D.prefab — 재구성된 Projectile 로직 프리펩
    ///   • Weapon_*.asset SO 필드가 새 프리펩을 참조
    ///
    /// 원본 2D 프리펩은 삭제하지 않음 — SO 필드만 스왑하므로 언제든 롤백 가능.
    /// </summary>
    public static class VFX3DMigration
    {
        // ═══════════ 경로 ═══════════

        private const string VFX3DFolder = "Assets/_Game/Prefabs/Weapons/VFX_3D";
        private const string WeaponPrefabFolder = "Assets/_Game/Prefabs/Weapons";
        private const string WeaponDataFolder = "Assets/_Game/Data/Weapons";
        private const string PA = "Assets/Polygon Arsenal/Prefabs";

        // 탑뷰 소켓 회전 — 파티클이 +Z(화면 위) 쪽으로 퍼지도록
        private static readonly Vector3 TopViewEuler = new Vector3(-90f, 0f, 0f);

        // ═══════════ 매핑 스펙 ═══════════

        /// <summary>
        /// One-shot VFX — Instantiate 후 파티클 종료 시 자동 삭제.
        /// 루트에 AutoDestroyPS 부착 + 루트 회전 (-90,0,0).
        /// </summary>
        private static readonly OneShotSpec[] OneShots = new[]
        {
            new OneShotSpec {
                Source = PA + "/Combat/Explosions/Sci-Fi/Rocket/RocketExplosionRed.prefab",
                Dst    = VFX3DFolder + "/Bomb_Explosion_3D.prefab",
                Role   = "Bomb 폭발"
            },
            new OneShotSpec {
                Source = PA + "/Combat/Explosions/Mini/MiniExploFire.prefab",
                Dst    = VFX3DFolder + "/Bomb_HitVfx_3D.prefab",
                Role   = "Bomb 개별 피격"
            },
            new OneShotSpec {
                Source = PA + "/Combat/Muzzleflash/Standard/ShotgunMuzzleStandard.prefab",
                Dst    = VFX3DFolder + "/Shotgun_Muzzle_3D.prefab",
                Role   = "Shotgun 머즐"
            },
            new OneShotSpec {
                Source = PA + "/Combat/Explosions/Sci-Fi/Bullet/BulletExplosionBlue.prefab",
                Dst    = VFX3DFolder + "/MG_HitVfx_3D.prefab",
                Role   = "MachineGun 피격"
            },
        };

        // Laser 스코치 (장판형 화염) — 루프 파티클이라 AutoDestroyPS 안 쓰고 LaserScorchDecay로 페이드 제어.
        private const string LaserScorchSource = PA + "/Combat/Aura/DamageAura/DamageAuraFire.prefab";
        private const string LaserScorchDst = VFX3DFolder + "/Laser_Scorch_3D.prefab";

        /// <summary>
        /// Projectile Visual — 지속형 프리펩의 VisualSocket 자식으로 들어감.
        /// AutoDestroyPS 불필요 (부모 로직이 Destroy 제어).
        /// 루트 회전만 (-90,0,0) 적용.
        /// </summary>
        private static readonly OneShotSpec[] ProjectileVisuals = new[]
        {
            new OneShotSpec {
                Source = PA + "/Combat/Missiles/Sci-Fi/Bullet/BulletBlue.prefab",
                Dst    = VFX3DFolder + "/MG_BulletVisual_3D.prefab",
                Role   = "MachineGun 탄환 비주얼",
                NoAutoDestroy = true
            },
            new OneShotSpec {
                Source = PA + "/Combat/Missiles/Sci-Fi/Rocket/RocketMissileRed.prefab",
                Dst    = VFX3DFolder + "/Bomb_ProjectileVisual_3D.prefab",
                Role   = "Bomb 투사체 비주얼",
                NoAutoDestroy = true
            },
        };

        // ═══════════ 메뉴 ═══════════

        private const string MenuRoot = "Tools/Drill-Corp/VFX 3D Migration/";

        [MenuItem(MenuRoot + "\u25B6 전체 실행 (Run All Phases)")]
        public static void RunAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "VFX 3D Migration — 전체 실행",
                    "다음 작업을 일괄 실행합니다:\n\n" +
                    "  1. VFX_3D/ 폴더 + 서브 프리펩 생성\n" +
                    "  2. MachineGunBullet_3D / BombProjectile_3D 프리펩 재구성\n" +
                    "  3. Weapon_MachineGun / Weapon_Bomb / Weapon_Shotgun SO 레퍼런스 갱신\n\n" +
                    "원본 2D 프리펩은 삭제하지 않습니다 (SO 필드만 스왑).\n" +
                    "계속하시겠습니까?",
                    "실행", "취소"))
            {
                return;
            }

            EnsureFolder("Assets/_Game/Prefabs", "Weapons");
            EnsureFolder(WeaponPrefabFolder, "VFX_3D");

            int ok = 0, skipped = 0;
            ApplySpecs(OneShots, withAutoDestroy: true, ref ok, ref skipped);
            ApplySpecs(ProjectileVisuals, withAutoDestroy: false, ref ok, ref skipped);
            if (CreateLaserScorchVariant()) ok++; else skipped++;

            // 비주얼 스케일 — 히트 판정에는 영향 없음 (VisualSocket에만 적용).
            // MG: PA BulletBlue가 얇은 편이라 2.5배로 키워 눈에 잘 띄게
            // Bomb: PA RocketMissile이 이미 커서 1.2배 약간만 보정
            var mgBullet3D = BuildProjectile3D(
                sourcePath: WeaponPrefabFolder + "/MachineGunBullet.prefab",
                dstPath: WeaponPrefabFolder + "/MachineGunBullet_3D.prefab",
                visualPath: VFX3DFolder + "/MG_BulletVisual_3D.prefab",
                role: "MachineGunBullet_3D",
                visualScale: 2.5f);

            var bombProj3D = BuildProjectile3D(
                sourcePath: WeaponPrefabFolder + "/BombProjectile.prefab",
                dstPath: WeaponPrefabFolder + "/BombProjectile_3D.prefab",
                visualPath: VFX3DFolder + "/Bomb_ProjectileVisual_3D.prefab",
                role: "BombProjectile_3D",
                visualScale: 1.2f);

            UpdateWeaponMachineGun(mgBullet3D);
            UpdateWeaponBomb(bombProj3D);
            UpdateWeaponShotgun();
            UpdateWeaponLaserBeam();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[VFX3DMigration] \u2705 완료 — 신규 {ok}개 / 생략 {skipped}개 (이미 존재)\n\n" +
                "다음 단계:\n" +
                "  \u2022 Play 모드에서 MachineGun·Shotgun·Bomb 발사 → 방향/스케일 육안 확인\n" +
                "  \u2022 어색하면 해당 Variant 선택 → Transform rotation/scale 조정\n" +
                "  \u2022 LockOn / LandingMarker / Laser는 별도 수동 작업 대상\n\n" +
                "롤백: 각 Weapon_*.asset의 VFX 필드를 원본 2D 프리펩으로 되돌리면 즉시 복구됨.");
        }

        [MenuItem(MenuRoot + "2. One-shot Variants만 생성")]
        public static void RunOneShotsOnly()
        {
            EnsureFolder("Assets/_Game/Prefabs", "Weapons");
            EnsureFolder(WeaponPrefabFolder, "VFX_3D");
            int ok = 0, skipped = 0;
            ApplySpecs(OneShots, withAutoDestroy: true, ref ok, ref skipped);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[VFX3DMigration] One-shot Variants — 신규 {ok} / 생략 {skipped}");
        }

        [MenuItem(MenuRoot + "3. Projectile Visual Variants만 생성")]
        public static void RunProjectileVisualsOnly()
        {
            EnsureFolder("Assets/_Game/Prefabs", "Weapons");
            EnsureFolder(WeaponPrefabFolder, "VFX_3D");
            int ok = 0, skipped = 0;
            ApplySpecs(ProjectileVisuals, withAutoDestroy: false, ref ok, ref skipped);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[VFX3DMigration] Projectile Visuals — 신규 {ok} / 생략 {skipped}");
        }

        [MenuItem(MenuRoot + "4. Projectile 프리펩 재구성만")]
        public static void RunProjectilePrefabsOnly()
        {
            BuildProjectile3D(
                WeaponPrefabFolder + "/MachineGunBullet.prefab",
                WeaponPrefabFolder + "/MachineGunBullet_3D.prefab",
                VFX3DFolder + "/MG_BulletVisual_3D.prefab",
                "MachineGunBullet_3D",
                visualScale: 2.5f);
            BuildProjectile3D(
                WeaponPrefabFolder + "/BombProjectile.prefab",
                WeaponPrefabFolder + "/BombProjectile_3D.prefab",
                VFX3DFolder + "/Bomb_ProjectileVisual_3D.prefab",
                "BombProjectile_3D",
                visualScale: 1.2f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem(MenuRoot + "7. Projectile _3D 프리펩 재생성 (삭제 후 재빌드)")]
        public static void RegenerateProjectilePrefabs()
        {
            if (!EditorUtility.DisplayDialog(
                    "Projectile _3D 재생성",
                    "MachineGunBullet_3D.prefab 와 BombProjectile_3D.prefab 를 삭제하고\n" +
                    "현재 설정(MG scale 2.5 / Bomb scale 1.2)으로 재빌드합니다.\n\n" +
                    "원본 2D 프리펩과 Data SO 참조는 그대로 유지됩니다.\n" +
                    "계속할까요?",
                    "재생성", "취소"))
                return;

            TryDelete(WeaponPrefabFolder + "/MachineGunBullet_3D.prefab");
            TryDelete(WeaponPrefabFolder + "/BombProjectile_3D.prefab");

            RunProjectilePrefabsOnly();

            // Data SO 참조가 삭제되면서 끊겼으므로 다시 연결
            var mg = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabFolder + "/MachineGunBullet_3D.prefab");
            var bomb = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabFolder + "/BombProjectile_3D.prefab");
            UpdateWeaponMachineGun(mg);
            UpdateWeaponBomb(bomb);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VFX3DMigration] \u2705 Projectile _3D 재생성 + SO 재연결 완료");
        }

        private static void TryDelete(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [MenuItem(MenuRoot + "5. Data SO 갱신만")]
        public static void RunDataOnly()
        {
            var mg = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabFolder + "/MachineGunBullet_3D.prefab");
            var bomb = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPrefabFolder + "/BombProjectile_3D.prefab");
            UpdateWeaponMachineGun(mg);
            UpdateWeaponBomb(bomb);
            UpdateWeaponShotgun();
            UpdateWeaponLaserBeam();
            AssetDatabase.SaveAssets();
        }

        [MenuItem(MenuRoot + "6. Laser 스코치만 생성 + SO 연결")]
        public static void RunLaserScorchOnly()
        {
            EnsureFolder("Assets/_Game/Prefabs", "Weapons");
            EnsureFolder(WeaponPrefabFolder, "VFX_3D");
            bool created = CreateLaserScorchVariant();
            UpdateWeaponLaserBeam();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[VFX3DMigration] Laser 스코치 — {(created ? "신규 생성" : "이미 존재")} + Weapon_LaserBeam SO 갱신");
        }

        // ═══════════ 핵심 로직 ═══════════

        /// <summary>
        /// PA 소스 프리펩을 기반으로 우리 VFX_3D 폴더에 Variant 생성.
        /// 루트 회전 (-90,0,0) 적용. 필요 시 AutoDestroyPS 부착.
        /// </summary>
        private static void ApplySpecs(OneShotSpec[] specs, bool withAutoDestroy, ref int ok, ref int skipped)
        {
            foreach (var spec in specs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(spec.Dst) != null)
                {
                    skipped++;
                    continue;
                }

                var source = AssetDatabase.LoadAssetAtPath<GameObject>(spec.Source);
                if (source == null)
                {
                    Debug.LogWarning($"[VFX3DMigration] 소스 없음 — 건너뜀: {spec.Source} ({spec.Role})");
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                try
                {
                    instance.transform.localRotation = Quaternion.Euler(TopViewEuler);

                    if (withAutoDestroy && !spec.NoAutoDestroy)
                    {
                        var ps = instance.GetComponent<ParticleSystem>();
                        if (ps != null && instance.GetComponent<AutoDestroyPS>() == null)
                        {
                            instance.AddComponent<AutoDestroyPS>();
                        }
                        else if (ps == null)
                        {
                            Debug.LogWarning(
                                $"[VFX3DMigration] 루트 PS 없음 — AutoDestroyPS 생략: {spec.Dst} ({spec.Role})");
                        }
                    }

                    var saved = PrefabUtility.SaveAsPrefabAsset(instance, spec.Dst);
                    if (saved != null) ok++;
                    else Debug.LogError($"[VFX3DMigration] 저장 실패: {spec.Dst}");
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        /// <summary>
        /// 지속형 프리펩(MachineGunBullet/BombProjectile) 재구성.
        /// 원본을 언팩 → SR/TrailRenderer 제거 → VisualSocket 자식 추가 → PA Visual 중첩 → 새 경로 저장.
        /// 원본 프리펩은 건드리지 않음. visualScale은 VisualSocket 스케일로 적용되어 히트 판정에는 영향 없음.
        /// </summary>
        private static GameObject BuildProjectile3D(string sourcePath, string dstPath, string visualPath, string role, float visualScale = 1.0f)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(dstPath) != null)
            {
                Debug.Log($"[VFX3DMigration] 이미 존재 — 건너뜀: {dstPath}");
                return AssetDatabase.LoadAssetAtPath<GameObject>(dstPath);
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogWarning($"[VFX3DMigration] 원본 없음 — 건너뜀: {sourcePath} ({role})");
                return null;
            }

            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
            if (visual == null)
            {
                Debug.LogWarning($"[VFX3DMigration] Visual Variant 없음 — 먼저 'Projectile Visual Variants만 생성' 실행 필요: {visualPath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                // 2D 비주얼 컴포넌트 제거
                if (instance.TryGetComponent<SpriteRenderer>(out var sr)) Object.DestroyImmediate(sr);
                if (instance.TryGetComponent<TrailRenderer>(out var tr)) Object.DestroyImmediate(tr);

                // 루트 회전·스케일 초기화 (2D용 90도 기울기 제거)
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                // VisualSocket 자식 — 탑뷰 회전 + 비주얼 스케일 담당 (히트 판정에는 영향 없음)
                var socket = new GameObject("VisualSocket");
                socket.transform.SetParent(instance.transform, false);
                socket.transform.localRotation = Quaternion.Euler(TopViewEuler);
                socket.transform.localScale = Vector3.one * visualScale;

                // PA Visual Variant 중첩
                var visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(visual, socket.transform);
                visualInstance.transform.localPosition = Vector3.zero;
                visualInstance.transform.localRotation = Quaternion.identity;

                var saved = PrefabUtility.SaveAsPrefabAsset(instance, dstPath);
                if (saved == null)
                {
                    Debug.LogError($"[VFX3DMigration] 저장 실패: {dstPath}");
                    return null;
                }

                Debug.Log($"[VFX3DMigration] \u2713 Projectile 3D 생성: {dstPath} ({role})");
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// Laser 지면 스코치 Variant 생성 — PA DamageAuraFire 기반 + LaserScorchDecay 부착.
        /// 루트 회전 (-90,0,0) 적용. 이미 존재하면 생략.
        /// </summary>
        private static bool CreateLaserScorchVariant()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LaserScorchDst) != null)
            {
                return false;
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(LaserScorchSource);
            if (source == null)
            {
                Debug.LogWarning($"[VFX3DMigration] Laser 스코치 소스 없음 — 건너뜀: {LaserScorchSource}");
                return false;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                instance.transform.localRotation = Quaternion.Euler(TopViewEuler);

                if (instance.GetComponent<LaserScorchDecay>() == null)
                {
                    instance.AddComponent<LaserScorchDecay>();
                }

                var saved = PrefabUtility.SaveAsPrefabAsset(instance, LaserScorchDst);
                if (saved == null)
                {
                    Debug.LogError($"[VFX3DMigration] Laser 스코치 저장 실패: {LaserScorchDst}");
                    return false;
                }
                Debug.Log($"[VFX3DMigration] \u2713 Laser 스코치 Variant 생성: {LaserScorchDst}");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // ═══════════ Data SO 갱신 ═══════════

        private static void UpdateWeaponMachineGun(GameObject bullet3D)
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableObject>(WeaponDataFolder + "/Weapon_MachineGun.asset");
            if (data == null)
            {
                Debug.LogWarning("[VFX3DMigration] Weapon_MachineGun.asset 없음 — 생략");
                return;
            }
            var so = new SerializedObject(data);
            if (bullet3D != null) SetObject(so, "_bulletPrefab", bullet3D);
            var hitVfx = AssetDatabase.LoadAssetAtPath<GameObject>(VFX3DFolder + "/MG_HitVfx_3D.prefab");
            if (hitVfx != null) SetObject(so, "_hitVfxPrefab", hitVfx);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            Debug.Log("[VFX3DMigration] \u2713 Weapon_MachineGun — bulletPrefab / hitVfxPrefab 갱신");
        }

        private static void UpdateWeaponBomb(GameObject projectile3D)
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableObject>(WeaponDataFolder + "/Weapon_Bomb.asset");
            if (data == null)
            {
                Debug.LogWarning("[VFX3DMigration] Weapon_Bomb.asset 없음 — 생략");
                return;
            }
            var so = new SerializedObject(data);
            if (projectile3D != null) SetObject(so, "_projectilePrefab", projectile3D);
            var explosion = AssetDatabase.LoadAssetAtPath<GameObject>(VFX3DFolder + "/Bomb_Explosion_3D.prefab");
            if (explosion != null) SetObject(so, "_explosionVfxPrefab", explosion);
            var hitVfx = AssetDatabase.LoadAssetAtPath<GameObject>(VFX3DFolder + "/Bomb_HitVfx_3D.prefab");
            if (hitVfx != null) SetObject(so, "_hitVfxPrefab", hitVfx);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            Debug.Log("[VFX3DMigration] \u2713 Weapon_Bomb — projectile / explosion / hitVfx 갱신 (landingMarker는 수동)");
        }

        private static void UpdateWeaponShotgun()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableObject>(WeaponDataFolder + "/Weapon_Shotgun.asset");
            if (data == null)
            {
                Debug.LogWarning("[VFX3DMigration] Weapon_Shotgun.asset 없음 — 생략");
                return;
            }
            var so = new SerializedObject(data);
            var muzzle = AssetDatabase.LoadAssetAtPath<GameObject>(VFX3DFolder + "/Shotgun_Muzzle_3D.prefab");
            if (muzzle != null) SetObject(so, "_muzzleVfxPrefab", muzzle);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            Debug.Log("[VFX3DMigration] \u2713 Weapon_Shotgun — muzzleVfxPrefab 갱신");
        }

        private static void UpdateWeaponLaserBeam()
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableObject>(WeaponDataFolder + "/Weapon_LaserBeam.asset");
            if (data == null)
            {
                Debug.LogWarning("[VFX3DMigration] Weapon_LaserBeam.asset 없음 — 생략");
                return;
            }
            var so = new SerializedObject(data);
            var scorch = AssetDatabase.LoadAssetAtPath<GameObject>(LaserScorchDst);
            if (scorch != null) SetObject(so, "_scorchPrefab", scorch);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            Debug.Log("[VFX3DMigration] \u2713 Weapon_LaserBeam — scorchPrefab 갱신");
        }

        // ═══════════ 유틸 ═══════════

        private static void EnsureFolder(string parent, string newFolder)
        {
            string full = parent + "/" + newFolder;
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, newFolder);
        }

        private static void SetObject(SerializedObject so, string field, Object v)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = v;
            else Debug.LogWarning($"[VFX3DMigration] SerializedProperty 없음: {field}");
        }

        private struct OneShotSpec
        {
            public string Source;
            public string Dst;
            public string Role;
            public bool NoAutoDestroy;
        }
    }
}
#endif

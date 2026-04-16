#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using DrillCorp.Weapon.MachineGun;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// 기관총 무기 자산 일괄 생성:
    /// 1) MachineGunBullet 스프라이트 (PNG, 작은 발광 핵)
    /// 2) BombTrail_Mat과 동일한 Sprites/Default 머티리얼 (탄환 트레일용 — 새로 만들거나 폭탄 머티리얼 재사용)
    /// 3) MachineGunBullet.prefab (SpriteRenderer + TrailRenderer + MachineGunBullet)
    /// 4) Weapon_MachineGun.asset (MachineGunData SO — 위 프리펩 자동 참조 + 프로토타입 기본값)
    ///
    /// BombPrefabCreator와 동일 패턴. 메뉴 한 번으로 기관총 동작에 필요한 자산 전부 생성.
    /// </summary>
    public static class MachineGunPrefabCreator
    {
        // === 경로 ===
        private const string PrefabFolder = "Assets/_Game/Prefabs/Weapons";
        private const string DataFolder = "Assets/_Game/Data/Weapons";

        private const string BulletSpritePath = PrefabFolder + "/MachineGunBulletSprite.png";
        private const string BulletPrefabPath = PrefabFolder + "/MachineGunBullet.prefab";
        private const string TrailMaterialPath = PrefabFolder + "/MachineGunTrail_Mat.mat";
        private const string DataAssetPath = DataFolder + "/Weapon_MachineGun.asset";

        // === 색 (프로토타입 #4fc3f7 파랑) ===
        private static readonly Color GunBlue = new Color(0.298f, 0.765f, 0.969f, 1f);

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/7. 기관총 자산 일괄 생성")]
        public static void CreateAll()
        {
            EnsureFolders();

            bool spriteCreated = EnsureSprite(BulletSpritePath, CreateBulletPng);
            if (spriteCreated)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var bulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BulletSpritePath);
            if (bulletSprite == null)
            {
                Debug.LogError("[MachineGunPrefabCreator] 스프라이트 로드 실패. 메뉴 재실행 권장.");
                return;
            }

            var trailMat = EnsureTrailMaterial();
            var bulletPrefab = CreateBulletPrefab(bulletSprite, trailMat);
            if (bulletPrefab == null) return;

            var data = CreateOrUpdateData(bulletPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
            Debug.Log(
                "[MachineGunPrefabCreator] ✅ 기관총 자산 일괄 생성 완료\n" +
                $"  • {BulletPrefabPath}\n" +
                $"  • {TrailMaterialPath}\n" +
                $"  • {DataAssetPath}\n" +
                "다음: 씬에 MachineGunWeapon GameObject를 추가하고 Data에 Weapon_MachineGun 할당."
            );
        }

        // ═══════════ 폴더 ═══════════

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Game", "Prefabs");
            EnsureFolder("Assets/_Game/Prefabs", "Weapons");
            EnsureFolder("Assets/_Game", "Data");
            EnsureFolder("Assets/_Game/Data", "Weapons");
        }

        private static void EnsureFolder(string parent, string newFolder)
        {
            string full = parent + "/" + newFolder;
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, newFolder);
        }

        // ═══════════ 스프라이트 ═══════════

        private static bool EnsureSprite(string path, System.Action createPng)
        {
            bool existed = File.Exists(path);
            if (!existed) createPng();

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return !existed;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (Mathf.Abs(importer.spritePixelsPerUnit - 100f) > 0.01f) { importer.spritePixelsPerUnit = 100f; changed = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }

            if (changed || !existed) importer.SaveAndReimport();
            return !existed;
        }

        /// <summary>
        /// 탄환 PNG: 32×32 작은 발광 핵 (흰 코어 → 하늘색 → 페이드)
        /// 빠르게 날아가는 트레이서 느낌
        /// </summary>
        private static void CreateBulletPng()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            float c = (size - 1) * 0.5f;
            float coreR = size * 0.18f;
            float midR = size * 0.34f;
            float outerR = size * 0.46f;

            Color hotCore = new Color(1f, 1f, 1f, 1f);              // 흰 코어
            Color hotMid = new Color(0.7f, 0.95f, 1f, 1f);           // 옅은 하늘
            Color edge = GunBlue;                                    // 파랑 외곽

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color px = new Color(0, 0, 0, 0);

                    if (d <= coreR) px = hotCore;
                    else if (d <= midR) px = Color.Lerp(hotCore, hotMid, Mathf.InverseLerp(coreR, midR, d));
                    else if (d <= outerR) px = Color.Lerp(hotMid, edge, Mathf.InverseLerp(midR, outerR, d));
                    else if (d <= outerR + 1.5f)
                    {
                        // 안티앨리어스 페이드
                        float a = 1f - Mathf.Clamp01((d - outerR) / 1.5f);
                        px = new Color(edge.r, edge.g, edge.b, a);
                    }

                    pixels[y * size + x] = px;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(BulletSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // ═══════════ 트레일 머티리얼 ═══════════

        /// <summary>
        /// TrailRenderer용 머티리얼 — 에셋으로 저장해야 프리펩에 참조 유지됨
        /// (런타임 new Material()을 sharedMaterial에 직접 넣으면 프리펩 저장 시 null로 빠져
        ///  마젠타/핑크 'Missing Material' 색으로 렌더링됨 — 폭탄에서 겪었던 버그)
        /// </summary>
        private static Material EnsureTrailMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(TrailMaterialPath);
            if (mat != null) return mat;

            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null)
            {
                Debug.LogError("[MachineGunPrefabCreator] Trail 머티리얼용 셰이더를 찾을 수 없습니다.");
                return null;
            }

            mat = new Material(sh) { name = "MachineGunTrail_Mat" };
            AssetDatabase.CreateAsset(mat, TrailMaterialPath);
            return mat;
        }

        // ═══════════ 프리펩 ═══════════

        private static GameObject CreateBulletPrefab(Sprite sprite, Material trailMat)
        {
            var root = new GameObject("MachineGunBullet");
            try
            {
                // 탑뷰 (X=90) — 스프라이트가 XZ 평면에 누움
                root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                // 32px / 100ppu × 1.5 = 0.48 유닛 지름 (작지만 또렷)
                root.transform.localScale = Vector3.one * 1.5f;

                var sr = root.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = Color.white; // 스프라이트 자체가 색 가지고 있음
                sr.sortingOrder = 8;

                // 짧은 트레이서 잔상 (탄이 작아서 트레일이 시각적 핵심)
                AddTrailRenderer(root, trailMat);

                root.AddComponent<MachineGunBullet>();

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, BulletPrefabPath, out bool ok);
                if (!ok || prefab == null)
                {
                    Debug.LogError($"[MachineGunPrefabCreator] 프리펩 저장 실패: {BulletPrefabPath}");
                    return null;
                }
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 탄환 트레일 — 0.15초 짧고 얇음 (탄이 빠르게 사라지므로 짧게)
        /// </summary>
        private static void AddTrailRenderer(GameObject root, Material trailMat)
        {
            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.15f;
            // 굵기는 transform.lossyScale에 곱해짐 → 본체 scale 1.5에서 startWidth 0.3 = 0.45 유닛
            trail.startWidth = 0.3f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.03f;
            trail.numCornerVertices = 1;
            trail.numCapVertices = 1;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sortingOrder = 7;

            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(GunBlue, 0f), new GradientColorKey(GunBlue, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            trail.colorGradient = grad;

            if (trailMat != null) trail.sharedMaterial = trailMat;
        }

        // ═══════════ 데이터 (MachineGunData SO) ═══════════

        private static MachineGunData CreateOrUpdateData(GameObject bulletPrefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<MachineGunData>(DataAssetPath);
            bool isNew = data == null;

            if (isNew)
            {
                data = ScriptableObject.CreateInstance<MachineGunData>();
                AssetDatabase.CreateAsset(data, DataAssetPath);
            }

            var so = new SerializedObject(data);

            // 신규 생성 시에만 기본값 채움 — 사용자 인스펙터 튜닝 값 보존
            if (isNew)
            {
                SetString(so, "_displayName", "기관총");
                SetString(so, "_description", "자동 연사 + 산포. 40발 탄창 / 5초 리로딩.");
                SetColor(so, "_themeColor", GunBlue);
                SetFloat(so, "_fireDelay", 0.14f);
                SetFloat(so, "_damage", 0.5f);
                SetInt(so, "_maxAmmo", 40);
                SetFloat(so, "_reloadDuration", 5f);
                SetInt(so, "_lowAmmoThreshold", 8);
                SetFloat(so, "_bulletSpeed", 9f);
                SetFloat(so, "_bulletLifetime", 1.5f);
                SetFloat(so, "_bulletHitRadius", 0.15f);
                SetFloat(so, "_spreadAngle", 0.06f);
            }

            // 프리펩 참조는 항상 갱신 (재생성된 프리펩으로 교체)
            SetObject(so, "_bulletPrefab", bulletPrefab);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void SetString(SerializedObject so, string field, string v)
        {
            var p = so.FindProperty(field); if (p != null) p.stringValue = v;
        }
        private static void SetFloat(SerializedObject so, string field, float v)
        {
            var p = so.FindProperty(field); if (p != null) p.floatValue = v;
        }
        private static void SetInt(SerializedObject so, string field, int v)
        {
            var p = so.FindProperty(field); if (p != null) p.intValue = v;
        }
        private static void SetColor(SerializedObject so, string field, Color v)
        {
            var p = so.FindProperty(field); if (p != null) p.colorValue = v;
        }
        private static void SetObject(SerializedObject so, string field, Object v)
        {
            var p = so.FindProperty(field); if (p != null) p.objectReferenceValue = v;
        }
    }
}
#endif

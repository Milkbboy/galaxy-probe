#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using DrillCorp.Ability.Runners;
using DrillCorp.Data;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// 지누스 어빌리티 3종 프리펩 + VFX/탄 참조 일괄 셋업.
    ///
    /// 처리 내용:
    ///   1. DroneInstance 프리펩 — 루트 + DroneInstance 컴포넌트 + Body(GlowPowerupBigGreen) + _bulletPrefab(BulletGreen)
    ///   2. MiningDroneInstance 프리펩 — 루트 + MiningDroneInstance 컴포넌트 + Body(CrystalGrowthGreen)
    ///   3. SpiderDroneInstance 프리펩 — 루트 + SpiderDroneInstance 컴포넌트 + Body(SparkleOrbGreen) + _bulletPrefab(BulletGreen)
    ///   4. 각 AbilityData._vfxPrefab 슬롯 자동 바인딩
    ///
    /// 의존 에셋(Polygon Arsenal 경로):
    ///   · Assets/Polygon Arsenal/Prefabs/Interactive/Powerups/Orbs/Big/GlowPowerupBigGreen.prefab
    ///   · Assets/Polygon Arsenal/Prefabs/Misc/CrystalGrowthGreen.prefab
    ///   · Assets/Polygon Arsenal/Prefabs/Interactive/Powerups/Orbs/SparkleOrb/SparkleOrbGreen.prefab
    ///   · Assets/Polygon Arsenal/Prefabs/Combat/Missiles/Sci-Fi/Bullet/BulletGreen.prefab
    ///
    /// 저장 위치:
    ///   · Assets/_Game/Prefabs/Abilities/{Drone,MiningDrone,SpiderDrone}Instance.prefab
    ///
    /// 메뉴: Tools/Drill-Corp/3. 게임 초기 설정/10. 지누스 드론 프리펩 생성
    /// </summary>
    public static class DronePrefabCreator
    {
        private const string PrefabFolder = "Assets/_Game/Prefabs/Abilities";
        private const string DronePrefabPath        = PrefabFolder + "/DroneInstance.prefab";
        private const string MiningDronePrefabPath  = PrefabFolder + "/MiningDroneInstance.prefab";
        private const string SpiderDronePrefabPath  = PrefabFolder + "/SpiderDroneInstance.prefab";

        private const string DroneBodyPath =
            "Assets/Polygon Arsenal/Prefabs/Interactive/Powerups/Orbs/Big/GlowPowerupBigGreen.prefab";
        private const string MiningBodyPath =
            "Assets/Polygon Arsenal/Prefabs/Misc/CrystalGrowthGreen.prefab";
        private const string SpiderBodyPath =
            "Assets/Polygon Arsenal/Prefabs/Interactive/Powerups/Orbs/SparkleOrb/SparkleOrbGreen.prefab";
        private const string BulletPath =
            "Assets/Polygon Arsenal/Prefabs/Combat/Missiles/Sci-Fi/Bullet/BulletGreen.prefab";

        private const string DroneAbilitySoPath        = "Assets/_Game/Data/Abilities/Ability_Jinus_Drone.asset";
        private const string MiningDroneAbilitySoPath  = "Assets/_Game/Data/Abilities/Ability_Jinus_MiningDrone.asset";
        private const string SpiderDroneAbilitySoPath  = "Assets/_Game/Data/Abilities/Ability_Jinus_SpiderDrone.asset";

        // Polygon Arsenal Orb/Sparkle/Crystal 계열은 Unity 표준 3D(Y-up)로 authoring 되어 있어 그대로 쓰면 된다.
        // MinePrefabCreator 의 GlowZoneRed 는 XY 평면 flat 스프라이트라 90°X 회전이 필요했지만, 이번 body 들은 해당 없음.
        // (90°X 회전 걸면 Cone 방출(로컬 +Z)이 world -Y 로 꺾여 파티클이 지면 아래로 꽂힘 = "뒤집어짐" 오인.)
        private static readonly Quaternion BodyRotation = Quaternion.identity;

        // Body VFX 기본 스케일 — 드론 본체감 주기 위해 원본보다 크게.
        private const float DroneBodyScale = 2.5f;
        private const float MiningBodyScale = 2.0f;
        private const float SpiderBodyScale = 1.8f;

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/10. 지누스 드론 프리펩 생성")]
        public static void CreateAllPrefabs()
        {
            EnsureFolders();

            // 공통 에셋 로드
            var droneBody  = Load<GameObject>(DroneBodyPath,  required: true);
            var miningBody = Load<GameObject>(MiningBodyPath, required: true);
            var spiderBody = Load<GameObject>(SpiderBodyPath, required: true);
            var bullet     = Load<GameObject>(BulletPath,     required: false); // 탄은 없어도 프리펩 자체는 생성

            if (droneBody == null || miningBody == null || spiderBody == null)
            {
                Debug.LogError("[DronePrefabCreator] Polygon Arsenal body 프리펩 누락 — 중단.");
                return;
            }

            if (bullet != null && bullet.GetComponent<DroneBullet>() == null)
            {
                // Polygon Arsenal BulletGreen 은 순수 VFX — DroneBullet 컴포넌트 없음.
                // 프리펩을 변조하는 대신 런타임 Instantiate 시 AddComponent 를 해도 되지만,
                // 이 Creator 가 DroneBullet 컴포넌트를 영구 부착한 '래퍼 프리펩' 을 만드는 게 더 명시적.
                bullet = CreateBulletWrapper(bullet);
            }

            // 1. 드론 포탑
            var drone = BuildDronePrefab(droneBody, bullet);
            if (drone != null) BindAbilityVfx(DroneAbilitySoPath, drone);

            // 2. 채굴 드론
            var miningDrone = BuildMiningDronePrefab(miningBody);
            if (miningDrone != null) BindAbilityVfx(MiningDroneAbilitySoPath, miningDrone);

            // 3. 드론 거미 — 근접 접촉 피해라 탄 프리펩 필요 없음
            var spiderDrone = BuildSpiderDronePrefab(spiderBody);
            if (spiderDrone != null) BindAbilityVfx(SpiderDroneAbilitySoPath, spiderDrone);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (drone != null)
            {
                Selection.activeObject = drone;
                EditorGUIUtility.PingObject(drone);
            }

            Debug.Log("[DronePrefabCreator] ✓ 지누스 프리펩 3종 생성 + AbilityData 바인딩 완료");
        }

        // ─── 개별 프리펩 빌드 ────────────────────────────────────────

        private static GameObject BuildDronePrefab(GameObject bodyPrefab, GameObject bulletPrefab)
        {
            var root = new GameObject("DroneInstance");
            var drone = root.AddComponent<DroneInstance>();

            var body = InstantiateBody(bodyPrefab, root.transform, DroneBodyScale);

            var so = new SerializedObject(drone);
            SetObject(so, "_bodyTransform", body.transform);
            if (bulletPrefab != null) SetObject(so, "_bulletPrefab", bulletPrefab);
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, DronePrefabPath);
        }

        private static GameObject BuildMiningDronePrefab(GameObject bodyPrefab)
        {
            var root = new GameObject("MiningDroneInstance");
            var mining = root.AddComponent<MiningDroneInstance>();

            var body = InstantiateBody(bodyPrefab, root.transform, MiningBodyScale);

            var so = new SerializedObject(mining);
            SetObject(so, "_bodyTransform", body.transform);
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, MiningDronePrefabPath);
        }

        private static GameObject BuildSpiderDronePrefab(GameObject bodyPrefab)
        {
            // 드론 거미는 근접 접촉 피해 방식 — 탄 프리펩 필요 없음.
            var root = new GameObject("SpiderDroneInstance");
            var spider = root.AddComponent<SpiderDroneInstance>();

            var body = InstantiateBody(bodyPrefab, root.transform, SpiderBodyScale);

            var so = new SerializedObject(spider);
            SetObject(so, "_bodyTransform", body.transform);
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, SpiderDronePrefabPath);
        }

        // ─── 공용 유틸 ─────────────────────────────────────────────────

        private static GameObject InstantiateBody(GameObject bodyPrefab, Transform parent, float scale)
        {
            var body = (GameObject)PrefabUtility.InstantiatePrefab(bodyPrefab);
            body.name = "Body";
            body.transform.SetParent(parent, worldPositionStays: false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = BodyRotation;
            body.transform.localScale = Vector3.one * scale;
            return body;
        }

        /// <summary>
        /// Polygon Arsenal BulletGreen 은 VFX 전용 — DroneBullet 동작이 없음.
        /// 이 래퍼는 BulletGreen 을 자식으로 두고 루트에 DroneBullet 을 부착한 프리펩.
        /// 런타임엔 Runner 가 이 래퍼를 Instantiate → GetComponent&lt;DroneBullet&gt; → Initialize.
        /// </summary>
        private static GameObject CreateBulletWrapper(GameObject bulletVfxPrefab)
        {
            const string wrapperPath = PrefabFolder + "/DroneBullet.prefab";

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(wrapperPath);
            if (existing != null) return existing;

            var root = new GameObject("DroneBullet");
            root.AddComponent<DroneBullet>();

            var vfx = (GameObject)PrefabUtility.InstantiatePrefab(bulletVfxPrefab);
            vfx.name = "Vfx";
            vfx.transform.SetParent(root.transform, worldPositionStays: false);
            vfx.transform.localPosition = Vector3.zero;
            // BulletGreen 도 XY authoring — 탑뷰 맞춤.
            vfx.transform.localRotation = BodyRotation;
            vfx.transform.localScale = Vector3.one;

            var prefab = SavePrefab(root, wrapperPath);
            if (prefab != null) Debug.Log($"[DronePrefabCreator] ✓ 탄 래퍼 생성: {wrapperPath}");
            return prefab;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            Object.DestroyImmediate(root);
            if (!success || prefab == null)
            {
                Debug.LogError($"[DronePrefabCreator] 프리펩 저장 실패: {path}");
                return null;
            }
            Debug.Log($"[DronePrefabCreator] ✓ 프리펩 생성: {path}");
            return prefab;
        }

        private static void BindAbilityVfx(string abilitySoPath, GameObject prefab)
        {
            var abilitySo = AssetDatabase.LoadAssetAtPath<AbilityData>(abilitySoPath);
            if (abilitySo == null)
            {
                Debug.LogWarning($"[DronePrefabCreator] AbilityData 를 찾을 수 없음: {abilitySoPath}");
                return;
            }
            var so = new SerializedObject(abilitySo);
            SetObject(so, "_vfxPrefab", prefab);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(abilitySo);
            Debug.Log($"[DronePrefabCreator] ✓ {Path.GetFileName(abilitySoPath)} ._vfxPrefab ← {prefab.name}");
        }

        private static T Load<T>(string path, bool required) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                var level = required ? "ERROR" : "WARN";
                Debug.Log($"[DronePrefabCreator][{level}] 에셋 로드 실패: {path}");
            }
            return asset;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "Abilities");
        }

        private static void SetObject(SerializedObject so, string field, Object v)
        {
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning($"[DronePrefabCreator] SerializedProperty not found: {field}");
                return;
            }
            p.objectReferenceValue = v;
        }
    }
}
#endif

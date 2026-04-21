#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using DrillCorp.Ability.Runners;
using DrillCorp.Data;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// 빅터 지뢰 프리펩 + 폭발 VFX 참조 일괄 셋업.
    ///
    /// 처리 내용:
    ///   1. MineInstance 프리펩 생성 — 루트 empty GO + MineInstance 컴포넌트
    ///      자식 "Body" = GlowZoneRed 프리펩 복제 (점멸용)
    ///      자식 "CenterDot" = GlowPowerupSmallRed 프리펩 복제 (armed 시 점등될 빨간 점, SetActive=false 상태로 시작)
    ///   2. MineInstance 의 _bodyTransform / _centerDotObject / _explosionPrefab / _explosionPrefabBaseRadius 자동 바인딩
    ///   3. Ability_Victor_Mine.asset 의 _vfxPrefab 슬롯에 MineInstance 프리펩 자동 할당
    ///
    /// 의존 에셋(Polygon Arsenal 가져오기 후 기대 경로):
    ///   · Assets/Polygon Arsenal/Prefabs/Interactive/Zone/Glow/GlowZoneRed.prefab
    ///   · Assets/Polygon Arsenal/Prefabs/Interactive/Powerups/Orbs/Small/GlowPowerupSmallRed.prefab
    ///   · Assets/Polygon Arsenal/Prefabs/Combat/Explosions/Sci-Fi/Grenade/GrenadeExplosionRed.prefab
    ///
    /// 저장 위치:
    ///   · Assets/_Game/Prefabs/Abilities/MineInstance.prefab
    ///
    /// 메뉴: Tools/Drill-Corp/3. 게임 초기 설정/빅터/1. 지뢰 프리펩 생성
    /// </summary>
    public static class MinePrefabCreator
    {
        private const string PrefabFolder = "Assets/_Game/Prefabs/Abilities";
        private const string MinePrefabPath = PrefabFolder + "/MineInstance.prefab";

        private const string GlowZonePath =
            "Assets/Polygon Arsenal/Prefabs/Interactive/Zone/Glow/GlowZoneRed.prefab";
        private const string CenterDotPath =
            "Assets/Polygon Arsenal/Prefabs/Interactive/Powerups/Orbs/Small/GlowPowerupSmallRed.prefab";
        private const string ExplosionPath =
            "Assets/Polygon Arsenal/Prefabs/Combat/Explosions/Sci-Fi/Grenade/GrenadeExplosionRed.prefab";

        private const string MineAbilitySoPath =
            "Assets/_Game/Data/Abilities/Ability_Victor_Mine.asset";

        // GrenadeExplosionRed 의 "기준 반경" — Polygon Arsenal 기본 스케일 기준 대략치.
        // MineInstance._explosionPrefabBaseRadius 로 들어감 — 실제 반경/이 값으로 폭발 VFX 스케일 배수 결정.
        // 지뢰 기본 폭발 반경이 1.5m 이므로 동일하게 두면 스케일 배수=1 (원본 크기 그대로).
        private const float ExplosionBaseRadius = 1.5f;

        // CenterDot 스케일 — GlowPowerupSmallRed 원본 크기. 1.0 = 원본 그대로.
        // 0.3으로 줄이면 너무 작아서 안 보이고, 1.0이 지뢰 본체(GlowZoneRed 스케일 0.5)와 적당한 비율.
        private const float CenterDotScale = 1.0f;

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/빅터/1. 지뢰 프리펩 생성")]
        public static void CreateMinePrefab()
        {
            EnsureFolders();

            var glowZone = AssetDatabase.LoadAssetAtPath<GameObject>(GlowZonePath);
            var centerDotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CenterDotPath);
            var explosion = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPath);

            if (glowZone == null)
            {
                Debug.LogError($"[MinePrefabCreator] GlowZoneRed 프리펩을 찾을 수 없습니다: {GlowZonePath}\n" +
                               "Polygon Arsenal 패키지가 정상 import 됐는지 확인하세요.");
                return;
            }
            if (centerDotPrefab == null)
            {
                Debug.LogWarning($"[MinePrefabCreator] GlowPowerupSmallRed 프리펩을 찾을 수 없습니다: {CenterDotPath}\n" +
                                 "중앙 점 없이 프리펩을 생성합니다. armed 연출이 밋밋해질 수 있으니 나중에 인스펙터에서 _centerDotObject 슬롯을 채워주세요.");
            }
            if (explosion == null)
            {
                Debug.LogWarning($"[MinePrefabCreator] GrenadeExplosionRed 프리펩을 찾을 수 없습니다: {ExplosionPath}\n" +
                                 "폭발 VFX 없이 프리펩을 생성합니다. 나중에 인스펙터에서 _explosionPrefab 슬롯을 채워주세요.");
            }

            // ── 1. 루트 + MineInstance 컴포넌트 ──
            var root = new GameObject("MineInstance");
            var mine = root.AddComponent<MineInstance>();

            // ── 2. Body 자식 (GlowZoneRed 복제) ──
            var body = (GameObject)PrefabUtility.InstantiatePrefab(glowZone);
            body.name = "Body";
            body.transform.SetParent(root.transform, worldPositionStays: false);
            body.transform.localPosition = Vector3.zero;
            // GlowZoneRed는 XY 평면에 세워진 authoring이라 탑뷰(카메라 -Y)에선 세로로 선다.
            // 90°X 회전으로 바닥(XZ 평면, 법선 +Y)에 눕힘.
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // 소형 지뢰 스케일 — 필요시 인스펙터에서 조정
            body.transform.localScale = Vector3.one * 0.5f;

            // ── 3. CenterDot 자식 (GlowPowerupSmallRed 복제, armed 시 점등) ──
            GameObject centerDot = null;
            if (centerDotPrefab != null)
            {
                centerDot = (GameObject)PrefabUtility.InstantiatePrefab(centerDotPrefab);
                centerDot.name = "CenterDot";
                centerDot.transform.SetParent(root.transform, worldPositionStays: false);
                centerDot.transform.localPosition = new Vector3(0f, 0.05f, 0f); // 본체 살짝 위
                centerDot.transform.localRotation = Quaternion.identity;
                centerDot.transform.localScale = Vector3.one * CenterDotScale;
                centerDot.SetActive(false); // Initialize에서 어차피 비활성화되지만 기본 상태도 맞춤
            }

            // ── 4. MineInstance 필드 바인딩 ──
            var so = new SerializedObject(mine);
            SetObject(so, "_bodyTransform", body.transform);
            if (centerDot != null) SetObject(so, "_centerDotObject", centerDot);
            if (explosion != null) SetObject(so, "_explosionPrefab", explosion);
            SetFloat(so, "_explosionPrefabBaseRadius", ExplosionBaseRadius);
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── 4. 프리펩 저장 ──
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, MinePrefabPath, out bool success);
            Object.DestroyImmediate(root);

            if (!success || prefab == null)
            {
                Debug.LogError($"[MinePrefabCreator] 프리펩 저장 실패: {MinePrefabPath}");
                return;
            }

            Debug.Log($"[MinePrefabCreator] ✓ 프리펩 생성: {MinePrefabPath}");

            // ── 5. AbilityData._vfxPrefab 자동 바인딩 ──
            var mineSo = AssetDatabase.LoadAssetAtPath<AbilityData>(MineAbilitySoPath);
            if (mineSo != null)
            {
                var abilitySo = new SerializedObject(mineSo);
                SetObject(abilitySo, "_vfxPrefab", prefab);
                abilitySo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mineSo);
                Debug.Log($"[MinePrefabCreator] ✓ {Path.GetFileName(MineAbilitySoPath)} 의 _vfxPrefab 슬롯에 바인딩 완료");
            }
            else
            {
                Debug.LogWarning($"[MinePrefabCreator] AbilityData 를 찾을 수 없음: {MineAbilitySoPath}\n" +
                                 "프리펩만 생성하고 바인딩은 스킵합니다.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
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
                Debug.LogWarning($"[MinePrefabCreator] SerializedProperty not found: {field}");
                return;
            }
            p.objectReferenceValue = v;
        }

        private static void SetFloat(SerializedObject so, string field, float v)
        {
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning($"[MinePrefabCreator] SerializedProperty not found: {field}");
                return;
            }
            p.floatValue = v;
        }
    }
}
#endif

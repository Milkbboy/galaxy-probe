#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using DrillCorp.Ability.Runners;
using DrillCorp.Data;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// 사라 메테오 낙하체 프리펩 + 관련 VFX 참조 일괄 셋업.
    ///
    /// 처리 내용:
    ///   1. MeteorInstance 프리펩 생성 — 루트 empty GO + MeteorInstance 컴포넌트
    ///      자식 "Body" = Sphere 메시 + 빨간 Unlit 머티리얼 (떨어지는 운석 본체)
    ///      자식 "ChargeAura" = AuraChargeRed 복제 (낙하 중 트레일 이펙트)
    ///   2. MeteorInstance 의 _impactVfxPrefab / _fireZoneVfxPrefab 자동 바인딩
    ///   3. Ability_Sara_Meteor.asset 의 _vfxPrefab 슬롯에 MeteorInstance 프리펩 자동 할당
    ///
    /// 의존 에셋(Polygon Arsenal 가져오기 후 기대 경로):
    ///   · Assets/Polygon Arsenal/Prefabs/Combat/Aura/ChargeAura/AuraChargeRed.prefab
    ///   · Assets/Polygon Arsenal/Prefabs/Combat/Nova/FireNova/FireNovaYellow.prefab
    ///   · Assets/Polygon Arsenal/Prefabs/Environment/FloorTrap/FloorTrapMolten.prefab
    ///
    /// 저장 위치:
    ///   · Assets/_Game/Prefabs/Abilities/MeteorInstance.prefab
    ///
    /// 메뉴: Tools/Drill-Corp/3. 게임 초기 설정/사라/1. 메테오 프리펩 생성
    /// </summary>
    public static class MeteorPrefabCreator
    {
        private const string PrefabFolder = "Assets/_Game/Prefabs/Abilities";
        private const string MeteorPrefabPath = PrefabFolder + "/MeteorInstance.prefab";
        private const string MaterialFolder = "Assets/_Game/Materials";
        private const string BodyMaterialPath = MaterialFolder + "/MeteorBody.mat";

        private const string ChargeAuraPath =
            "Assets/Polygon Arsenal/Prefabs/Combat/Aura/ChargeAura/AuraChargeRed.prefab";
        private const string ImpactVfxPath =
            "Assets/Polygon Arsenal/Prefabs/Combat/Nova/FireNova/FireNovaYellow.prefab";
        private const string FireZoneVfxPath =
            "Assets/Polygon Arsenal/Prefabs/Environment/FloorTrap/FloorTrapMolten.prefab";

        private const string MeteorAbilitySoPath =
            "Assets/_Game/Data/Abilities/Ability_Sara_Meteor.asset";

        // FireNovaYellow 기준 반경 — MeteorInstance 가 화염 반경과 대략 맞춤용. 플레이테스트 후 조정.
        private const float ImpactVfxReferenceRadius = 1.5f;

        // 낙하체 본체 크기 (Sphere 기준 지름). 너무 작으면 낙하 가시성 떨어지고, 너무 크면 과함.
        private const float BodyScale = 1.2f;
        // 본체 색 (빨강). Unlit 머티리얼에 적용.
        private static readonly Color BodyColor = new Color(1f, 0.3f, 0.1f, 1f);

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/사라/1. 메테오 프리펩 생성")]
        public static void CreateMeteorPrefab()
        {
            EnsureFolders();

            var chargeAura = AssetDatabase.LoadAssetAtPath<GameObject>(ChargeAuraPath);
            var impactVfx = AssetDatabase.LoadAssetAtPath<GameObject>(ImpactVfxPath);
            var fireZoneVfx = AssetDatabase.LoadAssetAtPath<GameObject>(FireZoneVfxPath);

            if (chargeAura == null)
            {
                Debug.LogWarning($"[MeteorPrefabCreator] AuraChargeRed 프리펩을 찾을 수 없습니다: {ChargeAuraPath}\n" +
                                 "낙하 트레일 없이 프리펩을 생성합니다.");
            }
            if (impactVfx == null)
            {
                Debug.LogWarning($"[MeteorPrefabCreator] FireNovaYellow 프리펩을 찾을 수 없습니다: {ImpactVfxPath}\n" +
                                 "폭발 VFX 없이 프리펩을 생성합니다. 나중에 인스펙터에서 _impactVfxPrefab 슬롯을 채워주세요.");
            }
            if (fireZoneVfx == null)
            {
                Debug.LogWarning($"[MeteorPrefabCreator] FloorTrapMolten 프리펩을 찾을 수 없습니다: {FireZoneVfxPath}\n" +
                                 "지속 화염 VFX 없이 프리펩을 생성합니다. 데칼만 표시됩니다.");
            }

            // ── 1. 루트 + MeteorInstance 컴포넌트 ──
            var root = new GameObject("MeteorInstance");
            var meteor = root.AddComponent<MeteorInstance>();

            // ── 2. Body 자식 (Sphere 메시, 빨간 Unlit) ──
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(root.transform, worldPositionStays: false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = Vector3.one * BodyScale;
            // Collider 제거 — 낙하체는 물리 상호작용 없이 시각만.
            var bodyCol = body.GetComponent<Collider>();
            if (bodyCol != null) Object.DestroyImmediate(bodyCol);
            // 머티리얼 에셋 준비 (또는 기존 재사용). new Material() 만으로는 프리펩에 직렬화되지 않아
            // AssetDatabase 로 .mat 파일을 먼저 저장해야 프리펩이 참조 가능.
            var bodyMaterial = EnsureBodyMaterial();

            var bodyRenderer = body.GetComponent<MeshRenderer>();
            if (bodyRenderer != null)
            {
                if (bodyMaterial != null) bodyRenderer.sharedMaterial = bodyMaterial;
                bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                bodyRenderer.receiveShadows = false;
            }

            // ── 3. ChargeAura 자식 (옵션 — 낙하 트레일) ──
            // AuraChargeRed 는 감싸는 오라 형태 — 낙하 중 운석 주변을 둘러싸 보이게.
            // 탑뷰 카메라(-Y 방향)에서 잘 보이도록 90°X 회전으로 눕힘 (오라가 XZ 평면에 퍼짐).
            // 낙하 트레일이 세로로 보이길 원하면 인스펙터에서 회전 0으로 변경 가능.
            if (chargeAura != null)
            {
                var aura = (GameObject)PrefabUtility.InstantiatePrefab(chargeAura);
                aura.name = "ChargeAura";
                aura.transform.SetParent(root.transform, worldPositionStays: false);
                aura.transform.localPosition = Vector3.zero;
                aura.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                aura.transform.localScale = Vector3.one;
            }

            // ── 4. MeteorInstance 필드 바인딩 ──
            var so = new SerializedObject(meteor);
            if (impactVfx != null) SetObject(so, "_impactVfxPrefab", impactVfx);
            SetFloat(so, "_impactVfxReferenceRadius", ImpactVfxReferenceRadius);
            if (fireZoneVfx != null) SetObject(so, "_fireZoneVfxPrefab", fireZoneVfx);
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── 5. 프리펩 저장 ──
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, MeteorPrefabPath, out bool success);
            Object.DestroyImmediate(root);

            if (!success || prefab == null)
            {
                Debug.LogError($"[MeteorPrefabCreator] 프리펩 저장 실패: {MeteorPrefabPath}");
                return;
            }

            Debug.Log($"[MeteorPrefabCreator] ✓ 프리펩 생성: {MeteorPrefabPath}");

            // ── 6. AbilityData._vfxPrefab 자동 바인딩 ──
            var meteorSo = AssetDatabase.LoadAssetAtPath<AbilityData>(MeteorAbilitySoPath);
            if (meteorSo != null)
            {
                var abilitySo = new SerializedObject(meteorSo);
                SetObject(abilitySo, "_vfxPrefab", prefab);
                abilitySo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(meteorSo);
                Debug.Log($"[MeteorPrefabCreator] ✓ {Path.GetFileName(MeteorAbilitySoPath)} 의 _vfxPrefab 슬롯에 바인딩 완료");
            }
            else
            {
                Debug.LogWarning($"[MeteorPrefabCreator] AbilityData 를 찾을 수 없음: {MeteorAbilitySoPath}\n" +
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
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/_Game", "Materials");
        }

        // 기존 MeteorBody.mat 이 있으면 색상만 갱신, 없으면 신규 생성 후 에셋 저장.
        // AssetDatabase 로 저장된 머티리얼이어야 프리펩이 sharedMaterial 로 참조 가능.
        private static Material EnsureBodyMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath);
            if (existing != null)
            {
                SetMaterialColor(existing, BodyColor);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("[MeteorPrefabCreator] Unlit 셰이더를 찾지 못했습니다. 머티리얼 생성을 건너뜁니다.");
                return null;
            }

            var mat = new Material(shader) { name = "MeteorBody" };
            SetMaterialColor(mat, BodyColor);
            AssetDatabase.CreateAsset(mat, BodyMaterialPath);
            Debug.Log($"[MeteorPrefabCreator] ✓ 머티리얼 생성: {BodyMaterialPath}");
            return mat;
        }

        private static void SetMaterialColor(Material mat, Color c)
        {
            // Unlit/Color 는 _Color, URP Unlit 은 _BaseColor — 모두 시도.
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        }

        private static void SetObject(SerializedObject so, string field, Object v)
        {
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning($"[MeteorPrefabCreator] SerializedProperty not found: {field}");
                return;
            }
            p.objectReferenceValue = v;
        }

        private static void SetFloat(SerializedObject so, string field, float v)
        {
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning($"[MeteorPrefabCreator] SerializedProperty not found: {field}");
                return;
            }
            p.floatValue = v;
        }
    }
}
#endif

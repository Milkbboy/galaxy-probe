#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// 빅터 네이팜 — VFX 타일 프리펩(OilFireRed) 자동 바인딩.
    /// NapalmRunner가 이 단일 프리펩을 길이축으로 N개 복제 배치한다.
    /// FloorTrapMolten 은 원형 효과라 부적절 — 반드시 작은 불꽃(OilFire*) 을 쓸 것.
    ///
    /// 메뉴: Tools/Drill-Corp/3. 게임 초기 설정/빅터/2. 네이팜 VFX 바인딩
    /// </summary>
    public static class NapalmVfxBinder
    {
        private const string NapalmAbilitySoPath =
            "Assets/_Game/Data/Abilities/Ability_Victor_Napalm.asset";

        // OilFireRed — 작은 지속 불꽃. 같은 폴더의 다른 색도 빅터 테마에 따라 대체 가능(Yellow).
        private const string OilFireTilePath =
            "Assets/Polygon Arsenal/Prefabs/Environment/Fire/OilFire/OilFireRed.prefab";

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/빅터/2. 네이팜 VFX 바인딩")]
        public static void BindNapalmVfx()
        {
            var tile = AssetDatabase.LoadAssetAtPath<GameObject>(OilFireTilePath);
            if (tile == null)
            {
                Debug.LogError($"[NapalmVfxBinder] 타일 프리펩을 찾을 수 없음: {OilFireTilePath}\n" +
                               "Polygon Arsenal 패키지가 정상 import 됐는지 확인.");
                return;
            }

            var so = AssetDatabase.LoadAssetAtPath<AbilityData>(NapalmAbilitySoPath);
            if (so == null)
            {
                Debug.LogError($"[NapalmVfxBinder] Ability SO 를 찾을 수 없음: {NapalmAbilitySoPath}");
                return;
            }

            var sObj = new SerializedObject(so);
            var p = sObj.FindProperty("_vfxPrefab");
            if (p == null)
            {
                Debug.LogError("[NapalmVfxBinder] _vfxPrefab 프로퍼티를 찾을 수 없음.");
                return;
            }
            p.objectReferenceValue = tile;
            sObj.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();

            Debug.Log($"[NapalmVfxBinder] ✓ {Path.GetFileName(NapalmAbilitySoPath)} " +
                      $".vfxPrefab ← {Path.GetFileName(OilFireTilePath)}");

            Selection.activeObject = so;
            EditorGUIUtility.PingObject(so);
        }
    }
}
#endif

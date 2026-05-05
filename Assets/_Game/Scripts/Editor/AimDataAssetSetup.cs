using UnityEditor;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// Drill-Corp/3. 게임 초기 설정/Aim/1. AimConfig.asset 생성 메뉴.
    /// BossDataAssetSetup / SpawnConfigAssetSetup 패턴 — idempotent, 이미 있으면 스킵.
    /// </summary>
    public static class AimDataAssetSetup
    {
        const string DATA_PATH = "Assets/_Game/Data/AimConfig.asset";
        const string FOLDER    = "Assets/_Game/Data";

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/Aim/1. AimConfig.asset 생성")]
        public static void CreateAimConfigAsset()
        {
            if (!AssetDatabase.IsValidFolder(FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_Game", "Data");
            }

            if (AssetDatabase.LoadAssetAtPath<AimData>(DATA_PATH) != null)
            {
                Debug.Log($"[AimDataAssetSetup] {DATA_PATH} 이미 존재 — 스킵");
                return;
            }

            var data = ScriptableObject.CreateInstance<AimData>();
            AssetDatabase.CreateAsset(data, DATA_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AimDataAssetSetup] 생성 완료: {DATA_PATH}");
            EditorGUIUtility.PingObject(data);
        }
    }
}

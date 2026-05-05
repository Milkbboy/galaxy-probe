using UnityEditor;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// Drill-Corp/3. 게임 초기 설정/Gem/1. GemConfig.asset 생성 메뉴.
    /// BossDataAssetSetup / AimDataAssetSetup 패턴 — idempotent.
    /// </summary>
    public static class GemDataAssetSetup
    {
        const string DATA_PATH = "Assets/_Game/Data/GemConfig.asset";
        const string FOLDER    = "Assets/_Game/Data";

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/Gem/1. GemConfig.asset 생성")]
        public static void CreateGemConfigAsset()
        {
            if (!AssetDatabase.IsValidFolder(FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_Game", "Data");
            }

            if (AssetDatabase.LoadAssetAtPath<GemData>(DATA_PATH) != null)
            {
                Debug.Log($"[GemDataAssetSetup] {DATA_PATH} 이미 존재 — 스킵");
                return;
            }

            var data = ScriptableObject.CreateInstance<GemData>();
            AssetDatabase.CreateAsset(data, DATA_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GemDataAssetSetup] 생성 완료: {DATA_PATH}");
            EditorGUIUtility.PingObject(data);
        }
    }
}

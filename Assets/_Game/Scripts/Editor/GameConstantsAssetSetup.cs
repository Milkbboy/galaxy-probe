using UnityEditor;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// Drill-Corp/3. 게임 초기 설정/Constants/1. GameConstants.asset 생성 메뉴.
    /// BossDataAssetSetup 패턴 — idempotent.
    /// </summary>
    public static class GameConstantsAssetSetup
    {
        const string DATA_PATH = "Assets/_Game/Data/GameConstants.asset";
        const string FOLDER    = "Assets/_Game/Data";

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/Constants/1. GameConstants.asset 생성")]
        public static void CreateGameConstantsAsset()
        {
            if (!AssetDatabase.IsValidFolder(FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_Game", "Data");
            }

            if (AssetDatabase.LoadAssetAtPath<GameConstantsData>(DATA_PATH) != null)
            {
                Debug.Log($"[GameConstantsAssetSetup] {DATA_PATH} 이미 존재 — 스킵");
                return;
            }

            var data = ScriptableObject.CreateInstance<GameConstantsData>();
            AssetDatabase.CreateAsset(data, DATA_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GameConstantsAssetSetup] 생성 완료: {DATA_PATH}");
            EditorGUIUtility.PingObject(data);
        }
    }
}

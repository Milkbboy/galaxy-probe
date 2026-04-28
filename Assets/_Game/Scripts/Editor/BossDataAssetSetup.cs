using UnityEditor;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// Drill-Corp/3. 게임 초기 설정/Boss/1. Boss_Spider.asset 생성 메뉴.
    /// SimpleWaveAssetSetup 과 동일 패턴 — idempotent.
    /// 이미 있으면 스킵, 없으면 BossData 인스턴스를 기본값으로 생성.
    /// </summary>
    public static class BossDataAssetSetup
    {
        const string DATA_PATH = "Assets/_Game/Data/Boss/Boss_Spider.asset";
        const string FOLDER    = "Assets/_Game/Data/Boss";

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/Boss/1. Boss_Spider.asset 생성")]
        public static void CreateBossSpiderAsset()
        {
            EnsureFolder();

            if (AssetDatabase.LoadAssetAtPath<BossData>(DATA_PATH) != null)
            {
                Debug.Log($"[BossDataAssetSetup] {DATA_PATH} 이미 존재 — 스킵");
                return;
            }

            var data = ScriptableObject.CreateInstance<BossData>();
            // 필드 기본값(BossData.cs 의 default initializer) 그대로.
            AssetDatabase.CreateAsset(data, DATA_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BossDataAssetSetup] 생성 완료: {DATA_PATH}");
            EditorGUIUtility.PingObject(data);
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Boss");
            }
        }
    }
}

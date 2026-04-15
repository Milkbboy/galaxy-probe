#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using DrillCorp.Bug.Simple;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// SimpleBug 프리팹 3종에 Models 폴더의 FBX를 자식 모델로 주입한다.
    /// 텍스트로 PrefabInstance YAML을 작성하는 것은 FBX 내부 fileID가 임포트마다 달라져 위험하므로,
    /// Unity API로 안전하게 처리한다.
    /// </summary>
    public static class SimpleBugPrefabSetup
    {
        private const string PrefabFolder = "Assets/_Game/Prefabs/Bugs/Simple";
        private const string ModelFolder = "Assets/_Game/Models";

        private struct Entry
        {
            public string PrefabPath;
            public string ModelPath;
            public float ModelScale;      // FBX가 너무 크거나 작을 때 보정
            public Vector3 ModelEuler;    // FBX가 세로로 서있을 때 눕히기 위한 회전 오프셋

            public Entry(string prefab, string model, float scale, Vector3 euler)
            {
                PrefabPath = prefab;
                ModelPath = model;
                ModelScale = scale;
                ModelEuler = euler;
            }
        }

        [MenuItem("Drill-Corp/Simple Bug/Attach FBX Models To Prefabs")]
        public static void AttachModels()
        {
            // FBX가 Z-up(세로로 서있음)으로 임포트되므로 X축 -90도 회전해 엎드리게 한다.
            // 탑뷰 기준: 머리가 +Z(화면 위)를 향하도록 한 상태로 바닥에 눕힘.
            Vector3 layFlat = new Vector3(-90f, 0f, 0f);

            var entries = new[]
            {
                new Entry($"{PrefabFolder}/SimpleBug_Normal.prefab", $"{ModelFolder}/SM_Bug_A_01.fbx", 1f, layFlat),
                new Entry($"{PrefabFolder}/SimpleBug_Elite.prefab",  $"{ModelFolder}/SM_Bug_B_01.fbx", 1f, layFlat),
                new Entry($"{PrefabFolder}/SimpleBug_Swift.prefab",  $"{ModelFolder}/SM_Bug_C_01.fbx", 1f, layFlat),
            };

            int success = 0;
            foreach (var e in entries)
            {
                if (ProcessEntry(e)) success++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SimpleBugPrefabSetup] {success}/{entries.Length}개 프리팹에 모델 주입 완료");
        }

        private static bool ProcessEntry(Entry e)
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(e.PrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[SimpleBugPrefabSetup] 프리팹 없음: {e.PrefabPath}");
                return false;
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(e.ModelPath);
            if (modelAsset == null)
            {
                Debug.LogError($"[SimpleBugPrefabSetup] 모델 없음: {e.ModelPath}");
                return false;
            }

            // 프리팹을 편집 가능한 인스턴스로 로드
            var root = PrefabUtility.LoadPrefabContents(e.PrefabPath);
            try
            {
                // 기존 자식 모두 제거 (재실행 대비)
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                }

                // FBX를 자식으로 Instantiate (프리팹 레퍼런스 유지)
                var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                modelInstance.name = "Model";
                modelInstance.transform.SetParent(root.transform, false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.Euler(e.ModelEuler);
                modelInstance.transform.localScale = Vector3.one * e.ModelScale;

                // 저장
                PrefabUtility.SaveAsPrefabAsset(root, e.PrefabPath);
                Debug.Log($"[SimpleBugPrefabSetup] {Path.GetFileName(e.PrefabPath)} ← {Path.GetFileName(e.ModelPath)}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif

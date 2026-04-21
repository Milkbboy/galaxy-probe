#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// Polygon Arsenal(및 기타 서드파티) FBX 의 deprecated 머티리얼 위치 설정을 일괄 수정.
    ///
    /// Unity 6 에서 `ModelImporterMaterialLocation.External` 가 obsolete — 콘솔에 경고:
    ///   "MaterialLocation.External is obsolete. External Material Location is no longer supported."
    ///
    /// 해결: 각 FBX 의 `materialLocation` 을 **`InPrefab`** (embedded materials) 로 교체 후 재임포트.
    /// 메뉴를 1회 실행하면 전체 FBX 의 경고가 사라진다.
    ///
    /// 메뉴: Tools/Drill-Corp/4. 서드파티 유틸/Polygon Arsenal FBX 머티리얼 경로 수정
    /// </summary>
    public static class PolygonArsenalFbxFixer
    {
        private const string TargetFolder = "Assets/Polygon Arsenal/Models";

        [MenuItem("Tools/Drill-Corp/4. 서드파티 유틸/Polygon Arsenal FBX 머티리얼 경로 수정")]
        public static void FixMaterialLocations()
        {
            if (!AssetDatabase.IsValidFolder(TargetFolder))
            {
                Debug.LogWarning($"[PolygonArsenalFbxFixer] 폴더 없음: {TargetFolder}");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Model", new[] { TargetFolder });
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[PolygonArsenalFbxFixer] FBX 가 없음: {TargetFolder}");
                return;
            }

            int changed = 0;
            int skipped = 0;

            try
            {
                AssetDatabase.StartAssetEditing(); // 재임포트 배치 처리

                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar(
                        "FBX MaterialLocation 수정",
                        $"{i + 1}/{guids.Length} — {System.IO.Path.GetFileName(path)}",
                        (float)(i + 1) / guids.Length);

                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer == null) { skipped++; continue; }

                    // InPrefab 이 아닌 모델(= deprecated External) 을 전부 InPrefab 으로 통일.
                    // enum `External` 값 자체가 obsolete 라 직접 참조하면 컴파일 경고가 나오므로
                    // "현재값 != 목표값" 형태로 비교해 경고를 피한다.
                    if (importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
                    {
                        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                        importer.SaveAndReimport();
                        changed++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[PolygonArsenalFbxFixer] ✓ 완료 — 변경 {changed}개 / 변경 없음 {skipped}개 (총 {guids.Length}개 검사)");
            if (changed > 0)
            {
                Debug.Log("[PolygonArsenalFbxFixer] 'MaterialLocation.External is obsolete' 경고가 사라졌는지 확인하세요. " +
                          "필요 시 Assets > Reimport All 로 전체 재임포트.");
            }
        }
    }
}
#endif

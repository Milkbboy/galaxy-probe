#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DrillCorp.VFX.Pool;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// VFX 프리팹에 <see cref="PooledVfx"/> 를 일괄 부착하는 에디터 도구.
    ///
    /// 목적:
    ///   런타임 VfxPool.CreateInstance 에서도 AddComponent 로 보장하지만,
    ///   미리 부착해두면 Awake 세팅(cullingMode/stopAction)이 첫 프레임부터 적용돼 확실하다.
    ///   또한 VFX 누락 감사(감지) 용도로도 사용.
    ///
    /// 메뉴:
    ///   Tools/Drill-Corp/Dev/VFX 풀링/모든 VFX 프리팹에 PooledVfx 부착
    ///   Tools/Drill-Corp/Dev/VFX 풀링/PooledVfx 누락 프리팹 목록
    ///   Tools/Drill-Corp/Dev/VFX 풀링/AutoDestroyPS 제거 (풀링 전환 시)
    ///
    /// 처리 대상:
    ///   1) `Assets/_Game/VFX/Prefabs/*.prefab`
    ///   2) 루트에 ParticleSystem 컴포넌트가 있는 프리팹만
    ///   3) 이미 PooledVfx 있으면 스킵
    ///   4) 루프 VFX(자체 수명 제어) 는 SkipList 에서 제외
    /// </summary>
    public static class VfxPoolAttacher
    {
        private const string MENU_ROOT = "Tools/Drill-Corp/Dev/VFX 풀링/";

        private static readonly string[] TargetFolders =
        {
            "Assets/_Game/VFX/Prefabs",
        };

        // 제외 — 자체 수명 로직이 있거나 의도적으로 loop 인 프리팹
        private static readonly HashSet<string> SkipList = new HashSet<string>
        {
            "FX_Laser_Trail",    // 무기 발사 중 트레일 — 부모가 파괴 제어
            "FX_Laser_Impact",   // 장판형 루프 — LaserScorchDecay 가 페이드 제어
        };

        [MenuItem(MENU_ROOT + "모든 VFX 프리팹에 PooledVfx 부착", priority = 100)]
        public static void AttachToAll()
        {
            var prefabPaths = CollectVfxPrefabPaths();
            int attached = 0;
            int skippedExcluded = 0;
            int skippedAlreadyHas = 0;
            int skippedNoPS = 0;
            var log = new List<string>();

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var path in prefabPaths)
                {
                    string fileName = Path.GetFileNameWithoutExtension(path);
                    if (SkipList.Contains(fileName))
                    {
                        skippedExcluded++;
                        log.Add($"[SKIP:EXCLUDED] {fileName}");
                        continue;
                    }

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    GameObject instance = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        var rootPs = instance.GetComponent<ParticleSystem>();
                        if (rootPs == null)
                        {
                            skippedNoPS++;
                            log.Add($"[SKIP:NO_ROOT_PS] {fileName}");
                            continue;
                        }

                        if (instance.GetComponent<PooledVfx>() != null)
                        {
                            skippedAlreadyHas++;
                            log.Add($"[SKIP:ALREADY] {fileName}");
                            continue;
                        }

                        instance.AddComponent<PooledVfx>();
                        PrefabUtility.SaveAsPrefabAsset(instance, path);
                        attached++;
                        log.Add($"[ATTACHED] {fileName}");
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(instance);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            string report =
                $"[VfxPoolAttacher]\n" +
                $"  attached       : {attached}\n" +
                $"  already has    : {skippedAlreadyHas}\n" +
                $"  no root PS     : {skippedNoPS}\n" +
                $"  excluded       : {skippedExcluded}\n" +
                $"  total scanned  : {prefabPaths.Count}\n\n" +
                string.Join("\n", log);

            Debug.Log(report);
            EditorUtility.DisplayDialog(
                "VFX PooledVfx 부착 완료",
                $"부착: {attached}\n이미 있음: {skippedAlreadyHas}\n루트 PS 없음: {skippedNoPS}\n제외 목록: {skippedExcluded}\n전체 스캔: {prefabPaths.Count}\n\n상세는 Console 확인.",
                "OK");
        }

        [MenuItem(MENU_ROOT + "PooledVfx 누락 프리팹 목록", priority = 101)]
        public static void ListMissing()
        {
            var prefabPaths = CollectVfxPrefabPaths();
            var missing = new List<string>();
            var okList = new List<string>();
            var skipList = new List<string>();
            var noPsList = new List<string>();

            foreach (var path in prefabPaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                if (SkipList.Contains(fileName)) { skipList.Add(fileName); continue; }

                var rootPs = prefab.GetComponent<ParticleSystem>();
                if (rootPs == null) { noPsList.Add(fileName); continue; }

                if (prefab.GetComponent<PooledVfx>() == null)
                    missing.Add(fileName);
                else
                    okList.Add(fileName);
            }

            Debug.Log(
                $"[VfxPoolAttacher] 상태 리포트\n" +
                $"  누락 ({missing.Count}):\n    " + string.Join("\n    ", missing) + "\n\n" +
                $"  부착됨 ({okList.Count}):\n    " + string.Join("\n    ", okList) + "\n\n" +
                $"  제외 목록 ({skipList.Count}):\n    " + string.Join("\n    ", skipList) + "\n\n" +
                $"  루트 PS 없음 ({noPsList.Count}):\n    " + string.Join("\n    ", noPsList));
        }

        [MenuItem(MENU_ROOT + "AutoDestroyPS 제거 (풀링 전환 시)", priority = 200)]
        public static void RemoveAutoDestroyPS()
        {
            if (!EditorUtility.DisplayDialog(
                "AutoDestroyPS 제거",
                "풀링 전환 후 모든 VFX 프리팹에서 AutoDestroyPS 컴포넌트를 제거합니다.\n진행하시겠습니까?",
                "진행", "취소"))
                return;

            var prefabPaths = CollectVfxPrefabPaths();
            int removed = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var path in prefabPaths)
                {
                    GameObject instance = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        var comp = instance.GetComponent<DrillCorp.VFX.AutoDestroyPS>();
                        if (comp != null)
                        {
                            Object.DestroyImmediate(comp, true);
                            PrefabUtility.SaveAsPrefabAsset(instance, path);
                            removed++;
                        }
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(instance);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[VfxPoolAttacher] AutoDestroyPS 제거 완료: {removed} 개");
            EditorUtility.DisplayDialog("제거 완료", $"{removed} 개 프리팹에서 AutoDestroyPS 제거", "OK");
        }

        private static List<string> CollectVfxPrefabPaths()
        {
            var paths = new List<string>();
            foreach (var folder in TargetFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                foreach (var guid in guids)
                    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
            return paths.Distinct().ToList();
        }
    }
}
#endif

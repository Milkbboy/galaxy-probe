#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DrillCorp.VFX;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// VFX 프리팹에 <see cref="AutoDestroyPS"/> 를 일괄 부착하는 에디터 도구.
    ///
    /// 누수 원인: 런타임에 `Destroy(vfx, ps.main.duration + startLifetime)` 로 수명 추정 후 파괴하는
    /// 패턴이 Polygon Arsenal 처럼 자식 PS 가 있는 복합 VFX 에서 잔류/누적 유발. 프리팹 루트에
    /// AutoDestroyPS 를 미리 부착해두면 `stopAction=Callback` 으로 실제 PS 종료 시점에 파괴됨.
    ///
    /// 메뉴:
    ///   Tools/Drill-Corp/Dev/VFX 정리/모든 VFX 프리팹에 AutoDestroyPS 부착
    ///   Tools/Drill-Corp/Dev/VFX 정리/AutoDestroyPS 누락 프리팹 목록
    ///
    /// 처리 대상:
    ///   1) `Assets/_Game/VFX/Prefabs/*.prefab` — 무기/벌레 공용 FX 프리팹
    ///   2) 루트에 ParticleSystem 컴포넌트가 있는 프리팹만
    ///   3) 이미 AutoDestroyPS 가 있으면 스킵
    ///   4) LaserBeam / LaserScorchDecay 등 루프 제어가 따로 있는 프리팹은 제외 목록
    /// </summary>
    public static class VfxAutoDestroyAttacher
    {
        private const string MENU_ROOT = "Tools/Drill-Corp/Dev/VFX 정리/";

        // 대상 폴더 (프로젝트 내 공용 VFX 프리팹 저장 위치)
        private static readonly string[] TargetFolders =
        {
            "Assets/_Game/VFX/Prefabs",
        };

        // 제외 대상 — 자체 수명 제어 로직이 있는 프리팹 (루프 VFX 등)
        private static readonly HashSet<string> SkipList = new HashSet<string>
        {
            "FX_Laser_Trail",    // 무기 발사 중 트레일 — 부모가 파괴 제어
            "FX_Laser_Impact",   // 장판형 루프 — LaserScorchDecay 가 페이드 제어
        };

        [MenuItem(MENU_ROOT + "모든 VFX 프리팹에 AutoDestroyPS 부착", priority = 100)]
        public static void AttachToAllVfx()
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

                    // 프리팹 에디팅
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

                        if (instance.GetComponent<AutoDestroyPS>() != null)
                        {
                            skippedAlreadyHas++;
                            log.Add($"[SKIP:ALREADY] {fileName}");
                            continue;
                        }

                        instance.AddComponent<AutoDestroyPS>();
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
                $"[VfxAutoDestroyAttacher]\n" +
                $"  attached       : {attached}\n" +
                $"  already has    : {skippedAlreadyHas}\n" +
                $"  no root PS     : {skippedNoPS}\n" +
                $"  excluded       : {skippedExcluded}\n" +
                $"  total scanned  : {prefabPaths.Count}\n\n" +
                string.Join("\n", log);

            Debug.Log(report);
            EditorUtility.DisplayDialog(
                "VFX AutoDestroyPS 부착 완료",
                $"부착: {attached}\n이미 있음: {skippedAlreadyHas}\n루트 PS 없음: {skippedNoPS}\n제외 목록: {skippedExcluded}\n전체 스캔: {prefabPaths.Count}\n\n상세는 Console 확인.",
                "OK");
        }

        [MenuItem(MENU_ROOT + "AutoDestroyPS 누락 프리팹 목록", priority = 101)]
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

                if (prefab.GetComponent<AutoDestroyPS>() == null)
                    missing.Add(fileName);
                else
                    okList.Add(fileName);
            }

            Debug.Log(
                $"[VfxAutoDestroyAttacher] 상태 리포트\n" +
                $"  누락 ({missing.Count}):\n    " + string.Join("\n    ", missing) + "\n\n" +
                $"  부착됨 ({okList.Count}):\n    " + string.Join("\n    ", okList) + "\n\n" +
                $"  제외 목록 ({skipList.Count}):\n    " + string.Join("\n    ", skipList) + "\n\n" +
                $"  루트 PS 없음 ({noPsList.Count}):\n    " + string.Join("\n    ", noPsList));
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

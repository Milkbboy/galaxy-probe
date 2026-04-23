#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// 현재 열려있는 씬(플레이 중이면 플레이 씬)의 Hierarchy 를 텍스트 파일로 덤프.
    /// 플레이 일시정지 상태에서 찍어 시점별 비교(diff)로 누수/누적 패턴 추적하는 도구.
    ///
    /// 메뉴:
    ///   Tools/Drill-Corp/Dev/Hierarchy 덤프/1. 스냅샷 A 저장        (Ctrl+Alt+Shift+Q)
    ///   Tools/Drill-Corp/Dev/Hierarchy 덤프/2. 스냅샷 B 저장        (Ctrl+Alt+Shift+W)
    ///   Tools/Drill-Corp/Dev/Hierarchy 덤프/3. A vs B diff 저장
    ///   Tools/Drill-Corp/Dev/Hierarchy 덤프/덤프 폴더 열기
    ///
    /// 출력: 프로젝트 루트/HierarchyDumps/{timestamp}_{A|B}.txt, diff_{AB}_{timestamp}.txt
    /// </summary>
    public static class HierarchyDumper
    {
        private const string MENU_ROOT = "Tools/Drill-Corp/Dev/Hierarchy 덤프/";
        private const string DUMP_DIR = "HierarchyDumps";

        private static string _lastSnapshotA;
        private static string _lastSnapshotB;

        // ─── 메뉴 ───

        [MenuItem(MENU_ROOT + "1. 스냅샷 A 저장 %&#q", priority = 100)]
        public static void DumpSnapshotA()
        {
            _lastSnapshotA = Dump("A");
        }

        [MenuItem(MENU_ROOT + "2. 스냅샷 B 저장 %&#w", priority = 101)]
        public static void DumpSnapshotB()
        {
            _lastSnapshotB = Dump("B");
        }

        [MenuItem(MENU_ROOT + "3. A vs B diff 저장", priority = 102)]
        public static void DiffAB()
        {
            // static 필드는 스크립트 재컴파일 시 리셋됨 → 파일시스템에서 최신 A/B 를 자동 탐색.
            string snapA = ResolveSnapshotPath(_lastSnapshotA, "A");
            string snapB = ResolveSnapshotPath(_lastSnapshotB, "B");

            if (snapA == null)
            {
                EditorUtility.DisplayDialog("Diff 실패",
                    $"스냅샷 A 파일을 찾을 수 없습니다.\n({DUMP_DIR}/ 폴더에 *_A.txt 파일이 있어야 합니다.)", "OK");
                return;
            }
            if (snapB == null)
            {
                EditorUtility.DisplayDialog("Diff 실패",
                    $"스냅샷 B 파일을 찾을 수 없습니다.\n({DUMP_DIR}/ 폴더에 *_B.txt 파일이 있어야 합니다.)", "OK");
                return;
            }

            string diffPath = WriteDiff(snapA, snapB);
            Debug.Log($"[HierarchyDumper] diff saved → {diffPath}\n  A: {Path.GetFileName(snapA)}\n  B: {Path.GetFileName(snapB)}");
            EditorUtility.RevealInFinder(diffPath);
        }

        /// <summary>
        /// static 필드가 유효하면 그것을, 아니면 덤프 폴더에서 가장 최근 {label}.txt 를 반환.
        /// </summary>
        private static string ResolveSnapshotPath(string cached, string label)
        {
            if (!string.IsNullOrEmpty(cached) && File.Exists(cached)) return cached;

            string dir = EnsureDumpDir();
            if (!Directory.Exists(dir)) return null;

            var candidates = Directory.GetFiles(dir, $"*_{label}.txt");
            if (candidates.Length == 0) return null;

            // 수정 시각 기준 최신
            Array.Sort(candidates, (x, y) => File.GetLastWriteTime(y).CompareTo(File.GetLastWriteTime(x)));
            return candidates[0];
        }

        [MenuItem(MENU_ROOT + "덤프 폴더 열기", priority = 200)]
        public static void OpenDumpFolder()
        {
            string dir = EnsureDumpDir();
            EditorUtility.RevealInFinder(dir);
        }

        // ─── 덤프 구현 ───

        private static string Dump(string label)
        {
            var sb = new StringBuilder(16 * 1024);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string path = Path.Combine(EnsureDumpDir(), $"{timestamp}_{label}.txt");

            // ─── 헤더 ───
            sb.AppendLine($"# Hierarchy Dump — {label}");
            sb.AppendLine($"timestamp={timestamp}");
            sb.AppendLine($"unityVersion={Application.unityVersion}");
            sb.AppendLine($"playMode={EditorApplication.isPlaying}");
            sb.AppendLine($"paused={EditorApplication.isPaused}");
            sb.AppendLine($"timeSinceStartup={EditorApplication.timeSinceStartup:F2}s");
            if (Application.isPlaying)
            {
                sb.AppendLine($"Time.time={Time.time:F2}s  frameCount={Time.frameCount}");
            }
            sb.AppendLine();

            // ─── 씬 루트 수집 ───
            var activeRoots = new List<GameObject>();
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                sb.AppendLine($"scene[{i}] name={scene.name} path={scene.path} rootCount={scene.rootCount}");
                foreach (var go in scene.GetRootGameObjects())
                    activeRoots.Add(go);
            }

            // DontDestroyOnLoad 객체 수집 (플레이 중일 때만 접근 가능)
            var ddolRoots = new List<GameObject>();
            if (Application.isPlaying)
            {
                ddolRoots = CollectDontDestroyOnLoadRoots();
                sb.AppendLine($"ddol rootCount={ddolRoots.Count}");
            }
            sb.AppendLine();

            // ─── 집계 수집 ───
            var componentCounts = new Dictionary<string, int>();
            var goStats = new GoStats();

            foreach (var root in activeRoots)
                Walk(root.transform, 0, sb, componentCounts, goStats, countOnly: false);
            foreach (var root in ddolRoots)
                Walk(root.transform, 0, sb, componentCounts, goStats, countOnly: false, prefix: "[DDOL] ");

            // ─── 런타임 상태 ───
            sb.AppendLine();
            sb.AppendLine("## Runtime Stats");
            sb.AppendLine($"GC.GetTotalMemory(false)={GC.GetTotalMemory(false) / (1024.0 * 1024.0):F2} MB");
            if (Application.isPlaying)
            {
                long totalAlloc = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                long totalReserved = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
                long monoUsed = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
                long monoHeap = UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong();
                sb.AppendLine($"Profiler.TotalAllocated={totalAlloc / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"Profiler.TotalReserved={totalReserved / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"Profiler.MonoUsed={monoUsed / (1024.0 * 1024.0):F2} MB");
                sb.AppendLine($"Profiler.MonoHeap={monoHeap / (1024.0 * 1024.0):F2} MB");
            }

            // ─── Unity 에셋 카운트 (Resources.FindObjectsOfTypeAll) ───
            sb.AppendLine();
            sb.AppendLine("## Asset Counts (FindObjectsOfTypeAll)");
            sb.AppendLine($"GameObject={Resources.FindObjectsOfTypeAll<GameObject>().Length}");
            sb.AppendLine($"Texture2D={Resources.FindObjectsOfTypeAll<Texture2D>().Length}");
            sb.AppendLine($"Material={Resources.FindObjectsOfTypeAll<Material>().Length}");
            sb.AppendLine($"Mesh={Resources.FindObjectsOfTypeAll<Mesh>().Length}");
            sb.AppendLine($"Sprite={Resources.FindObjectsOfTypeAll<Sprite>().Length}");
            sb.AppendLine($"ParticleSystem={Resources.FindObjectsOfTypeAll<ParticleSystem>().Length}");
            sb.AppendLine($"AudioClip={Resources.FindObjectsOfTypeAll<AudioClip>().Length}");
            sb.AppendLine($"ScriptableObject={Resources.FindObjectsOfTypeAll<ScriptableObject>().Length}");

            // ─── GameObject 집계 ───
            sb.AppendLine();
            sb.AppendLine("## GameObject 집계");
            sb.AppendLine($"total={goStats.Total}  active={goStats.Active}  inactive={goStats.Inactive}");
            sb.AppendLine($"with_children={goStats.WithChildren}  max_depth={goStats.MaxDepth}");

            // ─── 컴포넌트 타입별 카운트 (살아있는 인스턴스만) ───
            sb.AppendLine();
            sb.AppendLine("## Component 집계 (count, Top 100)");
            foreach (var kv in componentCounts.OrderByDescending(k => k.Value).Take(100))
            {
                sb.AppendLine($"{kv.Value,6}  {kv.Key}");
            }

            // ─── 덤프 트리 ───
            sb.AppendLine();
            sb.AppendLine("## Hierarchy Tree");
            sb.AppendLine("(format: depth-indent | [active] name  {comp1, comp2, ...})");
            sb.AppendLine();
            foreach (var root in activeRoots)
                Walk(root.transform, 0, sb, null, null, countOnly: false, emitTree: true);
            if (ddolRoots.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### DontDestroyOnLoad");
                foreach (var root in ddolRoots)
                    Walk(root.transform, 0, sb, null, null, countOnly: false, emitTree: true);
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[HierarchyDumper] snapshot {label} saved → {path}");
            EditorGUIUtility.systemCopyBuffer = path;
            EditorUtility.RevealInFinder(path);
            return path;
        }

        private static void Walk(
            Transform t, int depth, StringBuilder sb,
            Dictionary<string, int> componentCounts, GoStats goStats,
            bool countOnly, bool emitTree = false, string prefix = "")
        {
            if (t == null) return;

            // stats
            if (goStats != null)
            {
                goStats.Total++;
                if (t.gameObject.activeInHierarchy) goStats.Active++; else goStats.Inactive++;
                if (t.childCount > 0) goStats.WithChildren++;
                if (depth > goStats.MaxDepth) goStats.MaxDepth = depth;
            }

            // component counts
            var comps = t.GetComponents<Component>();
            if (componentCounts != null)
            {
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    string name = c.GetType().Name;
                    if (!componentCounts.TryGetValue(name, out int n)) n = 0;
                    componentCounts[name] = n + 1;
                }
            }

            // tree line
            if (emitTree)
            {
                string indent = depth == 0 ? string.Empty : new string(' ', depth * 2);
                string activeMark = t.gameObject.activeInHierarchy ? "+" : "-";
                string compList = string.Join(", ", comps.Where(c => c != null).Select(c => c.GetType().Name));
                sb.Append(prefix).Append(indent).Append('[').Append(activeMark).Append("] ")
                  .Append(t.name).Append("  {").Append(compList).Append('}').AppendLine();
            }

            for (int i = 0; i < t.childCount; i++)
                Walk(t.GetChild(i), depth + 1, sb, componentCounts, goStats, countOnly, emitTree, prefix);
        }

        // ─── DDOL 루트 수집 ───
        // Unity 는 DontDestroyOnLoad 씬을 직접 조회할 API 가 없음 → FindObjectsOfType 필터
        private static List<GameObject> CollectDontDestroyOnLoadRoots()
        {
            var results = new List<GameObject>();
            var all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var t in all)
            {
                if (t == null) continue;
                if (t.parent != null) continue; // root only
                var go = t.gameObject;
                var scene = go.scene;
                // DDOL 씬은 buildIndex == -1 이고 이름이 "DontDestroyOnLoad"
                if (!scene.IsValid()) continue;
                if (scene.buildIndex != -1) continue;
                if (scene.name != "DontDestroyOnLoad") continue;
                results.Add(go);
            }
            return results;
        }

        private static string EnsureDumpDir()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string dir = Path.Combine(projectRoot, DUMP_DIR);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        // ─── Diff ───

        private static string WriteDiff(string snapA, string snapB)
        {
            var compA = ParseComponentCounts(snapA);
            var compB = ParseComponentCounts(snapB);
            var assetA = ParseAssetCounts(snapA);
            var assetB = ParseAssetCounts(snapB);
            var runtimeA = ParseRuntimeStats(snapA);
            var runtimeB = ParseRuntimeStats(snapB);
            var goStatsA = ParseGoStats(snapA);
            var goStatsB = ParseGoStats(snapB);

            var sb = new StringBuilder(8 * 1024);
            sb.AppendLine("# Hierarchy Diff — A vs B");
            sb.AppendLine($"snapshotA={Path.GetFileName(snapA)}");
            sb.AppendLine($"snapshotB={Path.GetFileName(snapB)}");
            sb.AppendLine($"generated={DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // ─── Runtime delta ───
            sb.AppendLine("## Runtime Stats (A → B, Δ)");
            WriteMapDiff(sb, runtimeA, runtimeB, formatFloat: true);

            // ─── GameObject stats ───
            sb.AppendLine();
            sb.AppendLine("## GameObject 집계 (A → B, Δ)");
            WriteMapDiff(sb, goStatsA, goStatsB, formatFloat: false);

            // ─── Asset counts delta ───
            sb.AppendLine();
            sb.AppendLine("## Asset Counts (A → B, Δ)");
            WriteMapDiff(sb, assetA, assetB, formatFloat: false);

            // ─── Component delta ───
            sb.AppendLine();
            sb.AppendLine("## Component 집계 (A → B, Δ 내림차순)");
            var allKeys = new HashSet<string>(compA.Keys);
            allKeys.UnionWith(compB.Keys);
            var rows = new List<(string name, int a, int b, int d)>();
            foreach (var k in allKeys)
            {
                int a = compA.TryGetValue(k, out var va) ? va : 0;
                int b = compB.TryGetValue(k, out var vb) ? vb : 0;
                rows.Add((k, a, b, b - a));
            }
            // 증가분 내림차순 → 감소분(음수) 뒤쪽, 동일(0) 마지막
            rows.Sort((x, y) =>
            {
                int ax = Math.Sign(x.d), ay = Math.Sign(y.d);
                if (ax != ay) return ay.CompareTo(ax);
                return Math.Abs(y.d).CompareTo(Math.Abs(x.d));
            });
            sb.AppendLine($"{"Δ",6}  {"A",6}  {"B",6}  Component");
            foreach (var r in rows)
                sb.AppendLine($"{(r.d > 0 ? "+" + r.d : r.d.ToString()),6}  {r.a,6}  {r.b,6}  {r.name}");

            string path = Path.Combine(EnsureDumpDir(), $"diff_AB_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            EditorGUIUtility.systemCopyBuffer = path;
            return path;
        }

        private static void WriteMapDiff(
            StringBuilder sb, Dictionary<string, double> a, Dictionary<string, double> b, bool formatFloat)
        {
            var keys = new HashSet<string>(a.Keys);
            keys.UnionWith(b.Keys);
            foreach (var k in keys.OrderBy(x => x))
            {
                a.TryGetValue(k, out double va);
                b.TryGetValue(k, out double vb);
                double d = vb - va;
                string sign = d > 0 ? "+" : "";
                if (formatFloat)
                    sb.AppendLine($"{k}: {va:F2} → {vb:F2}  ({sign}{d:F2})");
                else
                    sb.AppendLine($"{k}: {(long)va} → {(long)vb}  ({sign}{(long)d})");
            }
        }

        // ─── 파싱 헬퍼 (자가 생성 포맷이므로 단순 라인 매칭) ───

        private static Dictionary<string, int> ParseComponentCounts(string path)
        {
            var map = new Dictionary<string, int>();
            bool inSection = false;
            foreach (var raw in File.ReadLines(path))
            {
                string line = raw.TrimEnd();
                if (line.StartsWith("## Component 집계")) { inSection = true; continue; }
                if (inSection && line.StartsWith("## ")) break;
                if (!inSection) continue;
                if (string.IsNullOrWhiteSpace(line)) continue;
                // "  1234  ComponentName" 형태. 첫 토큰 숫자 + 나머지 이름.
                var trimmed = line.TrimStart();
                int sp = trimmed.IndexOf(' ');
                if (sp <= 0) continue;
                if (!int.TryParse(trimmed.Substring(0, sp), out int count)) continue;
                string name = trimmed.Substring(sp).Trim();
                if (string.IsNullOrEmpty(name)) continue;
                map[name] = count;
            }
            return map;
        }

        private static Dictionary<string, double> ParseAssetCounts(string path)
        {
            var map = new Dictionary<string, double>();
            bool inSection = false;
            foreach (var raw in File.ReadLines(path))
            {
                string line = raw.TrimEnd();
                if (line.StartsWith("## Asset Counts")) { inSection = true; continue; }
                if (inSection && line.StartsWith("## ")) break;
                if (!inSection) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                if (double.TryParse(val, out double d)) map[key] = d;
            }
            return map;
        }

        private static Dictionary<string, double> ParseRuntimeStats(string path)
        {
            var map = new Dictionary<string, double>();
            bool inSection = false;
            foreach (var raw in File.ReadLines(path))
            {
                string line = raw.TrimEnd();
                if (line.StartsWith("## Runtime Stats")) { inSection = true; continue; }
                if (inSection && line.StartsWith("## ")) break;
                if (!inSection) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                // " MB" 붙어있으면 제거
                int spaceIdx = val.IndexOf(' ');
                if (spaceIdx > 0) val = val.Substring(0, spaceIdx);
                if (double.TryParse(val, out double d)) map[key] = d;
            }
            return map;
        }

        private static Dictionary<string, double> ParseGoStats(string path)
        {
            var map = new Dictionary<string, double>();
            bool inSection = false;
            foreach (var raw in File.ReadLines(path))
            {
                string line = raw.TrimEnd();
                if (line.StartsWith("## GameObject 집계")) { inSection = true; continue; }
                if (inSection && line.StartsWith("## ")) break;
                if (!inSection) continue;
                // "total=123  active=100  ..." 형태 파싱
                foreach (var pair in line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int eq = pair.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = pair.Substring(0, eq).Trim();
                    string val = pair.Substring(eq + 1).Trim();
                    if (double.TryParse(val, out double d)) map[key] = d;
                }
            }
            return map;
        }

        private class GoStats
        {
            public int Total;
            public int Active;
            public int Inactive;
            public int WithChildren;
            public int MaxDepth;
        }
    }
}
#endif

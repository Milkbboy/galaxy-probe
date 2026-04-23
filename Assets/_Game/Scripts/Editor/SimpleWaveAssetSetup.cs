// SimpleBug 전면 교체 — Phase B-1f/g
// ─────────────────────────────────────────────────────────────
// 생성 대상:
//   • SpawnConfig.asset (Assets/_Game/Data/SpawnConfig.asset) × 1
//   • Wave_01~05.asset (Assets/_Game/Data/Waves/Wave_01.asset …) × 5
//
// 기존 Wave_*.asset이 있으면 덮어쓰지 않고 스킵 (레거시 WaveData.cs 기반 에셋 보존).
// 새 에셋은 DrillCorp.Data.SimpleWaveData 타입으로 생성되어 타입 충돌 없음.
//
// Game 씬 바인딩:
//   • 씬에 "SimpleWaveManager" GameObject 없으면 생성 + 컴포넌트 추가
//   • _spawnConfig / _waves / _spawner / _tunnel 자동 바인딩 (씬 내 기존 SimpleBugSpawner·TunnelEventManager 참조)
//   • 씬 더티 마킹 — 사용자가 Ctrl+S로 저장
//
// 참고: docs/SimpleBugSheet.md (스키마), docs/_review/WaveData.tsv (초기값)
// ─────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DrillCorp.Data;
using DrillCorp.Bug.Simple;
using DrillCorp.Wave;

namespace DrillCorp.Editor
{
    public static class SimpleWaveAssetSetup
    {
        const string DATA_ROOT = "Assets/_Game/Data";
        const string WAVES_DIR = DATA_ROOT + "/Waves";
        const string SPAWN_CONFIG_PATH = DATA_ROOT + "/SpawnConfig.asset";

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/SimpleBug/1. SpawnConfig + Wave_01~05 에셋 생성")]
        public static void CreateWaveAssets()
        {
            EnsureFolders();
            CreateSpawnConfig();
            CreateWaves();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SimpleWaveAssetSetup] SpawnConfig + Wave_01~05 생성 완료. Data/SpawnConfig.asset, Data/Waves/ 확인.");
        }

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/SimpleBug/2. Game 씬에 SimpleWaveManager 바인딩")]
        public static void BindSimpleWaveManagerInScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Game")
            {
                Debug.LogError("[SimpleWaveAssetSetup] Game 씬을 열고 실행하세요. 현재: " + (scene.IsValid() ? scene.name : "(none)"));
                return;
            }

            var spawnConfig = AssetDatabase.LoadAssetAtPath<SpawnConfigData>(SPAWN_CONFIG_PATH);
            if (spawnConfig == null)
            {
                Debug.LogError("[SimpleWaveAssetSetup] SpawnConfig.asset 없음 — 1번 메뉴를 먼저 실행하세요.");
                return;
            }

            var waves = LoadAllWaveAssetsOrdered();
            if (waves.Count == 0)
            {
                Debug.LogError("[SimpleWaveAssetSetup] Wave_*.asset 없음 — 1번 메뉴를 먼저 실행하세요.");
                return;
            }

            SimpleBugSpawner spawner = Object.FindAnyObjectByType<SimpleBugSpawner>();
            TunnelEventManager tunnel = Object.FindAnyObjectByType<TunnelEventManager>();
            if (spawner == null) Debug.LogWarning("[SimpleWaveAssetSetup] 씬에 SimpleBugSpawner 없음 — 필드 비워둠");
            if (tunnel == null) Debug.LogWarning("[SimpleWaveAssetSetup] 씬에 TunnelEventManager 없음 — 필드 비워둠");

            var mgr = Object.FindAnyObjectByType<SimpleWaveManager>();
            GameObject go;
            if (mgr == null)
            {
                go = new GameObject("SimpleWaveManager");
                mgr = go.AddComponent<SimpleWaveManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create SimpleWaveManager");
            }
            else
            {
                go = mgr.gameObject;
            }

            var so = new SerializedObject(mgr);
            var wavesProp = so.FindProperty("_waves");
            wavesProp.arraySize = waves.Count;
            for (int i = 0; i < waves.Count; i++)
            {
                wavesProp.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];
            }
            so.FindProperty("_spawnConfig").objectReferenceValue = spawnConfig;
            so.FindProperty("_spawner").objectReferenceValue = spawner;
            so.FindProperty("_tunnel").objectReferenceValue = tunnel;
            so.FindProperty("_autoStart").boolValue = true;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = go;
            Debug.Log($"[SimpleWaveAssetSetup] SimpleWaveManager 바인딩 완료 — waves={waves.Count}, config={spawnConfig.name}, spawner={(spawner != null ? spawner.name : "null")}, tunnel={(tunnel != null ? tunnel.name : "null")}. 씬 저장 필요.");
        }

        // ═════════════════════════════════════════════════════

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(DATA_ROOT))
                AssetDatabase.CreateFolder("Assets/_Game", "Data");
            if (!AssetDatabase.IsValidFolder(WAVES_DIR))
                AssetDatabase.CreateFolder(DATA_ROOT, "Waves");
        }

        static void CreateSpawnConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<SpawnConfigData>(SPAWN_CONFIG_PATH) != null)
            {
                Debug.Log($"[SimpleWaveAssetSetup] SpawnConfig.asset 이미 존재 — 스킵");
                return;
            }
            var cfg = ScriptableObject.CreateInstance<SpawnConfigData>();
            // SpawnConfigData의 field initializer가 이미 기본값 담고 있어 추가 세팅 불필요
            AssetDatabase.CreateAsset(cfg, SPAWN_CONFIG_PATH);
            Debug.Log($"[SimpleWaveAssetSetup] 생성: {SPAWN_CONFIG_PATH}");
        }

        // docs/_review/WaveData.tsv 값 그대로
        static readonly WaveRow[] WAVE_ROWS = new[]
        {
            new WaveRow { Num = 1, Name = "시작",     Kills = 15f, Normal = 0.12f,  Elite = -1f, Max = 50,  Tunnel = false, TunnelInt = -1f, Swift = -1 },
            new WaveRow { Num = 2, Name = "가속",     Kills = 25f, Normal = 0.1f,   Elite = -1f, Max = 70,  Tunnel = false, TunnelInt = -1f, Swift = -1 },
            new WaveRow { Num = 3, Name = "땅굴 출현", Kills = 40f, Normal = 0.083f, Elite = 15f, Max = 90,  Tunnel = true,  TunnelInt = 15f, Swift = 10 },
            new WaveRow { Num = 4, Name = "러시",     Kills = 60f, Normal = 0.07f,  Elite = 12f, Max = 110, Tunnel = true,  TunnelInt = 12f, Swift = 12 },
            new WaveRow { Num = 5, Name = "최종",     Kills = -1f, Normal = 0.06f,  Elite = 10f, Max = 130, Tunnel = true,  TunnelInt = 10f, Swift = 15 },
        };

        struct WaveRow
        {
            public int Num;
            public string Name;
            public float Kills;
            public float Normal;
            public float Elite;
            public int Max;
            public bool Tunnel;
            public float TunnelInt;
            public int Swift;
        }

        static void CreateWaves()
        {
            foreach (var r in WAVE_ROWS)
            {
                string path = $"{WAVES_DIR}/Wave_{r.Num:D2}.asset";
                if (AssetDatabase.LoadAssetAtPath<SimpleWaveData>(path) != null)
                {
                    Debug.Log($"[SimpleWaveAssetSetup] {path} 이미 존재 — 스킵");
                    continue;
                }
                var w = ScriptableObject.CreateInstance<SimpleWaveData>();
                w.WaveNumber = r.Num;
                w.WaveName = r.Name;
                w.KillTarget = r.Kills;
                w.NormalSpawnInterval = r.Normal;
                w.EliteSpawnInterval = r.Elite;
                w.MaxBugs = r.Max;
                w.TunnelEnabled = r.Tunnel;
                w.TunnelEventInterval = r.TunnelInt;
                w.SwiftPerTunnel = r.Swift;
                AssetDatabase.CreateAsset(w, path);
                Debug.Log($"[SimpleWaveAssetSetup] 생성: {path}");
            }
        }

        static List<SimpleWaveData> LoadAllWaveAssetsOrdered()
        {
            var result = new List<SimpleWaveData>();
            var guids = AssetDatabase.FindAssets("t:SimpleWaveData", new[] { WAVES_DIR });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var w = AssetDatabase.LoadAssetAtPath<SimpleWaveData>(path);
                if (w != null) result.Add(w);
            }
            result.Sort((a, b) => a.WaveNumber.CompareTo(b.WaveNumber));
            return result;
        }
    }
}

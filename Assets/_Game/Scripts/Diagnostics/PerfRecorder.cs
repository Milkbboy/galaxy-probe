using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DrillCorp.Diagnostics
{
    /// <summary>
    /// ProfilerRecorder 기반 세션 로거. 수정 전/후 성능 비교용.
    ///
    /// 사용법 (씬 설정 불필요 — 플레이 모드 진입 시 자동 생성):
    /// 1. 플레이 모드 진입.
    /// 2. F9 눌러 녹화 시작.   (F10 으로 라벨 전환: baseline → wave_fighting → drones → heavy)
    /// 3. 원하는 시나리오 재현 (예: 벌레 웨이브 중 기관총 연사 60초).
    /// 4. 다시 F9 눌러 정지.
    ///    → 프로젝트 루트 `PerfLogs/` 에 CSV 2개 저장 + **클립보드 자동 복사** + Explorer 자동 오픈.
    ///    → 카톡/디스코드/메모장에 Ctrl+V 하면 바로 공유 가능.
    /// </summary>
    public class PerfRecorder : MonoBehaviour
    {
        [Header("Controls (New Input System)")]
        [SerializeField] private Key _toggleKey = Key.F9;
        [Tooltip("라벨 프리셋 순환 (녹화 중엔 비활성)")]
        [SerializeField] private Key _labelCycleKey = Key.F10;

        [Header("Output")]
        [Tooltip("프로젝트 루트 기준 상대 경로. Assets 폴더와 같은 레벨에 생성됨.")]
        [SerializeField] private string _outputFolder = "PerfLogs";

        [Tooltip("파일명 접두사 — 수정 전/후 구분용.")]
        [SerializeField] private string _sessionLabel = "baseline";

        [Tooltip("F10 으로 순환할 라벨 프리셋.")]
        [SerializeField] private string[] _labelPresets =
        {
            "baseline",
            "wave_fighting",
            "drones_active",
            "heavy_combat",
        };

        [Header("Spike Detection")]
        [Tooltip("이 ms 초과 프레임을 spikes 로그에 기록")]
        [SerializeField] private float _spikeThresholdMs = 33.3f;

        [Header("HUD Overlay")]
        [SerializeField] private bool _showOverlay = true;

        public enum OverlayCorner
        {
            MiddleRight,   // 기본값 — Game 씬 우측 중앙 (HUD·미니맵과 안 겹침)
            MiddleLeft,
            BottomRight,
            BottomLeft,
            TopRight,
            TopLeft,
        }
        [Tooltip("Game 씬 HUD 와 겹치지 않게 — 기본값은 우측 중앙.")]
        [SerializeField] private OverlayCorner _overlayCorner = OverlayCorner.MiddleRight;

        [Header("Share")]
        [Tooltip("녹화 종료 시 CSV 내용을 시스템 클립보드에 복사")]
        [SerializeField] private bool _copyToClipboardOnSave = true;
        [Tooltip("녹화 종료 시 PerfLogs 폴더를 Explorer 로 열기 (Windows)")]
        [SerializeField] private bool _openFolderOnSave = true;

        // 자동 부트스트랩 — 씬에 수동 배치 없어도 플레이 시 자동 생성.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            // 수동 배치된 인스턴스가 있으면 중복 생성 안 함.
            if (FindAnyObjectByType<PerfRecorder>() != null) return;
            var go = new GameObject("[PerfRecorder]");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfRecorder>();
        }

        // 저장 후 배너 표시용
        private string _bannerText;
        private float _bannerUntil;

        // ──────────────────────────────────────────────────────────
        // 녹화할 ProfilerRecorder 마커. .Valid 실패 시 자동 skip.
        // 이름 오타/버전차이로 일부 누락돼도 전체는 계속 동작.
        // ──────────────────────────────────────────────────────────
        private static readonly (ProfilerCategory Cat, string Name, string Alias)[] MarkerSpecs =
        {
            // CPU
            (ProfilerCategory.Internal, "Main Thread",                 "MainThread"),
            (ProfilerCategory.Internal, "CPU Main Thread Frame Time",  "MainFrameTime"),
            (ProfilerCategory.Render,   "Render Thread",               "RenderThread"),

            // Rendering counters
            (ProfilerCategory.Render,   "Batches Count",               "Batches"),
            (ProfilerCategory.Render,   "SetPass Calls Count",         "SetPass"),
            (ProfilerCategory.Render,   "Draw Calls Count",            "DrawCalls"),
            (ProfilerCategory.Render,   "Vertices Count",              "Vertices"),
            (ProfilerCategory.Render,   "Triangles Count",             "Triangles"),
            (ProfilerCategory.Render,   "Shadow Casters Count",        "ShadowCasters"),

            // Memory / GC
            (ProfilerCategory.Memory,   "GC Used Memory",              "GCUsed"),
            (ProfilerCategory.Memory,   "GC Reserved Memory",          "GCReserved"),
            (ProfilerCategory.Memory,   "GC Allocated In Frame",       "GCAllocInFrame"),
            (ProfilerCategory.Memory,   "System Used Memory",          "SystemUsed"),
            (ProfilerCategory.Memory,   "Total Reserved Memory",       "TotalReserved"),

            // ─── 커스텀 마커 (PerfMarkers.cs 에서 선언) ──────────────
            // 의심 구간별 실행 시간 — CSV 에 ns → ms 로 변환돼 찍힘.
            // 마커 이름이 PerfMarkers 선언과 정확히 일치해야 함.
            (ProfilerCategory.Scripts,  "DrillCorp.BugController.Update",     "Bug_Update"),
            (ProfilerCategory.Scripts,  "DrillCorp.BugLabel.LateUpdate",      "BugLabel_Late"),
            (ProfilerCategory.Scripts,  "DrillCorp.Drone.Update",             "Drone_Update"),
            (ProfilerCategory.Scripts,  "DrillCorp.Drone.OverlapSphere",      "Drone_Overlap"),
            (ProfilerCategory.Scripts,  "DrillCorp.Spider.Update",            "Spider_Update"),
            (ProfilerCategory.Scripts,  "DrillCorp.Spider.OverlapSphere",     "Spider_Overlap"),
            (ProfilerCategory.Scripts,  "DrillCorp.TopBarHud.Update",         "TopBarHud_Update"),
            (ProfilerCategory.Scripts,  "DrillCorp.MachineStatusUI.Update",   "MachineStatus_Update"),
            (ProfilerCategory.Scripts,  "DrillCorp.MiningUI.Update",          "MiningUI_Update"),
            (ProfilerCategory.Scripts,  "DrillCorp.AbilityHud.Update",        "AbilityHud_Update"),
            (ProfilerCategory.Scripts,  "DrillCorp.Hp3DBar.LateUpdate",       "Hp3DBar_Late"),
        };

        private class Channel
        {
            public string Alias;
            public ProfilerMarkerDataUnit Unit;
            public ProfilerRecorder Rec;

            // Rolling stats
            public double Sum;
            public double Max;
            public long Count;
            public List<double> Samples;
        }

        private readonly List<Channel> _channels = new();
        private bool _recording;
        private float _realtimeStart;
        private int _frameStart;
        private StringBuilder _spikes;

        // 빠른 접근용 핸들 (spikes 기록에 사용)
        private Channel _chMain, _chRender, _chBatches, _chDraws, _chGCAlloc;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb[_toggleKey].wasPressedThisFrame)
                {
                    if (_recording) StopAndSave();
                    else             BeginRecording();
                }
                else if (!_recording && kb[_labelCycleKey].wasPressedThisFrame
                         && _labelPresets != null && _labelPresets.Length > 0)
                {
                    int idx = Array.IndexOf(_labelPresets, _sessionLabel);
                    idx = (idx + 1) % _labelPresets.Length;
                    _sessionLabel = _labelPresets[Mathf.Max(0, idx)];
                }
            }

            if (_recording) SampleFrame();
        }

        // ──────────────────────────────────────────────────────────

        private void BeginRecording()
        {
            _channels.Clear();
            _chMain = _chRender = _chBatches = _chDraws = _chGCAlloc = null;

            foreach (var m in MarkerSpecs)
            {
                var rec = ProfilerRecorder.StartNew(m.Cat, m.Name, 1);
                if (!rec.Valid) { rec.Dispose(); continue; }

                var ch = new Channel
                {
                    Alias   = m.Alias,
                    Unit    = rec.UnitType,
                    Rec     = rec,
                    Samples = new List<double>(4096),
                };
                _channels.Add(ch);

                // spikes 컬럼용 핸들
                switch (m.Alias)
                {
                    case "MainThread":     _chMain    = ch; break;
                    case "RenderThread":   _chRender  = ch; break;
                    case "Batches":        _chBatches = ch; break;
                    case "DrawCalls":      _chDraws   = ch; break;
                    case "GCAllocInFrame": _chGCAlloc = ch; break;
                }
            }

            _recording     = true;
            _realtimeStart = Time.realtimeSinceStartup;
            _frameStart    = Time.frameCount;
            _spikes        = new StringBuilder();
            _spikes.AppendLine("realtime_s,frame,main_ms,render_ms,batches,draw_calls,gc_alloc_bytes");

            Debug.Log($"[PerfRecorder] START — label={_sessionLabel}, channels={_channels.Count}, threshold={_spikeThresholdMs}ms");
        }

        private void SampleFrame()
        {
            for (int i = 0; i < _channels.Count; i++)
            {
                var c = _channels[i];
                long raw = c.Rec.LastValue;
                double v = ToDisplayValue(raw, c.Unit);
                c.Sum += v;
                if (v > c.Max) c.Max = v;
                c.Count++;
                c.Samples.Add(v);
            }

            // spike 프레임 상세 기록
            if (_chMain != null)
            {
                double mainMs = _chMain.Samples[_chMain.Samples.Count - 1];
                if (mainMs >= _spikeThresholdMs)
                {
                    float rt = Time.realtimeSinceStartup - _realtimeStart;
                    _spikes.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "{0:0.000},{1},{2:0.00},{3:0.00},{4:0},{5:0},{6:0}",
                        rt,
                        Time.frameCount - _frameStart,
                        mainMs,
                        LastOr(_chRender,  0),
                        LastOr(_chBatches, 0),
                        LastOr(_chDraws,   0),
                        LastOr(_chGCAlloc, 0)));
                }
            }
        }

        private static double LastOr(Channel c, double fallback)
            => c != null && c.Samples.Count > 0 ? c.Samples[c.Samples.Count - 1] : fallback;

        // ──────────────────────────────────────────────────────────

        private void StopAndSave()
        {
            _recording = false;
            float duration = Time.realtimeSinceStartup - _realtimeStart;
            int   frames   = Time.frameCount - _frameStart;

            string dir = Path.Combine(Application.dataPath, "..", _outputFolder);
            Directory.CreateDirectory(dir);

            string stamp     = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string safeLabel = string.IsNullOrWhiteSpace(_sessionLabel) ? "session" : _sessionLabel;
            string csvPath   = Path.Combine(dir, $"{safeLabel}_{stamp}.csv");
            string spikePath = Path.Combine(dir, $"{safeLabel}_{stamp}_spikes.csv");

            // 메인 요약 CSV
            using (var w = new StreamWriter(csvPath))
            {
                w.WriteLine($"# session_label={safeLabel}");
                w.WriteLine($"# scene={SceneManager.GetActiveScene().name}");
                w.WriteLine($"# resolution={Screen.width}x{Screen.height}");
                w.WriteLine($"# platform={Application.platform}");
                w.WriteLine($"# isEditor={Application.isEditor}");
                w.WriteLine($"# unityVersion={Application.unityVersion}");
                w.WriteLine($"# duration_s={duration.ToString("0.00", CultureInfo.InvariantCulture)}");
                w.WriteLine($"# frames={frames}");
                w.WriteLine($"# avg_fps={(frames / Mathf.Max(duration, 0.001f)).ToString("0.0", CultureInfo.InvariantCulture)}");
                w.WriteLine($"# vSyncCount={QualitySettings.vSyncCount}");
                w.WriteLine($"# targetFrameRate={Application.targetFrameRate}");
                w.WriteLine($"# qualityLevel={QualitySettings.GetQualityLevel()}");
                w.WriteLine("name,unit,avg,max,p50,p95,p99,samples");

                foreach (var c in _channels)
                {
                    if (c.Count == 0) { c.Rec.Dispose(); continue; }
                    c.Samples.Sort();
                    double avg = c.Sum / c.Count;
                    double p50 = Percentile(c.Samples, 0.50);
                    double p95 = Percentile(c.Samples, 0.95);
                    double p99 = Percentile(c.Samples, 0.99);
                    w.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2:0.00},{3:0.00},{4:0.00},{5:0.00},{6:0.00},{7}",
                        c.Alias, UnitLabel(c.Unit), avg, c.Max, p50, p95, p99, c.Count));
                    c.Rec.Dispose();
                }
            }

            // spikes 상세 CSV
            string spikesText = _spikes.ToString();
            File.WriteAllText(spikePath, spikesText);

            Debug.Log($"[PerfRecorder] SAVED {frames} frames / {duration:0.0}s\n  → {csvPath}\n  → {spikePath}");

            // 1) 클립보드에 CSV + spikes 합쳐서 복사 — 채팅에 바로 붙여넣을 수 있도록.
            if (_copyToClipboardOnSave)
            {
                try
                {
                    string summaryText = File.ReadAllText(csvPath);
                    var combined = new StringBuilder(summaryText.Length + spikesText.Length + 64);
                    combined.Append("=== PERF SUMMARY (").Append(safeLabel).Append(") ===\n");
                    combined.Append(summaryText);
                    combined.Append("\n=== SPIKES (main_ms >= ").Append(_spikeThresholdMs.ToString("0.0", CultureInfo.InvariantCulture)).Append("ms) ===\n");
                    combined.Append(spikesText);
                    GUIUtility.systemCopyBuffer = combined.ToString();
                    Debug.Log("[PerfRecorder] 클립보드 복사 완료 — 채팅에 Ctrl+V 로 붙여넣기");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PerfRecorder] 클립보드 복사 실패: {e.Message}");
                }
            }

            // 2) PerfLogs 폴더를 Explorer 로 자동 오픈 (Windows).
            if (_openFolderOnSave)
            {
                try
                {
                    string absDir = Path.GetFullPath(dir);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                    System.Diagnostics.Process.Start("explorer.exe", absDir.Replace('/', '\\'));
#else
                    Application.OpenURL("file://" + absDir);
#endif
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PerfRecorder] 폴더 열기 실패: {e.Message}");
                }
            }

            // 3) 화면에 배너 — 기획자/플레이어가 "끝났다"는 걸 명확히 알 수 있게.
            _bannerText = $"저장 완료! {safeLabel} · {frames} frames / {duration:0.0}s\n" +
                          $"클립보드에 복사됨 → 채팅에 Ctrl+V 로 붙여넣기";
            _bannerUntil = Time.realtimeSinceStartup + 8f;

            _channels.Clear();
        }

        // ──────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────

        /// <summary>raw LastValue 를 사람이 읽기 편한 단위로 변환.</summary>
        /// Time: ns → ms (소수점 유지). Bytes/Count: 그대로.
        private static double ToDisplayValue(long raw, ProfilerMarkerDataUnit unit)
        {
            return unit switch
            {
                ProfilerMarkerDataUnit.TimeNanoseconds => raw * 1e-6, // ns → ms
                _                                      => (double)raw,
            };
        }

        private static string UnitLabel(ProfilerMarkerDataUnit unit)
        {
            return unit switch
            {
                ProfilerMarkerDataUnit.TimeNanoseconds => "ms",
                ProfilerMarkerDataUnit.Bytes           => "bytes",
                ProfilerMarkerDataUnit.Count           => "count",
                ProfilerMarkerDataUnit.Percent         => "percent",
                ProfilerMarkerDataUnit.FrequencyHz     => "hz",
                _                                      => "raw",
            };
        }

        /// <summary>선형 보간 없이 가장 가까운 rank. 샘플 수 많으면 충분.</summary>
        private static double Percentile(List<double> sortedAsc, double p)
        {
            if (sortedAsc.Count == 0) return 0;
            int idx = Mathf.Clamp(
                Mathf.RoundToInt((float)(p * (sortedAsc.Count - 1))),
                0, sortedAsc.Count - 1);
            return sortedAsc[idx];
        }

        // ──────────────────────────────────────────────────────────
        // 라이프사이클
        // ──────────────────────────────────────────────────────────

        private void OnDisable()
        {
            if (_recording) StopAndSave();
            foreach (var c in _channels) c.Rec.Dispose();
            _channels.Clear();
        }

        private void OnGUI()
        {
            if (!_showOverlay) return;

            const int W = 320, H = 82;
            const int MARGIN = 12;

            float x, y;
            switch (_overlayCorner)
            {
                case OverlayCorner.MiddleLeft:
                    x = MARGIN; y = (Screen.height - H) * 0.5f; break;
                case OverlayCorner.BottomRight:
                    x = Screen.width - W - MARGIN; y = Screen.height - H - MARGIN; break;
                case OverlayCorner.BottomLeft:
                    x = MARGIN; y = Screen.height - H - MARGIN; break;
                case OverlayCorner.TopRight:
                    x = Screen.width - W - MARGIN; y = MARGIN; break;
                case OverlayCorner.TopLeft:
                    x = MARGIN; y = MARGIN; break;
                case OverlayCorner.MiddleRight:
                default:
                    x = Screen.width - W - MARGIN; y = (Screen.height - H) * 0.5f; break;
            }

            GUI.Box(new Rect(x, y, W, H), GUIContent.none);

            string state = _recording ? "<color=#ff6b6b>● REC</color>" : "<color=#95a5a6>idle</color>";
            var style = new GUIStyle(GUI.skin.label) { richText = true };

            GUI.Label(new Rect(x + 8, y + 4,  W - 16, 20),
                $"[{_toggleKey}] PerfRecorder  {state}", style);

            GUI.Label(new Rect(x + 8, y + 22, W - 16, 20),
                $"label : <b>{_sessionLabel}</b>  ({_labelCycleKey} 로 변경)", style);

            if (_recording)
            {
                float elapsed = Time.realtimeSinceStartup - _realtimeStart;
                int frames    = Time.frameCount - _frameStart;
                float fps     = frames / Mathf.Max(elapsed, 0.001f);
                GUI.Label(new Rect(x + 8, y + 40, W - 16, 20),
                    $"t={elapsed:0.0}s  frames={frames}  avgFps={fps:0.0}", style);
                GUI.Label(new Rect(x + 8, y + 58, W - 16, 20),
                    $"<color=#ffd166>다시 {_toggleKey} 를 누르면 저장 + 클립보드 복사</color>", style);
            }
            else
            {
                GUI.Label(new Rect(x + 8, y + 40, W - 16, 20),
                    $"시작하려면 {_toggleKey} 누르세요", style);
                GUI.Label(new Rect(x + 8, y + 58, W - 16, 20),
                    $"<color=#95a5a6>목표 30~60초 동안 실제 렉 상황 재현</color>", style);
            }

            // 저장 완료 배너 — 큰 글씨로 중앙 상단에.
            if (!string.IsNullOrEmpty(_bannerText) && Time.realtimeSinceStartup < _bannerUntil)
            {
                var bannerStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 20,
                    richText  = true,
                    wordWrap  = true,
                };
                float bw = 720, bh = 90;
                float bx = (Screen.width - bw) * 0.5f;
                float by = 40;
                GUI.Box(new Rect(bx, by, bw, bh),
                    $"<color=#51cf66><b>{_bannerText}</b></color>",
                    bannerStyle);
            }
        }
    }
}

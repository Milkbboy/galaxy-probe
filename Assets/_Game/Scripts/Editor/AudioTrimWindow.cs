using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// AudioClip을 원하는 구간만 잘라내고 페이드 적용 후 16-bit PCM WAV로 저장.
    /// 파형 뷰에서 Start/End/Fade 핸들을 드래그해 시각적으로 조절 가능.
    /// </summary>
    public class AudioTrimWindow : EditorWindow
    {
        // Trim 파라미터
        private AudioClip _source;
        private float _startTime = 0f;
        private float _endTime = 0.3f;
        private float _fadeIn = 0f;
        private float _fadeOut = 0.05f;
        private bool _forceMono = true;
        private string _suffix = "_Short";

        // View (Zoom)
        private float _viewStart = 0f;
        private float _viewEnd = 0f;

        // Preview 상태
        private double _previewStopAt;
        private bool _previewScheduled;

        // 파형 캐시
        private float[] _cachedMono;
        private AudioClip _cachedSource;
        private Texture2D _waveformTex;
        private int _cachedTexWidth;
        private float _cachedViewStart = -1f;
        private float _cachedViewEnd = -1f;

        // 드래그 상태
        private enum Handle { None, Start, End, FadeIn, FadeOut }
        private Handle _activeDrag = Handle.None;

        private const float WaveformHeight = 100f;
        private const float HandleHitPx = 8f;

        [MenuItem("Tools/Drill-Corp/Audio/Trim AudioClip")]
        public static void Open()
        {
            var w = GetWindow<AudioTrimWindow>("Trim AudioClip");
            w.minSize = new Vector2(420, 440);
        }

        private void OnDisable()
        {
            StopPreview();
            if (_waveformTex != null) { DestroyImmediate(_waveformTex); _waveformTex = null; }
        }

        // ────── OnGUI ──────

        private void OnGUI()
        {
            DrawSourceField();

            EditorGUILayout.Space(6);
            DrawWaveformSection();

            EditorGUILayout.Space(6);
            DrawTrimParams();

            EditorGUILayout.Space();
            DrawOutputParams();

            EditorGUILayout.Space();
            DrawPreviewControls();

            EditorGUILayout.Space();
            GUI.enabled = _source != null && _endTime > _startTime;
            if (GUILayout.Button("Trim & Save WAV", GUILayout.Height(28))) TrimAndSave();
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "• 파형에서 녹색(Start)/빨강(End) 세로선을 드래그해 구간 조절\n" +
                "• 주황(페이드) 세로선도 드래그 가능 — Start에 가까운 쪽 = FadeIn, End 쪽 = FadeOut\n" +
                "• Preview는 원본 구간만 들려줌 (fade/mono 반영은 Save 결과물로 확인)",
                MessageType.Info);
        }

        private void DrawSourceField()
        {
            var newSrc = (AudioClip)EditorGUILayout.ObjectField("Source", _source, typeof(AudioClip), false);
            if (newSrc != _source)
            {
                _source = newSrc;
                _cachedSource = null;
                _cachedMono = null;
                InvalidateWaveformTex();
                if (_source != null)
                {
                    _viewStart = 0f;
                    _viewEnd = _source.length;
                    if (_endTime > _source.length) _endTime = _source.length;
                }
            }
            if (_source != null)
            {
                string info = $"{_source.length:F3}s · {_source.frequency}Hz · {_source.channels}ch · {_source.samples} samples";
                EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
            }
        }

        private void DrawWaveformSection()
        {
            EnsureMonoCache();

            // 줌 컨트롤
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("View", GUILayout.Width(40));
                float vs = EditorGUILayout.FloatField(_viewStart, GUILayout.Width(60));
                GUILayout.Label("~", GUILayout.Width(12));
                float ve = EditorGUILayout.FloatField(_viewEnd, GUILayout.Width(60));
                GUILayout.Label("s", GUILayout.Width(12));

                if (_source != null)
                {
                    vs = Mathf.Clamp(vs, 0f, _source.length);
                    ve = Mathf.Clamp(ve, vs + 0.001f, _source.length);
                }
                if (!Mathf.Approximately(vs, _viewStart) || !Mathf.Approximately(ve, _viewEnd))
                {
                    _viewStart = vs;
                    _viewEnd = ve;
                    InvalidateWaveformTex();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Fit All", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    if (_source != null) { _viewStart = 0f; _viewEnd = _source.length; InvalidateWaveformTex(); }
                }
                if (GUILayout.Button("Fit Sel", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    if (_source != null && _endTime > _startTime)
                    {
                        float margin = (_endTime - _startTime) * 0.15f;
                        _viewStart = Mathf.Max(0f, _startTime - margin);
                        _viewEnd = Mathf.Min(_source.length, _endTime + margin);
                        InvalidateWaveformTex();
                    }
                }
            }

            // 파형 영역
            var rect = GUILayoutUtility.GetRect(100, WaveformHeight, GUILayout.ExpandWidth(true));
            DrawWaveform(rect);
        }

        private void DrawTrimParams()
        {
            EditorGUILayout.LabelField("Trim 구간 (초)", EditorStyles.boldLabel);
            float s = EditorGUILayout.FloatField("Start", _startTime);
            float e = EditorGUILayout.FloatField("End", _endTime);
            float fi = EditorGUILayout.FloatField("Fade In", _fadeIn);
            float fo = EditorGUILayout.FloatField("Fade Out", _fadeOut);
            if (_source != null)
            {
                s = Mathf.Clamp(s, 0f, _source.length);
                e = Mathf.Clamp(e, s, _source.length);
            }
            fi = Mathf.Max(0f, fi);
            fo = Mathf.Max(0f, fo);
            if (s != _startTime || e != _endTime || fi != _fadeIn || fo != _fadeOut)
            {
                _startTime = s; _endTime = e; _fadeIn = fi; _fadeOut = fo;
                Repaint();
            }

            if (_source != null && GUILayout.Button("Preset: 첫 0.3s (MachineGun 1-shot)"))
            {
                _startTime = 0f; _endTime = 0.3f; _fadeIn = 0f; _fadeOut = 0.05f; _forceMono = true;
                _viewStart = 0f; _viewEnd = Mathf.Min(_source.length, 0.6f);
                InvalidateWaveformTex();
            }
        }

        private void DrawOutputParams()
        {
            EditorGUILayout.LabelField("저장 설정", EditorStyles.boldLabel);
            _forceMono = EditorGUILayout.Toggle("Force Mono (2D SFX 추천)", _forceMono);
            _suffix = EditorGUILayout.TextField("파일 Suffix", _suffix);
        }

        private void DrawPreviewControls()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _source != null && _endTime > _startTime;
                if (GUILayout.Button("▶ Play", GUILayout.Height(24))) PlayPreview();
                GUI.enabled = true;
                if (GUILayout.Button("■ Stop", GUILayout.Height(24))) StopPreview();
            }
        }

        // ────── 파형 Draw + Mouse 상호작용 ──────

        private void DrawWaveform(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.14f, 0.14f, 0.14f));

            if (_cachedMono == null || _source == null)
            {
                var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                GUI.Label(rect, "(Source AudioClip 드래그)", style);
                return;
            }

            EnsureWaveformTex((int)rect.width);
            if (_waveformTex != null)
                GUI.DrawTexture(rect, _waveformTex, ScaleMode.StretchToFill);

            float viewRange = Mathf.Max(0.0001f, _viewEnd - _viewStart);
            float TimeToX(float t) => rect.x + (t - _viewStart) / viewRange * rect.width;

            float xStart = TimeToX(_startTime);
            float xEnd = TimeToX(_endTime);
            float xFadeInEnd = TimeToX(_startTime + _fadeIn);
            float xFadeOutStart = TimeToX(_endTime - _fadeOut);

            // 선택 영역 하이라이트
            if (xEnd > xStart)
            {
                var sel = new Rect(Mathf.Max(rect.x, xStart), rect.y,
                                   Mathf.Min(rect.xMax, xEnd) - Mathf.Max(rect.x, xStart), rect.height);
                EditorGUI.DrawRect(sel, new Color(0.3f, 0.6f, 1f, 0.12f));
            }
            // 페이드 영역 (주황)
            if (_fadeIn > 0f && xFadeInEnd > xStart)
            {
                var fr = new Rect(Mathf.Max(rect.x, xStart), rect.y,
                                  Mathf.Min(rect.xMax, xFadeInEnd) - Mathf.Max(rect.x, xStart), rect.height);
                EditorGUI.DrawRect(fr, new Color(1f, 0.55f, 0.15f, 0.18f));
            }
            if (_fadeOut > 0f && xFadeOutStart < xEnd)
            {
                var fr = new Rect(Mathf.Max(rect.x, xFadeOutStart), rect.y,
                                  Mathf.Min(rect.xMax, xEnd) - Mathf.Max(rect.x, xFadeOutStart), rect.height);
                EditorGUI.DrawRect(fr, new Color(1f, 0.55f, 0.15f, 0.18f));
            }

            // 핸들 (세로선 + 라벨 플래그)
            DrawHandleLine(rect, xStart, new Color(0.3f, 0.95f, 0.4f), "S");
            DrawHandleLine(rect, xEnd, new Color(1f, 0.35f, 0.35f), "E");
            if (_fadeIn > 0f) DrawHandleLine(rect, xFadeInEnd, new Color(1f, 0.65f, 0.2f), "FI");
            if (_fadeOut > 0f) DrawHandleLine(rect, xFadeOutStart, new Color(1f, 0.65f, 0.2f), "FO");

            HandleMouseEvents(rect);
        }

        private static void DrawHandleLine(Rect rect, float x, Color c, string label)
        {
            if (x < rect.x - 1 || x > rect.xMax + 1) return;
            EditorGUI.DrawRect(new Rect(x - 1, rect.y, 2, rect.height), c);
            var flag = new Rect(x - 10, rect.y, 20, 14);
            EditorGUI.DrawRect(flag, c);
            var style = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 9, fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.black;
            GUI.Label(flag, label, style);
        }

        private void HandleMouseEvents(Rect rect)
        {
            var e = Event.current;
            float viewRange = Mathf.Max(0.0001f, _viewEnd - _viewStart);
            float TimeToX(float t) => rect.x + (t - _viewStart) / viewRange * rect.width;
            float XToTime(float x) => _viewStart + (x - rect.x) / rect.width * viewRange;

            // 커서 피드백
            if (rect.Contains(e.mousePosition) || _activeDrag != Handle.None)
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                float mx = e.mousePosition.x;
                float dS = Mathf.Abs(mx - TimeToX(_startTime));
                float dE = Mathf.Abs(mx - TimeToX(_endTime));
                float dFI = _fadeIn > 0f ? Mathf.Abs(mx - TimeToX(_startTime + _fadeIn)) : float.MaxValue;
                float dFO = _fadeOut > 0f ? Mathf.Abs(mx - TimeToX(_endTime - _fadeOut)) : float.MaxValue;

                // 가장 가까운 핸들 선택 (HandleHitPx 이내)
                float best = HandleHitPx;
                Handle pick = Handle.None;
                if (dS < best) { best = dS; pick = Handle.Start; }
                if (dE < best) { best = dE; pick = Handle.End; }
                if (dFI < best) { best = dFI; pick = Handle.FadeIn; }
                if (dFO < best) { best = dFO; pick = Handle.FadeOut; }

                if (pick != Handle.None)
                {
                    _activeDrag = pick;
                    e.Use();
                    Repaint();
                }
            }
            else if (e.type == EventType.MouseDrag && _activeDrag != Handle.None)
            {
                float t = Mathf.Clamp(XToTime(e.mousePosition.x), 0f, _source.length);
                switch (_activeDrag)
                {
                    case Handle.Start:
                        _startTime = Mathf.Min(t, _endTime);
                        break;
                    case Handle.End:
                        _endTime = Mathf.Max(t, _startTime);
                        break;
                    case Handle.FadeIn:
                        _fadeIn = Mathf.Clamp(t - _startTime, 0f, _endTime - _startTime);
                        break;
                    case Handle.FadeOut:
                        _fadeOut = Mathf.Clamp(_endTime - t, 0f, _endTime - _startTime);
                        break;
                }
                e.Use();
                Repaint();
            }
            else if (e.rawType == EventType.MouseUp && _activeDrag != Handle.None)
            {
                _activeDrag = Handle.None;
                e.Use();
                Repaint();
            }
        }

        // ────── 파형 텍스처 ──────

        private void EnsureMonoCache()
        {
            if (_cachedSource == _source && _cachedMono != null) return;
            _cachedSource = _source;
            if (_source == null) { _cachedMono = null; return; }

            int ch = _source.channels;
            int n = _source.samples;
            var raw = new float[n * ch];
            if (!_source.GetData(raw, 0)) { _cachedMono = null; return; }

            _cachedMono = new float[n];
            for (int i = 0; i < n; i++)
            {
                float sum = 0f;
                for (int c = 0; c < ch; c++) sum += raw[i * ch + c];
                _cachedMono[i] = sum / ch;
            }
            InvalidateWaveformTex();
        }

        private void InvalidateWaveformTex()
        {
            _cachedViewStart = -1f;
            _cachedViewEnd = -1f;
        }

        private void EnsureWaveformTex(int width)
        {
            if (width <= 1 || _cachedMono == null || _source == null) return;
            if (_waveformTex != null && _cachedTexWidth == width
                && Mathf.Approximately(_cachedViewStart, _viewStart)
                && Mathf.Approximately(_cachedViewEnd, _viewEnd))
                return;

            _cachedTexWidth = width;
            _cachedViewStart = _viewStart;
            _cachedViewEnd = _viewEnd;

            const int height = (int)WaveformHeight;
            if (_waveformTex == null || _waveformTex.width != width || _waveformTex.height != height)
            {
                if (_waveformTex != null) DestroyImmediate(_waveformTex);
                _waveformTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                _waveformTex.hideFlags = HideFlags.HideAndDontSave;
                _waveformTex.filterMode = FilterMode.Point;
            }

            var bg = new Color(0.14f, 0.14f, 0.14f, 1f);
            var centerLine = new Color(0.25f, 0.25f, 0.25f, 1f);
            var wave = new Color(0.42f, 0.82f, 0.52f, 1f);

            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;
            int halfH = height / 2;
            for (int x = 0; x < width; x++) pixels[halfH * width + x] = centerLine;

            int sr = _source.frequency;
            int startIdx = Mathf.Clamp(Mathf.FloorToInt(_viewStart * sr), 0, _cachedMono.Length);
            int endIdx = Mathf.Clamp(Mathf.CeilToInt(_viewEnd * sr), startIdx + 1, _cachedMono.Length);
            float range = endIdx - startIdx;

            for (int x = 0; x < width; x++)
            {
                int s0 = startIdx + (int)(x / (float)width * range);
                int s1 = startIdx + (int)((x + 1) / (float)width * range);
                s1 = Mathf.Clamp(s1, s0 + 1, _cachedMono.Length);
                float mn = 0f, mx = 0f;
                for (int i = s0; i < s1; i++)
                {
                    float v = _cachedMono[i];
                    if (v < mn) mn = v;
                    if (v > mx) mx = v;
                }
                int y0 = halfH + (int)(mn * (halfH - 1));
                int y1 = halfH + (int)(mx * (halfH - 1));
                y0 = Mathf.Clamp(y0, 0, height - 1);
                y1 = Mathf.Clamp(y1, 0, height - 1);
                for (int y = y0; y <= y1; y++) pixels[y * width + x] = wave;
            }
            _waveformTex.SetPixels(pixels);
            _waveformTex.Apply();
        }

        // ────── Preview ──────

        private void PlayPreview()
        {
            StopPreview();
            if (_source == null) return;

            int startSample = Mathf.Clamp(Mathf.FloorToInt(_startTime * _source.frequency), 0, _source.samples);
            float duration = Mathf.Max(0.01f, _endTime - _startTime);

            AudioUtilPlayFromSample(_source, startSample);

            _previewStopAt = EditorApplication.timeSinceStartup + duration;
            if (!_previewScheduled)
            {
                _previewScheduled = true;
                EditorApplication.update += TickPreview;
            }
        }

        private void TickPreview()
        {
            if (EditorApplication.timeSinceStartup >= _previewStopAt) StopPreview();
        }

        private void StopPreview()
        {
            AudioUtilStop();
            if (_previewScheduled)
            {
                _previewScheduled = false;
                EditorApplication.update -= TickPreview;
            }
        }

        private static System.Type _cachedAudioUtilType;
        private static System.Type FindAudioUtilType()
        {
            if (_cachedAudioUtilType != null) return _cachedAudioUtilType;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("UnityEditor.AudioUtil");
                if (t != null) { _cachedAudioUtilType = t; return t; }
            }
            return null;
        }

        private static void AudioUtilPlayFromSample(AudioClip clip, int startSample)
        {
            var t = FindAudioUtilType();
            if (t == null) return;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var m = t.GetMethod("PlayPreviewClip", flags, null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            if (m != null) { m.Invoke(null, new object[] { clip, startSample, false }); return; }

            m = t.GetMethod("PlayClip", flags, null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            if (m != null) { m.Invoke(null, new object[] { clip, startSample, false }); return; }
            m = t.GetMethod("PlayClip", flags, null, new[] { typeof(AudioClip) }, null);
            if (m != null) { m.Invoke(null, new object[] { clip }); return; }
        }

        private static void AudioUtilStop()
        {
            var t = FindAudioUtilType();
            if (t == null) return;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var m = t.GetMethod("StopAllPreviewClips", flags) ?? t.GetMethod("StopAllClips", flags);
            m?.Invoke(null, null);
        }

        // ────── Save ──────

        private void TrimAndSave()
        {
            string srcPath = AssetDatabase.GetAssetPath(_source);
            if (string.IsNullOrEmpty(srcPath))
            {
                EditorUtility.DisplayDialog("Error", "Source AudioClip의 에셋 경로를 찾을 수 없습니다.", "OK");
                return;
            }
            if (!BuildTrimmedData(out var outData, out int outCh, out int sampleRate)) return;

            string dir = Path.GetDirectoryName(srcPath).Replace("\\", "/");
            string name = Path.GetFileNameWithoutExtension(srcPath);
            string outPath = $"{dir}/{name}{_suffix}.wav";

            if (File.Exists(outPath))
            {
                if (!EditorUtility.DisplayDialog("Overwrite?",
                    $"{outPath}\n이미 존재합니다. 덮어쓸까요?", "Overwrite", "Cancel"))
                    return;
            }

            WriteWav(outPath, outData, outCh, sampleRate);
            AssetDatabase.ImportAsset(outPath);
            AssetDatabase.Refresh();

            var newClip = AssetDatabase.LoadAssetAtPath<AudioClip>(outPath);
            if (newClip != null)
            {
                Selection.activeObject = newClip;
                EditorGUIUtility.PingObject(newClip);
            }

            int outSamples = outData.Length / outCh;
            Debug.Log($"[AudioTrim] Saved {outPath} — {outSamples} samples ({(float)outSamples / sampleRate:F3}s, {outCh}ch)");
        }

        private bool BuildTrimmedData(out float[] outData, out int outChannels, out int sampleRate)
        {
            outData = null; outChannels = 0; sampleRate = 0;
            if (_source == null) return false;

            int srcChannels = _source.channels;
            int srcFreq = _source.frequency;
            int srcSamples = _source.samples;

            var srcData = new float[srcSamples * srcChannels];
            if (!_source.GetData(srcData, 0))
            {
                EditorUtility.DisplayDialog("Error", "AudioClip.GetData 호출 실패.", "OK");
                return false;
            }

            int startSample = Mathf.Clamp(Mathf.FloorToInt(_startTime * srcFreq), 0, srcSamples);
            int endSample = Mathf.Clamp(Mathf.CeilToInt(_endTime * srcFreq), startSample, srcSamples);
            int outSamples = endSample - startSample;
            if (outSamples <= 0)
            {
                EditorUtility.DisplayDialog("Error", "Start/End 범위가 유효하지 않습니다.", "OK");
                return false;
            }

            outChannels = (_forceMono && srcChannels > 1) ? 1 : srcChannels;
            outData = new float[outSamples * outChannels];
            for (int i = 0; i < outSamples; i++)
            {
                int srcIdx = (startSample + i) * srcChannels;
                if (_forceMono && srcChannels > 1)
                {
                    float sum = 0f;
                    for (int c = 0; c < srcChannels; c++) sum += srcData[srcIdx + c];
                    outData[i] = sum / srcChannels;
                }
                else
                {
                    for (int c = 0; c < outChannels; c++)
                        outData[i * outChannels + c] = srcData[srcIdx + c];
                }
            }

            int fadeInN = Mathf.Clamp(Mathf.FloorToInt(_fadeIn * srcFreq), 0, outSamples);
            for (int i = 0; i < fadeInN; i++)
            {
                float g = (float)i / fadeInN;
                for (int c = 0; c < outChannels; c++) outData[i * outChannels + c] *= g;
            }
            int fadeOutN = Mathf.Clamp(Mathf.FloorToInt(_fadeOut * srcFreq), 0, outSamples);
            for (int i = 0; i < fadeOutN; i++)
            {
                float g = (float)(fadeOutN - i) / fadeOutN;
                int idx = (outSamples - fadeOutN) + i;
                for (int c = 0; c < outChannels; c++) outData[idx * outChannels + c] *= g;
            }

            sampleRate = srcFreq;
            return true;
        }

        private static void WriteWav(string path, float[] samples, int channels, int sampleRate)
        {
            const int bitsPerSample = 16;
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            int blockAlign = channels * bitsPerSample / 8;
            int dataSize = samples.Length * 2;
            int fileSize = 36 + dataSize;

            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(fileSize);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)channels);
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)blockAlign);
                bw.Write((short)bitsPerSample);
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);
                for (int i = 0; i < samples.Length; i++)
                {
                    short v = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                    bw.Write(v);
                }
            }
        }
    }
}

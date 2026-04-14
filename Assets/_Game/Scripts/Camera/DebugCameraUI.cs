using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DrillCorp.CameraSystem
{
    public class DebugCameraUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DynamicCamera _dynamicCamera;

        [Header("Settings")]
        [Tooltip("디버그 UI 토글 키")]
        [SerializeField] private Key _toggleKey = Key.F1;

        [Tooltip("시작 시 UI 표시 여부")]
        [SerializeField] private bool _showOnStart = false;

        private bool _isVisible;
        private Rect _windowRect = new Rect(0f, 0f, 340f, 300f);
        private bool _windowInitialized;

        private float _orthographicSize;
        private float _mouseWeight;
        private float _maxOffset;
        private float _smoothSpeed;

        private void Start()
        {
            _isVisible = _showOnStart;

            if (_dynamicCamera == null)
                _dynamicCamera = FindAnyObjectByType<DynamicCamera>();

            LoadFromSettings();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[_toggleKey].wasPressedThisFrame)
            {
                _isVisible = !_isVisible;
                if (_isVisible)
                    LoadFromSettings();
            }
        }

        private void LoadFromSettings()
        {
            if (_dynamicCamera == null || _dynamicCamera.Settings == null)
                return;

            var s = _dynamicCamera.Settings;
            _orthographicSize = s.OrthographicSize;
            _mouseWeight = s.MouseWeight;
            _maxOffset = s.MaxOffset;
            _smoothSpeed = s.SmoothSpeed;
        }

        private void OnGUI()
        {
            if (!_isVisible || _dynamicCamera == null || _dynamicCamera.Settings == null)
                return;

            if (!_windowInitialized)
            {
                _windowRect.x = Screen.width - _windowRect.width - 20f;
                _windowRect.y = 170f;
                _windowInitialized = true;
            }

            _windowRect = GUI.Window(12345, _windowRect, DrawWindow, $"Camera Debug ({_toggleKey} to toggle)");
        }

        private void DrawWindow(int id)
        {
            var s = _dynamicCamera.Settings;

            GUILayout.Space(5f);

            DrawSlider("Orthographic Size", ref _orthographicSize, 3f, 20f);
            s.SetOrthographicSize(_orthographicSize);

            DrawSlider("Mouse Weight", ref _mouseWeight, 0f, 1f);
            s.SetMouseWeight(_mouseWeight);

            DrawSlider("Max Offset", ref _maxOffset, 1f, 20f);
            s.SetMaxOffset(_maxOffset);

            DrawSlider("Smooth Speed", ref _smoothSpeed, 1f, 20f);
            s.SetSmoothSpeed(_smoothSpeed);

            GUILayout.Space(10f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset"))
            {
                LoadFromSettings();
            }
            if (GUILayout.Button("Save to Asset"))
            {
                SaveToAsset();
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DrawSlider(string label, ref float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(140f));
            GUILayout.Label(value.ToString("F2"), GUILayout.Width(40f));
            GUILayout.EndHorizontal();
            value = GUILayout.HorizontalSlider(value, min, max);
        }

        private void SaveToAsset()
        {
#if UNITY_EDITOR
            if (_dynamicCamera.Settings == null)
                return;

            EditorUtility.SetDirty(_dynamicCamera.Settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[DebugCameraUI] Camera settings saved.");
#else
            Debug.Log("[DebugCameraUI] 빌드 환경에서는 에셋 저장 불가. 런타임 값만 유지됩니다.");
#endif
        }
    }
}

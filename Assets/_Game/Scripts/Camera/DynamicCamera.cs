using UnityEngine;
using UnityEngine.InputSystem;

namespace DrillCorp.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public class DynamicCamera : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("카메라 설정 SO (런타임에도 값 변경 가능)")]
        [SerializeField] private CameraSettingsData _settings;

        [Tooltip("머신 Transform (비어있으면 자동으로 찾음)")]
        [SerializeField] private Transform _machine;

        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _offsetRangeColor = new Color(1f, 0.5f, 0f, 0.3f);
        [SerializeField] private Color _currentTargetColor = new Color(0f, 1f, 1f, 0.8f);

        private Camera _camera;
        private float _cameraHeight;
        private Vector3 _lastTargetPos;

        public CameraSettingsData Settings => _settings;

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (!_camera.orthographic)
            {
                Debug.LogWarning("[DynamicCamera] 카메라가 Orthographic이 아닙니다. 자동 전환합니다.");
                _camera.orthographic = true;
            }

            if (_machine == null)
            {
                var machineObj = GameObject.FindGameObjectWithTag("Machine");
                if (machineObj != null)
                    _machine = machineObj.transform;
            }

            _cameraHeight = transform.position.y;

            if (_settings != null)
                _camera.orthographicSize = _settings.OrthographicSize;
        }

        private void LateUpdate()
        {
            if (_settings == null || _machine == null)
                return;

            _camera.orthographicSize = _settings.OrthographicSize;

            Vector3 targetPos = CalculateTargetPosition();
            _lastTargetPos = targetPos;

            transform.position = Vector3.Lerp(
                transform.position,
                targetPos,
                _settings.SmoothSpeed * Time.deltaTime
            );
        }

        private Vector3 CalculateTargetPosition()
        {
            Vector3 machinePos = _machine.position;
            Vector3 mouseWorld = GetMouseWorldPosition();

            if (mouseWorld == Vector3.zero)
                return new Vector3(machinePos.x, _cameraHeight, machinePos.z);

            Vector3 blended = Vector3.Lerp(machinePos, mouseWorld, _settings.MouseWeight);

            Vector3 offset = blended - machinePos;
            offset.y = 0f;

            if (offset.magnitude > _settings.MaxOffset)
                offset = offset.normalized * _settings.MaxOffset;

            return new Vector3(
                machinePos.x + offset.x,
                _cameraHeight,
                machinePos.z + offset.z
            );
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (Mouse.current == null)
                return Vector3.zero;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(screenPos);

            Plane groundPlane = new Plane(Vector3.up, _machine.position);
            if (groundPlane.Raycast(ray, out float enter))
                return ray.GetPoint(enter);

            return Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos || _settings == null || _machine == null)
                return;

            Vector3 center = _machine.position;

            Gizmos.color = _offsetRangeColor;
            DrawCircle(center, _settings.MaxOffset);

            if (Application.isPlaying)
            {
                Gizmos.color = _currentTargetColor;
                Gizmos.DrawWireSphere(new Vector3(_lastTargetPos.x, center.y, _lastTargetPos.z), 0.5f);
            }
        }

        private void DrawCircle(Vector3 center, float radius)
        {
            const int segments = 64;
            Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}

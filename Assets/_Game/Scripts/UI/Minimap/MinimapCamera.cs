using UnityEngine;
using DrillCorp.Machine;

namespace DrillCorp.UI.Minimap
{
    /// <summary>
    /// 머신 상공에서 아래(-Y)를 내려다보는 Orthographic 카메라.
    /// RenderTexture에 그려 MinimapUI의 RawImage로 표시.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MinimapCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _height = 50f;
        [SerializeField] private float _orthographicSize = 20f;
        [SerializeField] private RenderTexture _renderTexture;
        [SerializeField] private LayerMask _cullingMask;

        private Camera _camera;

        public RenderTexture RenderTexture => _renderTexture;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = _orthographicSize;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.8f);
            _camera.cullingMask = _cullingMask;
            _camera.targetTexture = _renderTexture;
            _camera.depth = -10;

            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            if (_target == null)
            {
                var machine = FindAnyObjectByType<MachineController>();
                if (machine != null) _target = machine.transform;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            transform.position = new Vector3(_target.position.x, _target.position.y + _height, _target.position.z);
        }
    }
}

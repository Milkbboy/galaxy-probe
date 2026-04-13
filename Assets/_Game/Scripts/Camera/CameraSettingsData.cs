using UnityEngine;

namespace DrillCorp.CameraSystem
{
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Drill-Corp/Camera Settings", order = 5)]
    public class CameraSettingsData : ScriptableObject
    {
        [Header("Orthographic")]
        [Tooltip("카메라 Orthographic Size (고정)")]
        [Range(3f, 20f)]
        [SerializeField] private float _orthographicSize = 8f;

        [Header("Nuclear Throne Offset")]
        [Tooltip("마우스 쪽으로 얼마나 따라갈지 (0=머신 고정, 0.5=정중간, 1=마우스 따라감)")]
        [Range(0f, 1f)]
        [SerializeField] private float _mouseWeight = 0.3f;

        [Tooltip("머신에서 카메라가 떨어질 수 있는 최대 거리")]
        [Range(1f, 20f)]
        [SerializeField] private float _maxOffset = 8f;

        [Header("Smoothness")]
        [Tooltip("카메라 추적 속도 (클수록 빠르게 따라감)")]
        [Range(1f, 20f)]
        [SerializeField] private float _smoothSpeed = 5f;

        public float OrthographicSize => _orthographicSize;
        public float MouseWeight => _mouseWeight;
        public float MaxOffset => _maxOffset;
        public float SmoothSpeed => _smoothSpeed;

        public void SetOrthographicSize(float value) => _orthographicSize = value;
        public void SetMouseWeight(float value) => _mouseWeight = value;
        public void SetMaxOffset(float value) => _maxOffset = value;
        public void SetSmoothSpeed(float value) => _smoothSpeed = value;
    }
}

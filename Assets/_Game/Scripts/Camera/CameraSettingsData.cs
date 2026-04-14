using UnityEngine;

namespace DrillCorp.CameraSystem
{
    /// <summary>
    /// 동적 카메라 설정 (Nuclear Throne 방식)
    /// - 줌 변경 없이 카메라 위치만 머신과 마우스 사이로 블렌드 이동
    /// - 조준 정확도 유지 + 마우스 쪽 시야 확보
    /// </summary>
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "Drill-Corp/Camera Settings", order = 5)]
    public class CameraSettingsData : ScriptableObject
    {
        [Header("Orthographic")]
        [Tooltip(
            "카메라 Orthographic Size (화면에 보이는 높이의 절반)\n" +
            "• 작을수록 확대 (머신 크게 보임)\n" +
            "• 클수록 축소 (맵 넓게 보임)\n" +
            "• 권장: 8 (1920x1080 기준 스폰반경 10f가 화면에 들어옴)"
        )]
        [Range(3f, 20f)]
        [SerializeField] private float _orthographicSize = 8f;

        [Header("Nuclear Throne Offset")]
        [Tooltip(
            "마우스 쪽으로 카메라가 따라가는 비율\n" +
            "• 0.0: 카메라 머신 고정 (마우스 무시)\n" +
            "• 0.3: 머신 쪽에 가깝게 (추천 - 머신 시야 확보)\n" +
            "• 0.5: 정중간 (머신과 마우스 사이)\n" +
            "• 1.0: 마우스 추적 (머신 화면 끝으로 밀림)\n" +
            "값이 높을수록 멀리 보이지만 머신이 중앙에서 벗어남"
        )]
        [Range(0f, 1f)]
        [SerializeField] private float _mouseWeight = 0.3f;

        [Tooltip(
            "머신에서 카메라가 떨어질 수 있는 최대 거리 (안전장치)\n" +
            "• 마우스가 너무 멀어도 카메라는 이 거리까지만 이동\n" +
            "• MouseWeight가 높고 마우스가 멀 때 머신이 화면 밖으로 나가는 것 방지\n" +
            "• 권장: 8 (OrthographicSize와 비슷한 값)"
        )]
        [Range(1f, 20f)]
        [SerializeField] private float _maxOffset = 8f;

        [Header("Smoothness")]
        [Tooltip(
            "카메라 추적 속도 (Lerp 계수)\n" +
            "• 1~2: 매우 부드러움 (둔한 반응, 영화 같은 느낌)\n" +
            "• 3~5: 부드러움 (추천 - 자연스러운 추적)\n" +
            "• 8~12: 빠른 반응 (조준 중시)\n" +
            "• 15+: 즉각 반응 (거의 스냅)\n" +
            "높을수록 마우스 흔들림이 그대로 반영되어 어지러울 수 있음"
        )]
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

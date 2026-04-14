using UnityEngine;

namespace DrillCorp.Weapon.LockOn
{
    /// <summary>
    /// 타겟 Bug 위에 따라다니는 락온 마커
    /// 일반 모드: 느린 펄스
    /// Imminent 모드: 빠른 빨간 깜빡임 (발사 직전)
    /// </summary>
    public class LockOnMarker : MonoBehaviour
    {
        [Header("Position")]
        [Tooltip(
            "타겟 위쪽 여백 (월드 단위)\n" +
            "• 타겟의 실제 높이(Renderer/Collider Bounds 상단)에 이 값만큼 더 위로 배치\n" +
            "• 0.2~0.5 권장"
        )]
        [SerializeField] private float _yPadding = 0.3f;

        [Tooltip("타겟 높이 자동 계산 (Renderer/Collider Bounds 사용)")]
        [SerializeField] private bool _autoHeight = true;

        [Tooltip("자동 계산 실패 시 사용할 기본 Y 오프셋")]
        [SerializeField] private float _fallbackYOffset = 0.5f;

        [Header("Auto Scale")]
        [Tooltip("타겟 크기에 맞춰 마커 스케일 자동 조정")]
        [SerializeField] private bool _autoScale = true;

        [Tooltip(
            "마커 대 타겟 크기 비율 (타겟 XZ 평균 크기 기준)\n" +
            "• 0.8: 타겟보다 살짝 작게 (추천 - 타겟 위에 얹은 느낌)\n" +
            "• 1.0: 타겟 크기와 동일\n" +
            "• 1.2~1.5: 타겟을 감싸는 느낌"
        )]
        [Range(0.3f, 3f)]
        [SerializeField] private float _scaleToTargetRatio = 0.9f;

        [Tooltip("자동 스케일 최소 월드 크기 (이보다 작아지지 않음)")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _minWorldSize = 0.2f;

        [Tooltip("자동 스케일 최대 월드 크기 (이보다 커지지 않음)")]
        [Range(0.3f, 5f)]
        [SerializeField] private float _maxWorldSize = 1.5f;

        [Header("Normal Pulse")]
        [SerializeField] private bool _pulse = true;
        [SerializeField] private float _pulseSpeed = 4f;
        [SerializeField] private float _pulseScaleMin = 0.8f;
        [SerializeField] private float _pulseScaleMax = 1.2f;

        [Header("Imminent (발사 직전)")]
        [Tooltip("Imminent 모드 깜빡임 속도 (클수록 빠름)")]
        [SerializeField] private float _imminentBlinkSpeed = 20f;

        [Tooltip("Imminent 모드 최소 알파 (깜빡임 어두운 쪽)")]
        [Range(0f, 1f)]
        [SerializeField] private float _imminentAlphaMin = 0.2f;

        [Tooltip("Imminent 모드 스케일 배율 (전체적으로 커져 보임)")]
        [Range(1f, 2f)]
        [SerializeField] private float _imminentScaleMultiplier = 1.3f;

        [Tooltip("Imminent 모드 색상")]
        [SerializeField] private Color _imminentColor = new Color(1f, 0f, 0f, 1f);

        private Transform _target;
        private Vector3 _baseScale;
        private SpriteRenderer _renderer;
        private Color _normalColor;
        private bool _isImminent;
        private float _targetHeight;
        private float _autoScaleMultiplier = 1f;
        private bool _heightCached;

        public Transform Target => _target;
        public bool IsImminent => _isImminent;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null)
                _normalColor = _renderer.color;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            _heightCached = false;
            CacheTargetHeight();
        }

        private void CacheTargetHeight()
        {
            if (_target == null) return;

            Bounds? bounds = CalculateTargetBounds();

            if (bounds.HasValue)
            {
                var b = bounds.Value;
                _targetHeight = _autoHeight ? (b.max.y - _target.position.y) : _fallbackYOffset;

                if (_autoScale)
                    _autoScaleMultiplier = CalcAutoScaleMultiplier(b);
                else
                    _autoScaleMultiplier = 1f;
            }
            else
            {
                _targetHeight = _fallbackYOffset;
                _autoScaleMultiplier = 1f;
            }

            _heightCached = true;
        }

        private Bounds? CalculateTargetBounds()
        {
            var renderers = _target.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                return b;
            }

            var col = _target.GetComponentInChildren<Collider>();
            if (col != null) return col.bounds;

            return null;
        }

        private float CalcAutoScaleMultiplier(Bounds bounds)
        {
            // XZ 평면 평균 크기 (긴 축 치우침 완화)
            float xzAvg = (bounds.size.x + bounds.size.z) * 0.5f;
            float desiredWorldSize = xzAvg * _scaleToTargetRatio;

            // 월드 크기 제한
            desiredWorldSize = Mathf.Clamp(desiredWorldSize, _minWorldSize, _maxWorldSize);

            // 프리펩 원본 Scale 기준 배율 계산
            float baseRef = Mathf.Max(0.0001f, _baseScale.x);
            return desiredWorldSize / baseRef;
        }

        public void SetImminent(bool value)
        {
            _isImminent = value;
            if (!value && _renderer != null)
                _renderer.color = _normalColor;
        }

        private void LateUpdate()
        {
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            if (!_heightCached) CacheTargetHeight();

            transform.position = _target.position + Vector3.up * (_targetHeight + _yPadding);
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            if (_isImminent)
                UpdateImminent();
            else if (_pulse)
                UpdatePulse();
        }

        private void UpdatePulse()
        {
            float t = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(_pulseScaleMin, _pulseScaleMax, t);
            transform.localScale = _baseScale * _autoScaleMultiplier * scale;
        }

        private void UpdateImminent()
        {
            float blink = (Mathf.Sin(Time.time * _imminentBlinkSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(_imminentAlphaMin, 1f, blink);

            if (_renderer != null)
            {
                var c = _imminentColor;
                c.a *= alpha;
                _renderer.color = c;
            }

            float scalePulse = 1f + Mathf.Sin(Time.time * _imminentBlinkSpeed * 0.5f) * 0.1f;
            transform.localScale = _baseScale * _autoScaleMultiplier * _imminentScaleMultiplier * scalePulse;
        }
    }
}

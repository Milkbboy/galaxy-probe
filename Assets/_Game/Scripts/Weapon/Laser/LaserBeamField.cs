using UnityEngine;

namespace DrillCorp.Weapon.Laser
{
    /// <summary>
    /// 레이저 바닥 필드 - 에임 위치를 Lerp로 따라다니며 원형 이펙트 표시
    /// 탑뷰 전용 (XZ 평면, 회전 90,0,0 기준)
    /// 이동 방향으로 스트레치되어 "쫓아가는" 느낌 표현
    /// </summary>
    public class LaserBeamField : MonoBehaviour
    {
        [Header("Rotation")]
        [Tooltip("초당 자전 각도 (Y축)")]
        [SerializeField] private float _rotationSpeed = 90f;

        [Header("Pulse")]
        [SerializeField] private bool _pulse = true;
        [SerializeField] private float _pulseSpeed = 4f;
        [Range(0.8f, 1.2f)]
        [SerializeField] private float _pulseScaleMin = 0.95f;
        [Range(0.8f, 1.2f)]
        [SerializeField] private float _pulseScaleMax = 1.05f;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new Color(1f, 0.3f, 0.1f, 0.7f);
        [SerializeField] private Color _overheatColor = new Color(1f, 0f, 0f, 0.9f);

        [Header("Overheat Blink")]
        [SerializeField] private float _overheatBlinkSpeed = 10f;
        [Range(0f, 1f)]
        [SerializeField] private float _overheatAlphaMin = 0.2f;

        private SpriteRenderer _renderer;
        private float _baseDiameter = 1f;
        private float _spin;
        private bool _overheatVisual;

        private Vector3 _targetPos;
        private Vector3 _lastPos;
        private Vector3 _smoothedVelocity;
        private float _followSpeed = 6f;
        private float _stretchAmount = 0.25f;
        private bool _hasTarget;

        private void Awake()
        {
            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _renderer.color = _normalColor;
        }

        public void SetWorldRadius(float radius)
        {
            _baseDiameter = radius * 2f;
        }

        public void SetFollowParams(float followSpeed, float stretchAmount)
        {
            _followSpeed = followSpeed;
            _stretchAmount = stretchAmount;
        }

        public void SetOverheatVisual(bool value)
        {
            _overheatVisual = value;
            if (_renderer != null && !value)
                _renderer.color = _normalColor;
        }

        /// <summary>
        /// 에임 타겟 위치 갱신 (매 프레임 호출)
        /// </summary>
        public void SetTargetPosition(Vector3 aimPosition)
        {
            _targetPos = aimPosition + Vector3.up * 0.02f;
            if (!_hasTarget)
            {
                transform.position = _targetPos;
                _lastPos = _targetPos;
                _smoothedVelocity = Vector3.zero;
                _hasTarget = true;
            }
        }

        /// <summary>
        /// 필드 재시작 - 다음 SetTargetPosition 호출 시 그 위치에 스냅
        /// </summary>
        public void ResetFollow()
        {
            _hasTarget = false;
            _smoothedVelocity = Vector3.zero;
        }

        private void OnEnable()
        {
            ResetFollow();
        }

        private void LateUpdate()
        {
            if (!_hasTarget) return;

            // Lerp 추적
            Vector3 prev = transform.position;
            if (_followSpeed <= 0f)
            {
                transform.position = _targetPos;
            }
            else
            {
                float t = 1f - Mathf.Exp(-_followSpeed * Time.deltaTime);
                transform.position = Vector3.Lerp(prev, _targetPos, t);
            }

            // 이동 방향/속도 (XZ 평면)
            Vector3 delta = transform.position - _lastPos;
            delta.y = 0f;
            Vector3 velocity = Time.deltaTime > 0f ? delta / Time.deltaTime : Vector3.zero;
            _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, velocity, Mathf.Clamp01(10f * Time.deltaTime));
            _lastPos = transform.position;

            _spin += _rotationSpeed * Time.deltaTime;

            // Pulse
            float pulse = 1f;
            if (_pulse)
            {
                float p = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
                pulse = Mathf.Lerp(_pulseScaleMin, _pulseScaleMax, p);
            }

            // Stretch (이동 방향으로 늘어짐)
            float speed = _smoothedVelocity.magnitude;
            float stretchK = Mathf.Clamp01(speed / 10f) * _stretchAmount;
            float stretchX = 1f + stretchK;
            float stretchZ = 1f - stretchK * 0.5f;

            // 이동 방향 기준 회전 (스트레치 축 맞추기)
            float yawDeg = _spin;
            if (_stretchAmount > 0f && speed > 0.05f)
            {
                float moveYaw = Mathf.Atan2(_smoothedVelocity.x, _smoothedVelocity.z) * Mathf.Rad2Deg;
                yawDeg = moveYaw;
            }

            transform.rotation = Quaternion.Euler(90f, yawDeg, 0f);

            if (_renderer != null && _overheatVisual)
            {
                float blink = (Mathf.Sin(Time.time * _overheatBlinkSpeed) + 1f) * 0.5f;
                var c = _overheatColor;
                c.a *= Mathf.Lerp(_overheatAlphaMin, 1f, blink);
                _renderer.color = c;
            }

            // 탑뷰 XZ 평면에 눕힌 상태(X=90°)에서 sprite의 local X,Y가 월드 X,Z에 해당
            float finalX = _baseDiameter * pulse * stretchX;
            float finalY = _baseDiameter * pulse * stretchZ;
            transform.localScale = new Vector3(finalX, finalY, _baseDiameter * pulse);
        }
    }
}

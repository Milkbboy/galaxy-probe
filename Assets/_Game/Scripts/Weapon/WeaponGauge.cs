using UnityEngine;

namespace DrillCorp.Weapon
{
    /// <summary>
    /// 무기 게이지 바 (쿨다운/Heat 등 공통 표시)
    /// 탑뷰 월드 공간, XZ 평면에 눕힌 상태 (회전 90,0,0)
    /// 배경 + 채움 2개 SpriteRenderer 구조. 채움은 왼쪽 기준 scale.x로 늘어남
    /// </summary>
    public class WeaponGauge : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("배경 SpriteRenderer (고정 크기)")]
        [SerializeField] private SpriteRenderer _background;

        [Tooltip("채움 SpriteRenderer (비율에 따라 scale.x 변경)")]
        [SerializeField] private SpriteRenderer _fill;

        [Tooltip("채움이 왼쪽 기준으로 늘어나도록 하는 피벗 Transform")]
        [SerializeField] private Transform _fillPivot;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new Color(0.3f, 1f, 0.4f, 0.9f);
        [SerializeField] private Color _warningColor = new Color(1f, 0.8f, 0.2f, 0.95f);
        [SerializeField] private Color _lockedColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.5f);

        [Header("Blink (Locked)")]
        [Tooltip("Locked 상태일 때 깜빡임 속도")]
        [SerializeField] private float _lockedBlinkSpeed = 10f;
        [Range(0f, 1f)]
        [SerializeField] private float _lockedBlinkAlphaMin = 0.2f;

        [Header("Size")]
        [Tooltip("채움 바 원본 너비 (가로 바일 때 로컬 스케일 X의 100% 기준)")]
        [SerializeField] private float _baseFillWidth = 1f;

        [Tooltip("채움 바 원본 높이 (세로 바일 때 로컬 스케일 Y의 100% 기준)")]
        [SerializeField] private float _baseFillHeight = 1f;

        [Tooltip("세로 바 모드 (true: Y축으로 아래→위 채움, false: X축으로 왼→오 채움)")]
        [SerializeField] private bool _vertical = false;

        private float _ratio;
        private WeaponBase.WeaponGaugeState _state;

        public void SetValue(float ratio, WeaponBase.WeaponGaugeState state)
        {
            _ratio = Mathf.Clamp01(ratio);
            _state = state;
            Apply();
        }

        private void OnEnable()
        {
            if (_background != null) _background.color = _backgroundColor;
            Apply();
        }

        private void Apply()
        {
            if (_fillPivot != null)
            {
                var s = _fillPivot.localScale;
                if (_vertical)
                    s.y = _baseFillHeight * _ratio;
                else
                    s.x = _baseFillWidth * _ratio;
                _fillPivot.localScale = s;
            }

            if (_fill != null)
            {
                Color c = GetStateColor();
                if (_state == WeaponBase.WeaponGaugeState.Locked)
                {
                    float blink = (Mathf.Sin(Time.time * _lockedBlinkSpeed) + 1f) * 0.5f;
                    c.a *= Mathf.Lerp(_lockedBlinkAlphaMin, 1f, blink);
                }
                else if (_state == WeaponBase.WeaponGaugeState.Warning)
                {
                    float blink = (Mathf.Sin(Time.time * _lockedBlinkSpeed * 0.6f) + 1f) * 0.5f;
                    c.a *= Mathf.Lerp(0.5f, 1f, blink);
                }
                _fill.color = c;
            }
        }

        private void LateUpdate()
        {
            if (_state == WeaponBase.WeaponGaugeState.Locked ||
                _state == WeaponBase.WeaponGaugeState.Warning)
            {
                Apply();
            }
        }

        private Color GetStateColor()
        {
            switch (_state)
            {
                case WeaponBase.WeaponGaugeState.Warning: return _warningColor;
                case WeaponBase.WeaponGaugeState.Locked: return _lockedColor;
                default: return _normalColor;
            }
        }
    }
}

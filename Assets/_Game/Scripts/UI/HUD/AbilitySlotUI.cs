using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Ability;
using DrillCorp.Data;

namespace DrillCorp.UI.HUD
{
    /// <summary>
    /// 어빌리티 HUD 슬롯 1개. AbilityHud 가 슬롯당 1개 인스턴스 보유.
    /// IAbilityRunner 의 CooldownNormalized 를 매 프레임 읽어 진행바 + 상태 텍스트 갱신.
    ///
    /// v2 drawItemUI 매핑:
    ///   - 박스 배경       rgba(10,10,30,0.75)         → _background.color
    ///   - 박스 테두리     itemColor @ alpha 0.53      → _border.color
    ///   - 상단 라벨       "[KEY] 이름" / itemColor    → _nameLabel
    ///   - 쿨다운 바       1 - cd/cdMax (0→1 차오름)   → _cooldownBarFill.fillAmount
    ///   - 준비완료 색     #51cf66                     → ReadyGreen
    ///   - 상태 텍스트     "사용가능" / "Ns"           → _statusLabel
    /// </summary>
    public class AbilitySlotUI : MonoBehaviour
    {
        [Header("Background / Border")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _border;

        [Header("Icon")]
        [SerializeField] private Image _iconImage;

        [Header("Cooldown Bar")]
        [SerializeField] private Image _cooldownBarBg;
        [SerializeField] private Image _cooldownBarFill;   // Image.Type = Filled / Horizontal / Left

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _statusLabel;

        // v2 #51cf66 (준비 완료 시 진행바 색)
        private static readonly Color ReadyGreen = new Color(0x51 / 255f, 0xcf / 255f, 0x66 / 255f, 1f);

        private IAbilityRunner _runner;
        private AbilityData _data;
        private Color _themeColor = Color.white;

        public void Bind(IAbilityRunner runner, AbilityData data)
        {
            _runner = runner;
            _data = data;

            // 둘 중 하나라도 없으면 슬롯 자체를 숨김 (해금 안 된 슬롯)
            gameObject.SetActive(runner != null && data != null);
            if (data == null) return;

            _themeColor = data.ThemeColor;

            if (_border != null)
                _border.color = WithAlpha(_themeColor, 0.53f);

            if (_iconImage != null)
            {
                _iconImage.sprite = data.Icon;
                _iconImage.enabled = data.Icon != null;
                _iconImage.preserveAspect = true;
            }

            if (_nameLabel != null)
            {
                string key = data.Trigger == AbilityTrigger.AutoInterval
                    ? "자동"
                    : data.SlotKey.ToString();
                _nameLabel.text = $"[{key}] {data.DisplayName}";
                _nameLabel.color = _themeColor;
            }

            // 초기 상태 1회 그려두기
            Refresh();
        }

        public void Refresh()
        {
            if (_runner == null || _data == null) return;

            float norm = _runner.CooldownNormalized; // 1=방금 사용, 0=준비
            float pct  = 1f - norm;                  // 진행바: 0→1 차오름

            if (_cooldownBarFill != null)
            {
                _cooldownBarFill.fillAmount = pct;
                _cooldownBarFill.color = (norm <= 0f) ? ReadyGreen : _themeColor;
            }

            if (_statusLabel != null)
            {
                _statusLabel.text = (norm <= 0f)
                    ? "사용가능"
                    : $"{Mathf.CeilToInt(norm * _data.CooldownSec)}s";
            }
        }

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}

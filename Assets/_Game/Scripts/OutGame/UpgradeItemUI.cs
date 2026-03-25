using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Data;

namespace DrillCorp.OutGame
{
    public class UpgradeItemUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Image _buttonImage;

        [Header("Colors")]
        [SerializeField] private Color _canAffordColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _cannotAffordColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _maxLevelColor = new Color(1f, 0.8f, 0.2f);

        private UpgradeData _upgradeData;

        private void Awake()
        {
            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        public void Setup(UpgradeData data)
        {
            _upgradeData = data;
            Refresh();
        }

        public void Refresh()
        {
            if (_upgradeData == null || UpgradeManager.Instance == null) return;

            int currentLevel = UpgradeManager.Instance.GetUpgradeLevel(_upgradeData.UpgradeId);
            bool isMaxLevel = currentLevel >= _upgradeData.MaxLevel;
            bool canAfford = UpgradeManager.Instance.CanUpgrade(_upgradeData.UpgradeId);

            // Icon
            if (_iconImage != null && _upgradeData.Icon != null)
                _iconImage.sprite = _upgradeData.Icon;

            // Name
            if (_nameText != null)
                _nameText.text = _upgradeData.DisplayName;

            // Description
            if (_descriptionText != null)
                _descriptionText.text = _upgradeData.Description;

            // Level
            if (_levelText != null)
            {
                if (isMaxLevel)
                    _levelText.text = $"Lv. MAX";
                else
                    _levelText.text = $"Lv. {currentLevel} / {_upgradeData.MaxLevel}";
            }

            // Current Value
            if (_valueText != null)
            {
                string currentValue = _upgradeData.GetValueString(currentLevel);
                if (!isMaxLevel)
                {
                    string nextValue = _upgradeData.GetValueString(currentLevel + 1);
                    _valueText.text = $"{currentValue} → {nextValue}";
                }
                else
                {
                    _valueText.text = currentValue;
                }
            }

            // Cost
            if (_costText != null)
            {
                if (isMaxLevel)
                {
                    _costText.text = "MAX";
                }
                else
                {
                    int cost = _upgradeData.GetCostForLevel(currentLevel);
                    _costText.text = $"{cost:N0}";
                }
            }

            // Button state
            if (_upgradeButton != null)
            {
                _upgradeButton.interactable = !isMaxLevel && canAfford;
            }

            // Button color
            if (_buttonImage != null)
            {
                if (isMaxLevel)
                    _buttonImage.color = _maxLevelColor;
                else if (canAfford)
                    _buttonImage.color = _canAffordColor;
                else
                    _buttonImage.color = _cannotAffordColor;
            }
        }

        private void OnUpgradeClicked()
        {
            if (_upgradeData == null || UpgradeManager.Instance == null) return;

            UpgradeManager.Instance.TryUpgrade(_upgradeData.UpgradeId);
        }
    }
}

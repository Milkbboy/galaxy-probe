using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Data;
using DrillCorp.Core;

namespace DrillCorp.OutGame
{
    public class UpgradeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _upgradeListContainer;
        [SerializeField] private GameObject _upgradeItemPrefab;
        [SerializeField] private Button _backButton;
        [SerializeField] private TitleUI _titleUI;

        [Header("Currency")]
        [SerializeField] private TextMeshProUGUI _currencyText;

        private List<UpgradeItemUI> _upgradeItems = new List<UpgradeItemUI>();

        private void Start()
        {
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);

            GameEvents.OnCurrencyChanged += OnCurrencyChanged;
            GameEvents.OnUpgradePurchased += OnUpgradePurchased;
        }

        private void OnDestroy()
        {
            GameEvents.OnCurrencyChanged -= OnCurrencyChanged;
            GameEvents.OnUpgradePurchased -= OnUpgradePurchased;
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            UpdateCurrencyDisplay();
            RefreshUpgradeList();
        }

        private void RefreshUpgradeList()
        {
            if (UpgradeManager.Instance == null) return;

            // 기존 아이템 갱신
            foreach (var item in _upgradeItems)
            {
                item.Refresh();
            }

            // 아이템이 없으면 생성
            if (_upgradeItems.Count == 0 && _upgradeItemPrefab != null && _upgradeListContainer != null)
            {
                var upgrades = UpgradeManager.Instance.GetAllUpgrades();
                foreach (var upgradeData in upgrades)
                {
                    if (upgradeData == null) continue;

                    var itemObj = Instantiate(_upgradeItemPrefab, _upgradeListContainer);
                    var itemUI = itemObj.GetComponent<UpgradeItemUI>();
                    if (itemUI != null)
                    {
                        itemUI.Setup(upgradeData);
                        _upgradeItems.Add(itemUI);
                    }
                }
            }
        }

        private void OnBackClicked()
        {
            if (_titleUI != null)
                _titleUI.ShowMainPanel();
        }

        private void OnCurrencyChanged(int currency)
        {
            UpdateCurrencyDisplay();
            RefreshUpgradeList();
        }

        private void OnUpgradePurchased(string upgradeId, int newLevel)
        {
            RefreshUpgradeList();
        }

        private void UpdateCurrencyDisplay()
        {
            if (_currencyText != null && DataManager.Instance != null)
            {
                _currencyText.text = $"{DataManager.Instance.Currency:N0}";
            }
        }
    }
}

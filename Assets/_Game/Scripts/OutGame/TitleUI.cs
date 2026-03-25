using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Core;

namespace DrillCorp.OutGame
{
    public class TitleUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _upgradePanel;
        [SerializeField] private GameObject _machineSelectPanel;
        [SerializeField] private GameObject _optionsPanel;

        [Header("Main Panel")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Button _machineButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _quitButton;

        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI _currencyText;

        private void Start()
        {
            SetupButtons();
            UpdateCurrencyDisplay();
            ShowMainPanel();

            GameEvents.OnCurrencyChanged += OnCurrencyChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnCurrencyChanged -= OnCurrencyChanged;
        }

        private void SetupButtons()
        {
            if (_startButton != null)
                _startButton.onClick.AddListener(OnStartClicked);

            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);

            if (_machineButton != null)
                _machineButton.onClick.AddListener(OnMachineClicked);

            if (_optionsButton != null)
                _optionsButton.onClick.AddListener(OnOptionsClicked);

            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnStartClicked()
        {
            GameManager.Instance?.LoadGameScene();
        }

        private void OnUpgradeClicked()
        {
            ShowUpgradePanel();
        }

        private void OnMachineClicked()
        {
            ShowMachineSelectPanel();
        }

        private void OnOptionsClicked()
        {
            ShowOptionsPanel();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ShowMainPanel()
        {
            SetAllPanelsActive(false);
            if (_mainPanel != null)
                _mainPanel.SetActive(true);
        }

        public void ShowUpgradePanel()
        {
            SetAllPanelsActive(false);
            if (_upgradePanel != null)
                _upgradePanel.SetActive(true);
        }

        public void ShowMachineSelectPanel()
        {
            SetAllPanelsActive(false);
            if (_machineSelectPanel != null)
                _machineSelectPanel.SetActive(true);
        }

        public void ShowOptionsPanel()
        {
            SetAllPanelsActive(false);
            if (_optionsPanel != null)
                _optionsPanel.SetActive(true);
        }

        private void SetAllPanelsActive(bool active)
        {
            if (_mainPanel != null) _mainPanel.SetActive(active);
            if (_upgradePanel != null) _upgradePanel.SetActive(active);
            if (_machineSelectPanel != null) _machineSelectPanel.SetActive(active);
            if (_optionsPanel != null) _optionsPanel.SetActive(active);
        }

        private void OnCurrencyChanged(int currency)
        {
            UpdateCurrencyDisplay();
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

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Core;

namespace DrillCorp.OutGame
{
    public class TitleUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _titleLandingPanel;
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _upgradePanel;
        [SerializeField] private GameObject _optionsPanel;

        [Tooltip("v2 통합 HubPanel — UPGRADE 버튼이 이걸 열도록 재활용")]
        [SerializeField] private GameObject _hubPanel;

        [Tooltip("true면 UPGRADE 버튼이 기존 UpgradePanel 대신 HubPanel을 연다 (v2 동작)")]
        [SerializeField] private bool _useHubForUpgrade = true;

        [Header("Main Panel")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _quitButton;

        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI _currencyText;

        private bool _returnToLandingAfterOptions;

        private void Start()
        {
            SetupButtons();
            UpdateCurrencyDisplay();

            if (_titleLandingPanel != null)
                ShowTitleLandingPanel();
            else if (_useHubForUpgrade && _hubPanel != null)
                ShowHubPanel();
            else
                ShowMainPanel();

            GameEvents.OnCurrencyChanged += OnCurrencyChanged;
        }

        private void Update()
        {
            if (_titleLandingPanel == null || !_titleLandingPanel.activeSelf)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                StartGame();
            }
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

            if (_optionsButton != null)
                _optionsButton.onClick.AddListener(OnOptionsClicked);

            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnStartClicked()
        {
            StartGame();
        }

        private void OnUpgradeClicked()
        {
            if (_useHubForUpgrade && _hubPanel != null)
                ShowHubPanel();
            else
                ShowUpgradePanel();
        }

        private void OnOptionsClicked()
        {
            ShowOptionsPanel();
        }

        private void OnQuitClicked()
        {
            QuitGame();
        }

        public void StartGame()
        {
            GameManager.Instance?.LoadGameScene();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ShowTitleLandingPanel()
        {
            SetAllPanelsActive(false);
            if (_titleLandingPanel != null)
                _titleLandingPanel.SetActive(true);
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

        public void ShowOptionsPanel()
        {
            _returnToLandingAfterOptions = _titleLandingPanel != null && _titleLandingPanel.activeSelf;
            SetAllPanelsActive(false);
            if (_optionsPanel != null)
                _optionsPanel.SetActive(true);
        }

        public void CloseOptionsPanel()
        {
            if (_returnToLandingAfterOptions && _titleLandingPanel != null)
            {
                ShowTitleLandingPanel();
                return;
            }

            if (_useHubForUpgrade && _hubPanel != null)
                ShowHubPanel();
            else
                ShowMainPanel();
        }

        public void ShowHubPanel()
        {
            SetAllPanelsActive(false);
            if (_hubPanel != null)
                _hubPanel.SetActive(true);
        }

        public void ShowUpgradeHubPanel()
        {
            ShowHubPanel(HubController.FocusTarget.Upgrades);
        }

        public void ShowCharacterHubPanel()
        {
            ShowHubPanel(HubController.FocusTarget.Character);
        }

        public void ShowCraftingHubPanel()
        {
            ShowHubPanel(HubController.FocusTarget.Crafting);
        }

        private void ShowHubPanel(HubController.FocusTarget focusTarget)
        {
            ShowHubPanel();

            if (_hubPanel == null)
                return;

            var hub = _hubPanel.GetComponent<HubController>();
            if (hub != null)
                hub.Focus(focusTarget);
        }

        private void SetAllPanelsActive(bool active)
        {
            if (_titleLandingPanel != null) _titleLandingPanel.SetActive(active);
            if (_mainPanel != null) _mainPanel.SetActive(active);
            if (_upgradePanel != null) _upgradePanel.SetActive(active);
            if (_optionsPanel != null) _optionsPanel.SetActive(active);
            if (_hubPanel != null) _hubPanel.SetActive(active);
        }

        private void OnCurrencyChanged(int currency)
        {
            UpdateCurrencyDisplay();
        }

        private void UpdateCurrencyDisplay()
        {
            if (_currencyText != null && DataManager.Instance != null)
            {
                _currencyText.text = $"{DataManager.Instance.Ore:N0}";
            }
        }
    }
}

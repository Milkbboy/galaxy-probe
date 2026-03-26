using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Core;
using DrillCorp.Machine;

namespace DrillCorp.UI
{
    public class SessionResultUI : MonoBehaviour
    {
        [Header("Success Panel")]
        [SerializeField] private GameObject _successPanel;
        [SerializeField] private TextMeshProUGUI _successMiningText;
        [SerializeField] private TextMeshProUGUI _successCurrencyText;
        [SerializeField] private Button _successContinueButton;

        [Header("Failed Panel")]
        [SerializeField] private GameObject _failedPanel;
        [SerializeField] private TextMeshProUGUI _failedMiningText;
        [SerializeField] private Button _failedRetryButton;
        [SerializeField] private Button _failedQuitButton;

        private MachineController _machine;

        private bool _buttonsSetup = false;

        private void Awake()
        {
            _machine = FindAnyObjectByType<MachineController>();
            SetupButtons();
        }

        private void Start()
        {
            SetupButtons();
        }

        private void OnEnable()
        {
            SetupButtons();
            GameEvents.OnSessionSuccess += ShowSuccess;
            GameEvents.OnSessionFailed += ShowFailed;
        }

        private void OnDisable()
        {
            GameEvents.OnSessionSuccess -= ShowSuccess;
            GameEvents.OnSessionFailed -= ShowFailed;
        }

        private void SetupButtons()
        {
            if (_buttonsSetup) return;
            _buttonsSetup = true;

            if (_successContinueButton != null)
            {
                _successContinueButton.onClick.AddListener(OnContinueClicked);
            }

            if (_failedRetryButton != null)
            {
                _failedRetryButton.onClick.AddListener(OnRetryClicked);
            }

            if (_failedQuitButton != null)
            {
                _failedQuitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void ShowSuccess()
        {
            if (_successPanel != null)
            {
                _successPanel.SetActive(true);
            }

            if (_failedPanel != null)
            {
                _failedPanel.SetActive(false);
            }

            UpdateSuccessUI();
        }

        private void ShowFailed()
        {
            if (_successPanel != null)
            {
                _successPanel.SetActive(false);
            }

            if (_failedPanel != null)
            {
                _failedPanel.SetActive(true);
            }

            UpdateFailedUI();
        }

        private void UpdateSuccessUI()
        {
            int mined = _machine != null ? _machine.TotalMined : 0;

            if (_successMiningText != null)
            {
                _successMiningText.text = $"채굴량: {mined}";
            }

            if (_successCurrencyText != null && DataManager.Instance != null)
            {
                _successCurrencyText.text = $"보유 재화: {DataManager.Instance.Data.Currency}";
            }
        }

        private void UpdateFailedUI()
        {
            int mined = _machine != null ? _machine.TotalMined : 0;

            if (_failedMiningText != null)
            {
                _failedMiningText.text = $"채굴량: {mined} (획득 불가)";
            }
        }

        private void OnContinueClicked()
        {
            GameManager.Instance?.RestartSession();
        }

        private void OnRetryClicked()
        {
            GameManager.Instance?.RestartSession();
        }

        private void OnQuitClicked()
        {
            GameManager.Instance?.LoadTitleScene();
        }
    }
}

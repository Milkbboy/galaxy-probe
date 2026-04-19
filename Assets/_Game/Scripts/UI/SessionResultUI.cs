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

        // v2 — 세션 중 채집한 보석 수 (OnGemCollected 이벤트로 누적)
        private int _sessionGems;

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
            _sessionGems = 0;  // 세션 시작 시 리셋
            GameEvents.OnSessionSuccess += ShowSuccess;
            GameEvents.OnSessionFailed += ShowFailed;
            GameEvents.OnGemCollected += OnGemCollected;
        }

        private void OnDisable()
        {
            GameEvents.OnSessionSuccess -= ShowSuccess;
            GameEvents.OnSessionFailed -= ShowFailed;
            GameEvents.OnGemCollected -= OnGemCollected;
        }

        private void OnGemCollected(int amount) => _sessionGems += amount;

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
                // v2 — 세션 중 채집한 보석도 함께 표시
                _successMiningText.text = _sessionGems > 0
                    ? $"채굴량: {mined} / 보석: {_sessionGems}"
                    : $"채굴량: {mined}";
            }

            if (_successCurrencyText != null && DataManager.Instance != null)
            {
                var dm = DataManager.Instance;
                _successCurrencyText.text = $"보유 재화: {dm.Ore} 광석 / {dm.Gems} 보석";
            }
        }

        private void UpdateFailedUI()
        {
            int mined = _machine != null ? _machine.TotalMined : 0;

            if (_failedMiningText != null)
            {
                // 광석은 획득 불가, 이미 채집한 보석은 유지됨 (v2 — 즉시 적립)
                _failedMiningText.text = _sessionGems > 0
                    ? $"채굴량: {mined} (광석 획득 불가) / 보석: {_sessionGems} 획득"
                    : $"채굴량: {mined} (획득 불가)";
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

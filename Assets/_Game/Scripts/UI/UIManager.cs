using UnityEngine;
using DrillCorp.Core;

namespace DrillCorp.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject _inGameUI;
        [SerializeField] private GameObject _sessionSuccessUI;
        [SerializeField] private GameObject _sessionFailedUI;
        [SerializeField] private GameObject _pauseUI;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnSessionSuccess += ShowSessionSuccess;
            GameEvents.OnSessionFailed += ShowSessionFailed;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnSessionSuccess -= ShowSessionSuccess;
            GameEvents.OnSessionFailed -= ShowSessionFailed;
        }

        private void Start()
        {
            HideAllPanels();
            ShowInGameUI();
        }

        private void HandleGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    HideAllPanels();
                    ShowInGameUI();
                    break;
                case GameState.Paused:
                    ShowPauseUI();
                    break;
                case GameState.SessionSuccess:
                    ShowSessionSuccess();
                    break;
                case GameState.SessionFailed:
                    ShowSessionFailed();
                    break;
            }
        }

        private void HideAllPanels()
        {
            SetPanelActive(_inGameUI, false);
            SetPanelActive(_sessionSuccessUI, false);
            SetPanelActive(_sessionFailedUI, false);
            SetPanelActive(_pauseUI, false);
        }

        private void ShowInGameUI()
        {
            SetPanelActive(_inGameUI, true);
        }

        private void ShowSessionSuccess()
        {
            SetPanelActive(_inGameUI, false);
            SetPanelActive(_sessionSuccessUI, true);
        }

        private void ShowSessionFailed()
        {
            SetPanelActive(_inGameUI, false);
            SetPanelActive(_sessionFailedUI, true);
        }

        private void ShowPauseUI()
        {
            SetPanelActive(_pauseUI, true);
        }

        public void HidePauseUI()
        {
            SetPanelActive(_pauseUI, false);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Core;
using DrillCorp.Machine;

namespace DrillCorp.UI
{
    /// <summary>
    /// v2.html #resultPanel 포팅.
    /// 승리/패배 통합 단일 팝업. 제목 색·아이콘·부제·획득 수치를 동적으로 전환.
    ///
    /// 에디터 스크립트 ResultPanelSetupEditor가 자식 참조 자동 바인딩.
    ///
    /// v2 지연: 승리 500ms, 패배 200ms 후 표시.
    /// 버튼 2개: "업그레이드 하기" → Title 복귀 / "다시 도전" → 씬 재시작.
    /// </summary>
    public class SessionResultUI : MonoBehaviour
    {
        [Header("Panel")]
        [Tooltip("결과 팝업 루트. 평상시 비활성.")]
        [SerializeField] private GameObject _panel;

        [Header("Title")]
        [SerializeField] private Image _titleIcon;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _subtitleText;

        [Header("Reward")]
        [SerializeField] private TextMeshProUGUI _oreText;
        [SerializeField] private TextMeshProUGUI _gemText;

        [Header("Buttons")]
        [SerializeField] private Button _upgradeButton;     // "업그레이드 하기" → Title
        [SerializeField] private Button _retryButton;       // "다시 도전" → 세션 재시작

        [Header("Icons")]
        [SerializeField] private Sprite _successIcon;
        [SerializeField] private Sprite _failureIcon;

        [Header("Timing")]
        [Tooltip("v2 원본 — 승리/패배 지연 (초)")]
        [SerializeField] private float _winDelay = 0.5f;
        [SerializeField] private float _loseDelay = 0.2f;

        // v2 팔레트
        private static readonly Color ColWinTitle  = new Color32(0xff, 0xd7, 0x00, 0xFF); // 금색
        private static readonly Color ColLoseTitle = new Color32(0xff, 0x6b, 0x6b, 0xFF); // 빨강

        private MachineController _machine;
        private Coroutine _showRoutine;

        private void Awake()
        {
            _machine = FindAnyObjectByType<MachineController>();
            if (_panel == null)
            {
                Debug.LogError("[SessionResultUI] _panel 참조가 비었습니다. 에디터 메뉴 Drill-Corp/HUD/Build Result Panel을 실행하세요.");
            }
            else
            {
                _panel.SetActive(false);
            }
            WireButtons();
        }

        private void OnEnable()
        {
            GameEvents.OnSessionSuccess += OnSessionSuccess;
            GameEvents.OnSessionFailed  += OnSessionFailed;
        }

        private void OnDisable()
        {
            GameEvents.OnSessionSuccess -= OnSessionSuccess;
            GameEvents.OnSessionFailed  -= OnSessionFailed;
        }

        private void WireButtons()
        {
            if (_upgradeButton != null)
            {
                _upgradeButton.onClick.RemoveAllListeners();
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
                _retryButton.onClick.AddListener(OnRetryClicked);
            }
        }

        // ───────────── 이벤트 ─────────────
        private void OnSessionSuccess() => ScheduleShow(isWin: true, _winDelay);
        private void OnSessionFailed()  => ScheduleShow(isWin: false, _loseDelay);

        private void ScheduleShow(bool isWin, float delay)
        {
            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = StartCoroutine(ShowAfter(isWin, delay));
        }

        private IEnumerator ShowAfter(bool isWin, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Show(isWin);
            _showRoutine = null;
        }

        // ───────────── 표시 ─────────────
        private void Show(bool isWin)
        {
            if (_panel != null) _panel.SetActive(true);

            // 타이틀 & 아이콘
            if (_titleText != null)
            {
                _titleText.text = isWin ? "채굴 완료!" : "채굴 실패";
                _titleText.color = isWin ? ColWinTitle : ColLoseTitle;
            }
            if (_titleIcon != null)
            {
                _titleIcon.sprite = isWin ? _successIcon : _failureIcon;
                _titleIcon.enabled = _titleIcon.sprite != null;
            }

            // 부제
            if (_subtitleText != null)
            {
                if (isWin)
                {
                    int target = _machine != null ? Mathf.RoundToInt(_machine.MiningTarget) : 0;
                    _subtitleText.text = $"목표 채굴량 {target}을 달성했습니다!";
                }
                else
                {
                    int kills = _machine != null ? _machine.SessionKills : 0;
                    _subtitleText.text = $"굴착기가 파괴되었습니다. 처치: {kills}마리";
                }
            }

            // 획득 수치 (v2: 승리 전액, 패배 50%)
            int sessionOre  = _machine != null ? _machine.SessionOre  : 0;
            int sessionGems = _machine != null ? _machine.SessionGems : 0;
            int oreReward = isWin ? sessionOre  : Mathf.FloorToInt(sessionOre  * 0.5f);
            int gemReward = isWin ? sessionGems : Mathf.FloorToInt(sessionGems * 0.5f);

            // 아이콘이 옆에 표시되므로 텍스트엔 수치만.
            if (_oreText != null) _oreText.text = $"+ {oreReward}";
            if (_gemText != null) _gemText.text = $"+ {gemReward}";
        }

        // ───────────── 버튼 ─────────────
        private void OnUpgradeClicked()
        {
            GameManager.Instance?.LoadTitleScene();
        }

        private void OnRetryClicked()
        {
            GameManager.Instance?.RestartSession();
        }
    }
}

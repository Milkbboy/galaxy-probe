using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Core;

namespace DrillCorp.OutGame
{
    /// <summary>
    /// Title 씬의 HubPanel 루트에 부착.
    /// TopBar 재화 표시 + 치트/리셋/시작/뒤로가기 버튼 처리.
    /// 내부 UI는 V2HubCanvasSetupEditor가 생성한 계층을 Find로 조회.
    /// </summary>
    public class HubController : MonoBehaviour
    {
        [SerializeField] private TitleUI _titleUI;

        // ─── 자동 탐색 캐시 ───
        private TextMeshProUGUI _oreValueText;
        private TextMeshProUGUI _gemValueText;
        private TextMeshProUGUI _targetLabel;
        private Button _cheatButton;
        private Button _resetButton;
        private Button _startButton;
        private Button _backButton;

        // 리셋 2단계 확인용
        private Coroutine _resetConfirmRoutine;
        private bool _awaitingResetConfirm;

        private void Awake()
        {
            CacheChildren();
            WireButtons();
        }

        private void OnEnable()
        {
            RefreshCurrency();
            RefreshTargetLabel();
            GameEvents.OnOreChanged  += OnOreChanged;
            GameEvents.OnGemsChanged += OnGemsChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnOreChanged  -= OnOreChanged;
            GameEvents.OnGemsChanged -= OnGemsChanged;
            CancelResetConfirm();
        }

        // ═══════════════════════════════════════════════════
        // 초기화
        // ═══════════════════════════════════════════════════
        private void CacheChildren()
        {
            // TopBar 경로 — V2HubCanvasSetupEditor 계층 기준
            var topBar = transform.Find("TopBar");
            if (topBar == null)
            {
                Debug.LogError("[HubController] TopBar 자식이 없습니다. V2HubCanvasSetupEditor로 생성되었는지 확인하세요.");
                return;
            }

            _oreValueText = topBar.Find("OreDisplay/Value")?.GetComponent<TextMeshProUGUI>();
            _gemValueText = topBar.Find("GemDisplay/Value")?.GetComponent<TextMeshProUGUI>();
            _targetLabel  = topBar.Find("TitleGroup/TargetLabel")?.GetComponent<TextMeshProUGUI>();

            _cheatButton  = topBar.Find("CheatButton")?.GetComponent<Button>();
            _resetButton  = topBar.Find("ResetButton")?.GetComponent<Button>();
            _startButton  = topBar.Find("StartButton")?.GetComponent<Button>();
            _backButton   = topBar.Find("BackButton")?.GetComponent<Button>();
        }

        private void WireButtons()
        {
            if (_cheatButton != null) _cheatButton.onClick.AddListener(OnCheatClicked);
            if (_resetButton != null) _resetButton.onClick.AddListener(OnResetClicked);
            if (_startButton != null) _startButton.onClick.AddListener(OnStartClicked);
            if (_backButton  != null) _backButton.onClick.AddListener(OnBackClicked);

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            // 릴리즈 빌드에서는 치트 버튼 숨김
            if (_cheatButton != null) _cheatButton.gameObject.SetActive(false);
#endif
        }

        // ═══════════════════════════════════════════════════
        // 재화 표시
        // ═══════════════════════════════════════════════════
        private void OnOreChanged(int _)  => RefreshCurrency();
        private void OnGemsChanged(int _) => RefreshCurrency();

        private void RefreshCurrency()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (_oreValueText != null) _oreValueText.text = $"{dm.Ore:N0}";
            if (_gemValueText != null) _gemValueText.text = $"{dm.Gems:N0}";
        }

        private void RefreshTargetLabel()
        {
            if (_targetLabel == null) return;

            // mineTarget / gem_drop / gem_speed 합산 표시 (v2 헤더 재현)
            var upgMgr = UpgradeManager.Instance;
            float miningTarget = 100f;
            float gemDropBonus = 0f;
            float gemSpeedMult = 1f;

            if (upgMgr != null)
            {
                miningTarget += upgMgr.GetTotalBonus(DrillCorp.Data.UpgradeType.MiningTarget);
                gemDropBonus = upgMgr.GetTotalBonus(DrillCorp.Data.UpgradeType.GemDropRate);
                gemSpeedMult = 1f + upgMgr.GetTotalBonus(DrillCorp.Data.UpgradeType.GemCollectSpeed);
            }

            int dropPct = Mathf.RoundToInt((0.05f + gemDropBonus) * 100f);
            float collectSec = 2f / Mathf.Max(0.01f, gemSpeedMult);

            _targetLabel.text =
                $"목표: <color=#f4a423>{miningTarget:F0}</color> 채굴 · " +
                $"보석 드랍 {dropPct}% ({collectSec:F1}초 수집)";
        }

        // ═══════════════════════════════════════════════════
        // 버튼 핸들러
        // ═══════════════════════════════════════════════════
        private void OnStartClicked()
        {
            GameManager.Instance?.LoadGameScene();
        }

        private void OnBackClicked()
        {
            CancelResetConfirm();
            if (_titleUI != null)
                _titleUI.ShowMainPanel();
        }

        private void OnCheatClicked()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DataManager.Instance?.AddOre(1000);
            DataManager.Instance?.AddGems(1000);
#endif
        }

        private void OnResetClicked()
        {
            if (_awaitingResetConfirm)
            {
                // 두 번째 클릭 — 확정
                PerformReset();
                return;
            }

            // 첫 번째 클릭 — 3초간 재클릭 대기
            _awaitingResetConfirm = true;
            SetResetButtonLabel("정말 초기화?");
            if (_resetConfirmRoutine != null) StopCoroutine(_resetConfirmRoutine);
            _resetConfirmRoutine = StartCoroutine(CancelResetAfter(3f));
        }

        private IEnumerator CancelResetAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            CancelResetConfirm();
        }

        private void CancelResetConfirm()
        {
            if (_resetConfirmRoutine != null) StopCoroutine(_resetConfirmRoutine);
            _resetConfirmRoutine = null;
            _awaitingResetConfirm = false;
            SetResetButtonLabel("초기화");
        }

        private void SetResetButtonLabel(string label)
        {
            if (_resetButton == null) return;
            var tmp = _resetButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = label;
        }

        private void PerformReset()
        {
            _awaitingResetConfirm = false;
            if (_resetConfirmRoutine != null) StopCoroutine(_resetConfirmRoutine);
            _resetConfirmRoutine = null;
            SetResetButtonLabel("초기화");

            DataManager.Instance?.ResetData();
            UpgradeManager.Instance?.ResetAllUpgrades();
            RefreshCurrency();
            RefreshTargetLabel();

            Debug.Log("[HubController] 데이터 초기화 완료.");
        }
    }
}

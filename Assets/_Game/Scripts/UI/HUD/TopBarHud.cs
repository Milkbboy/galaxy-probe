using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Ability;
using DrillCorp.Core;
using DrillCorp.Machine;

namespace DrillCorp.UI.HUD
{
    /// <summary>
    /// Game 씬 상단 HUD 통합 컴포넌트 (v2.html 상단바 포팅).
    /// 슬롯 5개 + 나가기 버튼: 체력 / 채굴 / 처치 / 광석 / 보석 / 나가기.
    ///
    /// v2 원본 규칙:
    /// - 광석 슬롯 = 세션 광석(sessionOre = 채굴*0.5 + 벌레score*0.5), 보유량 아님
    /// - 보석 슬롯 = 세션 보석(sessionGems), 보유량 아님
    /// - 나가기 = 정산 없이 즉시 Title 복귀
    /// - 승리 정산: 세션 광석·보석 전액 적립 (MachineController)
    /// - 패배 정산: 세션 광석·보석 50%만 적립 (MachineController)
    ///
    /// 에디터 스크립트 TopBarHudSetupEditor가 자식 참조 자동 바인딩.
    /// </summary>
    public class TopBarHud : MonoBehaviour
    {
        [Header("Slots — 값 텍스트")]
        [SerializeField] private TextMeshProUGUI _characterNameText;   // v2 좌상단 캐릭터 이름 — TopBar 좌측 흡수
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _miningText;
        [SerializeField] private TextMeshProUGUI _killsText;
        [SerializeField] private TextMeshProUGUI _oreText;
        [SerializeField] private TextMeshProUGUI _gemText;

        [Header("Exit Button")]
        [SerializeField] private Button _exitButton;

        [Header("References")]
        [SerializeField] private MachineController _machine;
        [Tooltip("비우면 FindAnyObjectByType<AbilitySlotController>() 자동 탐색. 캐릭터 이름·테마컬러 소스.")]
        [SerializeField] private AbilitySlotController _abilityController;

        private int _sessionKills;
        private int _sessionGems;
        private int _sessionOre;

        private void Awake()
        {
            if (_machine == null) _machine = FindAnyObjectByType<MachineController>();
            if (_exitButton != null) _exitButton.onClick.AddListener(OnExitClicked);
        }

        private void Start()
        {
            // AbilitySlotController.Start 가 먼저 끝나야 ResolvedCharacter 가 채워짐.
            // 못 찾으면 Update 에서 한 번 더 시도.
            TryApplyCharacter();
        }

        private bool _characterApplied;

        private void TryApplyCharacter()
        {
            if (_characterApplied) return;
            if (_characterNameText == null) { _characterApplied = true; return; }

            if (_abilityController == null)
                _abilityController = FindAnyObjectByType<AbilitySlotController>();

            var ch = _abilityController?.ResolvedCharacter;
            if (ch == null) return;

            _characterNameText.text  = ch.DisplayName;
            // v2 ch.color + 'cc' = alpha 0.8
            var c = ch.ThemeColor;
            _characterNameText.color = new Color(c.r, c.g, c.b, 0.8f);
            _characterApplied = true;
        }

        private void OnEnable()
        {
            _sessionKills = 0;
            _sessionGems  = 0;
            _sessionOre   = 0;

            GameEvents.OnMachineDamaged     += OnMachineDamaged;
            GameEvents.OnMiningGained       += OnMiningGained;
            GameEvents.OnBugKilled          += OnBugKilled;
            GameEvents.OnGemCollected       += OnGemCollected;
            GameEvents.OnSessionOreChanged  += OnSessionOreChanged;
            GameEvents.OnSessionGemsChanged += OnSessionGemsChanged;

            RefreshAll();
        }

        private void OnDisable()
        {
            GameEvents.OnMachineDamaged     -= OnMachineDamaged;
            GameEvents.OnMiningGained       -= OnMiningGained;
            GameEvents.OnBugKilled          -= OnBugKilled;
            GameEvents.OnGemCollected       -= OnGemCollected;
            GameEvents.OnSessionOreChanged  -= OnSessionOreChanged;
            GameEvents.OnSessionGemsChanged -= OnSessionGemsChanged;
        }

        // ───────────── 이벤트 핸들러 ─────────────
        private void OnMachineDamaged(float _) => UpdateHealth();
        private void OnMiningGained(int _)     => UpdateMining();
        private void OnBugKilled(int _)
        {
            _sessionKills++;
            UpdateKills();
        }
        private void OnGemCollected(int _) { /* SessionGemsChanged에서 최종 값 반영 */ }
        private void OnSessionOreChanged(int total)
        {
            _sessionOre = total;
            UpdateOre();
        }
        private void OnSessionGemsChanged(int total)
        {
            _sessionGems = total;
            UpdateGems();
        }

        // ───────────── 갱신 ─────────────
        private void RefreshAll()
        {
            UpdateHealth();
            UpdateMining();
            UpdateKills();
            UpdateOre();
            UpdateGems();
        }

        private void UpdateHealth()
        {
            if (_healthText == null || _machine == null) return;
            _healthText.text = $"체력 {Mathf.CeilToInt(_machine.CurrentHealth)}";
        }

        private void UpdateMining()
        {
            if (_miningText == null || _machine == null) return;
            _miningText.text = $"{_machine.TotalMined} / {Mathf.RoundToInt(_machine.MiningTarget)}";
        }

        private void UpdateKills()
        {
            if (_killsText == null) return;
            _killsText.text = $"처치 {_sessionKills}";
        }

        private void UpdateOre()
        {
            if (_oreText == null) return;
            _oreText.text = $"광석 {_sessionOre}";
        }

        private void UpdateGems()
        {
            if (_gemText == null) return;
            _gemText.text = $"보석 {_sessionGems}";
        }

        // 매 프레임 체력 갱신 (초기화/회복 타이밍 이슈 대비 — MachineStatusUI와 동일한 방식)
        private void Update()
        {
            UpdateHealth();
            if (!_characterApplied) TryApplyCharacter();
        }

        // ───────────── 나가기 ─────────────
        // v2 원본과 동일 — 세션 중단 시 정산 없이 Title로 복귀 (포기 = 0 보상).
        private void OnExitClicked()
        {
            GameManager.Instance?.LoadTitleScene();
        }
    }
}

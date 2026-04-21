using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.OutGame;
using DrillCorp.UI;
using DrillCorp.UI.Minimap;

namespace DrillCorp.Machine
{
    public class MachineController : MonoBehaviour, IDamageable
    {
        [Header("Data")]
        [SerializeField] private MachineData _machineData;

        [Header("Health")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _armor = 0f;
        private float _currentHealth;

        [Header("Mining")]
        [SerializeField] private float _miningRate = 10f;
        private int _totalMined;
        private float _miningAccumulator;
        private float _miningTarget = 100f;  // v2: BaseMiningTarget + lv*50

        [Header("Weapon")]
        [SerializeField] private float _attackDamage = 20f;
        [SerializeField] private float _attackCooldown = 0.5f;
        [SerializeField] private float _attackRange = 3f;

        // v2 Upgrade — 받는 피해 감소율 (0~1, UpgradeManager.Armor 합산).
        // legacy _armor(MachineData)와 별도 누적.
        private float _damageReductionPct;

        [Header("Debug")]
        [Tooltip("ON 이면 채굴 목표량을 무한으로 간주 — 세션이 채굴 성공으로 끝나지 않음. " +
                 "어빌리티/HUD 디버깅 용도. 영구 데이터 영향 없음.")]
        [SerializeField] private bool _debugInfiniteMiningTarget;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsDead => _currentHealth <= 0f && !_isInvincible;

        public int TotalMined => _totalMined;
        public float MiningTarget => _miningTarget;
        public bool IsMiningTargetReached => !_debugInfiniteMiningTarget && _totalMined >= _miningTarget;

        // Weapon properties for AimController
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float AttackRange => _attackRange;

        public MachineData MachineData => _machineData;

        private bool _isSessionActive;
        private int _sessionGemsCollected;
        private int _sessionBugsKilled;

        // v2 — 세션 광석 누적 (mineAmt*0.5 + bugScore*0.5).
        // 승리 시 전액, 패배 시 50%만 DataManager.Ore로 적립.
        private float _sessionOre;
        public int SessionOre => Mathf.FloorToInt(_sessionOre);
        public int SessionGems => _sessionGemsCollected;
        public int SessionKills => _sessionBugsKilled;

        [Header("Debug")]
        [Tooltip("시작 시 무적 상태 (디버그/테스트용)")]
        [SerializeField] private bool _startInvincible = true;

        private bool _isInvincible;
        public bool IsInvincible => _isInvincible;

        private void Awake()
        {
            ApplySelectedCharacter();
            ApplyMachineData();
            ApplyUpgradeBonuses();
            _isInvincible = _startInvincible;
        }

        // v2 — 선택한 캐릭터의 DefaultMachine으로 _machineData 교체.
        // CharacterRegistry 또는 DataManager가 없으면(Game 단독 실행) 프리펩 기본값 유지.
        private void ApplySelectedCharacter()
        {
            var dm = DataManager.Instance;
            var reg = CharacterRegistry.Instance;
            if (dm?.Data == null || reg == null) return;

            var character = reg.Find(dm.Data.SelectedCharacterId);
            if (character == null || character.DefaultMachine == null) return;

            _machineData = character.DefaultMachine;
        }

        private void Start()
        {
            InitializeSession();
            MinimapIcon.Create(transform, new Color(0.3f, 0.8f, 1f), 2f, MinimapIcon.IconShape.Square);
        }

        private void ApplyMachineData()
        {
            if (_machineData != null)
            {
                _maxHealth = _machineData.MaxHealth;
                _armor = _machineData.Armor;
                _miningRate = _machineData.TotalMiningRate;
                _attackDamage = _machineData.AttackDamage;
                _attackCooldown = _machineData.AttackCooldown;
                _attackRange = _machineData.AttackRange;
                _miningTarget = _machineData.BaseMiningTarget;
            }
        }

        // v2: UpgradeManager 누적 보너스를 적용. Title에서 사 모은 강화가 Game에서 살아남.
        private void ApplyUpgradeBonuses()
        {
            var um = UpgradeManager.Instance;
            if (um == null) return;

            _maxHealth += um.GetTotalBonus(UpgradeType.MaxHealth);     // +30/lv
            _miningRate += um.GetTotalBonus(UpgradeType.MiningRate);    // +2/lv
            _miningTarget += um.GetTotalBonus(UpgradeType.MiningTarget);  // +50/lv
            _damageReductionPct = Mathf.Clamp01(um.GetTotalBonus(UpgradeType.Armor)); // 0.15/lv → max 0.45
        }

        private void Update()
        {
            if (!_isSessionActive) return;

            Mining();
            CheckSessionEnd();
        }

        public void InitializeSession()
        {
            _currentHealth = _maxHealth;
            _totalMined = 0;
            _miningAccumulator = 0f;
            _sessionGemsCollected = 0;
            _sessionBugsKilled = 0;
            _sessionOre = 0f;
            _isSessionActive = true;
            GameEvents.OnSessionOreChanged?.Invoke(0);
            GameEvents.OnSessionGemsChanged?.Invoke(0);
        }

        private void OnEnable()
        {
            GameEvents.OnGemCollected   += OnGemCollected;
            GameEvents.OnBugKilled      += OnBugKilled;
            GameEvents.OnBugScoreEarned += OnBugScoreEarned;
        }

        private void OnDisable()
        {
            GameEvents.OnGemCollected   -= OnGemCollected;
            GameEvents.OnBugKilled      -= OnBugKilled;
            GameEvents.OnBugScoreEarned -= OnBugScoreEarned;
        }

        private void OnGemCollected(int amount)
        {
            _sessionGemsCollected += amount;
            GameEvents.OnSessionGemsChanged?.Invoke(_sessionGemsCollected);
        }
        private void OnBugKilled(int kind) => _sessionBugsKilled++;

        // v2 — 벌레 처치 시 score*0.5를 세션 광석에 보너스 누적.
        private void OnBugScoreEarned(float score)
        {
            _sessionOre += score * 0.5f;
            GameEvents.OnSessionOreChanged?.Invoke(SessionOre);
        }

        private void Mining()
        {
            // 채굴량 누적
            _miningAccumulator += _miningRate * Time.deltaTime;

            // 정수 단위로 변환
            int mined = Mathf.FloorToInt(_miningAccumulator);
            if (mined > 0)
            {
                _miningAccumulator -= mined;
                _totalMined += mined;
                GameEvents.OnMiningGained?.Invoke(mined);

                // v2 — sessionOre += mined * 0.5 (연속값이라 누적). 정수 바뀔 때만 이벤트 발행.
                int preSessionOre = SessionOre;
                _sessionOre += mined * 0.5f;
                if (SessionOre != preSessionOre)
                    GameEvents.OnSessionOreChanged?.Invoke(SessionOre);
            }
        }

        private void CheckSessionEnd()
        {
            if (IsDead)
            {
                _isSessionActive = false;
                GameEvents.OnMachineDestroyed?.Invoke();
                // v2 패배 정산 — 세션 광석·보석 각각 50%만 적립.
                int oreReward = Mathf.FloorToInt(_sessionOre * 0.5f);
                int gemReward = Mathf.FloorToInt(_sessionGemsCollected * 0.5f);
                DataManager.Instance?.AddOre(oreReward);
                DataManager.Instance?.AddGems(gemReward);
                DataManager.Instance?.StoreSessionResult(false, oreReward, gemReward, _sessionBugsKilled);
                GameManager.Instance?.SessionFailed();
            }
            else if (IsMiningTargetReached)
            {
                _isSessionActive = false;
                // v2 승리 정산 — 세션 광석·보석 전액 적립.
                int oreReward = SessionOre;
                int gemReward = _sessionGemsCollected;
                DataManager.Instance?.AddOre(oreReward);
                DataManager.Instance?.AddGems(gemReward);
                DataManager.Instance?.StoreSessionResult(true, oreReward, gemReward, _sessionBugsKilled);
                GameManager.Instance?.SessionSuccess();
            }
        }

        public void TakeDamage(float damage)
        {
            if (IsDead || !_isSessionActive) return;

            // Armor 적용
            float actualDamage = CalculateDamageReceived(damage);
            _currentHealth -= actualDamage;

            // 무적 상태면 HP 1 이하로 안 떨어짐
            if (_isInvincible)
            {
                _currentHealth = Mathf.Max(1f, _currentHealth);
            }
            else
            {
                _currentHealth = Mathf.Max(0f, _currentHealth);
            }

            // 데미지 팝업 표시 (콜라이더 크기 고려)
            DamagePopup.Create(transform, actualDamage, PopupType.Normal);

            GameEvents.OnMachineDamaged?.Invoke(actualDamage);
        }

        private float CalculateDamageReceived(float rawDamage)
        {
            float dmg = rawDamage;

            // legacy armor (MachineData.Armor): armor/(armor+100) 곡선
            if (_armor > 0f)
            {
                float reduction = _armor / (_armor + 100f);
                dmg *= 1f - reduction;
            }

            // v2 — 받는 피해 감소율 직접 차감 (excavator_armor 강화)
            if (_damageReductionPct > 0f)
            {
                dmg *= 1f - _damageReductionPct;
            }

            return dmg;
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            _currentHealth += amount;
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        }

        #region Debug

        /// <summary>
        /// 무적 모드 토글
        /// </summary>
        public void ToggleInvincible()
        {
            _isInvincible = !_isInvincible;
            Debug.Log($"[Machine] Invincible: {_isInvincible}");
        }

        /// <summary>
        /// 무적 모드 설정
        /// </summary>
        public void SetInvincible(bool value)
        {
            _isInvincible = value;
            Debug.Log($"[Machine] Invincible: {_isInvincible}");
        }

        #endregion
    }
}

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

        [Header("Fuel")]
        [SerializeField] private float _maxFuel = 60f;
        [SerializeField] private float _fuelConsumeRate = 1f;
        private float _currentFuel;

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

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsDead => _currentHealth <= 0f && !_isInvincible;

        public float CurrentFuel => _currentFuel;
        public float MaxFuel => _maxFuel;
        public bool IsFuelEmpty => _currentFuel <= 0f;

        public int TotalMined => _totalMined;
        public float MiningTarget => _miningTarget;
        public bool IsMiningTargetReached => _totalMined >= _miningTarget;

        // Weapon properties for AimController
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float AttackRange => _attackRange;

        public MachineData MachineData => _machineData;

        private bool _isSessionActive;

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
                _maxFuel = _machineData.MaxFuel;
                _fuelConsumeRate = _machineData.FuelConsumeRate;
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

            _maxHealth          += um.GetTotalBonus(UpgradeType.MaxHealth);     // +30/lv
            _miningRate         += um.GetTotalBonus(UpgradeType.MiningRate);    // +2/lv
            _miningTarget       += um.GetTotalBonus(UpgradeType.MiningTarget);  // +50/lv
            _damageReductionPct  = Mathf.Clamp01(um.GetTotalBonus(UpgradeType.Armor)); // 0.15/lv → max 0.45
        }

        private void Update()
        {
            if (!_isSessionActive) return;

            ConsumeFuel();
            Mining();
            CheckSessionEnd();
        }

        public void InitializeSession()
        {
            _currentHealth = _maxHealth;
            _currentFuel = _maxFuel;
            _totalMined = 0;
            _miningAccumulator = 0f;
            _isSessionActive = true;

            GameEvents.OnFuelChanged?.Invoke(_currentFuel);
        }

        private void ConsumeFuel()
        {
            _currentFuel -= _fuelConsumeRate * Time.deltaTime;
            _currentFuel = Mathf.Max(0f, _currentFuel);
            GameEvents.OnFuelChanged?.Invoke(_currentFuel);
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
            }
        }

        private void CheckSessionEnd()
        {
            if (IsDead)
            {
                _isSessionActive = false;
                GameEvents.OnMachineDestroyed?.Invoke();
                GameManager.Instance?.SessionFailed();
            }
            else if (IsFuelEmpty)
            {
                _isSessionActive = false;
                GameEvents.OnSessionSuccess?.Invoke();
                DataManager.Instance?.AddOre(_totalMined);
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

        public void AddFuel(float amount)
        {
            _currentFuel += amount;
            _currentFuel = Mathf.Min(_currentFuel, _maxFuel);
            GameEvents.OnFuelChanged?.Invoke(_currentFuel);
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

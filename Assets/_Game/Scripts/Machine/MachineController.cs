using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.UI;

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

        [Header("Weapon")]
        [SerializeField] private float _attackDamage = 20f;
        [SerializeField] private float _attackCooldown = 0.5f;
        [SerializeField] private float _attackRange = 3f;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsDead => _currentHealth <= 0f;

        public float CurrentFuel => _currentFuel;
        public float MaxFuel => _maxFuel;
        public bool IsFuelEmpty => _currentFuel <= 0f;

        public int TotalMined => _totalMined;

        // Weapon properties for AimController
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float AttackRange => _attackRange;

        public MachineData MachineData => _machineData;

        private bool _isSessionActive;

        // 디버그용
        private bool _isInvincible;
        public bool IsInvincible => _isInvincible;

        private void Awake()
        {
            ApplyMachineData();
        }

        private void Start()
        {
            InitializeSession();
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
            }
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
                DataManager.Instance?.AddCurrency(_totalMined);
                GameManager.Instance?.SessionSuccess();
            }
        }

        public void TakeDamage(float damage)
        {
            if (IsDead || !_isSessionActive) return;

            // 무적 상태면 데미지 무시
            if (_isInvincible)
            {
                DamagePopup.CreateText(transform, "INVINCIBLE", Color.cyan);
                return;
            }

            // Armor 적용
            float actualDamage = CalculateDamageReceived(damage);
            _currentHealth -= actualDamage;
            _currentHealth = Mathf.Max(0f, _currentHealth);

            // 데미지 팝업 표시 (콜라이더 크기 고려)
            DamagePopup.Create(transform, actualDamage, PopupType.Normal);

            GameEvents.OnMachineDamaged?.Invoke(actualDamage);
        }

        private float CalculateDamageReceived(float rawDamage)
        {
            if (_armor <= 0f) return rawDamage;
            float reduction = _armor / (_armor + 100f);
            return rawDamage * (1f - reduction);
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

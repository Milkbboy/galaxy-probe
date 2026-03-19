using UnityEngine;
using DrillCorp.Core;

namespace DrillCorp.Machine
{
    public class MachineController : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private float _maxHealth = 100f;
        private float _currentHealth;

        [Header("Fuel")]
        [SerializeField] private float _maxFuel = 60f;
        [SerializeField] private float _fuelConsumeRate = 1f;
        private float _currentFuel;

        [Header("Mining")]
        [SerializeField] private float _miningRate = 10f;  // 초당 채굴량
        private int _totalMined;
        private float _miningAccumulator;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsDead => _currentHealth <= 0f;

        public float CurrentFuel => _currentFuel;
        public float MaxFuel => _maxFuel;
        public bool IsFuelEmpty => _currentFuel <= 0f;

        public int TotalMined => _totalMined;

        private bool _isSessionActive;

        private void Start()
        {
            InitializeSession();
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

            _currentHealth -= damage;
            _currentHealth = Mathf.Max(0f, _currentHealth);
            GameEvents.OnMachineDamaged?.Invoke(damage);
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
    }
}

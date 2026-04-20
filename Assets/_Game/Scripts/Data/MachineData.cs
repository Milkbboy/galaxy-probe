using UnityEngine;

namespace DrillCorp.Data
{
    [CreateAssetMenu(fileName = "Machine_Default", menuName = "Drill-Corp/Machine Data", order = 3)]
    public class MachineData : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] private int _machineId;
        [SerializeField] private string _machineName;
        [SerializeField] private string _description;

        [Header("Health")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _healthRegen = 0f;
        [SerializeField] private float _armor = 0f;

        [Header("Mining")]
        [SerializeField] private float _miningRate = 10f;
        [SerializeField] private float _miningBonus = 0f;

        [Tooltip("v2 — 세션 승리를 위한 기본 채굴 목표량. MiningTarget 업그레이드로 증가.")]
        [Min(0f)]
        [SerializeField] private float _baseMiningTarget = 100f;

        [Header("Weapon")]
        [SerializeField] private float _attackDamage = 20f;
        [SerializeField] private float _attackCooldown = 0.5f;
        [SerializeField] private float _attackRange = 3f;
        [SerializeField] private float _critChance = 0f;
        [SerializeField] private float _critMultiplier = 1.5f;

        [Header("Visuals")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Sprite _icon;

        // Properties - Health
        public int MachineId => _machineId;
        public string MachineName => _machineName;
        public string Description => _description;
        public float MaxHealth => _maxHealth;
        public float HealthRegen => _healthRegen;
        public float Armor => _armor;

        // Properties - Mining
        public float MiningRate => _miningRate;
        public float MiningBonus => _miningBonus;
        public float TotalMiningRate => _miningRate * (1f + _miningBonus);
        public float BaseMiningTarget => _baseMiningTarget;

        // Properties - Weapon
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float AttackRange => _attackRange;
        public float CritChance => _critChance;
        public float CritMultiplier => _critMultiplier;

        // Properties - Visuals
        public GameObject Prefab => _prefab;
        public Sprite Icon => _icon;

        /// <summary>
        /// Calculate actual damage after armor reduction
        /// </summary>
        public float CalculateDamageReceived(float rawDamage)
        {
            // Armor reduces damage by percentage: 10 armor = 10% reduction
            float reduction = _armor / (_armor + 100f);
            return rawDamage * (1f - reduction);
        }
    }
}

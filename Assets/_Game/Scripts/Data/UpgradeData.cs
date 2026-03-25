using UnityEngine;

namespace DrillCorp.Data
{
    public enum UpgradeType
    {
        MaxHealth,
        Armor,
        HealthRegen,
        MaxFuel,
        FuelEfficiency,
        MiningRate,
        AttackDamage,
        AttackSpeed,
        AttackRange,
        CritChance,
        CritDamage
    }

    [CreateAssetMenu(fileName = "Upgrade_New", menuName = "Drill-Corp/Upgrade Data", order = 4)]
    public class UpgradeData : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] private string _upgradeId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Upgrade Settings")]
        [SerializeField] private UpgradeType _upgradeType;
        [SerializeField] private int _maxLevel = 10;
        [SerializeField] private float _baseValue = 0f;
        [SerializeField] private float _valuePerLevel = 1f;
        [SerializeField] private bool _isPercentage = false;

        [Header("Cost")]
        [SerializeField] private int _baseCost = 100;
        [SerializeField] private float _costMultiplier = 1.5f;

        // Properties
        public string UpgradeId => _upgradeId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public UpgradeType Type => _upgradeType;
        public int MaxLevel => _maxLevel;
        public float BaseValue => _baseValue;
        public float ValuePerLevel => _valuePerLevel;
        public bool IsPercentage => _isPercentage;
        public int BaseCost => _baseCost;
        public float CostMultiplier => _costMultiplier;

        /// <summary>
        /// 특정 레벨에서의 강화 값 계산
        /// </summary>
        public float GetValueAtLevel(int level)
        {
            return _baseValue + (_valuePerLevel * level);
        }

        /// <summary>
        /// 다음 레벨 업그레이드 비용 계산
        /// </summary>
        public int GetCostForLevel(int currentLevel)
        {
            if (currentLevel >= _maxLevel) return -1;
            return Mathf.RoundToInt(_baseCost * Mathf.Pow(_costMultiplier, currentLevel));
        }

        /// <summary>
        /// 표시용 값 문자열 반환
        /// </summary>
        public string GetValueString(int level)
        {
            float value = GetValueAtLevel(level);
            if (_isPercentage)
            {
                return $"+{value:F1}%";
            }
            return $"+{value:F1}";
        }
    }
}

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
        CritDamage,

        // === v2 신규 ===
        MiningTarget,       // 세션 승리 목표 채굴량 (+50/lv, Add)
        GemDropRate,        // 보석 드랍 확률 (+2%/lv, Add)
        GemCollectSpeed,    // 보석 채집 속도 (+20%/lv, Mul)
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

        [Header("Cost — Ore / Gem 이중 재화 (v2)")]
        [Tooltip("1레벨 광석 비용 (기존 BaseCost 대응)")]
        [SerializeField] private int _baseCost = 100;

        [Tooltip("1레벨 보석 비용 (보석 채집 스킬 등)")]
        [Min(0)]
        [SerializeField] private int _baseCostGem = 0;

        [SerializeField] private float _costMultiplier = 1.5f;

        [Tooltip("레벨별 광석 비용 명시 배열 (v2 핸드튜닝). 비어있으면 baseCost × multiplier^lv 사용")]
        [SerializeField] private int[] _oreCostSchedule;

        [Tooltip("레벨별 보석 비용 명시 배열 (v2 핸드튜닝). 비어있으면 baseCostGem × multiplier^lv 사용")]
        [SerializeField] private int[] _gemCostSchedule;

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
        public int BaseCost => _baseCost;                  // 레거시 호환 — Ore 비용
        public int BaseCostOre => _baseCost;               // v2 네이밍
        public int BaseCostGem => _baseCostGem;            // v2 신규
        public float CostMultiplier => _costMultiplier;

        /// <summary>
        /// 특정 레벨에서의 강화 값 계산
        /// </summary>
        public float GetValueAtLevel(int level)
        {
            return _baseValue + (_valuePerLevel * level);
        }

        /// <summary>
        /// 다음 레벨 광석 비용. MaxLevel이면 -1.
        /// schedule이 있으면 우선, 없으면 baseCost × multiplier^lv.
        /// </summary>
        public int GetCostForLevel(int currentLevel)
        {
            if (currentLevel >= _maxLevel) return -1;
            if (_oreCostSchedule != null && _oreCostSchedule.Length > currentLevel)
                return _oreCostSchedule[currentLevel];
            return Mathf.RoundToInt(_baseCost * Mathf.Pow(_costMultiplier, currentLevel));
        }

        /// <summary>
        /// 다음 레벨 보석 비용. schedule이 있으면 우선, 없고 BaseCostGem이 0이면 0.
        /// </summary>
        public int GetGemCostForLevel(int currentLevel)
        {
            if (currentLevel >= _maxLevel) return -1;
            if (_gemCostSchedule != null && _gemCostSchedule.Length > currentLevel)
                return _gemCostSchedule[currentLevel];
            if (_baseCostGem <= 0) return 0;
            return Mathf.RoundToInt(_baseCostGem * Mathf.Pow(_costMultiplier, currentLevel));
        }

        /// <summary>
        /// 이중 재화 비용을 한 번에 조회.
        /// </summary>
        public (int ore, int gem) GetCostsForLevel(int currentLevel)
        {
            if (currentLevel >= _maxLevel) return (-1, -1);
            return (GetCostForLevel(currentLevel), GetGemCostForLevel(currentLevel));
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

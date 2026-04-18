using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrillCorp.Data
{
    /// <summary>
    /// 무기 강화가 수정하는 스탯.
    /// TargetStat별로 WeaponBase가 EffectiveXxx 프로퍼티에서 보너스를 조회.
    /// </summary>
    public enum WeaponUpgradeStat
    {
        Damage,
        Range,
        Cooldown,       // ValuePerLevel을 음수로 두어 감소 처리
        AmmoBonus,      // Add (정수 탄환 수)
        ReloadTime,     // Mul (ValuePerLevel 음수)
        Radius,         // 폭탄 반경, 회전톱날 블레이드 반경
        SlowBonus,      // Add (슬로우 강도 %p)
    }

    public enum WeaponUpgradeOp
    {
        /// <summary>덧셈 — 최종값 = base + ValuePerLevel × level</summary>
        Add,
        /// <summary>곱셈 — 최종값 = base × (1 + ValuePerLevel × level)</summary>
        Multiply,
    }

    /// <summary>
    /// 레벨↔값 직렬화 헬퍼 (JsonUtility가 Dictionary 미지원).
    /// 재사용 가능 (ExcavatorUpgradeLevels 등).
    /// </summary>
    [Serializable]
    public class LevelEntry
    {
        public string Id;
        public int Level;
    }

    [Serializable]
    public struct WeaponUpgradeCostTuple
    {
        public int Ore;
        public int Gem;
    }

    /// <summary>
    /// 무기별 강화 항목 SO.
    /// v2 통합 가이드: docs/WeaponUnlockUpgradeSystem.md §4
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponUpgrade_New", menuName = "Drill-Corp/Weapon Upgrade Data", order = 7)]
    public class WeaponUpgradeData : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("고유 ID (예: 'sniper_dmg')")]
        [SerializeField] private string _upgradeId;

        [Tooltip("대상 무기 ID (WeaponData.WeaponId와 일치)")]
        [SerializeField] private string _weaponId;

        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

        [Header("Effect")]
        [SerializeField] private WeaponUpgradeStat _targetStat;

        [Min(1)]
        [SerializeField] private int _maxLevel = 5;

        [Tooltip("레벨당 증가량. Cooldown·ReloadTime 감소 강화는 음수로 입력 (예: -0.20)")]
        [SerializeField] private float _valuePerLevel = 0.25f;

        [SerializeField] private bool _isPercentage = true;

        [SerializeField] private WeaponUpgradeOp _operation = WeaponUpgradeOp.Multiply;

        [Header("Cost — 공식 방식 (ManualCosts가 비어있을 때 사용)")]
        [Min(0)]
        [SerializeField] private int _baseCostOre = 40;

        [Min(0)]
        [SerializeField] private int _baseCostGem = 2;

        [Min(1f)]
        [SerializeField] private float _oreCostMultiplier = 2f;

        [Min(1f)]
        [SerializeField] private float _gemCostMultiplier = 2f;

        [Header("Cost — 수동 방식 (레벨별 명시. 채워두면 공식 무시)")]
        [Tooltip("배열 길이가 MaxLevel과 같아야 완전 덮어쓰기. 짧으면 그 다음 레벨부터 공식 사용.")]
        [SerializeField] private List<WeaponUpgradeCostTuple> _manualCosts = new List<WeaponUpgradeCostTuple>();

        // ─── Properties ───
        public string UpgradeId => _upgradeId;
        public string WeaponId => _weaponId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public WeaponUpgradeStat TargetStat => _targetStat;
        public int MaxLevel => _maxLevel;
        public float ValuePerLevel => _valuePerLevel;
        public bool IsPercentage => _isPercentage;
        public WeaponUpgradeOp Operation => _operation;

        /// <summary>다음 레벨 비용 (level = 현재 레벨). 최대 레벨이면 (0, 0).</summary>
        public (int ore, int gem) GetCostForLevel(int level)
        {
            if (level >= _maxLevel) return (0, 0);

            // 수동 비용이 해당 레벨까지 정의되어 있으면 사용
            if (_manualCosts != null && level < _manualCosts.Count)
            {
                var m = _manualCosts[level];
                return (m.Ore, m.Gem);
            }

            int ore = Mathf.RoundToInt(_baseCostOre * Mathf.Pow(_oreCostMultiplier, level));
            int gem = Mathf.RoundToInt(_baseCostGem * Mathf.Pow(_gemCostMultiplier, level));
            return (ore, gem);
        }

        /// <summary>레벨만큼 적용된 보너스 — (add, mul). WeaponBase가 최종 스탯 계산에 사용.</summary>
        public (float add, float mul) GetBonusAtLevel(int level)
        {
            if (level <= 0) return (0f, 1f);

            if (_operation == WeaponUpgradeOp.Add)
                return (_valuePerLevel * level, 1f);

            return (0f, 1f + _valuePerLevel * level);
        }

        /// <summary>UI 표시용 — "+25% / Lv.3" 등</summary>
        public string GetValueString(int level)
        {
            float val = _valuePerLevel * level;
            string sign = val >= 0 ? "+" : "";
            return _isPercentage
                ? $"{sign}{val * 100f:F0}%"
                : $"{sign}{val:F1}";
        }
    }
}

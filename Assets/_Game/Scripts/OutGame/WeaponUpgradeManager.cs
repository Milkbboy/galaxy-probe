using System;
using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Data;

namespace DrillCorp.OutGame
{
    /// <summary>
    /// 무기별 강화 레벨 관리 (기존 UpgradeManager와 동일한 PlayerPrefs 패턴).
    /// WeaponBase는 런타임에 GetBonus(weaponId, stat)로 보너스 조회.
    /// v2 가이드: docs/Sys-Weapon.md §4, §5
    /// </summary>
    public class WeaponUpgradeManager : MonoBehaviour
    {
        public static WeaponUpgradeManager Instance { get; private set; }

        [Header("All Weapon Upgrades")]
        [SerializeField] private List<WeaponUpgradeData> _allUpgrades = new List<WeaponUpgradeData>();

        private Dictionary<string, int> _levels = new Dictionary<string, int>();
        private Dictionary<string, WeaponUpgradeData> _byId = new Dictionary<string, WeaponUpgradeData>();

        private const string SaveKey = "WeaponUpgrades";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            IndexUpgrades();
            LoadLevels();
        }

        private void IndexUpgrades()
        {
            _byId.Clear();
            foreach (var u in _allUpgrades)
            {
                if (u == null) continue;
                if (string.IsNullOrEmpty(u.UpgradeId)) continue;
                _byId[u.UpgradeId] = u;
                if (!_levels.ContainsKey(u.UpgradeId)) _levels[u.UpgradeId] = 0;
            }
        }

        // ═══════════════════════════════════════════════════
        // 조회
        // ═══════════════════════════════════════════════════
        public int GetLevel(string upgradeId)
            => _levels.TryGetValue(upgradeId, out var lv) ? lv : 0;

        public WeaponUpgradeData FindUpgrade(string upgradeId)
            => _byId.TryGetValue(upgradeId, out var u) ? u : null;

        public IEnumerable<WeaponUpgradeData> GetUpgradesFor(string weaponId)
        {
            foreach (var u in _allUpgrades)
                if (u != null && u.WeaponId == weaponId) yield return u;
        }

        public (int ore, int gem) GetNextCost(WeaponUpgradeData u)
        {
            if (u == null) return (0, 0);
            int lv = GetLevel(u.UpgradeId);
            if (lv >= u.MaxLevel) return (0, 0);
            return u.GetCostForLevel(lv);
        }

        public bool IsMaxed(WeaponUpgradeData u)
            => u != null && GetLevel(u.UpgradeId) >= u.MaxLevel;

        public bool CanAfford(WeaponUpgradeData u)
        {
            var (ore, gem) = GetNextCost(u);
            var dm = DataManager.Instance;
            return dm != null && dm.Ore >= ore && dm.Gems >= gem;
        }

        /// <summary>
        /// 지정 무기·스탯의 누적 보너스 반환.
        /// 계산: final = (base + add) * mul
        /// </summary>
        public (float add, float mul) GetBonus(string weaponId, WeaponUpgradeStat stat)
        {
            float add = 0f, mul = 1f;
            foreach (var u in _allUpgrades)
            {
                if (u == null) continue;
                if (u.WeaponId != weaponId) continue;
                if (u.TargetStat != stat) continue;

                int lv = GetLevel(u.UpgradeId);
                if (lv <= 0) continue;

                var (a, m) = u.GetBonusAtLevel(lv);
                add += a;
                mul *= m;
            }
            return (add, mul);
        }

        // ═══════════════════════════════════════════════════
        // 구매
        // ═══════════════════════════════════════════════════
        public bool TryBuy(WeaponUpgradeData u)
        {
            if (u == null) return false;
            if (IsMaxed(u)) return false;

            var (ore, gem) = GetNextCost(u);
            var dm = DataManager.Instance;
            if (dm == null) return false;
            if (dm.Ore < ore || dm.Gems < gem) return false;

            if (!dm.SpendOre(ore)) return false;
            if (gem > 0 && !dm.SpendGems(gem))
            {
                // 광석 환불 (드문 실패)
                dm.AddOre(ore);
                return false;
            }

            _levels[u.UpgradeId] = GetLevel(u.UpgradeId) + 1;
            SaveLevels();

            GameEvents.OnWeaponUpgraded?.Invoke(u.UpgradeId);
            return true;
        }

        // ═══════════════════════════════════════════════════
        // 영속
        // ═══════════════════════════════════════════════════
        [Serializable]
        private class LevelEntry
        {
            public string Id;
            public int Level;
        }

        [Serializable]
        private class LevelEntryList
        {
            public List<LevelEntry> Entries = new List<LevelEntry>();
        }

        private void SaveLevels()
        {
            var list = new LevelEntryList();
            foreach (var kv in _levels)
                list.Entries.Add(new LevelEntry { Id = kv.Key, Level = kv.Value });

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(list));
            PlayerPrefs.Save();
        }

        private void LoadLevels()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            var list = JsonUtility.FromJson<LevelEntryList>(PlayerPrefs.GetString(SaveKey));
            if (list?.Entries == null) return;
            foreach (var e in list.Entries)
                _levels[e.Id] = e.Level;
        }

        public void ResetAll()
        {
            foreach (var key in new List<string>(_levels.Keys))
                _levels[key] = 0;
            SaveLevels();
            GameEvents.OnWeaponUpgraded?.Invoke(string.Empty); // UI 갱신 트리거
        }
    }
}

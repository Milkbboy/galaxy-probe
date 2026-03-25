using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Data;

namespace DrillCorp.OutGame
{
    [System.Serializable]
    public class UpgradeState
    {
        public string UpgradeId;
        public int Level;
    }

    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("Upgrade Data")]
        [SerializeField] private List<UpgradeData> _availableUpgrades = new List<UpgradeData>();

        private Dictionary<string, int> _upgradeLevels = new Dictionary<string, int>();
        private Dictionary<string, UpgradeData> _upgradeDataDict = new Dictionary<string, UpgradeData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeUpgrades();
            LoadUpgrades();
        }

        private void InitializeUpgrades()
        {
            foreach (var upgrade in _availableUpgrades)
            {
                if (upgrade != null)
                {
                    _upgradeDataDict[upgrade.UpgradeId] = upgrade;
                    if (!_upgradeLevels.ContainsKey(upgrade.UpgradeId))
                    {
                        _upgradeLevels[upgrade.UpgradeId] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// 업그레이드 시도
        /// </summary>
        public bool TryUpgrade(string upgradeId)
        {
            if (!_upgradeDataDict.TryGetValue(upgradeId, out var data))
            {
                Debug.LogWarning($"[UpgradeManager] Unknown upgrade: {upgradeId}");
                return false;
            }

            int currentLevel = GetUpgradeLevel(upgradeId);
            if (currentLevel >= data.MaxLevel)
            {
                Debug.Log($"[UpgradeManager] {upgradeId} is already max level");
                return false;
            }

            int cost = data.GetCostForLevel(currentLevel);
            if (DataManager.Instance == null || DataManager.Instance.Currency < cost)
            {
                Debug.Log($"[UpgradeManager] Not enough currency for {upgradeId}");
                return false;
            }

            // 비용 차감 및 레벨 증가
            DataManager.Instance.SpendCurrency(cost);
            _upgradeLevels[upgradeId] = currentLevel + 1;
            SaveUpgrades();

            GameEvents.OnUpgradePurchased?.Invoke(upgradeId, currentLevel + 1);
            Debug.Log($"[UpgradeManager] Upgraded {upgradeId} to level {currentLevel + 1}");

            return true;
        }

        /// <summary>
        /// 업그레이드 레벨 조회
        /// </summary>
        public int GetUpgradeLevel(string upgradeId)
        {
            return _upgradeLevels.TryGetValue(upgradeId, out int level) ? level : 0;
        }

        /// <summary>
        /// 업그레이드 타입별 총 보너스 값 계산
        /// </summary>
        public float GetTotalBonus(UpgradeType type)
        {
            float total = 0f;
            foreach (var upgrade in _availableUpgrades)
            {
                if (upgrade != null && upgrade.Type == type)
                {
                    int level = GetUpgradeLevel(upgrade.UpgradeId);
                    total += upgrade.GetValueAtLevel(level);
                }
            }
            return total;
        }

        /// <summary>
        /// 특정 업그레이드의 현재 값
        /// </summary>
        public float GetUpgradeValue(string upgradeId)
        {
            if (!_upgradeDataDict.TryGetValue(upgradeId, out var data))
                return 0f;

            int level = GetUpgradeLevel(upgradeId);
            return data.GetValueAtLevel(level);
        }

        /// <summary>
        /// 업그레이드 데이터 조회
        /// </summary>
        public UpgradeData GetUpgradeData(string upgradeId)
        {
            return _upgradeDataDict.TryGetValue(upgradeId, out var data) ? data : null;
        }

        /// <summary>
        /// 모든 업그레이드 데이터 반환
        /// </summary>
        public List<UpgradeData> GetAllUpgrades()
        {
            return _availableUpgrades;
        }

        /// <summary>
        /// 업그레이드 가능 여부 확인
        /// </summary>
        public bool CanUpgrade(string upgradeId)
        {
            if (!_upgradeDataDict.TryGetValue(upgradeId, out var data))
                return false;

            int currentLevel = GetUpgradeLevel(upgradeId);
            if (currentLevel >= data.MaxLevel)
                return false;

            int cost = data.GetCostForLevel(currentLevel);
            return DataManager.Instance != null && DataManager.Instance.Currency >= cost;
        }

        private void SaveUpgrades()
        {
            var states = new List<UpgradeState>();
            foreach (var kvp in _upgradeLevels)
            {
                states.Add(new UpgradeState { UpgradeId = kvp.Key, Level = kvp.Value });
            }

            string json = JsonUtility.ToJson(new UpgradeStateList { States = states });
            PlayerPrefs.SetString("Upgrades", json);
            PlayerPrefs.Save();
        }

        private void LoadUpgrades()
        {
            if (PlayerPrefs.HasKey("Upgrades"))
            {
                string json = PlayerPrefs.GetString("Upgrades");
                var stateList = JsonUtility.FromJson<UpgradeStateList>(json);
                if (stateList?.States != null)
                {
                    foreach (var state in stateList.States)
                    {
                        _upgradeLevels[state.UpgradeId] = state.Level;
                    }
                }
            }
        }

        /// <summary>
        /// 모든 업그레이드 초기화 (디버그용)
        /// </summary>
        public void ResetAllUpgrades()
        {
            foreach (var key in new List<string>(_upgradeLevels.Keys))
            {
                _upgradeLevels[key] = 0;
            }
            SaveUpgrades();
        }

        [System.Serializable]
        private class UpgradeStateList
        {
            public List<UpgradeState> States;
        }
    }
}

using System;
using System.IO;
using UnityEngine;

namespace DrillCorp.Core
{
    [Serializable]
    public class PlayerData
    {
        public int Currency;
        public int MachineLevel;
        public int TotalSessionsPlayed;
        public int TotalBugsKilled;
    }

    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        public PlayerData Data { get; private set; }

        private string _savePath;
        private const string SaveFileName = "playerdata.json";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            Load();
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Data, true);
                File.WriteAllText(_savePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataManager] Save failed: {e.Message}");
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    string json = File.ReadAllText(_savePath);
                    Data = JsonUtility.FromJson<PlayerData>(json);
                }
                else
                {
                    Data = new PlayerData();
                    Save();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataManager] Load failed: {e.Message}");
                Data = new PlayerData();
            }
        }

        public void AddCurrency(int amount)
        {
            Data.Currency += amount;
            GameEvents.OnCurrencyChanged?.Invoke(Data.Currency);
            Save();
        }

        public bool SpendCurrency(int amount)
        {
            if (Data.Currency < amount) return false;

            Data.Currency -= amount;
            GameEvents.OnCurrencyChanged?.Invoke(Data.Currency);
            Save();
            return true;
        }

        public void IncrementSessionsPlayed()
        {
            Data.TotalSessionsPlayed++;
            Save();
        }

        public void AddBugsKilled(int count)
        {
            Data.TotalBugsKilled += count;
            Save();
        }

        public void ResetData()
        {
            Data = new PlayerData();
            Save();
        }
    }
}

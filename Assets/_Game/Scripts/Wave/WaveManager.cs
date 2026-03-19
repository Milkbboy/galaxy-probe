using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Bug;
using DrillCorp.Data;

namespace DrillCorp.Wave
{
    [System.Serializable]
    public class WaveEntry
    {
        public BugType BugType;
        public int Count;
        public float SpawnDelay = 0.5f;
    }

    [System.Serializable]
    public class WaveConfig
    {
        public string WaveName;
        public List<WaveEntry> Entries = new List<WaveEntry>();
        public float DelayBeforeNextWave = 3f;
    }

    public class WaveManager : MonoBehaviour
    {
        [Header("Wave Data (ScriptableObject)")]
        [SerializeField] private List<Data.WaveData> _waveDataAssets = new List<Data.WaveData>();

        [Header("Wave Settings (Legacy)")]
        [SerializeField] private List<WaveConfig> _waves = new List<WaveConfig>();
        [SerializeField] private bool _autoStartFirstWave = true;
        [SerializeField] private bool _useScriptableObjects = true;

        [Header("References")]
        [SerializeField] private BugSpawner _bugSpawner;

        private int _currentWaveIndex = -1;
        private int _remainingBugs;
        private bool _isWaveActive;
        private Coroutine _waveCoroutine;

        public int CurrentWave => _currentWaveIndex + 1;
        public int TotalWaves => _useScriptableObjects ? _waveDataAssets.Count : _waves.Count;
        public bool IsWaveActive => _isWaveActive;
        public int RemainingBugs => _remainingBugs;

        // Current wave multipliers (from ScriptableObject)
        private float _currentHealthMult = 1f;
        private float _currentDamageMult = 1f;
        private float _currentSpeedMult = 1f;

        private void Start()
        {
            GameEvents.OnBugKilled += OnBugKilled;

            if (_autoStartFirstWave)
            {
                StartNextWave();
            }
        }

        private void OnDestroy()
        {
            GameEvents.OnBugKilled -= OnBugKilled;
        }

        public void StartNextWave()
        {
            if (_isWaveActive) return;

            _currentWaveIndex++;

            int totalWaves = _useScriptableObjects ? _waveDataAssets.Count : _waves.Count;
            if (_currentWaveIndex >= totalWaves)
            {
                Debug.Log("[WaveManager] All waves completed");
                return;
            }

            if (_useScriptableObjects && _waveDataAssets.Count > 0)
            {
                _waveCoroutine = StartCoroutine(SpawnWaveFromDataCoroutine(_waveDataAssets[_currentWaveIndex]));
            }
            else
            {
                _waveCoroutine = StartCoroutine(SpawnWaveCoroutine(_waves[_currentWaveIndex]));
            }
        }

        private IEnumerator SpawnWaveFromDataCoroutine(Data.WaveData waveData)
        {
            _isWaveActive = true;
            _remainingBugs = waveData.TotalBugCount;

            // Store multipliers
            _currentHealthMult = waveData.HealthMultiplier;
            _currentDamageMult = waveData.DamageMultiplier;
            _currentSpeedMult = waveData.SpeedMultiplier;

            GameEvents.OnWaveStarted?.Invoke(_currentWaveIndex + 1);

            // Spawn each group
            foreach (var group in waveData.SpawnGroups)
            {
                if (group.BugData == null) continue;

                yield return new WaitForSeconds(group.StartDelay);

                for (int i = 0; i < group.Count; i++)
                {
                    _bugSpawner.SpawnBugFromData(group.BugData, _currentHealthMult, _currentDamageMult, _currentSpeedMult);
                    yield return new WaitForSeconds(group.SpawnInterval);
                }
            }

            // Wait until all bugs are killed
            while (_remainingBugs > 0)
            {
                yield return null;
            }

            _isWaveActive = false;
            GameEvents.OnWaveCompleted?.Invoke(_currentWaveIndex + 1);

            // Auto-start next wave
            if (_currentWaveIndex + 1 < _waveDataAssets.Count)
            {
                yield return new WaitForSeconds(waveData.DelayBeforeNextWave);
                StartNextWave();
            }
        }

        private IEnumerator SpawnWaveCoroutine(WaveConfig wave)
        {
            _isWaveActive = true;
            _remainingBugs = 0;

            foreach (var entry in wave.Entries)
            {
                _remainingBugs += entry.Count;
            }

            GameEvents.OnWaveStarted?.Invoke(_currentWaveIndex + 1);

            foreach (var entry in wave.Entries)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    _bugSpawner.SpawnBug(entry.BugType);
                    yield return new WaitForSeconds(entry.SpawnDelay);
                }
            }

            // Wait until all bugs are killed
            while (_remainingBugs > 0)
            {
                yield return null;
            }

            _isWaveActive = false;
            GameEvents.OnWaveCompleted?.Invoke(_currentWaveIndex + 1);

            // Auto-start next wave
            if (_currentWaveIndex + 1 < _waves.Count)
            {
                yield return new WaitForSeconds(wave.DelayBeforeNextWave);
                StartNextWave();
            }
        }

        private void OnBugKilled(int bugId)
        {
            if (_isWaveActive)
            {
                _remainingBugs--;
                _remainingBugs = Mathf.Max(0, _remainingBugs);
            }
        }

        public void StopWave()
        {
            if (_waveCoroutine != null)
            {
                StopCoroutine(_waveCoroutine);
                _waveCoroutine = null;
            }
            _isWaveActive = false;
        }

        public void ResetWaves()
        {
            StopWave();
            _currentWaveIndex = -1;
            _remainingBugs = 0;
        }
    }
}

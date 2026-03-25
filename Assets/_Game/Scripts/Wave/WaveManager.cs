using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Bug;
using DrillCorp.Data;

namespace DrillCorp.Wave
{
    public class WaveManager : MonoBehaviour
    {
        [Header("Wave Data")]
        [SerializeField] private List<Data.WaveData> _waveDataAssets = new List<Data.WaveData>();
        [SerializeField] private bool _autoStartFirstWave = true;

        [Header("References")]
        [SerializeField] private BugSpawner _bugSpawner;

        private int _currentWaveIndex = -1;
        private int _remainingBugs;
        private bool _isWaveActive;
        private Coroutine _waveCoroutine;

        public int CurrentWave => _currentWaveIndex + 1;
        public int TotalWaves => _waveDataAssets.Count;
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

            if (_currentWaveIndex >= _waveDataAssets.Count)
            {
                Debug.Log("[WaveManager] All waves completed");
                return;
            }

            _waveCoroutine = StartCoroutine(SpawnWaveCoroutine(_waveDataAssets[_currentWaveIndex]));
        }

        private IEnumerator SpawnWaveCoroutine(Data.WaveData waveData)
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

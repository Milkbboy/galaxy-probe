using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Bug;

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
    public class WaveData
    {
        public string WaveName;
        public List<WaveEntry> Entries = new List<WaveEntry>();
        public float DelayBeforeNextWave = 3f;
    }

    public class WaveManager : MonoBehaviour
    {
        [Header("Wave Settings")]
        [SerializeField] private List<WaveData> _waves = new List<WaveData>();
        [SerializeField] private bool _autoStartFirstWave = true;

        [Header("References")]
        [SerializeField] private BugSpawner _bugSpawner;

        private int _currentWaveIndex = -1;
        private int _remainingBugs;
        private bool _isWaveActive;
        private Coroutine _waveCoroutine;

        public int CurrentWave => _currentWaveIndex + 1;
        public int TotalWaves => _waves.Count;
        public bool IsWaveActive => _isWaveActive;
        public int RemainingBugs => _remainingBugs;

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

            if (_currentWaveIndex >= _waves.Count)
            {
                Debug.Log("[WaveManager] All waves completed");
                return;
            }

            _waveCoroutine = StartCoroutine(SpawnWaveCoroutine(_waves[_currentWaveIndex]));
        }

        private IEnumerator SpawnWaveCoroutine(WaveData wave)
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

            // 모든 벌레가 처치될 때까지 대기
            while (_remainingBugs > 0)
            {
                yield return null;
            }

            _isWaveActive = false;
            GameEvents.OnWaveCompleted?.Invoke(_currentWaveIndex + 1);

            // 다음 웨이브 자동 시작
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

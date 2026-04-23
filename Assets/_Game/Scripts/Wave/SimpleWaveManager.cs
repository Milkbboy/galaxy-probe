using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Bug.Simple;
using DrillCorp.Data;

namespace DrillCorp.Wave
{
    /// <summary>
    /// SimpleBugSpawner + TunnelEventManager에 웨이브별 파라미터를 주입하는 매니저.
    /// 전환 트리거: 벌레 처치 점수 누적 (GameEvents.OnBugScoreEarned)이 KillTarget 도달 시.
    /// 세션은 채굴 완료/머신 HP 0로만 종료되므로, 마지막 웨이브(KillTarget<=0)는 전환 없이 유지.
    /// </summary>
    public class SimpleWaveManager : MonoBehaviour
    {
        [Header("Wave Data")]
        [Tooltip("WaveNumber 오름차순으로 넣을 것")]
        [SerializeField] private List<SimpleWaveData> _waves = new();

        [Header("Config")]
        [SerializeField] private SpawnConfigData _spawnConfig;

        [Header("Targets")]
        [SerializeField] private SimpleBugSpawner _spawner;
        [SerializeField] private TunnelEventManager _tunnel;

        [Header("Controls")]
        [SerializeField] private bool _autoStart = true;

        private int _currentIndex = -1;
        private float _waveScoreAccum;
        private bool _waveActive;

        public int CurrentWaveNumber => _currentIndex >= 0 && _currentIndex < _waves.Count ? _waves[_currentIndex].WaveNumber : 0;
        public float CurrentWaveScore => _waveScoreAccum;
        public float CurrentKillTarget => _currentIndex >= 0 && _currentIndex < _waves.Count ? _waves[_currentIndex].KillTarget : 0f;
        public bool IsLastWave => _currentIndex == _waves.Count - 1;

        private void OnEnable()
        {
            GameEvents.OnBugScoreEarned += OnBugScoreEarned;
        }

        private void OnDisable()
        {
            GameEvents.OnBugScoreEarned -= OnBugScoreEarned;
        }

        private void Start()
        {
            if (_autoStart) StartWave(0);
        }

        /// <summary>웨이브 인덱스로 진입. 인덱스 = _waves 리스트 위치 (0부터).</summary>
        public void StartWave(int index)
        {
            if (_waves == null || _waves.Count == 0)
            {
                Debug.LogWarning("[SimpleWaveManager] _waves 리스트가 비어있음");
                return;
            }
            if (_spawnConfig == null)
            {
                Debug.LogError("[SimpleWaveManager] _spawnConfig가 비어있음 — 주입 불가");
                return;
            }
            if (_spawner == null && _tunnel == null)
            {
                Debug.LogError("[SimpleWaveManager] Spawner/Tunnel 양쪽 다 null");
                return;
            }

            index = Mathf.Clamp(index, 0, _waves.Count - 1);
            _currentIndex = index;
            var wave = _waves[_currentIndex];

            if (_spawner != null) _spawner.Configure(wave, _spawnConfig);
            if (_tunnel != null) _tunnel.Configure(wave, _spawnConfig);

            _waveScoreAccum = 0f;
            _waveActive = true;

            GameEvents.OnWaveStarted?.Invoke(wave.WaveNumber);
        }

        public void StartNextWave()
        {
            if (_currentIndex < 0) StartWave(0);
            else if (_currentIndex + 1 < _waves.Count) StartWave(_currentIndex + 1);
            // 더 진행할 웨이브 없으면 현재 웨이브 유지 (스폰은 계속)
        }

        private void OnBugScoreEarned(float score)
        {
            if (!_waveActive || _currentIndex < 0) return;
            if (_currentIndex >= _waves.Count) return;

            var wave = _waves[_currentIndex];
            _waveScoreAccum += score;

            // KillTarget <= 0 은 "전환 없음" — 세션 끝까지 현재 웨이브 유지
            if (wave.KillTarget <= 0f) return;

            if (_waveScoreAccum >= wave.KillTarget)
            {
                GameEvents.OnWaveCompleted?.Invoke(wave.WaveNumber);
                if (_currentIndex + 1 < _waves.Count)
                {
                    StartWave(_currentIndex + 1);
                }
                else
                {
                    // 마지막 웨이브에서 타겟 달성 — 파라미터는 유지, 더 이상 전환 이벤트 없음
                    _waveActive = false;
                }
            }
        }

        public void StopWave()
        {
            _waveActive = false;
        }

        public void ResetWaves()
        {
            _currentIndex = -1;
            _waveScoreAccum = 0f;
            _waveActive = false;
        }
    }
}

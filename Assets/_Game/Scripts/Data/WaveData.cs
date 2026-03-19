using UnityEngine;
using System;

namespace DrillCorp.Data
{
    [CreateAssetMenu(fileName = "Wave_01", menuName = "Drill-Corp/Wave Data", order = 2)]
    public class WaveData : ScriptableObject
    {
        [Header("Wave Info")]
        [SerializeField] private int _waveNumber;
        [SerializeField] private string _waveName;

        [Header("Spawn Settings")]
        [SerializeField] private SpawnGroup[] _spawnGroups;
        [SerializeField] private float _waveDuration = 60f;
        [SerializeField] private float _delayBeforeNextWave = 3f;

        [Header("Difficulty Scaling")]
        [SerializeField] private float _healthMultiplier = 1f;
        [SerializeField] private float _damageMultiplier = 1f;
        [SerializeField] private float _speedMultiplier = 1f;

        // Properties
        public int WaveNumber => _waveNumber;
        public string WaveName => _waveName;
        public SpawnGroup[] SpawnGroups => _spawnGroups;
        public float WaveDuration => _waveDuration;
        public float DelayBeforeNextWave => _delayBeforeNextWave;
        public float HealthMultiplier => _healthMultiplier;
        public float DamageMultiplier => _damageMultiplier;
        public float SpeedMultiplier => _speedMultiplier;

        public int TotalBugCount
        {
            get
            {
                int total = 0;
                if (_spawnGroups != null)
                {
                    foreach (var group in _spawnGroups)
                    {
                        total += group.Count;
                    }
                }
                return total;
            }
        }
    }

    [Serializable]
    public class SpawnGroup
    {
        [Tooltip("Bug Data to spawn")]
        public BugData BugData;

        [Tooltip("Number of bugs to spawn")]
        public int Count = 5;

        [Tooltip("Delay before spawning this group")]
        public float StartDelay = 0f;

        [Tooltip("Interval between each spawn")]
        public float SpawnInterval = 1f;

        [Tooltip("Spawn in random positions or sequential")]
        public bool RandomPosition = true;
    }
}

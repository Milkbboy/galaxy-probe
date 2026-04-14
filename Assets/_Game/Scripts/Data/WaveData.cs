using UnityEngine;
using System;
using DrillCorp.Bug.Formation;

namespace DrillCorp.Data
{
    [CreateAssetMenu(fileName = "Wave_01", menuName = "Drill-Corp/Wave Data", order = 2)]
    public class WaveData : ScriptableObject
    {
        [Header("Wave Info")]
        [SerializeField] private int _waveNumber;
        [SerializeField] private string _waveName;

        [Header("Spawn Settings - 개별 스폰 (레거시)")]
        [SerializeField] private SpawnGroup[] _spawnGroups;

        [Header("Spawn Settings - Formation 스폰")]
        [Tooltip("Formation(군집) 단위 스폰 설정")]
        [SerializeField] private FormationSpawnEntry[] _formationSpawns;

        [Header("Timing")]
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
        public FormationSpawnEntry[] FormationSpawns => _formationSpawns;
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
                        total += group.Count;
                }
                if (_formationSpawns != null)
                {
                    foreach (var entry in _formationSpawns)
                    {
                        if (entry.FormationData == null) continue;
                        int avgMembers = (entry.FormationData.MinMembers + entry.FormationData.MaxMembers) / 2 + 1;
                        total += avgMembers * entry.Count;
                    }
                }
                return total;
            }
        }

        public bool HasFormationSpawns => _formationSpawns != null && _formationSpawns.Length > 0;
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

    [Serializable]
    public class FormationSpawnEntry
    {
        [Tooltip("Formation Data (진형 설정)")]
        public FormationData FormationData;

        [Tooltip("이 Formation을 몇 번 스폰할지")]
        public int Count = 1;

        [Tooltip("웨이브 시작 후 첫 스폰까지 대기 시간 (초)")]
        public float StartDelay = 0f;

        [Tooltip("Formation 간 스폰 간격 (초)")]
        public float SpawnInterval = 5f;
    }
}

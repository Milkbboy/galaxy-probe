using System.Collections.Generic;
using UnityEngine;

namespace DrillCorp.Bug
{
    public enum BugType
    {
        Beetle,
        Fly,
        Centipede
    }

    [System.Serializable]
    public class BugSpawnData
    {
        public BugType Type;
        public GameObject Prefab;
        public float Health = 10f;
        public float Speed = 2f;
        public float Damage = 5f;
    }

    public class BugSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float _spawnRadius = 10f;
        [SerializeField] private Transform _centerPoint;

        [Header("Bug Prefabs")]
        [SerializeField] private List<BugSpawnData> _bugDataList = new List<BugSpawnData>();

        private Dictionary<BugType, BugSpawnData> _bugDataDict = new Dictionary<BugType, BugSpawnData>();

        private void Awake()
        {
            foreach (var data in _bugDataList)
            {
                _bugDataDict[data.Type] = data;
            }
        }

        public BugBase SpawnBug(BugType type)
        {
            if (!_bugDataDict.TryGetValue(type, out var data) || data.Prefab == null)
            {
                Debug.LogWarning($"[BugSpawner] Bug type {type} not configured");
                return null;
            }

            Vector3 spawnPosition = GetRandomSpawnPosition();
            GameObject bugObj = Instantiate(data.Prefab, spawnPosition, Quaternion.identity);

            var bug = bugObj.GetComponent<BugBase>();
            if (bug != null)
            {
                bug.Initialize((int)type, data.Health, data.Speed, data.Damage);
            }

            return bug;
        }

        public void SpawnBugs(BugType type, int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnBug(type);
            }
        }

        public void SpawnWave(List<(BugType type, int count)> waveData)
        {
            foreach (var (type, count) in waveData)
            {
                SpawnBugs(type, count);
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 center = _centerPoint != null ? _centerPoint.position : Vector3.zero;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(angle) * _spawnRadius;
            float z = center.z + Mathf.Sin(angle) * _spawnRadius;

            return new Vector3(x, 0f, z);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = _centerPoint != null ? _centerPoint.position : transform.position;

            Gizmos.color = Color.red;
            // XZ 평면에 원 그리기
            int segments = 64;
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(_spawnRadius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * _spawnRadius, 0f, Mathf.Sin(angle) * _spawnRadius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}

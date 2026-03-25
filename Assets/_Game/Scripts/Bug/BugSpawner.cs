using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Bug
{
    public class BugSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float _spawnRadius = 10f;
        [SerializeField] private Transform _centerPoint;

        public BugBase SpawnBugFromData(BugData bugData, float healthMult = 1f, float damageMult = 1f, float speedMult = 1f)
        {
            if (bugData == null)
            {
                Debug.LogWarning("[BugSpawner] BugData is null");
                return null;
            }

            GameObject prefab = bugData.Prefab;

            if (prefab == null)
            {
                Debug.LogWarning($"[BugSpawner] No prefab for bug: {bugData.BugName}");
                return null;
            }

            Vector3 spawnPosition = GetRandomSpawnPosition();
            GameObject bugObj = Instantiate(prefab, spawnPosition, Quaternion.identity);

            var bug = bugObj.GetComponent<BugBase>();
            if (bug != null)
            {
                bug.Initialize(bugData, healthMult, damageMult, speedMult);
            }

            return bug;
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

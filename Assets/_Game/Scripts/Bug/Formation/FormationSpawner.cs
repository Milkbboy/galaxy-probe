using UnityEngine;
using DrillCorp.Bug.Pool;
using DrillCorp.Data;
using DrillCorp.Bug;

namespace DrillCorp.Bug.Formation
{
    /// <summary>
    /// Formation 스폰 담당
    /// 맵 외곽에서 FormationData 기반으로 리더 + 멤버 스폰
    /// </summary>
    public class FormationSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("머신 (스폰 중심점 + 이동 타겟)")]
        [SerializeField] private Transform _machineTarget;

        [Header("Spawn")]
        [Tooltip("머신 중심 기준 스폰 반경")]
        [Range(5f, 50f)]
        [SerializeField] private float _spawnRadius = 18f;

        [Tooltip("스폰 시 멤버 초기 배치 여부 (true: 진형대로 자리 잡음, false: 한 점에 몰림)")]
        [SerializeField] private bool _spawnInFormation = true;

        public FormationGroup Spawn(FormationData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[FormationSpawner] FormationData is null");
                return null;
            }

            if (BugPool.Instance == null)
            {
                Debug.LogWarning("[FormationSpawner] BugPool.Instance == null");
                return null;
            }

            Vector3 center = _machineTarget != null ? _machineTarget.position : Vector3.zero;
            Vector3 spawnCenter = GetRandomSpawnPosition(center);

            BugData leaderData = data.LeaderBugData != null ? data.LeaderBugData : data.PickRandomMember();
            if (leaderData == null)
            {
                Debug.LogWarning($"[FormationSpawner] {data.name}: 리더 BugData 없음");
                return null;
            }

            GameObject leaderObj = BugPool.Instance.Get(leaderData, spawnCenter, Quaternion.identity);
            if (leaderObj == null)
                return null;

            InitializeBug(leaderObj, leaderData);

            int memberCount = data.GetRandomMemberCount();
            var offsets = FormationOffsetCalculator.Calculate(
                data.FormationType,
                memberCount,
                data.Spacing,
                data.FormationRadius,
                data.Jitter
            );

            var groupObj = new GameObject($"Formation_{data.DisplayName}");
            groupObj.transform.position = spawnCenter;
            var group = groupObj.AddComponent<FormationGroup>();
            group.Setup(data, _machineTarget, leaderObj.transform);

            int placed = 0;
            for (int i = 0; i < offsets.Count && placed < memberCount; i++)
            {
                Vector3 localOffset = offsets[i];
                if (localOffset == Vector3.zero && i == 0)
                    continue;

                BugData memberData = data.PickRandomMember();
                if (memberData == null)
                    continue;

                Vector3 worldPos = _spawnInFormation
                    ? spawnCenter + localOffset
                    : spawnCenter;

                GameObject memberObj = BugPool.Instance.Get(memberData, worldPos, Quaternion.identity);
                if (memberObj == null)
                    continue;

                InitializeBug(memberObj, memberData);

                var member = memberObj.GetComponent<FormationMember>();
                if (member == null)
                    member = memberObj.AddComponent<FormationMember>();

                group.AddMember(member, localOffset);
                placed++;
            }

            return group;
        }

        private void InitializeBug(GameObject obj, BugData data)
        {
            var controller = obj.GetComponent<BugController>();
            if (controller != null)
            {
                controller.Initialize(data);
                return;
            }

            var legacyBase = obj.GetComponent<BugBase>();
            if (legacyBase != null)
            {
                legacyBase.Initialize(data);
            }
        }

        private Vector3 GetRandomSpawnPosition(Vector3 center)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float x = center.x + Mathf.Cos(angle) * _spawnRadius;
            float z = center.z + Mathf.Sin(angle) * _spawnRadius;
            return new Vector3(x, 0f, z);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = _machineTarget != null ? _machineTarget.position : transform.position;
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
            const int segments = 64;
            Vector3 prev = center + new Vector3(_spawnRadius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * _spawnRadius, 0f, Mathf.Sin(angle) * _spawnRadius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}

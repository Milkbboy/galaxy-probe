using System.Collections.Generic;
using UnityEngine;

namespace DrillCorp.Bug.Formation
{
    /// <summary>
    /// 벌레 군집 관리
    /// 리더 이동 + 멤버 오프셋 추적 + Phase 전환
    /// </summary>
    public class FormationGroup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FormationData _data;

        [Header("Target")]
        [Tooltip("머신 Transform (Formation의 이동 타겟)")]
        [SerializeField] private Transform _machineTarget;

        [Header("Leader")]
        [Tooltip("리더 Transform (비어있으면 첫 멤버를 리더로 지정)")]
        [SerializeField] private Transform _leader;

        [Header("Movement")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _baseSpeed = 2f;

        [Header("Member Smoothing")]
        [Range(1f, 20f)]
        [SerializeField] private float _memberFollowSpeed = 6f;

        private readonly List<FormationMember> _members = new List<FormationMember>();
        private bool _initialized;

        public FormationData Data => _data;
        public Transform MachineTarget => _machineTarget;
        public Transform Leader => _leader;
        public Vector3 LeaderPosition => _leader != null ? _leader.position : transform.position;
        public Quaternion LeaderRotation => _leader != null ? _leader.rotation : transform.rotation;
        public IReadOnlyList<FormationMember> Members => _members;
        public int AliveCount => _members.Count;

        public void Setup(FormationData data, Transform machineTarget, Transform leader)
        {
            _data = data;
            _machineTarget = machineTarget;
            _leader = leader;
            _members.Clear();
            _initialized = true;

            if (_leader != null)
            {
                var leaderController = _leader.GetComponent<BugController>();
                if (leaderController != null)
                    leaderController.SetMovementExternallyControlled(true);
            }
        }

        public void AddMember(FormationMember member, Vector3 localOffset)
        {
            if (member == null)
                return;

            member.Setup(this, localOffset, _machineTarget);
            _members.Add(member);

            var controller = member.GetComponent<BugController>();
            if (controller != null)
                controller.SetMovementExternallyControlled(true);
        }

        public void RemoveMember(FormationMember member)
        {
            if (member == null)
                return;

            _members.Remove(member);
            member.OnReleased();
        }

        private void Update()
        {
            if (!_initialized || _machineTarget == null || _leader == null)
                return;

            UpdateLeader();
            UpdateMembers();
            PruneDeadMembers();
        }

        private void UpdateLeader()
        {
            Vector3 leaderPos = _leader.position;
            Vector3 toTarget = new Vector3(
                _machineTarget.position.x - leaderPos.x,
                0f,
                _machineTarget.position.z - leaderPos.z
            );

            float distToMachine = toTarget.magnitude;
            if (distToMachine < 0.5f)
                return;

            Vector3 dir = toTarget.normalized;
            float speed = _baseSpeed * (_data != null ? _data.SpeedMultiplier : 1f);

            _leader.position = leaderPos + dir * speed * Time.deltaTime;

            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                _leader.rotation = Quaternion.Slerp(_leader.rotation, targetRot, 5f * Time.deltaTime);
            }
        }

        private void UpdateMembers()
        {
            Quaternion leaderRot = _leader.rotation;

            for (int i = 0; i < _members.Count; i++)
            {
                var member = _members[i];
                if (member == null)
                    continue;

                var phase = member.EvaluatePhase();

                if (phase == FormationPhase.Phase3_Individual)
                {
                    ReleaseControl(member);
                    continue;
                }

                Vector3 targetPos = member.GetTargetWorldPosition();

                float lerpFactor = phase == FormationPhase.Phase1_Formation ? 1f : 0.4f;
                float t = _memberFollowSpeed * lerpFactor * Time.deltaTime;

                member.transform.position = Vector3.Lerp(
                    member.transform.position,
                    targetPos,
                    Mathf.Clamp01(t)
                );

                // 멤버도 리더와 같은 방향(머신 방향)을 바라보게 회전
                member.transform.rotation = Quaternion.Slerp(
                    member.transform.rotation,
                    leaderRot,
                    5f * Time.deltaTime
                );
            }
        }

        private void ReleaseControl(FormationMember member)
        {
            var controller = member.GetComponent<BugController>();
            if (controller != null && controller.MovementExternallyControlled)
                controller.SetMovementExternallyControlled(false);
        }

        private void PruneDeadMembers()
        {
            for (int i = _members.Count - 1; i >= 0; i--)
            {
                var m = _members[i];
                if (m == null || !m.gameObject.activeInHierarchy)
                {
                    _members.RemoveAt(i);
                }
            }
        }

        public void DisbandAll()
        {
            for (int i = 0; i < _members.Count; i++)
            {
                var m = _members[i];
                if (m != null)
                {
                    ReleaseControl(m);
                    m.OnReleased();
                }
            }
            _members.Clear();
        }
    }
}

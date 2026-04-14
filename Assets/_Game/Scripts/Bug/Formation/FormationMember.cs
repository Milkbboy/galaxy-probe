using UnityEngine;

namespace DrillCorp.Bug.Formation
{
    /// <summary>
    /// Formation 소속 멤버 마커
    /// 리더 기준 오프셋을 유지하며 Phase에 따라 제어권을 개별 Behavior로 이양
    /// </summary>
    public enum FormationPhase
    {
        Phase1_Formation,   // 진형 유지 (Formation 제어)
        Phase2_Loose,       // 느슨한 개별 행동 (혼합)
        Phase3_Individual,  // 완전 해체 (기존 BugController 제어)
    }

    public class FormationMember : MonoBehaviour
    {
        [Header("Phase Thresholds (Machine 거리 기준)")]
        [Tooltip("Phase 1 → 2 전환 거리")]
        [SerializeField] private float _phase1To2Distance = 12f;

        [Tooltip("Phase 2 → 3 전환 거리")]
        [SerializeField] private float _phase2To3Distance = 6f;

        private FormationGroup _group;
        private Vector3 _localOffset;
        private FormationPhase _phase = FormationPhase.Phase1_Formation;
        private Transform _machineTarget;

        public FormationGroup Group => _group;
        public Vector3 LocalOffset => _localOffset;
        public FormationPhase Phase => _phase;
        public bool IsIndividual => _phase == FormationPhase.Phase3_Individual;

        public void Setup(FormationGroup group, Vector3 localOffset, Transform machineTarget)
        {
            _group = group;
            _localOffset = localOffset;
            _machineTarget = machineTarget;
            _phase = FormationPhase.Phase1_Formation;
        }

        public void SetPhase(FormationPhase phase)
        {
            _phase = phase;
        }

        public Vector3 GetTargetWorldPosition()
        {
            if (_group == null)
                return transform.position;

            return _group.LeaderPosition + _group.LeaderRotation * _localOffset;
        }

        public FormationPhase EvaluatePhase()
        {
            if (_machineTarget == null)
                return _phase;

            Vector3 flat = new Vector3(
                transform.position.x - _machineTarget.position.x,
                0f,
                transform.position.z - _machineTarget.position.z
            );
            float dist = flat.magnitude;

            if (dist > _phase1To2Distance)
                _phase = FormationPhase.Phase1_Formation;
            else if (dist > _phase2To3Distance)
                _phase = FormationPhase.Phase2_Loose;
            else
                _phase = FormationPhase.Phase3_Individual;

            return _phase;
        }

        public void OnReleased()
        {
            _group = null;
            _phase = FormationPhase.Phase3_Individual;
        }
    }
}

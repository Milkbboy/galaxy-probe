using UnityEngine;
using DrillCorp.Bug.Behaviors.Data;

namespace DrillCorp.Bug.Behaviors.Movement
{
    /// <summary>
    /// 이동 행동 기본 클래스
    /// </summary>
    public abstract class MovementBehaviorBase : IMovementBehavior
    {
        protected BugController _bug;
        protected float _speedMultiplier = 1f;

        public float SpeedMultiplier
        {
            get => _speedMultiplier;
            set => _speedMultiplier = value;
        }

        public virtual void Initialize(BugController bug)
        {
            _bug = bug;
        }

        public virtual void Cleanup()
        {
            _bug = null;
        }

        public abstract void UpdateMovement(Transform target);

        /// <summary>
        /// 현재 적용할 이동 속도
        /// </summary>
        protected float GetMoveSpeed()
        {
            return _bug.MoveSpeed * _speedMultiplier;
        }

        /// <summary>
        /// XZ 평면에서 타겟 방향 계산
        /// </summary>
        protected Vector3 GetDirectionToTarget(Transform target)
        {
            if (target == null) return Vector3.zero;

            Vector3 myPos = new Vector3(_bug.transform.position.x, 0f, _bug.transform.position.z);
            Vector3 targetPos = new Vector3(target.position.x, 0f, target.position.z);
            return (targetPos - myPos).normalized;
        }

        /// <summary>
        /// 이동 방향으로 회전 (Y축 기준)
        /// </summary>
        protected void RotateTowards(Vector3 direction)
        {
            if (direction == Vector3.zero) return;

            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            _bug.transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        /// <summary>
        /// Movement 타입에 따른 인스턴스 생성
        /// </summary>
        public static MovementBehaviorBase Create(MovementType type, float param1, float param2, GameObject effectPrefab = null)
        {
            switch (type)
            {
                case MovementType.Linear:
                    return new LinearMovement();

                case MovementType.Hover:
                    return new HoverMovement(param1, param2);

                case MovementType.Burst:
                    return new BurstMovement(param1, param2);

                case MovementType.Ranged:
                    return new RangedMovement(param1, param2);

                case MovementType.Retreat:
                    return new RetreatMovement(param1, param2);

                case MovementType.SlowStart:
                    return new SlowStartMovement(param1, param2);

                case MovementType.Orbit:
                    return new OrbitMovement(param1, param2);

                case MovementType.Teleport:
                    return new TeleportMovement(param1, param2, effectPrefab);

                // TODO: Phase 3에서 추가
                // case MovementType.Burrow:
                // case MovementType.Dive:

                default:
                    return new LinearMovement();
            }
        }
    }
}

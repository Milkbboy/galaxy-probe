using UnityEngine;
using DrillCorp.Bug.Behaviors.Data;

namespace DrillCorp.Bug.Behaviors.Movement
{
    /// <summary>
    /// 공중 부유 이동 (위아래로 떠다니며)
    /// </summary>
    public class HoverMovement : MovementBehaviorBase
    {
        private float _hoverHeight;     // 부유 높이
        private float _hoverPeriod;     // 부유 주기 (초)
        private float _baseHeight;      // 기본 비행 높이
        private IdleType _idleType;
        private float _idleParam;

        // Strafe용
        private float _strafeDirection = 1f;
        private float _strafeTimer;
        private float _strafeChangeInterval = 2f;

        public HoverMovement(float height = 0.3f, float period = 2f, IdleType idleType = IdleType.Stop, float idleParam = 0f)
        {
            _hoverHeight = height > 0f ? height : 0.3f;
            _hoverPeriod = period > 0f ? period : 2f;
            _baseHeight = 0.5f;
            _idleType = idleType;
            _idleParam = idleParam > 0f ? idleParam : 0.5f;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            _strafeDirection = Random.value > 0.5f ? 1f : -1f;
            _strafeTimer = Random.Range(0f, _strafeChangeInterval);
        }

        public override void UpdateMovement(Transform target)
        {
            if (_bug == null || target == null) return;

            float distance = _bug.GetDistanceTo(target);
            Vector3 direction = GetDirectionToTarget(target);

            // 호버링 계산 (항상 적용)
            float hoverOffset = Mathf.Sin(Time.time * (2f * Mathf.PI / _hoverPeriod)) * _hoverHeight;
            float targetY = _baseHeight + hoverOffset;
            float currentY = _bug.transform.position.y;
            float newY = Mathf.Lerp(currentY, targetY, Time.deltaTime * 5f);

            // 공격 범위 내면 Idle 행동 (+ 호버링)
            if (distance <= _bug.AttackRange)
            {
                PerformIdleBehavior(target, direction, newY);
                return;
            }

            // 사거리 밖이면 접근
            Vector3 movement = direction * GetMoveSpeed() * Time.deltaTime;
            _bug.transform.position = new Vector3(
                _bug.transform.position.x + movement.x,
                newY,
                _bug.transform.position.z + movement.z
            );
            RotateTowards(direction);
        }

        private void PerformIdleBehavior(Transform target, Vector3 direction, float newY)
        {
            switch (_idleType)
            {
                case IdleType.Stop:
                    // 정지, 호버링만
                    _bug.transform.position = new Vector3(
                        _bug.transform.position.x,
                        newY,
                        _bug.transform.position.z
                    );
                    RotateTowards(direction);
                    break;

                case IdleType.Strafe:
                    PerformStrafe(target, direction, newY);
                    break;

                default:
                    // 기타는 Stop과 동일
                    _bug.transform.position = new Vector3(
                        _bug.transform.position.x,
                        newY,
                        _bug.transform.position.z
                    );
                    RotateTowards(direction);
                    break;
            }
        }

        private void PerformStrafe(Transform target, Vector3 direction, float newY)
        {
            _strafeTimer += Time.deltaTime;
            if (_strafeTimer >= _strafeChangeInterval)
            {
                _strafeTimer = 0f;
                _strafeDirection *= -1f;
            }

            Vector3 strafeDir = new Vector3(-direction.z, 0f, direction.x) * _strafeDirection;
            float strafeSpeed = GetMoveSpeed() * _idleParam;
            Vector3 movement = strafeDir * strafeSpeed * Time.deltaTime;

            Vector3 newPos = _bug.transform.position + new Vector3(movement.x, 0f, movement.z);
            Vector3 targetPos = new Vector3(target.position.x, 0f, target.position.z);
            float newDistance = Vector3.Distance(new Vector3(newPos.x, 0f, newPos.z), targetPos);

            if (newDistance <= _bug.AttackRange * 0.95f)
            {
                _bug.transform.position = new Vector3(newPos.x, newY, newPos.z);
            }
            else
            {
                Vector3 approachMovement = direction * GetMoveSpeed() * 0.3f * Time.deltaTime;
                _bug.transform.position = new Vector3(
                    _bug.transform.position.x + approachMovement.x,
                    newY,
                    _bug.transform.position.z + approachMovement.z
                );
            }

            RotateTowards(direction);
        }
    }
}

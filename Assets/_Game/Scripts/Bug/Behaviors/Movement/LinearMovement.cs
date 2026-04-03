using UnityEngine;
using DrillCorp.Bug.Behaviors.Data;

namespace DrillCorp.Bug.Behaviors.Movement
{
    /// <summary>
    /// 직선 이동 + Idle 행동
    /// </summary>
    public class LinearMovement : MovementBehaviorBase
    {
        private IdleType _idleType;
        private float _idleParam;

        // Strafe용
        private float _strafeDirection = 1f;
        private float _strafeTimer;
        private float _strafeChangeInterval = 2f;

        // Retreat용
        private bool _isRetreating;
        private float _retreatTimer;

        public LinearMovement(IdleType idleType = IdleType.Stop, float idleParam = 0f)
        {
            _idleType = idleType;
            _idleParam = idleParam > 0f ? idleParam : 0.5f; // 기본값
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

            // 공격 범위 내면 Idle 행동
            if (distance <= _bug.AttackRange)
            {
                PerformIdleBehavior(target, direction, distance);
                return;
            }

            // 사거리 밖이면 접근
            MoveTowardsTarget(direction);
            RotateTowards(direction);
        }

        private void MoveTowardsTarget(Vector3 direction)
        {
            Vector3 movement = direction * GetMoveSpeed() * Time.deltaTime;
            _bug.transform.position += new Vector3(movement.x, 0f, movement.z);
        }

        private void PerformIdleBehavior(Transform target, Vector3 direction, float distance)
        {
            switch (_idleType)
            {
                case IdleType.Stop:
                    // 정지, 타겟만 바라봄
                    RotateTowards(direction);
                    break;

                case IdleType.Strafe:
                    PerformStrafe(target, direction, distance);
                    break;

                case IdleType.Orbit:
                    PerformOrbit(target, direction);
                    break;

                case IdleType.Retreat:
                    PerformRetreat(direction);
                    break;
            }
        }

        private void PerformStrafe(Transform target, Vector3 direction, float distance)
        {
            // 타이머로 방향 전환
            _strafeTimer += Time.deltaTime;
            if (_strafeTimer >= _strafeChangeInterval)
            {
                _strafeTimer = 0f;
                _strafeDirection *= -1f;
            }

            // 타겟 방향의 수직 벡터 (좌우)
            Vector3 strafeDir = new Vector3(-direction.z, 0f, direction.x) * _strafeDirection;
            float strafeSpeed = GetMoveSpeed() * _idleParam;
            Vector3 movement = strafeDir * strafeSpeed * Time.deltaTime;

            // 이동 후 예상 위치 확인
            Vector3 newPos = _bug.transform.position + new Vector3(movement.x, 0f, movement.z);
            Vector3 targetPos = new Vector3(target.position.x, 0f, target.position.z);
            float newDistance = Vector3.Distance(new Vector3(newPos.x, 0f, newPos.z), targetPos);

            // 사거리 안에 있을 때만 이동
            if (newDistance <= _bug.AttackRange * 0.95f)
            {
                _bug.transform.position = newPos;
            }
            else
            {
                // 사거리 벗어나면 살짝 접근
                Vector3 approachMovement = direction * GetMoveSpeed() * 0.3f * Time.deltaTime;
                _bug.transform.position += new Vector3(approachMovement.x, 0f, approachMovement.z);
            }

            RotateTowards(direction);
        }

        private void PerformOrbit(Transform target, Vector3 direction)
        {
            // 타겟 주위를 회전
            float orbitSpeed = _idleParam > 0f ? _idleParam : 45f; // 기본 45도/초
            float angleStep = orbitSpeed * Time.deltaTime;

            // 타겟 중심으로 회전
            Vector3 offset = _bug.transform.position - target.position;
            offset.y = 0f;
            Quaternion rotation = Quaternion.Euler(0f, angleStep, 0f);
            Vector3 newOffset = rotation * offset;

            _bug.transform.position = target.position + newOffset;
            _bug.transform.position = new Vector3(_bug.transform.position.x, _bug.transform.position.y, _bug.transform.position.z);

            // 타겟을 바라봄
            RotateTowards(direction);
        }

        private void PerformRetreat(Vector3 direction)
        {
            if (!_isRetreating)
            {
                _isRetreating = true;
                _retreatTimer = _idleParam > 0f ? _idleParam : 1f;
            }

            if (_retreatTimer > 0f)
            {
                // 후퇴
                Vector3 movement = -direction * GetMoveSpeed() * 0.5f * Time.deltaTime;
                _bug.transform.position += new Vector3(movement.x, 0f, movement.z);
                _retreatTimer -= Time.deltaTime;
            }
            else
            {
                // 후퇴 완료 → 다시 접근 (Idle 종료)
                _isRetreating = false;
            }

            RotateTowards(direction);
        }
    }
}

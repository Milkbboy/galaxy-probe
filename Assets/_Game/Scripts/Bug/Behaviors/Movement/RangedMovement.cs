using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Movement
{
    /// <summary>
    /// 원거리 이동 - 사거리 유지 + 좌우 이동
    /// </summary>
    public class RangedMovement : MovementBehaviorBase
    {
        private float _preferredDistance;  // 유지할 거리 (0이면 AttackRange 사용)
        private float _strafeSpeed;        // 좌우 이동 속도 배율
        private float _strafeDirection = 1f;
        private float _strafeTimer;
        private float _strafeChangeInterval = 2f;  // 방향 전환 간격

        public RangedMovement(float preferredDistance = 0f, float strafeSpeed = 0.5f)
        {
            _preferredDistance = preferredDistance;
            _strafeSpeed = strafeSpeed;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);

            // 유지 거리가 0이면 AttackRange 사용
            if (_preferredDistance <= 0f)
            {
                _preferredDistance = bug.AttackRange * 0.8f;  // 사거리보다 20% 가깝게 (여유 확보)
            }

            // 초기 방향 랜덤
            _strafeDirection = Random.value > 0.5f ? 1f : -1f;
            _strafeTimer = Random.Range(0f, _strafeChangeInterval);
        }

        public override void UpdateMovement(Transform target)
        {
            if (_bug == null || target == null) return;

            float distance = _bug.GetDistanceTo(target);
            float attackRange = _bug.AttackRange;
            Vector3 directionToTarget = GetDirectionToTarget(target);

            // 사거리 밖이면 접근
            if (distance > _preferredDistance)
            {
                MoveTowardsTarget(directionToTarget);
            }
            // 너무 가까우면 후퇴 (유지거리의 절반 이하)
            else if (distance < _preferredDistance * 0.5f)
            {
                MoveAwayFromTarget(directionToTarget);
            }
            // 적정 거리면 좌우 이동 (단, 사거리 벗어나지 않게)
            else
            {
                StrafeWithinRange(directionToTarget, distance, attackRange);
            }

            // 항상 타겟을 바라봄
            RotateTowards(directionToTarget);
        }

        private void MoveTowardsTarget(Vector3 direction)
        {
            Vector3 movement = direction * GetMoveSpeed() * Time.deltaTime;
            _bug.transform.position += new Vector3(movement.x, 0f, movement.z);
        }

        private void MoveAwayFromTarget(Vector3 direction)
        {
            // 후퇴는 절반 속도
            Vector3 movement = -direction * GetMoveSpeed() * 0.5f * Time.deltaTime;
            _bug.transform.position += new Vector3(movement.x, 0f, movement.z);
        }

        private void Strafe(Vector3 directionToTarget)
        {
            // 타이머로 방향 전환
            _strafeTimer += Time.deltaTime;
            if (_strafeTimer >= _strafeChangeInterval)
            {
                _strafeTimer = 0f;
                _strafeDirection *= -1f;  // 방향 반전
            }

            // 타겟 방향의 수직 벡터 (좌우)
            Vector3 strafeDir = new Vector3(-directionToTarget.z, 0f, directionToTarget.x) * _strafeDirection;

            Vector3 movement = strafeDir * GetMoveSpeed() * _strafeSpeed * Time.deltaTime;
            _bug.transform.position += new Vector3(movement.x, 0f, movement.z);
        }

        private void StrafeWithinRange(Vector3 directionToTarget, float currentDistance, float attackRange)
        {
            // 타이머로 방향 전환
            _strafeTimer += Time.deltaTime;
            if (_strafeTimer >= _strafeChangeInterval)
            {
                _strafeTimer = 0f;
                _strafeDirection *= -1f;
            }

            // 타겟 방향의 수직 벡터 (좌우)
            Vector3 strafeDir = new Vector3(-directionToTarget.z, 0f, directionToTarget.x) * _strafeDirection;
            Vector3 movement = strafeDir * GetMoveSpeed() * _strafeSpeed * Time.deltaTime;

            // 이동 후 예상 위치
            Vector3 newPos = _bug.transform.position + new Vector3(movement.x, 0f, movement.z);
            Vector3 targetPos = new Vector3(_bug.Target.position.x, 0f, _bug.Target.position.z);
            float newDistance = Vector3.Distance(new Vector3(newPos.x, 0f, newPos.z), targetPos);

            // 사거리 안에 있을 때만 이동
            if (newDistance <= attackRange * 0.95f)
            {
                _bug.transform.position = newPos;
            }
            else
            {
                // 사거리 벗어나면 살짝 접근
                Vector3 approachMovement = directionToTarget * GetMoveSpeed() * 0.3f * Time.deltaTime;
                _bug.transform.position += new Vector3(approachMovement.x, 0f, approachMovement.z);
            }
        }
    }
}

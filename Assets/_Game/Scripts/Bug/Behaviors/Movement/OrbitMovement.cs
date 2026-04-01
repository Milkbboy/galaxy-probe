using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Movement
{
    /// <summary>
    /// 선회 이동 - 타겟 주위를 일정 거리 유지하며 원형으로 이동
    /// param1 = 선회 반경 (0이면 AttackRange 사용)
    /// param2 = 초당 회전 각도 (도 단위, 기본 60도)
    /// </summary>
    public class OrbitMovement : MovementBehaviorBase
    {
        private float _orbitRadius;
        private float _angularSpeedDegrees;
        private float _currentAngle;
        private int _orbitDirection; // 1 = 반시계, -1 = 시계
        private bool _reachedOrbit;

        public OrbitMovement(float orbitRadius = 0f, float angularSpeedDegrees = 60f)
        {
            _orbitRadius = orbitRadius;
            _angularSpeedDegrees = angularSpeedDegrees > 0f ? angularSpeedDegrees : 60f;
            _orbitDirection = Random.value > 0.5f ? 1 : -1;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);

            if (_orbitRadius <= 0f)
            {
                _orbitRadius = bug.AttackRange;
            }

            _reachedOrbit = false;
            _currentAngle = 0f;
        }

        public override void UpdateMovement(Transform target)
        {
            if (_bug == null || target == null) return;

            float distance = _bug.GetDistanceTo(target);

            // 아직 궤도에 도달하지 않았으면 접근
            if (!_reachedOrbit)
            {
                if (distance <= _orbitRadius + 0.2f)
                {
                    // 궤도 도달! 현재 위치 기준 각도 계산
                    _reachedOrbit = true;
                    Vector3 offset = _bug.transform.position - target.position;
                    _currentAngle = Mathf.Atan2(offset.z, offset.x);
                }
                else
                {
                    // 타겟으로 접근
                    ApproachTarget(target);
                    return;
                }
            }

            // 궤도 이동
            PerformOrbit(target);
        }

        private void ApproachTarget(Transform target)
        {
            Vector3 direction = GetDirectionToTarget(target);
            Vector3 movement = direction * GetMoveSpeed() * Time.deltaTime;
            _bug.transform.position += new Vector3(movement.x, 0f, movement.z);
            RotateTowards(direction);
        }

        private void PerformOrbit(Transform target)
        {
            // 각도 업데이트
            float angularSpeedRad = _angularSpeedDegrees * Mathf.Deg2Rad;
            _currentAngle += angularSpeedRad * _orbitDirection * Time.deltaTime;

            // 목표 궤도 위치 계산
            Vector3 targetPos = target.position;
            float desiredX = targetPos.x + Mathf.Cos(_currentAngle) * _orbitRadius;
            float desiredZ = targetPos.z + Mathf.Sin(_currentAngle) * _orbitRadius;
            Vector3 desiredPos = new Vector3(desiredX, _bug.transform.position.y, desiredZ);

            // 현재 위치에서 목표 위치로 부드럽게 이동
            Vector3 currentPos = _bug.transform.position;
            Vector3 toDesired = desiredPos - currentPos;
            float distToDesired = toDesired.magnitude;

            if (distToDesired > 0.01f)
            {
                // 궤도 선속도 계산 (v = r * omega)
                float orbitSpeed = _orbitRadius * angularSpeedRad;
                // 궤도 위치 보정을 위한 추가 속도
                float correctionSpeed = distToDesired * 3f;
                float totalSpeed = orbitSpeed + correctionSpeed;

                Vector3 moveDir = toDesired.normalized;
                Vector3 movement = moveDir * totalSpeed * Time.deltaTime;

                // 목표 위치를 넘지 않도록
                if (movement.magnitude > distToDesired)
                {
                    movement = toDesired;
                }

                _bug.transform.position += new Vector3(movement.x, 0f, movement.z);
            }

            // 타겟을 바라보기
            Vector3 lookDir = GetDirectionToTarget(target);
            RotateTowards(lookDir);
        }
    }
}

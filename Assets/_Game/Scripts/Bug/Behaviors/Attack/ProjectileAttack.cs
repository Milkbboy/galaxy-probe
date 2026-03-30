using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Attack
{
    /// <summary>
    /// 원거리 투사체 공격
    /// </summary>
    public class ProjectileAttack : AttackBehaviorBase
    {
        private float _projectileSpeed;
        private GameObject _projectilePrefab;

        public ProjectileAttack(float speed = 10f, GameObject prefab = null)
        {
            _projectileSpeed = speed > 0f ? speed : 10f;
            _projectilePrefab = prefab;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            // 원거리는 보통 더 긴 사거리
            _attackRange = bug.AttackRange > 1f ? bug.AttackRange : 5f;
        }

        protected override void PerformAttack(Transform target)
        {
            if (_projectilePrefab != null)
            {
                // 프리펩이 있으면 투사체 생성
                SpawnProjectile(target);
            }
            else
            {
                // 없으면 즉발 데미지 (히트스캔)
                DealDamage(target, GetDamage());
            }
        }

        private void SpawnProjectile(Transform target)
        {
            Vector3 spawnPos = _bug.transform.position + Vector3.up * 0.5f;
            Vector3 direction = (target.position - spawnPos).normalized;

            GameObject projectile = Object.Instantiate(_projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

            // BugProjectile 컴포넌트가 있으면 초기화
            var bugProjectile = projectile.GetComponent<BugProjectile>();
            if (bugProjectile != null)
            {
                bugProjectile.Initialize(GetDamage(), _projectileSpeed, direction);
            }
            else
            {
                // 없으면 기본 Rigidbody 사용
                var rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * _projectileSpeed;
                }

                // 5초 후 자동 파괴
                Object.Destroy(projectile, 5f);
            }
        }
    }
}

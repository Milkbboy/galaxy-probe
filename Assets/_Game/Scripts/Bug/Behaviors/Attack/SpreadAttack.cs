using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Attack
{
    /// <summary>
    /// 다발 발사 공격 - 여러 투사체를 부채꼴 형태로 발사
    /// param1 = 발사 개수 (기본 3발)
    /// param2 = 전체 각도 (기본 60도)
    /// </summary>
    public class SpreadAttack : AttackBehaviorBase
    {
        private int _projectileCount;
        private float _spreadAngle;
        private float _projectileSpeed;
        private GameObject _projectilePrefab;
        private GameObject _projectileHitVfxPrefab;

        public SpreadAttack(int count = 3, float angle = 60f, float speed = 10f,
            GameObject prefab = null, GameObject hitVfxPrefab = null)
        {
            _projectileCount = count > 0 ? count : 3;
            _spreadAngle = angle > 0f ? angle : 60f;
            _projectileSpeed = speed > 0f ? speed : 10f;
            _projectilePrefab = prefab;
            _projectileHitVfxPrefab = hitVfxPrefab;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            // Range는 SO에서 설정되거나 BugController.AttackRange에서 폴백
        }

        protected override void PerformAttack(Transform target)
        {
            if (_bug == null || target == null) return;

            Vector3 spawnPos = _bug.transform.position + Vector3.up * 0.5f;
            Vector3 baseDirection = (target.position - spawnPos).normalized;
            baseDirection.y = 0f;
            baseDirection.Normalize();

            float damage = GetDamage();

            // 각 투사체의 각도 계산
            float angleStep = _projectileCount > 1 ? _spreadAngle / (_projectileCount - 1) : 0f;
            float startAngle = -_spreadAngle / 2f;

            for (int i = 0; i < _projectileCount; i++)
            {
                float angle = _projectileCount > 1 ? startAngle + (angleStep * i) : 0f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * baseDirection;

                SpawnProjectile(spawnPos, direction, damage);
            }
        }

        private void SpawnProjectile(Vector3 position, Vector3 direction, float damage)
        {
            if (_projectilePrefab != null)
            {
                // 프리펩이 있으면 사용
                GameObject projectile = Object.Instantiate(_projectilePrefab, position, Quaternion.LookRotation(direction));

                var bugProjectile = projectile.GetComponent<BugProjectile>();
                if (bugProjectile != null)
                {
                    bugProjectile.Initialize(damage, _projectileSpeed, direction, _bug.gameObject, _projectileHitVfxPrefab);
                }
                else
                {
                    var rb = projectile.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = direction * _projectileSpeed;
                    }
                    Object.Destroy(projectile, 5f);
                }
            }
            else
            {
                // 프리펩이 없으면 기본 투사체 생성
                CreateDefaultProjectile(position, direction, damage);
            }
        }

        private void CreateDefaultProjectile(Vector3 position, Vector3 direction, float damage)
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "SpreadProjectile";
            projectile.transform.position = position;
            projectile.transform.localScale = Vector3.one * 0.3f;

            // Collider를 Trigger로 설정
            var collider = projectile.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            // Rigidbody 추가
            var rb = projectile.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = direction * _projectileSpeed;

            // BugProjectile 컴포넌트 추가
            var bugProjectile = projectile.AddComponent<BugProjectile>();
            bugProjectile.Initialize(damage, _projectileSpeed, direction, _bug.gameObject, _projectileHitVfxPrefab);
        }
    }
}

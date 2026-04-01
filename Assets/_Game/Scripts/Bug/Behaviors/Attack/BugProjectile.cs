using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Machine;
using DrillCorp.VFX;
using DrillCorp.Bug;

namespace DrillCorp.Bug.Behaviors.Attack
{
    /// <summary>
    /// 벌레 투사체
    /// </summary>
    public class BugProjectile : MonoBehaviour
    {
        [SerializeField] private float _lifetime = 5f;
        [SerializeField] private GameObject _hitEffect;

        private float _damage;
        private float _speed;
        private Vector3 _direction;
        private bool _initialized;
        private GameObject _owner;
        private BugController _ownerBug;
        private float _spawnTime;

        public void Initialize(float damage, float speed, Vector3 direction, GameObject owner = null, GameObject hitVfxPrefab = null)
        {
            _damage = damage;
            _speed = speed;
            _direction = direction.normalized;
            _owner = owner;
            _ownerBug = owner?.GetComponent<BugController>();
            _initialized = true;
            _spawnTime = Time.time;

            // 외부에서 HitVfx 전달받으면 사용
            if (hitVfxPrefab != null)
            {
                _hitEffect = hitVfxPrefab;
            }

            // BugProjectile 레이어 설정 (Bug 레이어와 충돌 안 함)
            int projectileLayer = LayerMask.NameToLayer("BugProjectile");
            if (projectileLayer != -1)
            {
                gameObject.layer = projectileLayer;
            }

            // 자동 파괴
            Destroy(gameObject, _lifetime);
        }

        private void Update()
        {
            if (!_initialized) return;

            float moveDistance = _speed * Time.deltaTime;

            // Raycast로 충돌 체크 (터널링 방지)
            if (Physics.Raycast(transform.position, _direction, out RaycastHit hit, moveDistance))
            {
                // Bug는 무시
                if (hit.collider.GetComponent<BugController>() == null &&
                    hit.collider.GetComponent<BugBase>() == null)
                {
                    var damageable = hit.collider.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        DealDamageWithPassives(hit.collider.transform, _damage);

                        // 히트 이펙트
                        if (_hitEffect != null)
                        {
                            var effect = Instantiate(_hitEffect, hit.point, Quaternion.identity);
                            Destroy(effect, 2f);
                        }
                        else
                        {
                            SimpleVFX.PlayProjectileHit(hit.point);
                        }

                        Destroy(gameObject);
                        return;
                    }
                }
            }

            // 이동
            transform.position += _direction * moveDistance;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 스폰 직후 0.1초간 충돌 무시 (발사자와 겹침 방지)
            if (Time.time - _spawnTime < 0.1f)
                return;

            // 발사자 자신은 무시
            if (_owner != null && other.gameObject == _owner)
                return;

            // Bug 레이어는 무시
            int bugLayer = LayerMask.NameToLayer("Bug");
            if (bugLayer != -1 && other.gameObject.layer == bugLayer)
                return;

            // BugController/BugBase는 무시 (같은 편)
            if (other.GetComponent<BugController>() != null || other.GetComponent<BugBase>() != null)
                return;

            // IDamageable에게만 데미지
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                DealDamageWithPassives(other.transform, _damage);

                // 히트 이펙트
                if (_hitEffect != null)
                {
                    var effect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
                    Destroy(effect, 2f);
                }
                else
                {
                    SimpleVFX.PlayProjectileHit(transform.position);
                }

                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 패시브 효과 적용 후 데미지 처리
        /// </summary>
        private void DealDamageWithPassives(Transform target, float damage)
        {
            // 패시브에게 공격 알림 (독 공격 등)
            if (_ownerBug != null)
            {
                foreach (var passive in _ownerBug.Passives)
                {
                    passive.ProcessOutgoingDamage(damage, target);
                }
            }

            var damageable = target.GetComponent<IDamageable>();
            damageable?.TakeDamage(damage);
        }
    }
}

using UnityEngine;
using DrillCorp.Core;

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

        public void Initialize(float damage, float speed, Vector3 direction)
        {
            _damage = damage;
            _speed = speed;
            _direction = direction.normalized;
            _initialized = true;

            // 자동 파괴
            Destroy(gameObject, _lifetime);
        }

        private void Update()
        {
            if (!_initialized) return;

            // 이동
            transform.position += _direction * _speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Machine 레이어 또는 IDamageable 체크
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null && !other.CompareTag("Bug"))
            {
                damageable.TakeDamage(_damage);

                // 히트 이펙트
                if (_hitEffect != null)
                {
                    var effect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
                    Destroy(effect, 2f);
                }

                Destroy(gameObject);
            }
        }
    }
}

using UnityEngine;
using DrillCorp.Machine;

namespace DrillCorp.Bug.Behaviors.Trigger
{
    /// <summary>
    /// 사망 시 폭발 트리거 - 죽을 때 주변에 데미지
    /// param1 = 폭발 범위 (기본 3)
    /// param2 = 폭발 데미지 (기본 50)
    /// </summary>
    public class ExplodeOnDeathTrigger : TriggerBehaviorBase
    {
        private float _explosionRadius;
        private float _explosionDamage;

        public ExplodeOnDeathTrigger(float radius = 3f, float damage = 50f)
        {
            _explosionRadius = radius > 0f ? radius : 3f;
            _explosionDamage = damage > 0f ? damage : 50f;
            _triggerOnDeath = true;
        }

        public override void CheckAndTrigger()
        {
            // 사망 트리거는 CheckAndTrigger에서 동작하지 않음
        }

        public override void OnDeath()
        {
            if (_hasTriggered) return;
            Trigger();
        }

        protected override void OnTriggered()
        {
            if (_bug == null) return;

            Vector3 center = _bug.transform.position;

            // 범위 내 모든 대상에게 데미지 (버그 제외)
            Collider[] hits = Physics.OverlapSphere(center, _explosionRadius);

            foreach (var hit in hits)
            {
                // 자기 자신 제외
                if (hit.transform == _bug.transform) continue;
                if (hit.transform.IsChildOf(_bug.transform)) continue;

                // 다른 버그 제외
                if (hit.GetComponent<BugController>() != null) continue;
                if (hit.GetComponent<BugBase>() != null) continue;

                // IDamageable에게 데미지
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(_explosionDamage);
                }
            }

            // 폭발 이펙트
            PlayExplosionEffect(center);

            Debug.Log($"[ExplodeOnDeathTrigger] {_bug.name} exploded! Radius: {_explosionRadius}, Damage: {_explosionDamage}");
        }

        private void PlayExplosionEffect(Vector3 position)
        {
            // 큰 폭발 이펙트 (반경에 비례)
            VFX.SimpleVFX.PlayExplosion(position, _explosionRadius);
        }
    }
}

using UnityEngine;
using System;
using DrillCorp.Bug.Behaviors.Data;
using DrillCorp.Core;
using DrillCorp.Machine;

namespace DrillCorp.Bug.Behaviors.Attack
{
    /// <summary>
    /// 공격 행동 기본 클래스
    /// </summary>
    public abstract class AttackBehaviorBase : IAttackBehavior
    {
        protected BugController _bug;
        protected float _damageMultiplier = 1f;
        protected float _lastAttackTime;
        protected float _attackRange;
        protected GameObject _hitVfxPrefab;

        public event Action OnAttackPerformed;

        public float DamageMultiplier
        {
            get => _damageMultiplier;
            set => _damageMultiplier = value;
        }

        public float AttackRange => _attackRange;

        public virtual void Initialize(BugController bug)
        {
            _bug = bug;
            _attackRange = bug.AttackRange;
            _lastAttackTime = -999f; // 즉시 공격 가능
        }

        public virtual void Cleanup()
        {
            _bug = null;
        }

        public bool TryAttack(Transform target)
        {
            if (_bug == null || target == null) return false;

            // 쿨다운 체크
            if (Time.time < _lastAttackTime + _bug.AttackCooldown) return false;

            // 공격 수행
            PerformAttack(target);
            _lastAttackTime = Time.time;
            OnAttackPerformed?.Invoke();

            return true;
        }

        protected abstract void PerformAttack(Transform target);

        /// <summary>
        /// 현재 적용할 공격력
        /// </summary>
        protected float GetDamage()
        {
            return _bug.AttackDamage * _damageMultiplier;
        }

        /// <summary>
        /// 대상에게 데미지 적용
        /// </summary>
        protected void DealDamage(Transform target, float damage)
        {
            // 패시브에게 공격 알림 (독 공격 등)
            if (_bug != null)
            {
                foreach (var passive in _bug.Passives)
                {
                    passive.ProcessOutgoingDamage(damage, target);
                }
            }

            var damageable = target.GetComponent<IDamageable>();
            damageable?.TakeDamage(damage);
        }

        /// <summary>
        /// Hit VFX 재생 (프리펩 있으면 사용, 없으면 SimpleVFX 폴백)
        /// </summary>
        protected void PlayHitVfx(Vector3 position)
        {
            if (_hitVfxPrefab != null)
            {
                var vfx = UnityEngine.Object.Instantiate(_hitVfxPrefab, position, Quaternion.identity);
                // ParticleSystem 있으면 duration 후 파괴
                var ps = vfx.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    UnityEngine.Object.Destroy(vfx, ps.main.duration + ps.main.startLifetime.constantMax);
                }
                else
                {
                    UnityEngine.Object.Destroy(vfx, 2f);
                }
            }
            else
            {
                VFX.SimpleVFX.PlayMeleeHit(position);
            }
        }

        /// <summary>
        /// Attack 타입에 따른 인스턴스 생성
        /// </summary>
        public static AttackBehaviorBase Create(AttackType type, float param1, float param2, GameObject projectilePrefab = null, GameObject hitVfxPrefab = null)
        {
            AttackBehaviorBase attack = null;

            switch (type)
            {
                case AttackType.None:
                    return null;

                case AttackType.Melee:
                    attack = new MeleeAttack();
                    break;

                case AttackType.Projectile:
                    attack = new ProjectileAttack(param1, projectilePrefab, hitVfxPrefab);
                    break;

                case AttackType.Cleave:
                    attack = new CleaveAttack(param1);
                    break;

                case AttackType.Spread:
                    attack = new SpreadAttack((int)param1, param2, 10f, projectilePrefab, hitVfxPrefab);
                    break;

                case AttackType.Beam:
                    attack = new BeamAttack(param1, param2, projectilePrefab); // projectilePrefab을 beamVfxPrefab으로 사용
                    break;

                // TODO: Phase 3에서 추가
                // case AttackType.Lob:

                default:
                    attack = new MeleeAttack();
                    break;
            }

            if (attack != null)
            {
                attack._hitVfxPrefab = hitVfxPrefab;
            }

            return attack;
        }
    }
}

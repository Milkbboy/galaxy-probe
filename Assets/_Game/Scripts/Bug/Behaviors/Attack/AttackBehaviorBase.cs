using UnityEngine;
using System;
using DrillCorp.Bug.Behaviors.Data;
using DrillCorp.Core;

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
            var damageable = target.GetComponent<IDamageable>();
            damageable?.TakeDamage(damage);
        }

        /// <summary>
        /// Attack 타입에 따른 인스턴스 생성
        /// </summary>
        public static AttackBehaviorBase Create(AttackType type, float param1, float param2, GameObject projectilePrefab = null)
        {
            switch (type)
            {
                case AttackType.None:
                    return null;

                case AttackType.Melee:
                    return new MeleeAttack();

                case AttackType.Projectile:
                    return new ProjectileAttack(param1, projectilePrefab);

                // TODO: Phase 2에서 추가
                // case AttackType.Cleave:
                // case AttackType.Spread:
                // case AttackType.Homing:
                // case AttackType.Beam:
                // case AttackType.Lob:

                default:
                    return new MeleeAttack();
            }
        }
    }
}

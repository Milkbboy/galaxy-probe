using UnityEngine;
using DrillCorp.Bug.Behaviors.Data;

namespace DrillCorp.Bug.Behaviors.Passive
{
    /// <summary>
    /// 패시브 행동 기본 클래스
    /// </summary>
    public abstract class PassiveBehaviorBase : IPassiveBehavior
    {
        protected BugController _bug;

        public virtual void Initialize(BugController bug)
        {
            _bug = bug;
        }

        public virtual void Cleanup()
        {
            _bug = null;
        }

        /// <summary>
        /// 매 프레임 업데이트 (기본: 아무것도 안 함)
        /// </summary>
        public virtual void UpdatePassive(float deltaTime)
        {
        }

        /// <summary>
        /// 받는 데미지 처리 (기본: 그대로 반환)
        /// </summary>
        public virtual float ProcessIncomingDamage(float damage)
        {
            return damage;
        }

        /// <summary>
        /// 주는 데미지 처리 (기본: 아무것도 안 함)
        /// </summary>
        public virtual void ProcessOutgoingDamage(float damage, Transform target)
        {
        }

        /// <summary>
        /// Passive 타입에 따른 인스턴스 생성
        /// </summary>
        public static PassiveBehaviorBase Create(PassiveType type, float param1, float param2)
        {
            switch (type)
            {
                case PassiveType.Armor:
                    return new ArmorPassive(param1);

                case PassiveType.Dodge:
                    return new DodgePassive(param1);

                case PassiveType.Shield:
                    return new ShieldPassive(param1, param2);

                case PassiveType.Regen:
                    return new RegenPassive(param1, param2);

                case PassiveType.PoisonAttack:
                    return new PoisonAttackPassive(param1, param2);

                // TODO: Phase 3에서 추가
                // case PassiveType.Reflect:
                // case PassiveType.Lifesteal:
                // case PassiveType.CritChance:
                // case PassiveType.Fast:

                default:
                    return null;
            }
        }
    }
}

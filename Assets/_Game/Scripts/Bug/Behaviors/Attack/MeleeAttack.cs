using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Attack
{
    /// <summary>
    /// 근접 공격
    /// </summary>
    public class MeleeAttack : AttackBehaviorBase
    {
        protected override void PerformAttack(Transform target)
        {
            // 직접 데미지
            DealDamage(target, GetDamage());

            // TODO: 공격 애니메이션 트리거
            // TODO: 공격 이펙트 재생
        }
    }
}

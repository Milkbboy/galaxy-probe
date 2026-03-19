using UnityEngine;

namespace DrillCorp.Bug
{
    /// <summary>
    /// 딱정벌레 - 기본 근접 공격 벌레
    /// 특징: 느리지만 체력이 높음
    /// </summary>
    public class BeetleBug : BugBase
    {
        protected override float GetAttackRange()
        {
            return 1.2f;
        }
    }
}

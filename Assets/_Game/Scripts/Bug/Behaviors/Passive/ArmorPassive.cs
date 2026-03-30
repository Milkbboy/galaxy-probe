using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Passive
{
    /// <summary>
    /// 방어력 패시브 - 받는 데미지 고정값 감소
    /// </summary>
    public class ArmorPassive : PassiveBehaviorBase
    {
        private float _armorValue;
        private bool _isBroken; // ArmorBreak 트리거에 의해 파괴됨

        public ArmorPassive(float value)
        {
            _armorValue = value > 0f ? value : 0f;
        }

        /// <summary>
        /// 방어력 파괴 (ArmorBreak 트리거용)
        /// </summary>
        public void Break()
        {
            _isBroken = true;
        }

        public override float ProcessIncomingDamage(float damage)
        {
            if (_isBroken) return damage;

            // 데미지에서 방어력만큼 감소 (최소 1)
            float reduced = damage - _armorValue;
            return Mathf.Max(1f, reduced);
        }
    }
}

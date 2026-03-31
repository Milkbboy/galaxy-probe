using UnityEngine;
using DrillCorp.UI;
using DrillCorp.VFX;

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
            float finalDamage = Mathf.Max(1f, reduced);

            // 방어력으로 감소된 데미지 표시
            if (_bug != null && _armorValue > 0f && damage > finalDamage)
            {
                float blocked = damage - finalDamage;
                DamagePopup.CreateText(_bug.transform.position, $"-{blocked:F0}", Color.gray);
                SimpleVFX.PlayArmorBlock(_bug.transform.position);
            }

            return finalDamage;
        }
    }
}

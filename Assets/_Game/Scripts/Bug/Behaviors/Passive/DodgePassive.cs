using UnityEngine;
using DrillCorp.UI;
using DrillCorp.VFX;

namespace DrillCorp.Bug.Behaviors.Passive
{
    /// <summary>
    /// 회피 패시브 - 확률적으로 데미지 무시
    /// </summary>
    public class DodgePassive : PassiveBehaviorBase
    {
        private float _dodgeChance; // 0~100

        public DodgePassive(float chance)
        {
            _dodgeChance = Mathf.Clamp(chance, 0f, 100f);
        }

        public override float ProcessIncomingDamage(float damage)
        {
            // 확률 체크
            float roll = Random.Range(0f, 100f);
            if (roll < _dodgeChance)
            {
                // 회피 성공! - 팝업 및 이펙트 표시
                if (_bug != null)
                {
                    DamagePopup.Create(_bug.transform.position, 0f, PopupType.Dodge);
                    SimpleVFX.PlayDodge(_bug.transform.position);
                }
                return 0f; // 데미지 완전 무효
            }

            return damage;
        }
    }
}

using UnityEngine;

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
                // 회피 성공!
                // TODO: 회피 이펙트/텍스트 표시 ("Miss!")
                Debug.Log($"[DodgePassive] Dodged! (roll: {roll:F1} < chance: {_dodgeChance})");
                return 0f; // 데미지 완전 무효
            }

            return damage;
        }
    }
}

using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Passive
{
    /// <summary>
    /// 체력 재생 패시브 - 지속적으로 체력 회복
    /// param1 = 초당 회복량 (기본 5)
    /// param2 = 회복 시작 대기시간 (피격 후, 기본 3초)
    /// </summary>
    public class RegenPassive : PassiveBehaviorBase
    {
        private float _regenPerSecond;
        private float _regenDelay;
        private float _timeSinceLastHit;

        public RegenPassive(float regenPerSecond = 5f, float regenDelay = 3f)
        {
            _regenPerSecond = regenPerSecond > 0f ? regenPerSecond : 5f;
            _regenDelay = regenDelay >= 0f ? regenDelay : 3f;
            _timeSinceLastHit = _regenDelay; // 시작 시 재생 가능
        }

        public override void UpdatePassive(float deltaTime)
        {
            if (_bug == null) return;

            _timeSinceLastHit += deltaTime;

            // 재생 대기시간이 지났고, 체력이 최대가 아니면 재생
            if (_timeSinceLastHit >= _regenDelay && _bug.CurrentHealth < _bug.MaxHealth)
            {
                float healAmount = _regenPerSecond * deltaTime;
                _bug.Heal(healAmount);
            }
        }

        public override float ProcessIncomingDamage(float damage)
        {
            // 피격 시 재생 타이머 리셋
            _timeSinceLastHit = 0f;
            return damage;
        }
    }
}

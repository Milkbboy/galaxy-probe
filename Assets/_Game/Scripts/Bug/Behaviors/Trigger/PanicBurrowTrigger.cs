using UnityEngine;
using DrillCorp.Bug.Behaviors.Passive;

namespace DrillCorp.Bug.Behaviors.Trigger
{
    /// <summary>
    /// 위협 회피 트리거 - HP가 일정 이하일 때 피격 시 Burrow 발동
    /// param1 = HP 임계값 % (기본 50%)
    /// param2 = 쿨다운 (초, 기본 5초)
    /// </summary>
    public class PanicBurrowTrigger : TriggerBehaviorBase
    {
        private float _hpThresholdPercent;
        private float _cooldown;
        private float _currentCooldown;
        private float _lastHp;

        public PanicBurrowTrigger(float hpThreshold = 50f, float cooldown = 5f)
        {
            _hpThresholdPercent = hpThreshold > 0f ? hpThreshold : 50f;
            _cooldown = cooldown > 0f ? cooldown : 5f;
            _currentCooldown = 0f;
            _triggerOnDeath = false;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            _lastHp = bug.CurrentHealth;
        }

        public override void CheckAndTrigger()
        {
            if (_bug == null) return;

            // 쿨다운 감소
            if (_currentCooldown > 0f)
            {
                _currentCooldown -= Time.deltaTime;
            }

            // 피격 체크 (HP 감소 감지)
            float currentHp = _bug.CurrentHealth;
            bool wasHit = currentHp < _lastHp;
            _lastHp = currentHp;

            if (!wasHit) return;

            // 쿨다운 중이면 무시
            if (_currentCooldown > 0f) return;

            // HP가 임계값 이하인지 체크
            if (_bug.HealthPercent > _hpThresholdPercent) return;

            // Burrow 패시브 확인 및 발동
            var burrow = _bug.GetBurrowPassive();
            if (burrow != null && burrow.TryBurrow())
            {
                _currentCooldown = _cooldown;
                Debug.Log($"[PanicBurrowTrigger] {_bug.name} burrowed! HP: {_bug.HealthPercent:F0}%");
            }
        }

        protected override void OnTriggered()
        {
            // 반복 발동이므로 사용 안 함
        }
    }
}

using UnityEngine;
using DrillCorp.UI;

namespace DrillCorp.Bug.Behaviors.Trigger
{
    /// <summary>
    /// 광폭화 트리거 - HP가 일정 이하로 떨어지면 공격력/이동속도 증가
    /// param1 = HP 임계값 % (기본 30%)
    /// param2 = 공격력/속도 배율 (기본 2배)
    /// </summary>
    public class EnrageTrigger : TriggerBehaviorBase
    {
        private float _hpThresholdPercent;
        private float _multiplier;

        public EnrageTrigger(float hpThreshold = 30f, float multiplier = 2f)
        {
            _hpThresholdPercent = hpThreshold > 0f ? hpThreshold : 30f;
            _multiplier = multiplier > 0f ? multiplier : 2f;
            _triggerOnDeath = false;
        }

        public override void CheckAndTrigger()
        {
            if (_bug == null || _hasTriggered) return;

            // HP가 임계값 이하인지 체크
            if (_bug.HealthPercent <= _hpThresholdPercent)
            {
                Trigger();
            }
        }

        protected override void OnTriggered()
        {
            if (_bug == null) return;

            // 이동 속도 증가
            var movement = _bug.CurrentMovement;
            if (movement != null)
            {
                movement.SpeedMultiplier *= _multiplier;
            }

            // 공격력 증가
            var attack = _bug.CurrentAttack;
            if (attack != null)
            {
                attack.DamageMultiplier *= _multiplier;
            }

            // 팝업 표시
            DamagePopup.CreateText(_bug.transform.position, "ENRAGED!", new Color(1f, 0.3f, 0.1f));

            // 이펙트 (빨간 플래시)
            VFX.SimpleVFX.PlayBugHit(_bug.transform.position);

            Debug.Log($"[EnrageTrigger] {_bug.name} enraged! x{_multiplier}");
        }
    }
}

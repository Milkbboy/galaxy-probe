using UnityEngine;
using DrillCorp.Bug.Behaviors.Data;

namespace DrillCorp.Bug.Behaviors.Trigger
{
    /// <summary>
    /// 트리거 행동 기본 클래스 (조건 발동)
    /// </summary>
    public abstract class TriggerBehaviorBase : ITriggerBehavior
    {
        protected BugController _bug;
        protected bool _hasTriggered;
        protected bool _triggerOnDeath;

        public bool HasTriggered => _hasTriggered;
        public bool TriggerOnDeath => _triggerOnDeath;

        public virtual void Initialize(BugController bug)
        {
            _bug = bug;
            _hasTriggered = false;
        }

        public virtual void Cleanup()
        {
            _bug = null;
        }

        public abstract void CheckAndTrigger();

        public virtual void OnDeath()
        {
            // 사망 트리거가 아니면 무시
        }

        /// <summary>
        /// 트리거 발동
        /// </summary>
        protected void Trigger()
        {
            if (_hasTriggered) return;

            _hasTriggered = true;
            OnTriggered();
        }

        /// <summary>
        /// 트리거 발동 시 실행
        /// </summary>
        protected abstract void OnTriggered();

        /// <summary>
        /// Trigger 타입에 따른 인스턴스 생성
        /// </summary>
        public static TriggerBehaviorBase Create(TriggerType type, float param1, float param2, float param3, GameObject prefab = null)
        {
            switch (type)
            {
                case TriggerType.Enrage:
                    return new EnrageTrigger(param1, param2);

                case TriggerType.ExplodeOnDeath:
                    return new ExplodeOnDeathTrigger(param1, param2);

                case TriggerType.PanicBurrow:
                    return new PanicBurrowTrigger(param1, param2);

                // TODO: Phase 3에서 추가
                // case TriggerType.LastStand:
                // case TriggerType.Transform:
                // case TriggerType.SplitOnDeath:
                // case TriggerType.Revive:

                default:
                    return null;
            }
        }
    }
}

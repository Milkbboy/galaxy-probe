using UnityEngine;
using DrillCorp.Bug.Behaviors.Data;

namespace DrillCorp.Bug.Behaviors.Skill
{
    /// <summary>
    /// 스킬 행동 기본 클래스 (쿨다운 기반)
    /// </summary>
    public abstract class SkillBehaviorBase : ISkillBehavior
    {
        protected BugController _bug;
        protected float _cooldown;
        protected float _currentCooldown;
        protected float _range;

        public float Cooldown => _cooldown;
        public bool IsReady => _currentCooldown <= 0f;

        public SkillBehaviorBase(float cooldown = 5f, float range = 5f)
        {
            _cooldown = cooldown > 0f ? cooldown : 5f;
            _range = range > 0f ? range : 5f;
            _currentCooldown = 0f;
        }

        public virtual void Initialize(BugController bug)
        {
            _bug = bug;
            _currentCooldown = 0f; // 시작 시 즉시 사용 가능
        }

        public virtual void Cleanup()
        {
            _bug = null;
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (_currentCooldown > 0f)
            {
                _currentCooldown -= deltaTime;
            }
        }

        public bool TryUse(Transform target)
        {
            if (_bug == null || target == null) return false;
            if (!IsReady) return false;

            // 거리 체크
            float distance = _bug.GetDistanceTo(target);
            if (distance > _range) return false;

            // 스킬 사용
            UseSkill(target);
            _currentCooldown = _cooldown;

            return true;
        }

        protected abstract void UseSkill(Transform target);

        /// <summary>
        /// Skill 타입에 따른 인스턴스 생성
        /// </summary>
        public static SkillBehaviorBase Create(SkillType type, float cooldown, float param1, float param2,
            GameObject spawnPrefab = null, GameObject effectPrefab = null)
        {
            switch (type)
            {
                case SkillType.Spawn:
                    return new SpawnSkill(cooldown, (int)param1, spawnPrefab);

                case SkillType.Nova:
                    return new NovaSkill(cooldown, param1, effectPrefab);

                case SkillType.BuffAlly:
                    // param1 = 범위, param2 = 공격력 배율, cooldown = 이속 배율
                    return new BuffAllySkill(param1, param2, cooldown, effectPrefab);

                case SkillType.HealAlly:
                    // param1 = 범위, param2 = 회복량 (초당), cooldown = 회복 주기
                    return new HealAllySkill(param1, param2, cooldown, effectPrefab);

                default:
                    return null;
            }
        }
    }
}

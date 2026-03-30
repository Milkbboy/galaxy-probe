using UnityEngine;

namespace DrillCorp.Bug.Behaviors
{
    /// <summary>
    /// 모든 행동의 기본 인터페이스
    /// </summary>
    public interface IBehavior
    {
        /// <summary>
        /// 행동 초기화
        /// </summary>
        void Initialize(BugController bug);

        /// <summary>
        /// 행동 정리 (버그 사망 시 등)
        /// </summary>
        void Cleanup();
    }

    /// <summary>
    /// 이동 행동 인터페이스
    /// </summary>
    public interface IMovementBehavior : IBehavior
    {
        /// <summary>
        /// 매 프레임 이동 처리
        /// </summary>
        void UpdateMovement(Transform target);

        /// <summary>
        /// 현재 이동 속도 배율 (버프/디버프 적용용)
        /// </summary>
        float SpeedMultiplier { get; set; }
    }

    /// <summary>
    /// 기본 공격 행동 인터페이스
    /// </summary>
    public interface IAttackBehavior : IBehavior
    {
        /// <summary>
        /// 공격 시도 (쿨다운 체크 포함)
        /// </summary>
        /// <returns>공격 성공 여부</returns>
        bool TryAttack(Transform target);

        /// <summary>
        /// 공격 사거리
        /// </summary>
        float AttackRange { get; }

        /// <summary>
        /// 현재 공격력 배율 (버프/디버프 적용용)
        /// </summary>
        float DamageMultiplier { get; set; }

        /// <summary>
        /// 공격 후 이벤트
        /// </summary>
        event System.Action OnAttackPerformed;
    }

    /// <summary>
    /// 스킬 행동 인터페이스 (쿨다운 기반)
    /// </summary>
    public interface ISkillBehavior : IBehavior
    {
        /// <summary>
        /// 스킬 쿨다운 (초)
        /// </summary>
        float Cooldown { get; }

        /// <summary>
        /// 스킬 사용 가능 여부
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// 스킬 사용 시도
        /// </summary>
        /// <returns>사용 성공 여부</returns>
        bool TryUse(Transform target);

        /// <summary>
        /// 쿨다운 업데이트 (매 프레임)
        /// </summary>
        void UpdateCooldown(float deltaTime);
    }

    /// <summary>
    /// 패시브 행동 인터페이스 (상시 적용)
    /// </summary>
    public interface IPassiveBehavior : IBehavior
    {
        /// <summary>
        /// 매 프레임 업데이트 (Regen 등)
        /// </summary>
        void UpdatePassive(float deltaTime);

        /// <summary>
        /// 데미지 받기 전 처리 (Armor, Shield, Dodge 등)
        /// </summary>
        /// <param name="damage">원래 데미지</param>
        /// <returns>처리 후 데미지 (0이면 회피/흡수됨)</returns>
        float ProcessIncomingDamage(float damage);

        /// <summary>
        /// 데미지 주기 전 처리 (PoisonAttack 등)
        /// </summary>
        /// <param name="damage">원래 데미지</param>
        /// <param name="target">공격 대상</param>
        void ProcessOutgoingDamage(float damage, Transform target);
    }

    /// <summary>
    /// 트리거 행동 인터페이스 (조건 발동)
    /// </summary>
    public interface ITriggerBehavior : IBehavior
    {
        /// <summary>
        /// 트리거 조건 체크 및 발동
        /// </summary>
        void CheckAndTrigger();

        /// <summary>
        /// 이미 발동되었는지 (1회성 트리거용)
        /// </summary>
        bool HasTriggered { get; }

        /// <summary>
        /// 사망 시 발동 트리거인지
        /// </summary>
        bool TriggerOnDeath { get; }

        /// <summary>
        /// 사망 시 호출
        /// </summary>
        void OnDeath();
    }
}

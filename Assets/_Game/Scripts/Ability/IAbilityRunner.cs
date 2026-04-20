using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Ability
{
    /// <summary>
    /// 어빌리티 타입별 런타임 실행 계약.
    /// AbilitySlotController가 AbilityType별로 구현체를 인스턴스화 → Initialize → Tick/TryUse.
    ///
    /// 생명주기:
    ///   1. Initialize(data, ctx)      — 세션 시작 시 1회
    ///   2. Tick(dt) 매 프레임          — 쿨다운 감소 / 지속형 틱 / AutoInterval 자동 발동
    ///   3. TryUse(aimPoint) 키 입력시  — Manual 트리거만. 쿨다운 남았으면 false 반환
    ///
    /// 해석 규칙(스펙: docs/Phase5_VictorAbility_Plan.md):
    ///   · AbilityData.Damage  — ability마다 의미가 다르다 (틱당 / 초당 / 회당)
    ///     네이팜=틱당, 화염방사기=초당(dps), 지뢰=1회당(BombWeapon 실효값에 배율로 덧대짐)
    ///   · AbilityData.Angle   — 라디안 (화염방사기 부채꼴 반각)
    ///   · AbilityData.Range   — Unity 월드 유닛 (반경 / 길이 / 반폭)
    /// </summary>
    public interface IAbilityRunner
    {
        AbilityType Type { get; }

        /// <summary>세션 시작 시 1회 호출. 이후 data/ctx는 변경되지 않는 것으로 가정.</summary>
        void Initialize(AbilityData data, AbilityContext ctx);

        /// <summary>매 프레임 호출. dt = Time.deltaTime.</summary>
        void Tick(float dt);

        /// <summary>
        /// 수동 발동. AutoInterval 타입은 무시하고 false.
        /// 쿨다운 중이거나 가드 조건(최대 배치 수 초과 등) 미충족이면 false.
        /// </summary>
        bool TryUse(Vector3 aimPoint);

        /// <summary>UI 쿨다운 표시용. 0 = 사용가능, 1 = 방금 사용. [0,1] 범위.</summary>
        float CooldownNormalized { get; }
    }
}

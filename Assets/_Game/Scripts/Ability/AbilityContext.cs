using UnityEngine;
using DrillCorp.Aim;
using DrillCorp.Machine;
using DrillCorp.Weapon.Bomb;

namespace DrillCorp.Ability
{
    /// <summary>
    /// Runner 초기화 시 전달되는 런타임 참조 묶음.
    /// AbilitySlotController가 매 세션 1회만 구성한다.
    /// 모든 필드는 Runner 구현체가 필요시에만 읽는다 — 일부 필드는 null일 수 있음.
    /// </summary>
    public class AbilityContext
    {
        /// <summary>머신 중심 Transform (네이팜/화염방사기의 발사 원점).</summary>
        public Transform MachineTransform;

        /// <summary>마우스 에임 포인트·벌레 레이어·머신 참조 제공자.</summary>
        public AimController Aim;

        /// <summary>Physics.OverlapXxx 에 사용할 Bug 레이어 마스크.</summary>
        public LayerMask BugLayer;

        /// <summary>스폰된 VFX/배치형 오브젝트의 부모 Transform (하이어라키 정리용). null 허용.</summary>
        public Transform VfxParent;

        /// <summary>
        /// 폭탄 무기 참조 — 지뢰 어빌리티가 `EffectiveDamage`/`EffectiveRadius`를 읽어 쓴다.
        /// 씬에 BombWeapon이 없거나 해금 전이면 null. MineRunner가 null-safe 처리.
        /// </summary>
        public BombWeapon BombWeapon;

        /// <summary>
        /// 머신 참조 — 지누스 채굴 드론이 `AddBonusMining(x)`로 세션 채굴량을 증가시킨다.
        /// 씬에 MachineController가 없으면 null. MiningDroneRunner가 null-safe 처리.
        /// </summary>
        public MachineController Machine;
    }
}

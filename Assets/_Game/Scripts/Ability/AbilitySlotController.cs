using UnityEngine;
using UnityEngine.InputSystem;
using DrillCorp.Ability.Runners;
using DrillCorp.Aim;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.Machine;
using DrillCorp.OutGame;
using DrillCorp.Weapon.Bomb;

namespace DrillCorp.Ability
{
    /// <summary>
    /// Game 씬에 1개 배치. 선택된 캐릭터의 3슬롯 어빌리티를 런타임 실행한다.
    ///
    /// Start:
    ///   1) CharacterRegistry + DataManager 에서 SelectedCharacterId로 CharacterData 조회
    ///      (단독 실행 호환: 둘 중 하나라도 없으면 인스펙터 `_character` fallback)
    ///   2) 슬롯 1~3 각각 AbilityData 꺼내서
    ///      - PlayerData.HasAbility 체크(UnlockedAbilities 기반)
    ///      - 해금된 타입에 맞는 IAbilityRunner 생성 + Initialize
    ///
    /// Update:
    ///   - 매 프레임 모든 runner.Tick(dt)
    ///   - 키 1/2/3 눌림 → 해당 슬롯 runner.TryUse(aim.AimPosition)
    ///   - AutoInterval 트리거는 Runner 내부에서 Tick으로 자동 발동 (여기선 TryUse 안 부름)
    ///
    /// CLAUDE.md 준수: New Input System(Keyboard.current) 사용, 레거시 UnityEngine.Input 금지.
    /// </summary>
    public class AbilitySlotController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("비우면 DataManager.Data.SelectedCharacterId + CharacterRegistry로 자동 조회. " +
                 "Game 씬 단독 실행 시 인스펙터로 직접 할당할 수 있다.")]
        [SerializeField] private CharacterData _characterOverride;

        [Tooltip("비우면 FindAnyObjectByType<AimController>()로 자동 탐색.")]
        [SerializeField] private AimController _aim;

        [Tooltip("비우면 자기 자신(this.transform)을 VFX 부모로 사용.")]
        [SerializeField] private Transform _vfxParent;

        [Header("Dev")]
        [Tooltip("true면 PlayerData.UnlockedAbilities 검사를 건너뛰고 3슬롯 모두 인스턴스화. " +
                 "단독 실행/플레이테스트 용.")]
        [SerializeField] private bool _ignoreUnlockGate;

        private readonly IAbilityRunner[] _runners = new IAbilityRunner[3];

        private CharacterData _resolvedCharacter;

        private void Start()
        {
            ResolveCharacter();
            if (_resolvedCharacter == null)
            {
                Debug.LogWarning("[AbilitySlotController] CharacterData 를 해결할 수 없어 비활성화합니다. " +
                                 "(CharacterRegistry/DataManager 또는 _characterOverride 필요)");
                enabled = false;
                return;
            }

            ResolveReferences();

            var ctx = BuildContext();

            for (int slot = 1; slot <= 3; slot++)
            {
                var data = _resolvedCharacter.GetAbility(slot);
                if (data == null) continue;

                if (!_ignoreUnlockGate && !IsUnlocked(data))
                    continue;

                var runner = CreateRunner(data.Type);
                if (runner == null) continue; // 미구현 타입 — Step 3/4/5에서 채움

                runner.Initialize(data, ctx);
                _runners[slot - 1] = runner;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _runners.Length; i++)
                _runners[i]?.Tick(dt);

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) TryUseSlot(0);
            if (kb.digit2Key.wasPressedThisFrame) TryUseSlot(1);
            if (kb.digit3Key.wasPressedThisFrame) TryUseSlot(2);
        }

        private void TryUseSlot(int idx)
        {
            var runner = _runners[idx];
            if (runner == null || _aim == null) return;
            runner.TryUse(_aim.AimPosition);
        }

        // ─── 외부 조회 (UI 바인딩용) ───

        public IAbilityRunner GetRunner(int slotKey)
        {
            int idx = slotKey - 1;
            return (idx >= 0 && idx < _runners.Length) ? _runners[idx] : null;
        }

        public CharacterData ResolvedCharacter => _resolvedCharacter;

        // ─── 내부 ───

        private void ResolveCharacter()
        {
            if (_characterOverride != null)
            {
                _resolvedCharacter = _characterOverride;
                return;
            }

            var dm = DataManager.Instance;
            var reg = CharacterRegistry.Instance;
            if (dm?.Data != null && reg != null)
                _resolvedCharacter = reg.Find(dm.Data.SelectedCharacterId);
        }

        private void ResolveReferences()
        {
            if (_aim == null) _aim = FindAnyObjectByType<AimController>();
            if (_vfxParent == null) _vfxParent = transform;
        }

        private AbilityContext BuildContext()
        {
            var ctx = new AbilityContext
            {
                MachineTransform = _aim != null ? _aim.MachineTransform : null,
                Aim = _aim,
                BugLayer = _aim != null ? _aim.BugLayer : 0,
                VfxParent = _vfxParent,
                BombWeapon = FindAnyObjectByType<BombWeapon>(),
                Machine = FindAnyObjectByType<MachineController>(),
            };
            return ctx;
        }

        private bool IsUnlocked(AbilityData data)
        {
            var dm = DataManager.Instance;
            if (dm?.Data == null) return false;
            return dm.Data.HasAbility(data.AbilityId);
        }

        /// <summary>
        /// AbilityType → IAbilityRunner 구현체 매핑.
        /// Phase5: Napalm/Flame/Mine. Phase6: BlackHole/Shockwave/Meteor. Jinus 3종은 후속 Phase.
        /// </summary>
        private static IAbilityRunner CreateRunner(AbilityType type)
        {
            switch (type)
            {
                case AbilityType.Napalm:    return new NapalmRunner();
                case AbilityType.Flame:     return new FlameRunner();
                case AbilityType.Mine:      return new MineRunner();
                case AbilityType.BlackHole:   return new BlackHoleRunner();
                case AbilityType.Shockwave:   return new ShockwaveRunner();
                case AbilityType.Meteor:      return new MeteorRunner();
                case AbilityType.Drone:       return new DroneRunner();
                case AbilityType.MiningDrone: return new MiningDroneRunner();
                case AbilityType.SpiderDrone: return new SpiderDroneRunner();
                default:
                    return null;
            }
        }
    }
}

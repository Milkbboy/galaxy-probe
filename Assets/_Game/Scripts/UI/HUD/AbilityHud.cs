using UnityEngine;
using DrillCorp.Ability;
using DrillCorp.Data;

namespace DrillCorp.UI.HUD
{
    /// <summary>
    /// Game 씬 우상단 어빌리티 HUD 컨테이너.
    /// AbilitySlotController 에서 캐릭터 + 3 Runner 를 가져와 AbilitySlotUI 3개에 분배한다.
    ///
    /// 캐릭터 이름은 본 컴포넌트가 표시하지 않는다 — TopBarHud 좌측 슬롯이 전담.
    /// (Game 씬 레이아웃: 좌상단 TopBarHud / 좌측 WeaponPanel / 우상단 AbilityHud)
    ///
    /// 에디터 스크립트 AbilityHudSetupEditor 가 Canvas 자식으로 자동 생성 + 자식 참조 자동 바인딩.
    /// </summary>
    public class AbilityHud : MonoBehaviour
    {
        [Header("Slots — 1/2/3 순서 고정 (길이 3)")]
        [SerializeField] private AbilitySlotUI[] _slots = new AbilitySlotUI[3];

        [Header("References")]
        [Tooltip("비우면 FindAnyObjectByType<AbilitySlotController>() 자동 탐색")]
        [SerializeField] private AbilitySlotController _controller;

        private bool _bound;

        private void Start()
        {
            // AbilitySlotController.Start 가 먼저 끝나야 Runner 가 생성되어 있다.
            // 같은 Start 라도 실행 순서는 보장되지 않으므로, 못 찾으면 다음 Update 에서 한 번 더.
            TryBind();
        }

        private void Update()
        {
            if (!_bound)
            {
                TryBind();
                if (!_bound) return;
            }

            for (int i = 0; i < _slots.Length; i++)
                _slots[i]?.Refresh();
        }

        private void TryBind()
        {
            if (_controller == null)
                _controller = FindAnyObjectByType<AbilitySlotController>();

            if (_controller == null) return;
            var character = _controller.ResolvedCharacter;
            if (character == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot == null) continue;

                int slotKey = i + 1;
                IAbilityRunner runner = _controller.GetRunner(slotKey);
                AbilityData data = character.GetAbility(slotKey);
                slot.Bind(runner, data);
            }

            _bound = true;
        }
    }
}

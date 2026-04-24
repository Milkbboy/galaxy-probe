using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.Machine;
using DrillCorp.OutGame;

namespace DrillCorp.Pickup
{
    /// <summary>
    /// 벌레 사망 이벤트를 구독해 보석 드랍 여부를 결정·스폰하는 씬 싱글턴.
    /// v2 규칙: 기본 드랍 + gem_drop 업그레이드 %p 추가, 엘리트는 100%.
    /// 기본 드랍 확률은 MachineController 의 MachineData.BaseGemDropRate (v2: 0.05) 참조 —
    /// 없으면 _fallbackDropChance 사용 (디버그·에디터 세팅 안 된 상태 방어).
    ///
    /// Game 씬에 하나의 GameObject로 존재. DontDestroyOnLoad 안 씀 (세션 스코프).
    /// </summary>
    public class GemDropSpawner : MonoBehaviour
    {
        [Tooltip("MachineController 미발견 시 폴백 값. 정상 동작 시 MachineData.BaseGemDropRate 가 사용됨.")]
        [Range(0f, 1f)]
        [SerializeField] private float _fallbackDropChance = 0.05f;

        [Tooltip("엘리트 벌레 드랍 확률 (강화 무관 고정). v2: 1.0")]
        [Range(0f, 1f)]
        [SerializeField] private float _eliteDropChance = 1f;

        [Tooltip("월드 보석 스프라이트. 비워두면 단색 Quad로 대체.")]
        [SerializeField] private Sprite _gemSprite;

        // v2 보석 규칙 — 일반 보석 color #aadfff / value 1, 엘리트 보석 color #ffd700 / value 5.
        private static readonly Color NormalGemColor = new Color(0.67f, 0.87f, 1f, 1f);
        private static readonly Color EliteGemColor  = new Color(1f, 0.84f, 0f, 1f);
        private const int NormalGemValue = 1;
        private const int EliteGemValue  = 5;

        private MachineController _machine;

        private void OnEnable()
        {
            GameEvents.OnBugDied += HandleBugDied;
            _machine = FindAnyObjectByType<MachineController>();
        }

        private void OnDisable()
        {
            GameEvents.OnBugDied -= HandleBugDied;
        }

        private float GetBaseDropChance()
        {
            if (_machine == null) _machine = FindAnyObjectByType<MachineController>();
            var data = _machine != null ? _machine.MachineData : null;
            return data != null ? data.BaseGemDropRate : _fallbackDropChance;
        }

        private void HandleBugDied(Vector3 position, bool isElite)
        {
            float chance = isElite
                ? _eliteDropChance
                : GetBaseDropChance() + GetUpgradeBonus();

            if (Random.value > chance) return;

            if (isElite)
                Gem.Create(position, _gemSprite, EliteGemValue, EliteGemColor);
            else
                Gem.Create(position, _gemSprite, NormalGemValue, NormalGemColor);
        }

        private static float GetUpgradeBonus()
        {
            var um = UpgradeManager.Instance;
            if (um == null) return 0f;
            // GemDropRate: +2%/lv Add — SO _isPercentage=false, _valuePerLevel=0.02
            return um.GetTotalBonus(UpgradeType.GemDropRate);
        }
    }
}

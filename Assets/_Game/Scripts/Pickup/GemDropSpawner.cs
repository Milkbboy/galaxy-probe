using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.OutGame;

namespace DrillCorp.Pickup
{
    /// <summary>
    /// 벌레 사망 이벤트를 구독해 보석 드랍 여부를 결정·스폰하는 씬 싱글턴.
    /// v2 규칙: 기본 5% 드랍 + gem_drop 업그레이드 %p 추가, 엘리트는 100%.
    ///
    /// Game 씬에 하나의 GameObject로 존재. DontDestroyOnLoad 안 씀 (세션 스코프).
    /// </summary>
    public class GemDropSpawner : MonoBehaviour
    {
        [Tooltip("일반 벌레의 기본 드랍 확률 (gem_drop 강화 전). v2: 0.05")]
        [Range(0f, 1f)]
        [SerializeField] private float _baseDropChance = 0.05f;

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

        private void OnEnable()
        {
            GameEvents.OnBugDied += HandleBugDied;
        }

        private void OnDisable()
        {
            GameEvents.OnBugDied -= HandleBugDied;
        }

        private void HandleBugDied(Vector3 position, bool isElite)
        {
            float chance = isElite
                ? _eliteDropChance
                : _baseDropChance + GetUpgradeBonus();

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

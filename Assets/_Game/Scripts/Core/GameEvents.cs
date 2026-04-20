using System;
using UnityEngine;

namespace DrillCorp.Core
{
    public static class GameEvents
    {
        // 게임 상태
        public static Action<GameState> OnGameStateChanged;

        // 머신 관련
        public static Action<float> OnMachineDamaged;       // 받은 데미지
        public static Action OnMachineDestroyed;

        // 벌레 관련
        public static Action<int> OnBugKilled;              // 처치한 벌레 ID (UI 카운터용)
        public static Action<Vector3, bool> OnBugDied;      // v2 — 사망 위치 + 엘리트 여부 (GemDropSpawner용)
        public static Action<float> OnBugScoreEarned;       // v2 — 벌레 처치 점수 (세션 광석 보너스 계산용)

        // 웨이브 관련
        public static Action<int> OnWaveStarted;            // 웨이브 번호
        public static Action<int> OnWaveCompleted;          // 웨이브 번호

        // 세션 결과
        public static Action OnSessionSuccess;
        public static Action OnSessionFailed;

        // 채굴/재화
        public static Action<int> OnCurrencyChanged;        // 레거시 (Ore와 동일) — 기존 UI 호환용
        public static Action<int> OnOreChanged;             // v2 — 플레이어 보유 광석 변동 (DataManager.Ore)
        public static Action<int> OnGemsChanged;            // v2 — 플레이어 보유 보석 변동 (DataManager.Gems)
        public static Action<int> OnMiningGained;           // 획득한 채굴량 (세션 내 누적)
        public static Action<int> OnGemCollected;           // v2 — 세션 중 채집한 보석 수 (1회당 invoke, HUD 누적용)
        public static Action<int> OnSessionOreChanged;      // v2 — 세션 광석 총량 (MachineController._sessionOre)
        public static Action<int> OnSessionGemsChanged;     // v2 — 세션 보석 총량 (Gem 채집마다 invoke)

        // 강화 시스템
        public static Action<string, int> OnUpgradePurchased;  // 업그레이드 ID, 새 레벨
        public static Action<string> OnWeaponUpgraded;         // v2 — 무기 강화 ID
        public static Action<string> OnWeaponUnlocked;         // v2 — 해금된 무기 ID
        public static Action<string> OnAbilityUnlocked;        // v2 — 해금된 어빌리티 ID

        // 머신/캐릭터 선택
        public static Action<int> OnMachineSelected;          // 선택한 머신 ID (레거시)
        public static Action<string> OnCharacterSelected;     // v2 — 선택된 캐릭터 ID
    }
}

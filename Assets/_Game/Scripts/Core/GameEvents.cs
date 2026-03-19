using System;

namespace DrillCorp.Core
{
    public static class GameEvents
    {
        // 게임 상태
        public static Action<GameState> OnGameStateChanged;

        // 머신 관련
        public static Action<float> OnMachineDamaged;       // 받은 데미지
        public static Action OnMachineDestroyed;
        public static Action<float> OnFuelChanged;          // 현재 연료량

        // 벌레 관련
        public static Action<int> OnBugKilled;              // 처치한 벌레 ID

        // 웨이브 관련
        public static Action<int> OnWaveStarted;            // 웨이브 번호
        public static Action<int> OnWaveCompleted;          // 웨이브 번호

        // 세션 결과
        public static Action OnSessionSuccess;
        public static Action OnSessionFailed;

        // 채굴/재화
        public static Action<int> OnCurrencyChanged;        // 현재 재화량
        public static Action<int> OnMiningGained;           // 획득한 채굴량
    }
}

using UnityEngine;

namespace DrillCorp.Data
{
    /// <summary>
    /// 웨이브 진행 시 Spawner/TunnelEventManager에 주입되는 파라미터 오버라이드 테이블.
    /// -1 또는 빈 값 = SpawnConfig 폴백 사용. 0 = 명시적 비활성.
    /// 시트 WaveData 탭 1행 = 이 SO 1개.
    /// (Phase C 완료 후 기존 WaveData.cs 제거되면 이 클래스가 WaveData로 rename 예정)
    /// </summary>
    [CreateAssetMenu(fileName = "Wave_", menuName = "Drill-Corp/Simple Wave Data", order = 2)]
    public class SimpleWaveData : ScriptableObject
    {
        [Header("Wave Info")]
        public int WaveNumber = 1;
        public string WaveName = "";

        [Tooltip("이 웨이브에서 누적해야 할 처치 점수. 도달 시 다음 웨이브로 전환. -1 또는 0 = 전환 없음(세션 끝까지 유지).")]
        public float KillTarget = 15f;

        [Header("Spawn Overrides (-1 = SpawnConfig 폴백)")]
        [Tooltip("일반 벌레 스폰 주기 (초). -1이면 SpawnConfig.DefaultNormalSpawnInterval")]
        public float NormalSpawnInterval = -1f;

        [Tooltip("엘리트 스폰 주기 (초). -1이면 폴백, 0이면 엘리트 비활성")]
        public float EliteSpawnInterval = -1f;

        [Tooltip("동시 생존 상한. -1이면 SpawnConfig.DefaultMaxBugs")]
        public int MaxBugs = -1;

        [Header("Tunnel Overrides")]
        [Tooltip("이 웨이브부터 땅굴 이벤트 활성")]
        public bool TunnelEnabled = false;

        [Tooltip("땅굴 주기 (초). -1이면 SpawnConfig.DefaultTunnelEventInterval")]
        public float TunnelEventInterval = -1f;

        [Tooltip("한 땅굴당 Swift 수. -1이면 SpawnConfig.DefaultSwiftPerTunnel")]
        public int SwiftPerTunnel = -1;

        // -1 sentinel 해석 헬퍼. SpawnConfig 폴백을 주입해 실제 값을 반환.
        public float ResolveNormalSpawnInterval(SpawnConfigData cfg)
            => NormalSpawnInterval >= 0f ? NormalSpawnInterval : cfg.DefaultNormalSpawnInterval;

        // -1 (미지정) → 폴백 대신 "엘리트 비활성"으로 해석 (시트 Wave 1·2 의도).
        // 0 명시도 비활성. 양수면 그 값 사용.
        public float ResolveEliteSpawnInterval(SpawnConfigData cfg)
            => EliteSpawnInterval > 0f ? EliteSpawnInterval : 0f;

        public int ResolveMaxBugs(SpawnConfigData cfg)
            => MaxBugs >= 0 ? MaxBugs : cfg.DefaultMaxBugs;

        public float ResolveTunnelEventInterval(SpawnConfigData cfg)
            => TunnelEventInterval >= 0f ? TunnelEventInterval : cfg.DefaultTunnelEventInterval;

        public int ResolveSwiftPerTunnel(SpawnConfigData cfg)
            => SwiftPerTunnel >= 0 ? SwiftPerTunnel : cfg.DefaultSwiftPerTunnel;
    }
}

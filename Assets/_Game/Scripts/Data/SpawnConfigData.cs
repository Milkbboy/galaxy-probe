using UnityEngine;

namespace DrillCorp.Data
{
    /// <summary>
    /// 화면 밖 스폰 영역 모양.
    /// Circle: 머신 중심 원형 둘레 (대각선이 화면 모서리에 닿아 진입까지 시간 소요)
    /// Rect:   카메라 사각형 둘레 (방향에 관계없이 비슷한 시간에 화면 진입)
    /// </summary>
    public enum SpawnShape
    {
        Circle = 0,
        Rect = 1
    }

    /// <summary>
    /// SimpleBugSpawner / TunnelEventManager가 사용하는 전역 폴백값.
    /// 시트에 없는 값(튜닝 빈도 낮은 것) + 웨이브별 오버라이드의 디폴트.
    /// SSoT: 시트 'SpawnConfigData' 1행 ↔ SpawnConfig.asset (BossData 와 동일 패턴).
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnConfig", menuName = "Drill-Corp/Spawn Config", order = 2)]
    public class SpawnConfigData : ScriptableObject
    {
        [Header("Spawn Defaults")]
        [Tooltip("일반 벌레 기본 스폰 주기 (초)")]
        public float DefaultNormalSpawnInterval = 0.083f;

        [Tooltip("엘리트 기본 스폰 주기 (초)")]
        public float DefaultEliteSpawnInterval = 15f;

        [Tooltip("동시 생존 상한")]
        public int DefaultMaxBugs = 90;

        [Header("Tunnel Defaults")]
        [Tooltip("게임 시작 후 이 시간 지나야 땅굴 활성 (TunnelEnabled 웨이브라도 이 값 미만이면 대기)")]
        public float TunnelGameTimeStart = 30f;

        [Tooltip("땅굴 이벤트 기본 주기 (초)")]
        public float DefaultTunnelEventInterval = 15f;

        [Tooltip("한 땅굴당 Swift 기본 수")]
        public int DefaultSwiftPerTunnel = 10;

        [Tooltip("한 땅굴 내 Swift 생성 간격 (튜닝 빈도 낮아 웨이브 오버라이드 없음)")]
        public float TunnelSpawnInterval = 0.2f;

        [Header("Spawn Area")]
        [Tooltip("Circle: 머신 중심 원형 둘레 (현재 기본). Rect: 카메라 사각형 둘레 (어느 방향이든 균등하게 진입 — 5초 정적 해소)")]
        public SpawnShape SpawnShape = SpawnShape.Rect;

        [Tooltip("true면 카메라에서 자동 반경 계산 (Circle 모드만 적용)")]
        public bool AutoRadius = true;

        [Tooltip("AutoRadius=false일 때 수동 반경 (Circle 모드만 적용)")]
        public float ManualRadius = 15f;

        [Tooltip("일반 벌레 스폰 반경 추가 여유 (Circle: 반지름 가산 / Rect: 사각형 외곽 두께)")]
        public float NormalMargin = 0.4f;

        [Tooltip("엘리트 스폰 반경 추가 여유 (Circle: 반지름 가산 / Rect: 사각형 외곽 두께)")]
        public float EliteMargin = 0.5f;

        [Tooltip("땅굴 위치 화면 가장자리 안쪽 여유")]
        public float EdgeMargin = 0.4f;

        [Tooltip("땅굴 지점 주변 Swift 랜덤 오프셋")]
        public float SpawnJitter = 0.15f;
    }
}

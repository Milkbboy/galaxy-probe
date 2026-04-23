using UnityEngine;

namespace DrillCorp.Data
{
    /// <summary>
    /// SimpleBugSpawner / TunnelEventManager가 사용하는 전역 폴백값.
    /// 시트에 없는 값(튜닝 빈도 낮은 것) + 웨이브별 오버라이드의 디폴트.
    /// 시트 Import가 건드리지 않음 — 인스펙터 직편집 전용.
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
        [Tooltip("true면 카메라에서 자동 반경 계산")]
        public bool AutoRadius = true;

        [Tooltip("AutoRadius=false일 때 수동 반경")]
        public float ManualRadius = 15f;

        [Tooltip("일반 벌레 스폰 반경 추가 여유")]
        public float NormalMargin = 0.4f;

        [Tooltip("엘리트 스폰 반경 추가 여유")]
        public float EliteMargin = 0.5f;

        [Tooltip("땅굴 위치 화면 가장자리 안쪽 여유")]
        public float EdgeMargin = 0.4f;

        [Tooltip("땅굴 지점 주변 Swift 랜덤 오프셋")]
        public float SpawnJitter = 0.15f;
    }
}

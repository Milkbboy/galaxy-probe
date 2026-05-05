using UnityEngine;

namespace DrillCorp.Data
{
    /// <summary>
    /// 게임 전역 상수 — 1행 SO 들이 늘어나는 것을 막기 위해 한 곳으로 통합.
    /// 시트 'Constants' 의 Key/Value 행이 여기 public 필드로 1:1 매핑되며,
    /// Importer 가 SerializedObject.FindProperty(Key) 로 자동 디스패치 — 새 상수 추가는
    /// 이 파일에 필드 한 줄 + 시트에 row 한 줄만 추가하면 끝(Importer 코드 변경 불필요).
    ///
    /// 필드명 = 시트 Key 컬럼 값. 그룹 prefix (Aim/Gem/...) 로 의미 분리.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConstants", menuName = "Drill-Corp/Game Constants", order = 5)]
    public class GameConstantsData : ScriptableObject
    {
        [Header("Aim — 마우스 조준")]
        [Tooltip("마우스 커서 판정 반경(월드 유닛). 크로스헤어 비주얼이 자동으로 이 반경에 맞춰 스케일된다 — '비주얼 = 판정' 항상 일치.")]
        [Min(0.05f)] public float AimRadius = 0.5f;

        [Tooltip("크로스헤어·링·라벨이 지면(Y=0)보다 떠있는 높이. 벌레 스프라이트보다 크게 둘 것.")]
        [Min(0.1f)] public float AimCrosshairHeight = 2f;

        [Header("Gem — 보석")]
        [Tooltip("보석 비주얼 지름(월드 유닛). 스프라이트 PPU 와 무관하게 이 크기로 정규화.")]
        [Min(0.1f)] public float GemSpriteSize = 1.2f;

        [Tooltip("마우스가 이 반경 안에 들어오면 호버 판정. 보통 GemSpriteSize/2 와 비슷하게.")]
        [Min(0.05f)] public float GemPickupRadius = 0.6f;

        [Tooltip("호버 누적 시 채집 완료까지 기본 시간(초). gem_speed 업그레이드는 이 값 위에 +20%/lv.")]
        [Min(0.1f)] public float GemPickupDuration = 2.0f;

        [Tooltip("호버 이탈 시 진행도 감소 배율. 0=리셋 안 함, 1.0=채집 속도와 동일 속도로 감소. 작을수록 잠시 벗어나도 진행도 유지.")]
        [Min(0f)] public float GemHoverDecayMul = 0.5f;

        [Tooltip("진행 링 반경(월드 유닛). 보통 GemPickupRadius 와 동일.")]
        [Min(0.05f)] public float GemRingRadius = 0.6f;

        [Tooltip("진행 링 두께(LineRenderer widthMultiplier).")]
        [Min(0.005f)] public float GemRingWidth = 0.07f;
    }
}

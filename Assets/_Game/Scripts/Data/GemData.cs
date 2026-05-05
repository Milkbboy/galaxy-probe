using UnityEngine;

namespace DrillCorp.Data
{
    /// <summary>
    /// 보석(Gem) 크기·채집 튜닝 1행 SO. 시트 'GemData' ↔ Assets/_Game/Data/GemConfig.asset.
    /// GemDropSpawner 가 OnEnable 에서 Gem.ActiveData 에 주입 → Gem.Create() 시점에 인스턴스 필드로 스냅샷.
    /// 보석 가치(Normal/Elite Value)·드랍 확률은 GemDropSpawner / MachineData 에서 관리, 여기는 크기/타이밍만.
    /// </summary>
    [CreateAssetMenu(fileName = "GemConfig", menuName = "Drill-Corp/Gem Config", order = 4)]
    public class GemData : ScriptableObject
    {
        [Header("Visual Size")]
        [Tooltip("보석 비주얼 지름 (월드 유닛). 스프라이트 PPU 와 무관하게 이 크기로 정규화.")]
        [Min(0.1f)] public float SpriteSize = 1.2f;

        [Header("Pickup")]
        [Tooltip("마우스가 이 반경 안에 들어오면 호버 판정. 시각적으로는 SpriteSize/2 와 비슷하게 두면 자연스럽다.")]
        [Min(0.05f)] public float PickupRadius = 0.6f;

        [Tooltip("호버 누적 시 채집 완료까지 걸리는 기본 시간(초). gem_speed 업그레이드는 이 값 위에 +20%/lv.")]
        [Min(0.1f)] public float PickupDuration = 2.0f;

        [Tooltip("호버 이탈 시 진행도 감소 배율(1.0 = 채집 속도와 같은 속도로 감소). 작을수록 잠시 벗어나도 진행도가 잘 안 빠짐.")]
        [Range(0f, 4f)] public float HoverDecayMul = 0.5f;

        [Header("Progress Ring")]
        [Tooltip("진행 링 반경(월드 유닛). 보통 PickupRadius 와 동일.")]
        [Min(0.05f)] public float RingRadius = 0.6f;

        [Tooltip("진행 링 두께(LineRenderer widthMultiplier).")]
        [Min(0.005f)] public float RingWidth = 0.07f;
    }
}

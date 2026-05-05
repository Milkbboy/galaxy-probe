using UnityEngine;

namespace DrillCorp.Data
{
    /// <summary>
    /// 에임(조준) 크기·시각 튜닝 1행 SO. 시트 'AimData' ↔ Assets/_Game/Data/AimConfig.asset.
    /// AimController가 Awake에서 읽어 적용 — 자동 반경 계산이 켜진 경우 CrosshairScale이
    /// 크로스헤어 SpriteRenderer.localScale 에 적용된 뒤 bounds 가 측정되므로
    /// "크로스헤어 비주얼 크기 = 판정 반경" 관계가 유지된다.
    /// SpawnConfigData / BossData 와 동일 패턴.
    /// </summary>
    [CreateAssetMenu(fileName = "AimConfig", menuName = "Drill-Corp/Aim Config", order = 3)]
    public class AimData : ScriptableObject
    {
        [Header("Aim Radius")]
        [Tooltip("AutoCalculateRadius=false 일 때만 사용. 마우스 커서 판정 반경(월드 유닛).")]
        [Min(0.05f)] public float AimRadius = 0.5f;

        [Tooltip("true면 크로스헤어 스프라이트 bounds 로부터 자동 계산. CrosshairScale 적용 후 측정.")]
        public bool AutoCalculateRadius = true;

        [Header("Crosshair Visual")]
        [Tooltip("크로스헤어 SpriteRenderer.localScale 배율. 자동 계산 모드에선 판정 반경도 함께 변함.")]
        [Min(0.1f)] public float CrosshairScale = 1.0f;

        [Tooltip("크로스헤어·링·라벨이 지면(Y=0)보다 얼마나 위에 떠있을지. 벌레 스프라이트보다 크게 둘 것.")]
        [Range(0.1f, 10f)] public float CrosshairHeight = 2f;
    }
}

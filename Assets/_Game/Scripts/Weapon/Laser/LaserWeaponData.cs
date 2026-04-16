using UnityEngine;

namespace DrillCorp.Weapon.Laser
{
    /// <summary>
    /// 레이저 (Phase 4 — 자동 스폰 빔 추적 사격)
    /// 프로토타입 _.html L166 기준: cd=300프레임=5s, dur=360프레임=6s, speed=1.725 u/s, range=0.48u, dmg=0.8/tick, tick=6프레임=0.1s
    /// 베이스 FireDelay는 0으로 강제 — 게이팅은 LaserWeapon.ShouldFire가 전담.
    /// 레거시 LaserBeamData(Heat 시스템)와 공존 — 이름 구분: Weapon_LaserBeam.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_LaserBeam", menuName = "Drill-Corp/Weapons/Laser (Beam)", order = 50)]
    public class LaserWeaponData : WeaponData
    {
        [Header("Laser — Timing")]
        [Tooltip("빔 소멸 후 다음 빔 스폰까지 대기 시간 (초)\n• 프로토 300프레임 = 5s\n• 빔 스폰 순간 즉시 풀로 세팅, 빔 소멸 후 감소 시작")]
        [Range(1f, 15f)]
        [SerializeField] private float _cooldown = 5f;

        [Tooltip("빔 수명 (초)\n• 프로토 360프레임 = 6s\n• 빔 스폰 시 life = maxLife = BeamDuration")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _beamDuration = 6f;

        [Header("Laser — Movement")]
        [Tooltip("마우스 추적 속도 (유닛/초)\n• 프로토 1.725 px/frame ÷ 60fps = 103.5 px/s → 월드 1 유닛 = 프로토 1px 매핑\n• 낮을수록 느린 추적")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _beamSpeed = 1.725f;

        [Tooltip("추적 정지 거리 (유닛)\n• 빔 중심과 마우스 거리가 이 값 이내면 이동 안 함 (미세 지글링 방지)\n• 프로토 2px = 0.033u")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _stopDistance = 0.033f;

        [Header("Laser — Damage")]
        [Tooltip("피격 반경 (유닛)\n• 빔 중심 기준 OverlapSphere 반경\n• 프로토 28.8px = 0.48u (게임 시각/플레이감 위해 1.0으로 키움)")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _beamRadius = 1.0f;

        [Tooltip("데미지 틱 간격 (초)\n• 이 간격마다 OverlapSphere → Damage 적용\n• 프로토 6프레임 = 0.1s → DPS = Damage × (1 / 0.1) = 8")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _tickInterval = 0.1f;

        [Header("Laser — Prefab")]
        [Tooltip("LaserBeam 컴포넌트가 붙은 빔 프리펩 (4겹 SR + LifeArc LineRenderer 자식 포함)")]
        [SerializeField] private GameObject _beamPrefab;

        public float Cooldown => _cooldown;
        public float BeamDuration => _beamDuration;
        public float BeamSpeed => _beamSpeed;
        public float StopDistance => _stopDistance;
        public float BeamRadius => _beamRadius;
        public float TickInterval => _tickInterval;
        public GameObject BeamPrefab => _beamPrefab;
    }
}

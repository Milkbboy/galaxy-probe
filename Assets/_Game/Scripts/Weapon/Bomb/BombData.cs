using UnityEngine;

namespace DrillCorp.Weapon.Bomb
{
    /// <summary>
    /// 폭탄 데이터 (수동 클릭 발사 + 투사체 + AoE 폭발)
    /// 프로토타입 _.html L166: cd=360frame=6s, radius=110px=1.8u, speed=5, dmg=3
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_Bomb", menuName = "Drill-Corp/Weapons/Bomb", order = 30)]
    public class BombData : WeaponData
    {
        [Header("Bomb")]
        [Tooltip("폭발 AoE 반경 (월드 유닛)\n• 도달 위치 중심으로 OverlapSphere 적용\n• 업그레이드로 +20%씩 증가 가능")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _explosionRadius = 1.8f;

        [Header("Delivery")]
        [Tooltip(
            "즉시 폭발 모드 (true)\n" +
            "• true:  클릭한 자리에서 바로 폭발 (투사체 비행 없음, '바로 펑')\n" +
            "• false: ProjectilePrefab을 머신에서 발사해 클릭 위치까지 비행 후 폭발 (수류탄 투척)"
        )]
        [SerializeField] private bool _instant = true;

        [Tooltip("[비행 모드 전용] 투사체 비행 속도 (유닛/초). _instant=true면 무시됨")]
        [Range(1f, 20f)]
        [SerializeField] private float _projectileSpeed = 5f;

        [Tooltip(
            "[비행 모드 전용] 투사체 최대 수명 (초)\n" +
            "• 타겟 도달하지 못해도 이 시간 후 강제 폭발 (안전망)\n" +
            "• 속도 × 수명 = 이론적 최대 사거리"
        )]
        [Range(1f, 10f)]
        [SerializeField] private float _projectileLifetime = 5f;

        [Tooltip("[비행 모드 전용] BombProjectile 컴포넌트가 붙은 투사체 프리펩. _instant=true면 사용 안 함")]
        [SerializeField] private GameObject _projectilePrefab;

        [Header("Explosion VFX")]
        [Tooltip("폭발 위치에 스폰되는 VFX 프리펩 (선택)")]
        [SerializeField] private GameObject _explosionVfxPrefab;

        [Tooltip("폭발 VFX 자동 파괴 시간 (초)")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _explosionVfxLifetime = 1.5f;

        [Header("Landing Marker")]
        [Tooltip(
            "착탄 예정 위치에 표시되는 마커 프리펩 (선택)\n" +
            "• 클릭 즉시 타겟 위치에 스폰, 폭발 시 함께 제거\n" +
            "• 스케일은 ExplosionRadius × 2 로 자동 조정 (반경과 정확히 일치)"
        )]
        [SerializeField] private GameObject _landingMarkerPrefab;

        public float ExplosionRadius => _explosionRadius;
        public bool Instant => _instant;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public GameObject ExplosionVfxPrefab => _explosionVfxPrefab;
        public float ExplosionVfxLifetime => _explosionVfxLifetime;
        public GameObject LandingMarkerPrefab => _landingMarkerPrefab;
    }
}

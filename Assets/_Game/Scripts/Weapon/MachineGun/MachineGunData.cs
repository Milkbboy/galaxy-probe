using UnityEngine;

namespace DrillCorp.Weapon.MachineGun
{
    /// <summary>
    /// 기관총 데이터 (자동 연사 + 산포 + 탄창/리로딩)
    /// 프로토타입 _.html L166: fireCD=8.4프레임=0.14s, dmg=0.5, speed=9, maxAmmo=40, reload=300프레임=5s
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_MachineGun", menuName = "Drill-Corp/Weapons/MachineGun", order = 40)]
    public class MachineGunData : WeaponData
    {
        [Header("Magazine")]
        [Tooltip("탄창 최대 탄수")]
        [Range(1, 200)]
        [SerializeField] private int _maxAmmo = 40;

        [Tooltip("리로딩 시간 (초)")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _reloadDuration = 5f;

        [Tooltip(
            "탄 잔량 경고 임계값\n" +
            "• 이 이하로 떨어지면 슬롯 테두리가 빨강 경고 색으로 변함"
        )]
        [Range(1, 50)]
        [SerializeField] private int _lowAmmoThreshold = 8;

        [Header("Bullet")]
        [Tooltip("탄환 비행 속도 (유닛/초)")]
        [Range(1f, 30f)]
        [SerializeField] private float _bulletSpeed = 9f;

        [Tooltip(
            "탄환 최대 수명 (초)\n" +
            "• 명중하지 못해도 이 시간 후 사라짐\n" +
            "• 속도 × 수명 = 최대 사거리 (9 × 1.5 = 13.5유닛)"
        )]
        [Range(0.2f, 5f)]
        [SerializeField] private float _bulletLifetime = 1.5f;

        [Tooltip(
            "탄환 명중 판정 반경 (유닛)\n" +
            "• 매 프레임 OverlapSphere(반경)로 충돌 검사\n" +
            "• 너무 작으면 빠른 탄이 벌레를 통과할 수 있음"
        )]
        [Range(0.05f, 1f)]
        [SerializeField] private float _bulletHitRadius = 0.15f;

        [Header("Spread")]
        [Tooltip(
            "발사 산포 (라디안)\n" +
            "• 매 발사마다 ±이 값 사이의 무작위 각도 적용\n" +
            "• 0.06 ≈ ±3.4° (프로토타입 기본값)"
        )]
        [Range(0f, 0.5f)]
        [SerializeField] private float _spreadAngle = 0.06f;

        [Header("Bullet Prefab")]
        [Tooltip("MachineGunBullet 컴포넌트가 붙은 탄환 프리펩")]
        [SerializeField] private GameObject _bulletPrefab;

        public int MaxAmmo => _maxAmmo;
        public float ReloadDuration => _reloadDuration;
        public int LowAmmoThreshold => _lowAmmoThreshold;
        public float BulletSpeed => _bulletSpeed;
        public float BulletLifetime => _bulletLifetime;
        public float BulletHitRadius => _bulletHitRadius;
        public float SpreadAngle => _spreadAngle;
        public GameObject BulletPrefab => _bulletPrefab;
    }
}

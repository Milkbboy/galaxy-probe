using UnityEngine;

namespace DrillCorp.Weapon.Shotgun
{
    [CreateAssetMenu(fileName = "Weapon_Shotgun", menuName = "Drill-Corp/Weapons/Shotgun", order = 20)]
    public class ShotgunData : WeaponData
    {
        [Header("Shotgun")]
        [Tooltip(
            "발사 시 스폰되는 머즐 플래시 VFX\n" +
            "• 에임 위치에서 스폰\n" +
            "• null 허용"
        )]
        [SerializeField] private GameObject _muzzleVfxPrefab;

        [Tooltip("머즐 VFX 자동 파괴 시간 (초)")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _muzzleVfxLifetime = 0.3f;

        [Tooltip(
            "데미지 적용 비율 (에임 범위 내 전체 Bug에)\n" +
            "• 1.0: 100% 데미지\n" +
            "• 0.5: 50% 데미지 (많은 대상에 효율적으로 분산)"
        )]
        [Range(0.1f, 2f)]
        [SerializeField] private float _damageMultiplier = 1f;

        public GameObject MuzzleVfxPrefab => _muzzleVfxPrefab;
        public float MuzzleVfxLifetime => _muzzleVfxLifetime;
        public float DamageMultiplier => _damageMultiplier;
    }
}

using UnityEngine;
using DrillCorp.Aim;

namespace DrillCorp.Weapon.Shotgun
{
    /// <summary>
    /// 샷건 - 에임 범위 내 전체 Bug에 AoE 데미지 (탕!)
    /// </summary>
    public class ShotgunWeapon : WeaponBase
    {
        [Header("Data")]
        [SerializeField] private ShotgunData _data;

        private void Awake()
        {
            _baseData = _data;
        }

        protected override void Fire(AimController aim)
        {
            if (_data == null) return;

            float damage = _data.Damage * _data.DamageMultiplier;

            var bugs = aim.BugsInRange;
            for (int i = 0; i < bugs.Count; i++)
            {
                var c = bugs[i];
                if (c == null) continue;
                DealDamage(c.transform, damage);
            }

            SpawnMuzzleVfx(aim.AimPosition);
        }

        private void SpawnMuzzleVfx(Vector3 pos)
        {
            if (_data.MuzzleVfxPrefab == null) return;
            var vfx = Instantiate(_data.MuzzleVfxPrefab, pos, Quaternion.identity);
            Destroy(vfx, _data.MuzzleVfxLifetime);
        }
    }
}

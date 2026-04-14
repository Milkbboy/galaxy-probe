using UnityEngine;
using DrillCorp.Aim;

namespace DrillCorp.Weapon.BurstGun
{
    /// <summary>
    /// 버스트건 - 에임 중심에 가장 가까운 Bug 1마리에 단발 데미지 (다!다!다!)
    /// </summary>
    public class BurstGunWeapon : WeaponBase
    {
        [Header("Data")]
        [SerializeField] private BurstGunData _data;

        private void Awake()
        {
            _baseData = _data;
        }

        protected override void Fire(AimController aim)
        {
            if (_data == null) return;

            var target = aim.GetClosestBugToAim();
            if (target == null) return;

            DealDamage(target.transform, _data.Damage);
        }
    }
}

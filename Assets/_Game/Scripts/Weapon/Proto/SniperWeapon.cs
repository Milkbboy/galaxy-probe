using UnityEngine;
using DrillCorp.Aim;
using DrillCorp.Audio;

namespace DrillCorp.Weapon.Proto
{
    /// <summary>
    /// 저격총 (_.html 프로토타입 포팅)
    /// - 에임 범위 내 모든 Bug 동시 피격
    /// - 쿨다운은 실제 발사 시에만 소비 (타겟 없으면 쿨 안 돌아감)
    /// - 자동 발사 (타겟 있으면 쿨 끝나고 발사)
    /// </summary>
    public class SniperWeapon : WeaponBase
    {
        [Header("Data")]
        [SerializeField] private SniperWeaponData _data;

        public SniperWeaponData Data => _data;
        // ThemeColor는 WeaponBase가 _baseData(= _data)에서 자동 제공 — 중복 정의 제거됨

        private void Awake()
        {
            _baseData = _data;
        }

        protected override void Fire(AimController aim)
        {
            if (_data == null) return;

            float damage = _data.Damage;
            var bugs = aim.BugsInRange;
            int hit = 0;

            for (int i = 0; i < bugs.Count; i++)
            {
                var c = bugs[i];
                if (c == null) continue;
                DealDamage(c.transform, damage);
                hit++;
            }

            if (hit > 0)
            {
                SpawnHitVfx(aim.AimPosition);
                AudioManager.Instance?.PlaySniperFire();
            }
        }

        /// <summary>
        /// UI 상태 텍스트 (슬롯 .w-cool에 표시)
        /// 프로토타입 규칙: 타겟 없음→"대기", 준비+타겟→"발사!", 쿨 중→"0.3s"
        /// </summary>
        public string GetStateText(AimController aim)
        {
            if (aim == null) return "--";
            bool hasTarget = aim.HasBugInRange;
            if (!hasTarget) return "대기";
            if (CanFire) return "발사!";
            float remain = Mathf.Max(0f, _nextFireTime - Time.time);
            return $"{remain:0.0}s";
        }

        /// <summary>
        /// UI 바 색상 (ready/sniper)
        /// </summary>
        public Color GetBarColor(AimController aim, Color readyColor)
        {
            if (aim == null || !aim.HasBugInRange) return readyColor;
            return CanFire ? readyColor : ThemeColor;
        }
    }
}

using UnityEngine;
using DrillCorp.Aim;
using DrillCorp.Audio;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.OutGame;

namespace DrillCorp.Weapon.Proto
{
    /// <summary>
    /// 저격총 (_.html / v2.html 프로토타입 포팅)
    /// - 에임 범위 내 모든 Bug 동시 피격
    /// - 쿨다운은 실제 발사 시에만 소비 (타겟 없으면 쿨 안 돌아감)
    /// - 자동 발사 (타겟 있으면 쿨 끝나고 발사)
    ///
    /// v2 포팅 후 자체 구동 — AimController.EquipWeapon 경로 없이 매 프레임 TryFire 호출.
    /// (폭탄·기관총·레이저·톱날과 동일 패턴)
    /// </summary>
    public class SniperWeapon : WeaponBase
    {
        [Header("Data")]
        [SerializeField] private SniperWeaponData _data;

        [Header("Self-Driven")]
        [Tooltip("AimController 참조 (비우면 Start에서 자동 탐색)")]
        [SerializeField] private AimController _aimController;

        [Tooltip("게임 시작 직후 발사 지연 (초) — 씬 로딩 직후 즉시 발사 방지")]
        [SerializeField] private float _startDelay = 0.3f;

        private float _fireEnableTime;

        // === Effective stats (WeaponUpgrade 반영) ===
        private float _effectiveDamage;
        private float _effectiveFireDelayMul = 1f;

        public SniperWeaponData Data => _data;
        // ThemeColor는 WeaponBase가 _baseData(= _data)에서 자동 제공 — 중복 정의 제거됨

        protected override float EffectiveFireDelay
            => _data != null ? _data.FireDelay * _effectiveFireDelayMul : 0f;

        private void Awake()
        {
            _baseData = _data;
        }

        private void OnEnable()
        {
            GameEvents.OnWeaponUpgraded += OnWeaponUpgradedAny;
            RefreshEffectiveStats();
        }

        private void OnDisable()
        {
            GameEvents.OnWeaponUpgraded -= OnWeaponUpgradedAny;
        }

        private void Start()
        {
            if (TryDisableIfLocked()) return;

            if (_aimController == null)
                _aimController = FindAnyObjectByType<AimController>();

            if (_aimController != null) _aim = _aimController;
            RefreshEffectiveStats();
            _fireEnableTime = Time.time + _startDelay;
        }

        private void Update()
        {
            if (Time.time < _fireEnableTime) return;
            if (_aimController != null) TryFire(_aimController);
        }

        // === Upgrade 반영 ===
        private void OnWeaponUpgradedAny(string upgradeId)
        {
            if (_data == null || string.IsNullOrEmpty(_data.WeaponId)) return;
            if (string.IsNullOrEmpty(upgradeId)) { RefreshEffectiveStats(); return; }

            var mgr = WeaponUpgradeManager.Instance;
            var u = mgr != null ? mgr.FindUpgrade(upgradeId) : null;
            if (u != null && u.WeaponId == _data.WeaponId) RefreshEffectiveStats();
        }

        private void RefreshEffectiveStats()
        {
            if (_data == null) return;

            float dmgMul = 1f, cdMul = 1f, rangeMul = 1f;
            var mgr = WeaponUpgradeManager.Instance;
            if (mgr != null && !string.IsNullOrEmpty(_data.WeaponId))
            {
                (_, dmgMul)   = mgr.GetBonus(_data.WeaponId, WeaponUpgradeStat.Damage);
                (_, cdMul)    = mgr.GetBonus(_data.WeaponId, WeaponUpgradeStat.Cooldown);
                (_, rangeMul) = mgr.GetBonus(_data.WeaponId, WeaponUpgradeStat.Range);
            }
            _effectiveDamage = _data.Damage * dmgMul;
            _effectiveFireDelayMul = Mathf.Max(0.1f, cdMul);  // 너무 짧아지지 않게 클램프

            // v2 — 저격총 range 업그레이드는 에임 반경 자체를 확장 (에임 원·호·판정 모두).
            // 저격총이 에임의 기본 주인: 업그레이드 배율을 AimController에 주입.
            if (_aimController != null)
                _aimController.SetRangeMultiplier(rangeMul);
        }

        protected override void Fire(AimController aim)
        {
            if (_data == null) return;

            float damage = _effectiveDamage;
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

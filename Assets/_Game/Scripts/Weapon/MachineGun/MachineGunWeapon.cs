using UnityEngine;
using DrillCorp.Aim;
using DrillCorp.Audio;

namespace DrillCorp.Weapon.MachineGun
{
    /// <summary>
    /// 기관총 (자동 연사 + 산포 + 탄창/리로딩)
    /// 프로토타입 _.html L266~ fireGun + L295 update bullets 동작 이식.
    ///
    /// 동작:
    /// - 에임 범위 내 적 있을 때 fireDelay 간격으로 자동 발사
    /// - 매 발 산포 ±_spreadAngle (라디안)
    /// - 탄창 0이 되면 리로딩 시작 (reloadDuration 후 maxAmmo 회복)
    /// - 리로딩 중에는 발사 불가
    ///
    /// UI:
    /// - 평소: 파랑 바 (탄 잔량 비율, 감소형) / "32발"
    /// - 리로딩: 빨강 바 (진행 비율, 차오름) / "리로딩 2.3s" + 검은 오버레이
    /// - 탄 8개 이하: 테두리 빨강 경고
    /// </summary>
    public class MachineGunWeapon : WeaponBase
    {
        [Header("Data")]
        [SerializeField] private MachineGunData _data;

        [Header("Self-Driven")]
        [Tooltip("AimController 참조 (비우면 Start에서 자동 탐색). " +
                 "기관총은 메인 무기(스나이퍼) 슬롯과 무관하게 매 프레임 자체 발사 루프를 돈다 — " +
                 "레벨업으로 해금되는 보조 무기 컨셉. 폭탄과 동일 패턴.")]
        [SerializeField] private AimController _aimController;

        [Header("Fire Point (선택)")]
        [Tooltip(
            "탄환 발사 위치 + 방향을 결정하는 Transform.\n" +
            "• 비워두면: 머신 중앙(aim.MachineTransform)에서 에임 방향으로 발사\n" +
            "• Turret의 배럴 끝 자식 GameObject를 할당하면: 배럴 끝에서 배럴이 바라보는 +Z 방향(forward)으로 발사 — 시각적으로 자연스러움\n" +
            "• 권장 셋업: TurretController의 'Pivot' 자식에 빈 GameObject 'FirePoint' 추가, localPosition = (0, 0, barrelSize.z) (배럴 끝점)"
        )]
        [SerializeField] private Transform _firePoint;

        public MachineGunData Data => _data;

        // 프로토타입 #4fc3f7 (cool-bar.gun 색)
        private static readonly Color GunBlue = new Color(0.298f, 0.765f, 0.969f, 1f);

        private int _currentAmmo;
        private float _reloadEndTime;     // Time.time 기준 리로딩 종료 시각
        private bool _isReloading;

        public int CurrentAmmo => _currentAmmo;
        public bool IsReloading => _isReloading;
        public float ReloadRemaining => Mathf.Max(0f, _reloadEndTime - Time.time);

        private void Awake()
        {
            _baseData = _data;
            if (_data != null) _currentAmmo = _data.MaxAmmo;
        }

        private void Start()
        {
            if (_aimController == null)
                _aimController = FindAnyObjectByType<AimController>();

            // _aim 캐싱 — UI 프로퍼티(HasTarget 등)가 _aim을 참조
            if (_aimController != null) _aim = _aimController;
        }

        private void Update()
        {
            // 자체 발사 루프 — AimController.TryFireWeapon에 의존하지 않음
            // (기관총은 보조 무기로 메인 무기 슬롯과 병렬 동작)
            if (_aimController != null) TryFire(_aimController);
        }

        public override void OnEquip(AimController aim)
        {
            base.OnEquip(aim);
            // 장착 시점에 탄창이 0이거나 이상하면 풀 충전 (안전망)
            // (메인 무기로 쓸 경우에 대비 — 자체 구동 모드에선 이 경로 안 거침)
            if (_data != null && _currentAmmo <= 0 && !_isReloading)
                _currentAmmo = _data.MaxAmmo;
        }

        // ────── 발사 흐름 ──────

        protected override bool ShouldFire(AimController aim)
        {
            // 리로딩 중엔 발사 불가
            if (_isReloading)
            {
                // 리로딩 완료 체크 — 완료 시점에 탄창 회복
                if (Time.time >= _reloadEndTime)
                {
                    _isReloading = false;
                    if (_data != null) _currentAmmo = _data.MaxAmmo;
                }
                return false;
            }

            if (_data == null || _currentAmmo <= 0) return false;

            // 자동 연사 — 적 유무 무관, 에임 방향으로 계속 발사
            // (탄창 소진 시 리로딩 사이클로 자연스럽게 들어감)
            return true;
        }

        protected override void Fire(AimController aim)
        {
            if (_data == null || _data.BulletPrefab == null) return;

            // 발사 위치: FirePoint 우선 (Turret 배럴 끝), 없으면 머신 중앙
            Vector3 spawnPos = _firePoint != null
                ? _firePoint.position
                : (aim.MachineTransform != null ? aim.MachineTransform.position : aim.AimPosition);
            spawnPos.y = 0f;

            // 발사 방향:
            // - FirePoint 있으면 그 forward (배럴이 바라보는 +Z) — 배럴 회전과 시각적으로 일치
            // - 없으면 머신→에임 방향
            Vector3 toAim;
            if (_firePoint != null)
            {
                toAim = _firePoint.forward;
            }
            else
            {
                toAim = aim.AimPosition - spawnPos;
            }
            toAim.y = 0f;
            if (toAim.sqrMagnitude < 0.0001f) toAim = Vector3.forward;
            else toAim.Normalize();

            // 산포 — Y축 기준 ±spreadAngle 라디안 회전
            float spread = Random.Range(-_data.SpreadAngle, _data.SpreadAngle);
            Vector3 dir = Quaternion.AngleAxis(spread * Mathf.Rad2Deg, Vector3.up) * toAim;

            // 프리펩 회전 보존 (탑뷰 스프라이트는 90,0,0 — identity 쓰면 카메라 쪽으로 서버림)
            var obj = Instantiate(_data.BulletPrefab, spawnPos, _data.BulletPrefab.transform.rotation);
            var bullet = obj.GetComponent<MachineGunBullet>();
            if (bullet != null)
            {
                bullet.Initialize(dir, _data, aim.BugLayer);
                AudioManager.Instance?.PlayMachineGunFire();
            }
            else
            {
                Debug.LogWarning("[MachineGunWeapon] BulletPrefab에 MachineGunBullet 컴포넌트가 없습니다.");
            }

            // 탄 1발 소비 — 0 도달 시 리로딩 시작
            _currentAmmo--;
            if (_currentAmmo <= 0)
            {
                _isReloading = true;
                _reloadEndTime = Time.time + _data.ReloadDuration;
            }
        }

        // ────── UI 오버라이드 ──────

        public override float BarFillAmount
        {
            get
            {
                if (_data == null) return 0f;
                if (_isReloading)
                {
                    // 리로딩 진행: 0 → 1 (차오름)
                    return _data.ReloadDuration > 0f
                        ? 1f - (ReloadRemaining / _data.ReloadDuration)
                        : 1f;
                }
                // 평상시: 탄 잔량 비율 (감소형)
                return _data.MaxAmmo > 0 ? (float)_currentAmmo / _data.MaxAmmo : 0f;
            }
        }

        public override Color BarColor =>
            _isReloading ? WarningColor : GunBlue;

        public override string StateText
        {
            get
            {
                if (_isReloading)
                {
                    float r = ReloadRemaining;
                    return r >= 1f ? $"리로딩 {r:0.0}s" : $"리로딩 {r:0.00}s";
                }
                return $"{_currentAmmo}발";
            }
        }

        public override Color BorderColor
        {
            get
            {
                if (_isReloading) return WarningColor;
                if (_data != null && _currentAmmo <= _data.LowAmmoThreshold) return WarningColor;
                // 기관총은 적 유무 무관 발사 — 활성 상태면 항상 강조
                return CanFire ? GunBlue : IdleBorderColor;
            }
        }

        // 적 유무와 무관 — 발사 가능 상태면 항상 "타겟 있음"으로 처리
        // (크로스헤어 색·에임 호 색에 영향)
        public override bool IsHittingTarget(AimController aim) => CanFire;

        public override bool ShowOverlay => _isReloading;

        public override string OverlayText
        {
            get
            {
                float r = ReloadRemaining;
                return r >= 1f ? $"리로딩\n{r:0.0}s" : $"리로딩\n{r:0.00}s";
            }
        }

        // 탄창 pip 행 — Phase 3.5에서 WeaponBase 가상 프로퍼티 추가됨
        public override bool ShowAmmoRow => true;
        public override int AmmoCurrent => _currentAmmo;
        public override int AmmoMax => _data != null ? _data.MaxAmmo : 0;
    }
}

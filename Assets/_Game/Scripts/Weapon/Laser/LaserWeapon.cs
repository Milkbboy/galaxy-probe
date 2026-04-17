using UnityEngine;
using DrillCorp.Aim;
using DrillCorp.Audio;

namespace DrillCorp.Weapon.Laser
{
    /// <summary>
    /// 레이저 무기 (Phase 4 — 자체 구동, 보조 무기)
    ///
    /// 프로토 _.html L269·281·293 동작 이식:
    /// - 빔 없음 + CD 0 → 마우스 위치에 빔 자동 스폰 (6s 수명, 1.725 u/s 추적)
    /// - 빔 활성 중엔 CD 타이머 정지 (프로토 L293)
    /// - 빔 소멸 순간부터 CD 5s 카운트다운 → 다음 빔
    ///
    /// 폭탄/기관총과 동일 패턴:
    /// - EquipWeapon 경로 거치지 않음 (씬에 항상 활성)
    /// - Update에서 매 프레임 TryFire(_aimController)
    /// - 실제 게이팅은 ShouldFire 오버라이드가 전담 (FireDelay=0이라 베이스 CanFire는 항상 true)
    ///
    /// UI (WeaponSlotUI + LaserAimRingBinder):
    /// - Active: 슬롯 바 연분홍(#ff6090) 수명% 감소 / 테두리 진홍 / 크로스헤어 호 숨김
    /// - Cooling: 슬롯 바 진홍(#ff1744) 쿨% 차오름 + 검은 오버레이 / 크로스헤어 호 차오름
    /// - Ready: 슬롯 바 초록 100% + "자동발사" / 크로스헤어 호 100%
    /// </summary>
    public class LaserWeapon : WeaponBase
    {
        [Header("Data")]
        [SerializeField] private LaserWeaponData _data;

        [Header("Self-Driven")]
        [Tooltip("AimController 참조 (비우면 Start에서 자동 탐색). " +
                 "레이저는 메인 무기 슬롯과 무관하게 자체 발사 루프를 돈다 — 폭탄/기관총과 동일 패턴.")]
        [SerializeField] private AimController _aimController;

        [Tooltip("게임 시작 직후 발사 지연 (초) — 씬 로딩/초기화 중 즉시 빔 스폰 방지")]
        [SerializeField] private float _startDelay = 0.3f;

        public LaserWeaponData Data => _data;

        // 프로토 색상 상수 (_.html L281 slot bar / L307 beam)
        private static readonly Color LaserRed = new Color(1f, 0.09f, 0.267f, 1f);     // #ff1744
        private static readonly Color LaserPink = new Color(1f, 0.376f, 0.565f, 1f);   // #ff6090

        private LaserBeam _activeBeam;
        private float _laserCD;    // 0 = 준비 완료
        private float _fireEnableTime;   // Time.time 기준 이 시각 이후부터 발사 허용

        /// <summary>빔 활성 중 여부. _activeBeam이 파괴되면 Unity가 null로 처리하므로 자동 false.</summary>
        private bool IsActive => _activeBeam != null;

        /// <summary>빔 남은 수명 비율 (0 → 1). 빔 없으면 0.</summary>
        private float BeamLifeRatio => _activeBeam != null ? _activeBeam.LifeRatio : 0f;

        private void Awake()
        {
            _baseData = _data;
        }

        private void Start()
        {
            if (_aimController == null)
                _aimController = FindAnyObjectByType<AimController>();

            // _aim 캐싱 — UI 프로퍼티(HasTarget 등)가 _aim을 참조
            if (_aimController != null) _aim = _aimController;

            _fireEnableTime = Time.time + _startDelay;
        }

        private void Update()
        {
            if (Time.time < _fireEnableTime) return;

            // 빔 없을 때만 CD 감소 (프로토 L293: 빔 활성 중엔 laserCD 흐르지 않음)
            if (_activeBeam == null && _laserCD > 0f)
                _laserCD = Mathf.Max(0f, _laserCD - Time.deltaTime);

            // 자체 발사 루프 — AimController.TryFireWeapon에 의존하지 않음
            if (_aimController != null) TryFire(_aimController);
        }

        // ────── 발사 흐름 ──────

        /// <summary>
        /// 레이저의 실제 게이팅은 여기서 — 베이스 CanFire(FireDelay=0으로 항상 true)를 통과한 뒤
        /// 이 메서드가 빔/쿨 상태를 보고 진짜 발사 여부 결정.
        /// </summary>
        protected override bool ShouldFire(AimController aim) =>
            _activeBeam == null && _laserCD <= 0f;

        protected override void Fire(AimController aim)
        {
            if (_data == null || _data.BeamPrefab == null)
            {
                Debug.LogWarning("[LaserWeapon] Data 또는 BeamPrefab이 비어있습니다.");
                return;
            }

            // 클릭 시점의 마우스 위치 캡처 (Y=0 평면). 이후 빔이 마우스를 추적함.
            Vector3 spawnPos = aim.AimPosition;
            spawnPos.y = 0f;

            // 프리펩 회전(탑뷰 90,0,0) 보존 — Quaternion.identity 금지
            // (메모리: feedback_topdown_instantiate.md)
            var obj = Instantiate(_data.BeamPrefab, spawnPos, _data.BeamPrefab.transform.rotation);
            _activeBeam = obj.GetComponent<LaserBeam>();

            if (_activeBeam != null)
            {
                _activeBeam.Initialize(aim, _data, aim.BugLayer);
                AudioManager.Instance?.StartLaserBeamLoop();
            }
            else
            {
                Debug.LogWarning("[LaserWeapon] BeamPrefab에 LaserBeam 컴포넌트가 없습니다.");
            }

            // 스폰 순간 즉시 풀 쿨다운 세팅 (프로토 L269) — 빔 수명 동안 멈춰있다가 소멸 후 감소 시작
            _laserCD = _data.Cooldown;
        }

        // ────── UI 오버라이드 (슬롯 바 WeaponSlotUI용) ──────

        public override float BarFillAmount
        {
            get
            {
                if (_data == null) return 0f;
                if (IsActive) return BeamLifeRatio;                         // Active: 수명% 감소
                if (_laserCD > 0f) return 1f - _laserCD / _data.Cooldown;   // Cooling: 쿨% 차오름
                return 1f;                                                  // Ready: 100%
            }
        }

        public override Color BarColor
        {
            get
            {
                if (IsActive) return LaserPink;      // 빔 활성 연분홍
                if (_laserCD > 0f) return LaserRed;  // 쿨중 진홍
                return ReadyBarColor;                // 준비 초록 (베이스 static)
            }
        }

        public override string StateText
        {
            get
            {
                if (IsActive && _data != null)
                {
                    float remainSec = _data.BeamDuration * BeamLifeRatio;
                    return remainSec >= 1f ? $"{remainSec:0.0}s" : $"{remainSec:0.00}s";
                }
                if (_laserCD > 0f)
                {
                    return _laserCD >= 1f ? $"{_laserCD:0.0}s" : $"{_laserCD:0.00}s";
                }
                return "자동발사";
            }
        }

        public override Color BorderColor
        {
            get
            {
                if (IsActive) return LaserRed;              // 활성: 진홍 강조
                if (_laserCD > 0f) return IdleBorderColor;  // 쿨중: idle
                return LaserRed;                            // 준비: 진홍 강조
            }
        }

        public override bool ShowOverlay => !IsActive && _laserCD > 0f;

        public override string OverlayText =>
            _laserCD >= 1f ? $"{_laserCD:0.0}s" : $"{_laserCD:0.00}s";

        /// <summary>활성 중이면 true (크로스헤어 빨강 전환용). AimController._currentWeapon 아니어서 직접 호출 안 됨.</summary>
        public override bool IsHittingTarget(AimController aim) => IsActive;

        // ────── 크로스헤어 쿨 호 전용 (LaserAimRingBinder용, §1.5 ①) ──────

        /// <summary>
        /// 슬롯 바와 분리된 크로스헤어 호 채움 값.
        /// Active 시 0 (호 숨김 — 프로토 L293 laserCD 고정 → lp=0 재현),
        /// Cooling 시 쿨 진행도, Ready 시 1.
        /// </summary>
        public float CrosshairRingFill
        {
            get
            {
                if (IsActive) return 0f;
                if (_data == null) return 1f;
                return Mathf.Clamp01(1f - _laserCD / _data.Cooldown);
            }
        }
    }
}

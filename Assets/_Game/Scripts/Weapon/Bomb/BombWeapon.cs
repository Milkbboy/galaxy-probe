using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DrillCorp.Aim;
using DrillCorp.Audio;

namespace DrillCorp.Weapon.Bomb
{
    /// <summary>
    /// 폭탄 무기 (수동 클릭 발사 / 자체 구동)
    ///
    /// 다른 무기와 달리 AimController.EquipWeapon 슬롯에 들어가지 않는다.
    /// 씬에 항상 활성으로 존재하며 좌클릭 시 즉시 발사.
    /// (저격총/기관총 등 메인 무기와 병렬로 동작 — 프로토타입과 동일)
    ///
    /// - 자체 Update에서 매 프레임 TryFire(_aimController) 호출
    /// - ShouldFire가 마우스 클릭 검사 → 클릭이 없으면 발사 안 됨
    /// - Fire에서 머신 위치에 BombProjectile 스폰, 클릭 시점 에임 좌표 캡처
    /// - 발사 후 마우스가 움직여도 폭탄은 원래 클릭한 곳으로 비행
    /// </summary>
    public class BombWeapon : WeaponBase
    {
        [Header("Data")]
        [SerializeField] private BombData _data;

        [Header("Self-Driven")]
        [Tooltip("AimController 참조 (비우면 Start에서 자동 탐색). " +
                 "폭탄은 무기 슬롯과 무관하게 매 프레임 자체 발사 루프를 돈다.")]
        [SerializeField] private AimController _aimController;

        public BombData Data => _data;

        // 프로토타입 #f4a423 (cool-bar.bomb 색)
        private static readonly Color BombOrangeBar = new Color(0.957f, 0.643f, 0.137f, 1f);

        private void Awake()
        {
            _baseData = _data;
        }

        private void Start()
        {
            if (_aimController == null)
                _aimController = FindAnyObjectByType<AimController>();

            // _aim 캐싱 — IsHittingTarget 등 UI 프로퍼티가 _aim을 참조할 수 있음
            if (_aimController != null) _aim = _aimController;
        }

        private void Update()
        {
            // 자체 발사 루프 — AimController.TryFireWeapon에 의존하지 않음
            // (폭탄은 무기 교체 슬롯이 아니므로 EquipWeapon 흐름을 거치지 않음)
            if (_aimController != null) TryFire(_aimController);
        }

        // ────── 수동 발사 ──────

        /// <summary>
        /// 마우스 좌클릭 + UI 위가 아닐 때만 발사. 적 유무는 무시 (어디든 던질 수 있음).
        /// </summary>
        protected override bool ShouldFire(AimController aim)
        {
            if (Mouse.current == null) return false;
            if (!Mouse.current.leftButton.wasPressedThisFrame) return false;

            // 슬롯/오버레이 등 UI 위 클릭은 발사로 오인하지 않음
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;

            return true;
        }

        protected override void Fire(AimController aim)
        {
            if (_data == null) return;

            // 클릭 시점의 에임 좌표 캡처 (Y=0 평면)
            Vector3 targetPos = aim.AimPosition;
            targetPos.y = 0f;

            // 즉시 폭발 모드 — 투사체 비행 없이 바로 펑
            if (_data.Instant)
            {
                BombProjectile.Detonate(targetPos, _data, aim.BugLayer);
                return;
            }

            // 비행 모드 — ProjectilePrefab을 머신에서 발사
            if (_data.ProjectilePrefab == null)
            {
                Debug.LogWarning("[BombWeapon] 비행 모드인데 ProjectilePrefab이 비어있습니다.");
                return;
            }

            Vector3 spawnPos = aim.MachineTransform != null
                ? aim.MachineTransform.position
                : aim.AimPosition;

            // 3D 투사체 — 타겟 방향을 바라보게 회전 (+Z forward 모델 기준)
            Vector3 launchDir = targetPos - spawnPos;
            launchDir.y = 0f;
            Quaternion spawnRot = launchDir.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(launchDir.normalized, Vector3.up)
                : _data.ProjectilePrefab.transform.rotation;
            var obj = Instantiate(_data.ProjectilePrefab, spawnPos, spawnRot);
            var proj = obj.GetComponent<BombProjectile>();
            if (proj != null)
            {
                proj.Initialize(targetPos, _data, aim.BugLayer);
                AudioManager.Instance?.PlayBombLaunch();
            }
            else
            {
                Debug.LogWarning("[BombWeapon] ProjectilePrefab에 BombProjectile 컴포넌트가 없습니다.");
            }
        }

        // ────── UI 오버라이드 ──────

        public override string StateText
        {
            get
            {
                if (CanFire) return "[클릭]";
                float remain = CooldownRemaining;
                return remain >= 1f ? $"{remain:0.0}s" : $"{remain:0.00}s";
            }
        }

        // 폭탄은 준비/쿨중 모두 주황 — 슬롯 바·에임 호 둘 다 폭탄 정체성 유지
        public override Color BarColor => BombOrangeBar;

        public override Color BorderColor => CanFire ? ReadyBarColor : IdleBorderColor;

        public override bool ShowOverlay => !CanFire;

        // 폭탄은 적 유무와 무관 — 준비 상태면 항상 "타겟 있음"으로 처리
        // (크로스헤어 색·링 색이 준비 상태로 표시됨)
        public override bool IsHittingTarget(AimController aim) => CanFire;

        // ────── InfoLabel ("클릭→폭탄") ──────

        public override void TryFire(AimController aim)
        {
            base.TryFire(aim);
            UpdateInfoLabel(aim);
        }

        public override void OnEquip(AimController aim)
        {
            base.OnEquip(aim);
            UpdateInfoLabel(aim);
        }

        public override void OnUnequip()
        {
            if (_aim != null) _aim.SetInfoText(null);
            base.OnUnequip();
        }

        private void UpdateInfoLabel(AimController aim)
        {
            if (aim == null) return;
            aim.SetInfoText(CanFire ? "클릭→폭탄" : null);
        }
    }
}

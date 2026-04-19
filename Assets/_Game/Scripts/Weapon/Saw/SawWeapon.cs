using UnityEngine;
using DrillCorp.Aim;
using DrillCorp.Bug;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.OutGame;

namespace DrillCorp.Weapon.Saw
{
    /// <summary>
    /// 회전톱날 — 머신을 기준으로 마우스 방향 궤도에 떠있으며, 블레이드 충돌 반경 내 Bug에 tick 데미지 + 슬로우.
    /// 쿨타임 없음 (상시 발동). 매 프레임 Update에서 궤도 위치·스핀·tick 갱신.
    /// WeaponUpgradeManager의 Damage/Radius/SlowBonus 보너스를 캐시로 반영.
    ///
    /// v2 포팅 후 자체 구동 (폭탄·기관총·레이저·저격총과 동일 패턴) — AimController 장착 경로 없음.
    /// v2 가이드: docs/WeaponUnlockUpgradeSystem.md §7
    /// </summary>
    public class SawWeapon : WeaponBase
    {
        [Header("Data")]
        [SerializeField] private SawWeaponData _data;

        [Header("Self-Driven")]
        [Tooltip("AimController 참조 (비우면 Start에서 자동 탐색)")]
        [SerializeField] private AimController _aimController;

        [Tooltip("게임 시작 직후 발사 지연 (초)")]
        [SerializeField] private float _startDelay = 0.3f;

        private float _fireEnableTime;
        private Transform _bladeVisual;
        private Quaternion _bladeBaseRotation;
        private float _bladeSpinAngle;
        private float _tickTimer;
        private readonly Collider[] _overlapBuffer = new Collider[64];

        // Cached effective stats
        private float _effectiveDamage;
        private float _effectiveBladeRadius;
        private float _effectiveSlowFactor;

        // === WeaponBase 오버라이드 ===

        public override bool IsHittingTarget(AimController aim)
        {
            if (aim == null || _data == null || aim.MachineTransform == null) return false;
            Vector3 orbitPos = ResolveOrbitPosition(aim);
            int count = Physics.OverlapSphereNonAlloc(
                orbitPos, _effectiveBladeRadius, _overlapBuffer, aim.BugLayer);
            return count > 0;
        }

        // 쿨타임 개념 없음 — 슬롯 UI는 "상시 준비"로 표시
        public override float BarFillAmount => 1f;
        public override Color BarColor => ReadyBarColor;
        public override Color BorderColor => HasTarget ? ThemeColor : IdleBorderColor;
        public override string StateText => HasTarget ? "절단!" : "회전";

        // === Unity / Life-cycle ===

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

            EnsureBladeVisual();
            SetBladeVisible(true);
            RefreshEffectiveStats();

            _fireEnableTime = Time.time + _startDelay;
        }

        private void OnDestroy()
        {
            if (_bladeVisual != null)
                Destroy(_bladeVisual.gameObject);
        }

        // === Update — 매 프레임 궤도/스핀/tick ===

        private void Update()
        {
            if (Time.time < _fireEnableTime) return;
            if (_aimController == null || _data == null || _aimController.MachineTransform == null) return;

            _aim = _aimController;

            Vector3 orbitPos = ResolveOrbitPosition(_aimController);
            UpdateBladeTransform(orbitPos);

            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                _tickTimer = _data.DamageTickInterval;
                ApplyTickDamage(orbitPos, _aimController.BugLayer);
            }
        }

        // WeaponBase 필수 메서드 — 자체 Update에서 직접 처리하므로 no-op
        protected override void Fire(AimController aim) { }

        // === Internals ===

        private Vector3 ResolveOrbitPosition(AimController aim)
        {
            Vector3 machinePos = aim.MachineTransform.position;
            Vector3 toAim = aim.AimPosition - machinePos;
            toAim.y = 0f; // XZ 평면 (CLAUDE.md 탑다운 좌표계)
            if (toAim.sqrMagnitude < 0.0001f) toAim = Vector3.forward;

            Vector3 pos = machinePos + toAim.normalized * _data.OrbitRadius;
            pos.y = machinePos.y; // 지면 높이 맞춤
            return pos;
        }

        private void EnsureBladeVisual()
        {
            if (_bladeVisual != null) return;
            if (_data == null || _data.BladeVisualPrefab == null) return;

            // CLAUDE.md: Quaternion.identity 금지 — 프리펩 회전 보존 후, 이후 프레임에서 스핀 합성
            var obj = Instantiate(
                _data.BladeVisualPrefab,
                transform.position,
                _data.BladeVisualPrefab.transform.rotation);
            obj.name = "SawBladeVisual";
            _bladeVisual = obj.transform;
            _bladeBaseRotation = _data.BladeVisualPrefab.transform.rotation;
        }

        private void UpdateBladeTransform(Vector3 orbitPos)
        {
            if (_bladeVisual == null) return;

            _bladeSpinAngle += _data.SpinSpeed * Time.deltaTime;
            _bladeVisual.position = orbitPos;

            // 탑다운 카메라는 -Y로 내려다봄 → Y축 회전이 "화면에서 도는" 축
            Quaternion spin = Quaternion.AngleAxis(_bladeSpinAngle * Mathf.Rad2Deg, Vector3.up);
            _bladeVisual.rotation = spin * _bladeBaseRotation;
        }

        private void SetBladeVisible(bool visible)
        {
            if (_bladeVisual == null) return;
            if (_bladeVisual.gameObject.activeSelf != visible)
                _bladeVisual.gameObject.SetActive(visible);
        }

        private void ApplyTickDamage(Vector3 center, LayerMask bugLayer)
        {
            int count = Physics.OverlapSphereNonAlloc(
                center, _effectiveBladeRadius, _overlapBuffer, bugLayer);

            for (int i = 0; i < count; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;

                DealDamage(col.transform, _effectiveDamage);

                var bug = col.GetComponent<BugController>();
                if (bug == null) bug = col.GetComponentInParent<BugController>();
                if (bug != null)
                    bug.ApplySlow(_effectiveSlowFactor, _data.SlowDuration);
            }
        }

        // === Upgrade 반영 ===

        private void OnWeaponUpgradedAny(string upgradeId)
        {
            if (_data == null || string.IsNullOrEmpty(_data.WeaponId)) return;

            // 빈 문자열 = 전체 리셋 (UpgradeManager.ResetAll) — 항상 재계산
            if (string.IsNullOrEmpty(upgradeId)) { RefreshEffectiveStats(); return; }

            var mgr = WeaponUpgradeManager.Instance;
            if (mgr == null) { RefreshEffectiveStats(); return; }

            var u = mgr.FindUpgrade(upgradeId);
            if (u != null && u.WeaponId == _data.WeaponId) RefreshEffectiveStats();
        }

        private void RefreshEffectiveStats()
        {
            if (_data == null) return;

            float dmgMul = 1f, radMul = 1f, slowAdd = 0f;
            var mgr = WeaponUpgradeManager.Instance;
            if (mgr != null && !string.IsNullOrEmpty(_data.WeaponId))
            {
                (_, dmgMul)  = mgr.GetBonus(_data.WeaponId, WeaponUpgradeStat.Damage);
                (_, radMul)  = mgr.GetBonus(_data.WeaponId, WeaponUpgradeStat.Radius);
                (slowAdd, _) = mgr.GetBonus(_data.WeaponId, WeaponUpgradeStat.SlowBonus);
            }

            _effectiveDamage = _data.Damage * dmgMul;
            _effectiveBladeRadius = _data.BladeRadius * radMul;
            _effectiveSlowFactor = Mathf.Min(0.9f, _data.SlowFactor + slowAdd);
        }
    }
}

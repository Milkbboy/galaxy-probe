using UnityEngine;
using DrillCorp.Aim;

namespace DrillCorp.Weapon.Laser
{
    /// <summary>
    /// 레이저 필드 - 에임 위치에 원형 바닥 필드 + 범위 내 Bug에 Tick 데미지
    /// 탑뷰 전용. 필드는 장착 중 항상 에임을 따라가며, 과열 시엔 비주얼만 깜빡임.
    /// 게이지는 필드 위쪽에 떠있는 상태로 Heat/쿨다운 진행을 표시.
    /// </summary>
    public class LaserBeamWeapon : WeaponBase
    {
        [Header("Data")]
        [SerializeField] private LaserBeamData _data;

        [Header("Field")]
        [Tooltip("바닥 필드 프리펩 (LaserBeamField 컴포넌트 필요)")]
        [SerializeField] private GameObject _fieldPrefab;

        [Header("Gauge")]
        [Tooltip("Heat/쿨다운 게이지 프리펩 (WeaponGauge 컴포넌트 필요)")]
        [SerializeField] private GameObject _gaugePrefab;

        [Tooltip("필드 가장자리(반경 끝)에서 게이지까지 추가 여유 거리 (화면 오른쪽 X+)\n" +
                 "최종 오프셋 = 필드 반경 + 이 값")]
        [Range(0f, 5f)]
        [SerializeField] private float _gaugeDistanceRight = 0.5f;

        [Tooltip("게이지 지면 띄움 (월드 Y+, 렌더 겹침 방지용. 음수면 지면 아래로 파묻혀 안 보임)")]
        [Range(0f, 5f)]
        [SerializeField] private float _gaugeYOffset = 0.1f;

        [Tooltip("화면 상하 오프셋 (월드 Z+: 화면 위쪽, Z-: 화면 아래쪽)")]
        [Range(-10f, 10f)]
        [SerializeField] private float _gaugeScreenVerticalOffset = 0f;

        [Tooltip("게이지 스케일")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _gaugeScale = 1f;

        private LaserBeamField _field;
        private WeaponGauge _gauge;
        private float _heat;
        private bool _overheated;
        private float _overheatTimer;
        private float _tickTimer;
        private readonly Collider[] _fieldBuffer = new Collider[128];

        public float Heat => _heat;
        public float HeatRatio => _data != null ? _heat / _data.MaxHeat : 0f;
        public bool IsOverheated => _overheated;

        public override bool SuppressAimBugDetection => true;
        public override bool IsHittingTarget(AimController aim)
        {
            if (_overheated || aim == null) return false;
            return CountBugsInField(aim) > 0;
        }

        public override bool ShowGauge => true;

        public override float GaugeRatio
        {
            get
            {
                if (_data == null) return 0f;
                return Mathf.Clamp01(_heat / _data.MaxHeat);
            }
        }

        public override WeaponGaugeState GaugeState
        {
            get
            {
                if (_overheated) return WeaponGaugeState.Locked;
                if (_data != null && _heat / _data.MaxHeat >= 0.85f) return WeaponGaugeState.Warning;
                return WeaponGaugeState.Normal;
            }
        }

        private void Awake()
        {
            _baseData = _data;
        }

        public override void OnEquip(AimController aim)
        {
            base.OnEquip(aim);
            _heat = 0f;
            _overheated = false;
            EnsureField(aim);
            EnsureGauge();
            SetFieldVisible(true);
            SetGaugeVisible(true);
        }

        public override void OnUnequip()
        {
            base.OnUnequip();
            SetFieldVisible(false);
            SetGaugeVisible(false);
        }

        private void EnsureField(AimController aim)
        {
            if (_field != null || _fieldPrefab == null) return;
            var obj = Instantiate(_fieldPrefab);
            _field = obj.GetComponent<LaserBeamField>();
            if (_field == null) _field = obj.AddComponent<LaserBeamField>();
            _field.SetWorldRadius(ResolveRadius(aim));
            if (_data != null)
                _field.SetFollowParams(_data.FollowSpeed, _data.StretchAmount);
        }

        private void EnsureGauge()
        {
            if (_gauge != null || _gaugePrefab == null) return;
            var obj = Instantiate(_gaugePrefab);
            _gauge = obj.GetComponent<WeaponGauge>();
            if (_gauge == null) _gauge = obj.AddComponent<WeaponGauge>();
            obj.transform.localScale = Vector3.one * _gaugeScale;
        }

        private float ResolveRadius(AimController aim)
        {
            if (_data != null && _data.FieldRadius > 0f) return _data.FieldRadius;
            return aim != null ? aim.AimRadius : 1f;
        }

        private void SetFieldVisible(bool visible)
        {
            if (_field != null && _field.gameObject.activeSelf != visible)
                _field.gameObject.SetActive(visible);
        }

        private void SetGaugeVisible(bool visible)
        {
            if (_gauge != null && _gauge.gameObject.activeSelf != visible)
                _gauge.gameObject.SetActive(visible);
        }

        public override void TryFire(AimController aim)
        {
            _aim = aim;
            if (aim == null || _data == null) return;

            EnsureField(aim);
            EnsureGauge();

            // 필드는 장착 중 항상 에임 추적 (과열 여부 무관)
            UpdateFieldTracking(aim);

            // 게이지 위치는 필드 위쪽(탑뷰 화면 위 = 월드 Z+)에 고정, 회전 탑뷰 눕힘
            UpdateGaugePlacement();

            // 과열 상태
            if (_overheated)
            {
                UpdateOverheat();
                CoolDown();
                PushGauge();
                return;
            }

            // 필드 범위 기준 Bug 감지
            int bugsInField = CountBugsInField(aim);
            bool hasBugInField = bugsInField > 0;

            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                _tickTimer = _data.DamageTickInterval;
                ApplyTickDamageInField(bugsInField);
            }

            if (hasBugInField)
                _heat += _data.HeatPerSecond * Time.deltaTime;
            else
                CoolDown();

            if (_heat >= _data.MaxHeat)
            {
                _heat = _data.MaxHeat;
                _overheated = true;
                _overheatTimer = _data.OverheatLockTime;
                if (_field != null) _field.SetOverheatVisual(true);
            }

            PushGauge();
        }

        protected override void Fire(AimController aim) { /* TryFire에서 직접 처리 */ }

        private void UpdateFieldTracking(AimController aim)
        {
            if (_field == null) return;
            SetFieldVisible(true);
            _field.SetTargetPosition(aim.AimPosition);
            _field.SetWorldRadius(ResolveRadius(aim));
            if (_data != null)
                _field.SetFollowParams(_data.FollowSpeed, _data.StretchAmount);
        }

        private void UpdateGaugePlacement()
        {
            if (_gauge == null || _field == null) return;
            SetGaugeVisible(true);
            // 필드 위치 기준 화면 오른쪽(X+)으로 오프셋. Y는 지면 띄움.
            Vector3 p = _field.transform.position;
            p.y = _gaugeYOffset;
            // 필드 반경 밖으로 밀어냄 + 추가 여유 (X+ = 화면 오른쪽)
            p.x += ResolveRadius(_aim) + _gaugeDistanceRight;
            // 화면 상하 오프셋 (Z+ = 화면 위쪽)
            p.z += _gaugeScreenVerticalOffset;
            _gauge.transform.position = p;
            // 탑뷰 고정 (XZ 평면에 눕힘, 스트레치·회전 영향 받지 않음)
            _gauge.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _gauge.transform.localScale = Vector3.one * _gaugeScale;
        }

        private void PushGauge()
        {
            if (_gauge == null) return;
            _gauge.SetValue(GaugeRatio, GaugeState);
        }

        private int CountBugsInField(AimController aim)
        {
            Vector3 center = _field != null ? _field.transform.position : aim.AimPosition;
            float radius = ResolveRadius(aim);
            int count = Physics.OverlapSphereNonAlloc(center, radius, _fieldBuffer, aim.BugLayer);
            return count;
        }

        private void ApplyTickDamageInField(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var c = _fieldBuffer[i];
                if (c == null) continue;
                DealDamage(c.transform, _data.Damage);
            }
        }

        private void CoolDown()
        {
            _heat = Mathf.Max(0f, _heat - _data.CoolPerSecond * Time.deltaTime);
        }

        private void UpdateOverheat()
        {
            _overheatTimer -= Time.deltaTime;
            if (_overheatTimer <= 0f && _heat <= 0.01f)
            {
                _overheated = false;
                if (_field != null) _field.SetOverheatVisual(false);
            }
        }
    }
}

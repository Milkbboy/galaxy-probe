using UnityEngine;
using DrillCorp.Aim;

namespace DrillCorp.Weapon
{
    /// <summary>
    /// 무기 베이스 클래스
    /// AimController가 매 프레임 TryFire(aim)를 호출
    /// FireDelay 경과 시 Fire()로 실제 데미지 처리 (투사체 없음)
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        protected WeaponData _baseData;
        protected AimController _aim;
        protected float _nextFireTime;

        public WeaponData BaseData => _baseData;
        public bool CanFire => Time.time >= _nextFireTime;

        public string DisplayName => _baseData != null ? _baseData.DisplayName : "";
        public Sprite Icon => _baseData != null ? _baseData.Icon : null;
        public Color ThemeColor => _baseData != null ? _baseData.ThemeColor : Color.white;

        /// <summary>쿨다운 남은 시간(초). 0이면 준비 완료.</summary>
        public float CooldownRemaining => Mathf.Max(0f, _nextFireTime - Time.time);

        /// <summary>현재 조준 상태에서 타겟을 잡고 있는지. (마지막 TryFire/OnEquip의 AimController 기준)</summary>
        public bool HasTarget => IsHittingTarget(_aim);

        // === 슬롯 UI 표현 프로퍼티 (WEAPON_IMPLEMENTATION_PLAN.md §4.6.3) ===
        // 슬롯은 이 값들만 읽고 자체 분기 없음. 무기별 차이는 파생에서 오버라이드.

        /// <summary>프로토타입 ready 바 색 (#51cf66)</summary>
        public static readonly Color ReadyBarColor = new Color(0.318f, 0.812f, 0.4f, 1f);

        /// <summary>슬롯 테두리 기본 색 (타겟 없음/쿨중)</summary>
        public static readonly Color IdleBorderColor = new Color(1f, 1f, 1f, 0.2f);

        /// <summary>기관총 탄부족/리로딩 경고 색 (#ff6b6b)</summary>
        public static readonly Color WarningColor = new Color(1f, 0.42f, 0.42f, 1f);

        /// <summary>쿨바 채움 비율 (0~1). 에임 호와 동일하게 실제 쿨 진행을 항상 표시.</summary>
        public virtual float BarFillAmount => CooldownProgress;

        /// <summary>쿨바 색. 준비 완료 = ready 초록 / 쿨중 = ThemeColor.</summary>
        public virtual Color BarColor => CanFire ? ReadyBarColor : ThemeColor;

        /// <summary>슬롯 상태 텍스트. 쿨중="1.2s" / 준비+타겟="발사!" / 준비+타겟없음="대기".</summary>
        public virtual string StateText
        {
            get
            {
                if (!CanFire)
                {
                    float remain = CooldownRemaining;
                    return remain >= 1f ? $"{remain:0.0}s" : $"{remain:0.00}s";
                }
                return HasTarget ? "발사!" : "대기";
            }
        }

        /// <summary>슬롯 테두리 색. 저격총: 준비+타겟 = ThemeColor 강조 / 그 외 = idle.</summary>
        public virtual Color BorderColor => (CanFire && HasTarget) ? ThemeColor : IdleBorderColor;

        /// <summary>쿨 오버레이(검은 덮개) 표시 여부. Phase 2+ 폭탄/레이저/리로딩용.</summary>
        public virtual bool ShowOverlay => false;

        /// <summary>오버레이에 표시될 큰 텍스트. 기본: 남은 쿨 초.</summary>
        public virtual string OverlayText => CooldownRemaining >= 1f ? $"{CooldownRemaining:0.0}s" : $"{CooldownRemaining:0.00}s";

        /// <summary>탄창 pip 행 표시 여부. 기관총 등 탄 기반 무기가 true 오버라이드.</summary>
        public virtual bool ShowAmmoRow => false;

        /// <summary>현재 탄 수 (pip 행에서 활성으로 표시할 개수)</summary>
        public virtual int AmmoCurrent => 0;

        /// <summary>최대 탄 수 (pip 행 총 개수)</summary>
        public virtual int AmmoMax => 0;

        /// <summary>
        /// true면 AimController가 AimPosition 기준 Bug 감지를 스킵함
        /// (예: 레이저처럼 무기 자체가 자기 기준으로 타겟을 찾는 경우)
        /// </summary>
        public virtual bool SuppressAimBugDetection => false;

        /// <summary>
        /// 크로스헤어 Ready 색 판정용. 기본: 에임 범위에 Bug 있음
        /// 무기가 자체 판정을 쓰면 오버라이드
        /// </summary>
        public virtual bool IsHittingTarget(AimController aim)
        {
            return aim != null && aim.HasBugInRange;
        }

        public enum WeaponGaugeState { Normal, Warning, Locked }

        /// <summary>게이지를 표시할지 여부</summary>
        public virtual bool ShowGauge => false;

        /// <summary>게이지 채움 비율 (0~1)</summary>
        public virtual float GaugeRatio => 0f;

        /// <summary>게이지 상태 (색상/깜빡임 결정)</summary>
        public virtual WeaponGaugeState GaugeState => WeaponGaugeState.Normal;
        public float CooldownProgress
        {
            get
            {
                if (_baseData == null || _baseData.FireDelay <= 0f) return 1f;
                float elapsed = _baseData.FireDelay - (_nextFireTime - Time.time);
                return Mathf.Clamp01(elapsed / _baseData.FireDelay);
            }
        }

        /// <summary>
        /// AimController가 매 프레임 호출 (장착 중인 무기에만)
        /// </summary>
        public virtual void TryFire(AimController aim)
        {
            _aim = aim;
            if (aim == null) return;

            if (!CanFire) return;
            if (!ShouldFire(aim)) return;

            Fire(aim);

            if (_baseData != null && _baseData.FireDelay > 0f)
                _nextFireTime = Time.time + _baseData.FireDelay;
        }

        /// <summary>
        /// 발사 조건 (기본: 범위 내 적 있을 때). 파생이 오버라이드 가능.
        /// </summary>
        protected virtual bool ShouldFire(AimController aim)
        {
            return aim.HasBugInRange;
        }

        /// <summary>
        /// 실제 발사 로직 (파생 구현)
        /// </summary>
        protected abstract void Fire(AimController aim);

        public virtual void OnEquip(AimController aim)
        {
            _aim = aim;
        }

        public virtual void OnUnequip() { }

        /// <summary>
        /// Bug Transform에 데미지 적용 + Hit VFX 스폰
        /// </summary>
        protected void DealDamage(Transform target, float damage)
        {
            if (target == null) return;

            var damageable = target.GetComponent<DrillCorp.Machine.IDamageable>();
            if (damageable == null)
            {
                damageable = target.GetComponentInParent<DrillCorp.Machine.IDamageable>();
            }
            damageable?.TakeDamage(damage);

            SpawnHitVfx(target.position);
        }

        protected void SpawnHitVfx(Vector3 worldPos)
        {
            if (_baseData == null || _baseData.HitVfxPrefab == null) return;
            var vfx = Instantiate(_baseData.HitVfxPrefab, worldPos, Quaternion.identity);
            Destroy(vfx, _baseData.HitVfxLifetime);
        }
    }
}

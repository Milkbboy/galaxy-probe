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

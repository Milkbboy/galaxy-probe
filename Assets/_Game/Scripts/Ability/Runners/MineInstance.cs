using System;
using UnityEngine;
using DrillCorp.Data;
using DrillCorp.Machine;
using DrillCorp.Weapon.Bomb;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 빅터 지뢰 본체 — 배치된 지뢰의 런타임 MonoBehaviour.
    /// MineRunner가 Instantiate 직후 <see cref="Initialize"/>를 호출해 의존성을 주입한다.
    ///
    /// 생명주기 (v2.html:1243~1263):
    ///   1. ArmTimer(0.5s) 대기 — 이 시간엔 트리거 불가. BodyTransform이 있으면 스케일 pingpong으로 시각적 점멸.
    ///   2. Arm 완료 → 매 프레임 OverlapSphere(_detectionRadius)로 벌레 탐지.
    ///   3. 감지 시 즉시 폭발 — 반경 내 대상에 데미지 + VFX 스폰 → Destroy(this).
    ///
    /// 폭발 수치 (BombWeapon 강화 반영):
    ///   · Radius     = bomb.EffectiveRadius × 0.5f
    ///   · BugDamage  = bomb.EffectiveDamage × 1.5f
    ///   · BossDamage = bomb.EffectiveDamage × 2.0f   (v2.html:1259 준수)
    ///
    /// BombWeapon이 null이면 <see cref="AbilityData"/> 값으로 fallback — 이 경우 보스 배율 없음.
    /// </summary>
    public class MineInstance : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("활성화 대기 시간(초). v2 기본 30 frame = 0.5s.")]
        [SerializeField] private float _armDuration = 0.5f;

        [Tooltip("벌레 감지 반경. 이 반경 안에 bug가 들어오면 즉시 폭발. " +
                 "v2 원본은 'mbg.sz+14' 동적이었지만 여기선 고정.")]
        [SerializeField] private float _detectionRadius = 1.4f;

        [Header("Visual")]
        [Tooltip("Arm 중 스케일 pingpong 적용할 자식 Transform. 비우면 시각 효과 없이 동작만.")]
        [SerializeField] private Transform _bodyTransform;

        [Tooltip("Arm 중 최소 스케일 배율. 1.0 = 변화 없음.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _armingPulseMin = 0.6f;

        [Tooltip("Arm 중 스케일 pingpong 속도(rad/sec).")]
        [SerializeField] private float _armingPulseSpeed = 12f;

        [Header("Explosion VFX")]
        [Tooltip("폭발 시 스폰할 프리펩 (예: MiniExploFire). 반경 비례 스케일됨.")]
        [SerializeField] private GameObject _explosionPrefab;

        [Tooltip("_explosionPrefab이 '기준 반경' 얼마로 제작됐는지. 실제 폭발 반경을 이 값으로 나눠 스케일 배율 계산. " +
                 "0이면 스케일 적용 안 함(원본 크기 그대로).")]
        [SerializeField] private float _explosionPrefabBaseRadius = 1f;

        // ─── runtime state ───
        private BombWeapon _bombWeapon;
        private AbilityData _abilityData;
        private LayerMask _bugLayer;
        private float _armRemaining;
        private bool _armed;
        private bool _exploded;
        private Vector3 _bodyBaseScale = Vector3.one;

        private readonly Collider[] _overlapBuffer = new Collider[32];

        /// <summary>
        /// 지뢰 파괴 시 (폭발 또는 씬 종료) 호출. MineRunner가 배치 수를 추적할 때 사용.
        /// </summary>
        public event Action OnDestroyed;

        public bool IsArmed => _armed;

        public void Initialize(BombWeapon bombWeapon, AbilityData data, LayerMask bugLayer)
        {
            _bombWeapon = bombWeapon;
            _abilityData = data;
            _bugLayer = bugLayer;
            _armRemaining = _armDuration;
            _armed = false;
            _exploded = false;

            if (_bodyTransform != null)
                _bodyBaseScale = _bodyTransform.localScale;
        }

        private void Update()
        {
            if (_exploded) return;

            if (!_armed)
            {
                _armRemaining -= Time.deltaTime;
                UpdateArmingPulse();
                if (_armRemaining <= 0f) SwitchToArmed();
                return;
            }

            TryDetect();
        }

        private void UpdateArmingPulse()
        {
            if (_bodyTransform == null) return;
            float k = (Mathf.Sin(Time.time * _armingPulseSpeed) + 1f) * 0.5f; // 0~1
            float s = Mathf.Lerp(_armingPulseMin, 1f, k);
            _bodyTransform.localScale = _bodyBaseScale * s;
        }

        private void SwitchToArmed()
        {
            _armed = true;
            if (_bodyTransform != null)
                _bodyTransform.localScale = _bodyBaseScale;
        }

        private void TryDetect()
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, _detectionRadius, _overlapBuffer, _bugLayer);
            if (hits > 0) Explode();
        }

        private void Explode()
        {
            _exploded = true;
            Vector3 pos = transform.position;

            float radius, bugDamage, bossDamage;
            ResolveStats(out radius, out bugDamage, out bossDamage);

            int hits = Physics.OverlapSphereNonAlloc(pos, radius, _overlapBuffer, _bugLayer);
            for (int i = 0; i < hits; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;

                float dmg = IsBoss(col) ? bossDamage : bugDamage;
                if (col.TryGetComponent<IDamageable>(out var d))
                    d.TakeDamage(dmg);
            }

            SpawnExplosionVfx(pos, radius);
            OnDestroyed?.Invoke();
            Destroy(gameObject);
        }

        private void ResolveStats(out float radius, out float bugDamage, out float bossDamage)
        {
            if (_bombWeapon != null)
            {
                radius = _bombWeapon.EffectiveRadius * 0.5f;
                bugDamage = _bombWeapon.EffectiveDamage * 1.5f;
                bossDamage = _bombWeapon.EffectiveDamage * 2.0f;
                return;
            }

            // Fallback — BombWeapon 없음. AbilityData 값 그대로.
            radius = _abilityData != null && _abilityData.Range > 0f ? _abilityData.Range : 2f;
            bugDamage = _abilityData != null && _abilityData.Damage > 0f ? _abilityData.Damage : 1f;
            bossDamage = bugDamage; // 보스 배율은 BombWeapon 강화 개념 없으면 의미 X
        }

        /// <summary>
        /// 보스 판정 — 현재는 tag "Boss". BossController 실구현이 생기면 교체.
        /// </summary>
        private static bool IsBoss(Collider col)
        {
            return col.CompareTag("Boss");
        }

        private void SpawnExplosionVfx(Vector3 pos, float radius)
        {
            if (_explosionPrefab == null) return;

            var vfx = Instantiate(_explosionPrefab, pos, Quaternion.identity);
            if (_explosionPrefabBaseRadius > 0.01f)
            {
                float mul = radius / _explosionPrefabBaseRadius;
                vfx.transform.localScale *= mul;
            }
        }

        private void OnDestroy()
        {
            // 씬 종료 등으로 Destroy 된 경우에도 Runner가 정리되도록.
            if (!_exploded) OnDestroyed?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _armed ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        }
    }
}

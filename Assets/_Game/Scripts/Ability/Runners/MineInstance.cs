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

        [Tooltip(
            "벌레 접촉 감지 반경(월드 유닛). 이 반경 안에 bug가 들어오면 즉시 폭발.\n" +
            "• 폭발 반경과는 별개 — 감지는 접촉(좁음), 폭발은 AoE(넓음).\n" +
            "• 벌레 평균 반경 ~0.3m 기준 0.7 = 벌레가 코앞에 왔을 때 터짐."
        )]
        [Min(0.1f)]
        [SerializeField] private float _detectionRadius = 0.7f;

        [Header("Visual")]
        [Tooltip("Arm 중 스케일 pingpong 적용할 자식 Transform. 비우면 시각 효과 없이 동작만.")]
        [SerializeField] private Transform _bodyTransform;

        [Tooltip("Arm 중 최소 스케일 배율. 1.0 = 변화 없음.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _armingPulseMin = 0.6f;

        [Tooltip("Arm 중 스케일 pingpong 속도(rad/sec).")]
        [SerializeField] private float _armingPulseSpeed = 12f;

        [Header("Armed Idle Pulse (arm 완료 후)")]
        [Tooltip("armed 상태에서 본체 스케일이 살짝 숨쉬는 pingpong. 1.0이면 효과 없음.")]
        [Range(0.8f, 1.2f)]
        [SerializeField] private float _armedPulseMin = 0.92f;

        [Tooltip("armed pingpong 속도. arm 중보다 낮게.")]
        [SerializeField] private float _armedPulseSpeed = 3f;

        [Header("Range Decal (폭발 예고 링)")]
        [Tooltip("폭발 예고 링을 그릴지. off면 지뢰 설치 시 범위 표시 없음.")]
        [SerializeField] private bool _showRangeDecal = true;

        [Tooltip("링 외경 대비 내경 비율. 0.85 = 얇은 링, 0.5 = 두꺼운 링.")]
        [Range(0.1f, 0.99f)]
        [SerializeField] private float _rangeDecalInnerRatio = 0.8f;

        [Tooltip("링 색 (armed 전 · 옅은 상태).")]
        [SerializeField] private Color _rangeDecalArmingColor = new Color(1f, 0.6f, 0.1f, 1f);

        [Tooltip("링 색 (armed 후 · 또렷한 경고).")]
        [SerializeField] private Color _rangeDecalArmedColor = new Color(1f, 0.2f, 0.15f, 1f);

        [Header("Center Dot (중앙 점)")]
        [Tooltip(
            "armed 완료 시 활성화할 중앙 점 GameObject.\n" +
            "프리펩 자식으로 미리 배치 (예: Polygon Arsenal GlowPowerupSmallRed 복제).\n" +
            "Initialize에서 비활성 → SwitchToArmed에서 활성. v2.html 원본의 '준비 완료 = 빨간 점 점등' 연출."
        )]
        [SerializeField] private GameObject _centerDotObject;

        [Header("Explosion VFX")]
        [Tooltip("폭발 시 스폰할 프리펩 (예: GrenadeExplosionRed). 폭탄 무기와 시각적으로 구분되는 자체 연출.")]
        [SerializeField] private GameObject _explosionPrefab;

        [Tooltip("_explosionPrefab이 '기준 반경' 얼마로 제작됐는지. 실제 폭발 반경을 이 값으로 나눠 스케일 배율 계산. " +
                 "0이면 스케일 적용 안 함(원본 크기 그대로).")]
        [SerializeField] private float _explosionPrefabBaseRadius = 1.5f;

        // ─── runtime state ───
        private BombWeapon _bombWeapon;
        private AbilityData _abilityData;
        private LayerMask _bugLayer;
        private float _armRemaining;
        private bool _armed;
        private bool _exploded;
        private Vector3 _bodyBaseScale = Vector3.one;
        private AbilityRangeDecal _rangeDecal;

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

            if (_showRangeDecal) BuildRangeDecal();

            // 중앙 점은 arm 완료 전까지 숨김. v2 원본 연출 준수.
            if (_centerDotObject != null) _centerDotObject.SetActive(false);
        }

        // 폭발 반경과 동일 크기의 링을 지뢰 아래에 깔아 "여기 터지면 여기까지 휩쓸림"을 예고.
        private void BuildRangeDecal()
        {
            ResolveStats(out float radius, out _, out _);
            if (radius <= 0.01f) return;

            var go = new GameObject("MineRangeDecal");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.02f, 0f); // 지뢰 본체 아래 살짝 띄움
            go.transform.localRotation = Quaternion.identity;

            float outer = radius;
            float inner = Mathf.Clamp(radius * _rangeDecalInnerRatio, 0.01f, radius - 0.01f);

            _rangeDecal = go.AddComponent<AbilityRangeDecal>();
            _rangeDecal.SetupMesh(AbilityDecalMeshBuilder.BuildRing(inner, outer));
            _rangeDecal.SetTint(_rangeDecalArmingColor);
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

            UpdateArmedIdlePulse();
            TryDetect();
        }

        private void UpdateArmingPulse()
        {
            if (_bodyTransform == null) return;
            float k = (Mathf.Sin(Time.time * _armingPulseSpeed) + 1f) * 0.5f; // 0~1
            float s = Mathf.Lerp(_armingPulseMin, 1f, k);
            _bodyTransform.localScale = _bodyBaseScale * s;
        }

        // armed 상태에서도 지뢰가 살아있다는 느낌으로 느리게 숨쉬게.
        private void UpdateArmedIdlePulse()
        {
            if (_bodyTransform == null) return;
            float k = (Mathf.Sin(Time.time * _armedPulseSpeed) + 1f) * 0.5f;
            float s = Mathf.Lerp(_armedPulseMin, 1f, k);
            _bodyTransform.localScale = _bodyBaseScale * s;
        }

        private void SwitchToArmed()
        {
            _armed = true;
            if (_bodyTransform != null)
                _bodyTransform.localScale = _bodyBaseScale;
            if (_rangeDecal != null)
                _rangeDecal.SetTint(_rangeDecalArmedColor);
            if (_centerDotObject != null)
                _centerDotObject.SetActive(true);
        }

        private void TryDetect()
        {
            // 감지 = 접촉(좁음), 폭발 = AoE(넓음). 두 반경은 의도적으로 다름 — v2 원본과 동일.
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

            // 링은 지뢰 본체 자식이라 Destroy와 함께 사라지지만, 자연스럽게 페이드아웃하도록
            // detach 후 Dispose — 잠깐 남아 페이드되면서 폭발 영역을 강조.
            // 중앙 점 프리펩은 지뢰 Destroy와 함께 즉시 사라져도 폭발 VFX가 덮으므로 별도 처리 불필요.
            if (_rangeDecal != null)
            {
                _rangeDecal.transform.SetParent(null, worldPositionStays: true);
                _rangeDecal.Dispose();
                _rangeDecal = null;
            }

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

            // 프리펩에 베이크된 회전 보존 — Polygon Arsenal VFX는 대부분 identity면 카메라 쪽으로 서버림.
            var vfx = Instantiate(_explosionPrefab, pos, _explosionPrefab.transform.rotation);
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
            // 감지 반경 (노랑/빨강)
            Gizmos.color = _armed ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            // 폭발 반경 (주황) — 런타임에만 정확, 에디트 모드엔 대략치.
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.6f);
            float explR = 1.5f;
            if (Application.isPlaying) ResolveStats(out explR, out _, out _);
            Gizmos.DrawWireSphere(transform.position, explR);
        }
    }
}

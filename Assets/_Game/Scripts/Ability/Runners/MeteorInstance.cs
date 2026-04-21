using UnityEngine;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 반중력 메테오 낙하체. MeteorRunner 가 AutoInterval 주기마다
    /// AbilityData.VfxPrefab (= 낙하체 프리펩) 을 Instantiate 후 <see cref="Initialize"/> 호출.
    ///
    /// 프리펩 구성 (에디터 메뉴 MeteorPrefabCreator 로 자동 생성):
    ///   - MeteorInstance 컴포넌트
    ///   - Body 자식 (Sphere 메시, 빨간 재질)
    ///   - Trail 자식 (TrailRenderer)
    ///   - _impactVfxPrefab / _fireZoneVfxPrefab 필드에 Polygon Arsenal 프리펩 바인딩
    ///
    /// 동작 (v2.html:1167~1184):
    ///   1. targetPos + Y 오프셋에서 아래로 _fallSpeed 로 낙하
    ///   2. Y <= targetPos.y 도달 시 Impact:
    ///      · 폭발 VFX (_impactVfxPrefab) 1회 스폰
    ///      · MeteorFireZone new GameObject 생성 → _fireZoneDuration 초 지속 화염
    ///      · 자기 자신 Destroy
    /// </summary>
    public class MeteorInstance : MonoBehaviour
    {
        [Header("Impact VFX (착지 폭발 — 1회성)")]
        [Tooltip("착지 순간 1회 스폰. FireNovaYellow / GrenadeExplosionRed 등.")]
        [SerializeField] private GameObject _impactVfxPrefab;
        [Tooltip("_impactVfxPrefab 의 기준 반경 — 실제 화염 반경으로 스케일 보정에 사용. 보통 1.5.")]
        [SerializeField] private float _impactVfxReferenceRadius = 1.5f;

        [Header("Fire Zone VFX (지속 화염 — MeteorFireZone 에 전달)")]
        [Tooltip("지속 화염 지대 루프 VFX. FloorTrapMolten 권장. 비우면 데칼만 표시.")]
        [SerializeField] private GameObject _fireZoneVfxPrefab;

        // runtime state — Initialize 로 주입
        private Vector3 _startPos;
        private Vector3 _targetPos;
        private Vector3 _fallDir;     // 정규화된 낙하 방향 (start→target)
        private float _fallSpeed;
        private float _fireZoneRadius;
        private float _fireZoneDuration;
        private float _fireZoneTickDamage;
        private float _fireZoneTickInterval;
        private LayerMask _bugLayer;
        private Transform _vfxParent;

        private bool _impacted;

        /// <summary>
        /// MeteorRunner 가 Instantiate 직후 호출.
        /// startPos → targetPos 직선 궤적을 따라 fallSpeed 로 이동. XZ 오프셋이 있으면 비스듬히 떨어짐.
        /// </summary>
        public void Initialize(
            Vector3 startPos, Vector3 targetPos, float fallSpeed,
            float fireZoneRadius, float fireZoneDuration,
            float fireZoneTickDamage, float fireZoneTickInterval,
            LayerMask bugLayer, Transform vfxParent)
        {
            _startPos = startPos;
            _targetPos = targetPos;
            _fallSpeed = Mathf.Max(0.01f, fallSpeed);
            _fireZoneRadius = fireZoneRadius;
            _fireZoneDuration = fireZoneDuration;
            _fireZoneTickDamage = fireZoneTickDamage;
            _fireZoneTickInterval = fireZoneTickInterval;
            _bugLayer = bugLayer;
            _vfxParent = vfxParent;

            Vector3 delta = _targetPos - _startPos;
            _fallDir = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.down;

            transform.position = _startPos;
            // 낙하 방향으로 운석 기울이기 — ChargeAura 트레일이 자연스럽게 뒤로 흐르도록.
            transform.rotation = Quaternion.LookRotation(_fallDir, Vector3.up);
        }

        private void Update()
        {
            if (_impacted) return;

            float step = _fallSpeed * Time.deltaTime;
            Vector3 toTarget = _targetPos - transform.position;

            if (toTarget.magnitude <= step)
            {
                transform.position = _targetPos;
                Impact();
                return;
            }

            transform.position += _fallDir * step;
        }

        private void Impact()
        {
            _impacted = true;

            SpawnImpactVfx();
            SpawnFireZone();

            Destroy(gameObject);
        }

        private void SpawnImpactVfx()
        {
            if (_impactVfxPrefab == null) return;
            var vfx = Instantiate(_impactVfxPrefab, _targetPos, _impactVfxPrefab.transform.rotation, _vfxParent);
            float refR = Mathf.Max(0.01f, _impactVfxReferenceRadius);
            float ratio = Mathf.Max(0.01f, _fireZoneRadius / refR);
            vfx.transform.localScale *= ratio;
        }

        private void SpawnFireZone()
        {
            var go = new GameObject("MeteorFireZone");
            go.transform.SetParent(_vfxParent, worldPositionStays: true);
            var zone = go.AddComponent<MeteorFireZone>();
            zone.Initialize(
                center: _targetPos,
                radius: _fireZoneRadius,
                duration: _fireZoneDuration,
                damagePerTick: _fireZoneTickDamage,
                tickInterval: _fireZoneTickInterval,
                bugLayer: _bugLayer,
                fireVfxPrefab: _fireZoneVfxPrefab);
        }
    }
}

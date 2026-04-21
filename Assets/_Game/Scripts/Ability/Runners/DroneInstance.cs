using System;
using UnityEngine;
using DrillCorp.Data;
using DrillCorp.UI;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 지누스 드론 포탑 본체 — 배치된 드론의 런타임 MonoBehaviour.
    /// DroneRunner 가 Instantiate 직후 <see cref="Initialize"/> 로 의존성을 주입한다.
    ///
    /// v2.html:1042~1053 (tickDrones) + 1321 (fromDrone 탄 데미지) 포팅:
    ///   1. 사거리(_data.Range) 내 최근접 벌레 탐색 (OverlapSphereNonAlloc)
    ///   2. 타겟 있으면 yaw 회전 + 발사 쿨 소비 시 DroneBullet 스폰 (±_spreadRadians 산포)
    ///   3. 접촉 피해 — _contactRadius 반경 내 벌레 수만큼 _hp -= _contactDamagePerBug * dt
    ///   4. _hp <= 0 → Destroy + OnDestroyed 이벤트
    ///
    /// 탄 데미지는 AbilityData.Damage (v2 원본 0.8 고정). 드론 HP 는 본 프리펩의 _maxHp 로 관리
    /// (SO 는 리스폰 쿨·최대수 등 '어빌리티 레벨' 값만 들고, 유닛 레벨 스탯은 프리펩에서).
    /// </summary>
    public class DroneInstance : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("몸체 VFX 자식 Transform (GlowPowerupBigGreen 등). yaw 회전·스케일 적용 대상.")]
        [SerializeField] private Transform _bodyTransform;

        [Tooltip("드론이 발사할 탄 프리펩. DroneBullet 컴포넌트가 붙어있어야 함.")]
        [SerializeField] private GameObject _bulletPrefab;

        [Header("Combat (Runner 기본값 — 필요시 인스펙터로 개별 조정)")]
        [Tooltip("드론 최대 HP. v2 원본 30.")]
        [SerializeField] private float _maxHp = 30f;

        [Tooltip("발사 재장전 시간(초). v2 원본 30프레임 = 0.5초.")]
        [SerializeField] private float _fireDelay = 0.5f;

        [Tooltip("탄 속도(유닛/초). v2 원본 8/frame = 480 pix/sec → ÷10 = 48 u/sec.")]
        [SerializeField] private float _bulletSpeed = 48f;

        [Tooltip("탄 수명(초). v2 원본 60프레임 = 1초.")]
        [SerializeField] private float _bulletLifetime = 1f;

        [Tooltip("접촉 피해 감지 반경(유닛). v2 sz+12 → 작은 벌레(0.3) 기준 1.2.")]
        [SerializeField] private float _contactRadius = 1.2f;

        [Tooltip("접촉한 벌레 1마리가 초당 드론에 주는 피해. v2 원본 0.5/dt/마리 → 30/sec 동등.")]
        [SerializeField] private float _contactDamagePerBug = 30f;

        [Tooltip("발사 산포(라디안). v2 원본 ±0.1/2 = ±0.05.")]
        [SerializeField] private float _spreadRadians = 0.1f;

        [Header("Range Decal (사거리 링)")]
        [Tooltip("사거리 링을 자식으로 그릴지.")]
        [SerializeField] private bool _showRangeDecal = true;

        [Tooltip("링 외경 대비 내경 비율 (0.92 = 얇은 링).")]
        [Range(0.1f, 0.99f)]
        [SerializeField] private float _rangeDecalInnerRatio = 0.92f;

        [Tooltip("링 색 (지누스 테마 초록).")]
        [SerializeField] private Color _rangeDecalColor = new Color(0.32f, 0.81f, 0.4f, 1f);

        [Header("HP Bar (3D Cube)")]
        [Tooltip("드론 본체 위에 떠있는 HP 바 크기(가로×높이×깊이, 월드유닛).")]
        [SerializeField] private Vector3 _hpBarSize = new Vector3(2f, 0.22f, 0.3f);

        [Tooltip("HP 바를 드론 위로 얼마나 띄울지 (offset). 탑뷰에서 Z+ = 화면 위쪽.")]
        [SerializeField] private Vector3 _hpBarOffset = new Vector3(0f, 0.6f, 0.9f);

        // ─── runtime state ───
        private AbilityData _abilityData;
        private LayerMask _bugLayer;
        private float _currentHp;
        private float _fireCooldown;
        private AbilityRangeDecal _rangeDecal;
        private Hp3DBar _hpBar;

        // 타겟팅·접촉판정 공용. 버퍼 크기는 동시 탐지 한도 — 드론 1기당이라 16 충분.
        private readonly Collider[] _overlapBuffer = new Collider[16];

        /// <summary>드론 파괴 시 발행. Runner 가 리스트에서 제거.</summary>
        public event Action OnDestroyed;

        public void Initialize(AbilityData data, LayerMask bugLayer)
        {
            _abilityData = data;
            _bugLayer = bugLayer;
            _currentHp = _maxHp;
            _fireCooldown = 0f;

            if (_showRangeDecal) BuildRangeDecal();
            BuildHpBar();
        }

        private void BuildHpBar()
        {
            _hpBar = Hp3DBar.Create(transform, _hpBarOffset, _hpBarSize);
            // 지누스 테마 초록 → 위험 시 빨강.
            _hpBar.SetColors(
                full: new Color(0.32f, 0.81f, 0.4f, 1f),
                low:  new Color(0.95f, 0.28f, 0.22f, 1f),
                lowThreshold: 0.3f);
            _hpBar.SetHealth(1f);
        }

        private void BuildRangeDecal()
        {
            float range = _abilityData != null ? _abilityData.Range : 0f;
            if (range <= 0.01f) return;

            var go = new GameObject("DroneRangeDecal");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            go.transform.localRotation = Quaternion.identity;

            float outer = range;
            float inner = Mathf.Clamp(range * _rangeDecalInnerRatio, 0.01f, range - 0.01f);

            _rangeDecal = go.AddComponent<AbilityRangeDecal>();
            _rangeDecal.SetupMesh(AbilityDecalMeshBuilder.BuildRing(inner, outer));
            _rangeDecal.SetTint(_rangeDecalColor);
            _rangeDecal.SetBaseAlpha(0.18f);
        }

        private void Update()
        {
            if (_currentHp <= 0f) return;

            float dt = Time.deltaTime;

            var target = FindClosestBug(_abilityData != null ? _abilityData.Range : 0f);
            if (target != null) AimBodyAt(target.transform.position);

            _fireCooldown = Mathf.Max(0f, _fireCooldown - dt);
            if (target != null && _fireCooldown <= 0f)
            {
                FireBulletAt(target.transform.position);
                _fireCooldown = _fireDelay;
            }

            ApplyContactDamage(dt);

            if (_currentHp <= 0f) DestroySelf();
        }

        // ─── 타겟팅 ──────────────────────────────────────────────

        private Collider FindClosestBug(float range)
        {
            if (range <= 0f) return null;
            int hits = Physics.OverlapSphereNonAlloc(transform.position, range, _overlapBuffer, _bugLayer);
            if (hits <= 0) return null;

            Collider closest = null;
            float bestDistSqr = float.MaxValue;
            for (int i = 0; i < hits; i++)
            {
                var c = _overlapBuffer[i];
                if (c == null) continue;
                Vector3 delta = c.transform.position - transform.position;
                delta.y = 0f;
                float d = delta.sqrMagnitude;
                if (d < bestDistSqr) { bestDistSqr = d; closest = c; }
            }
            return closest;
        }

        private void AimBodyAt(Vector3 targetPos)
        {
            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            // yaw 만 회전 — 탑뷰 XZ 평면에서 LookRotation(dir, up).
            // Body VFX 는 이미 상위 Transform 에 XY→XZ 눕힘 회전이 들어있을 수 있으므로
            // 루트 자체를 회전 (자식 VFX 의 로컬 회전은 유지).
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        // ─── 발사 ────────────────────────────────────────────────

        private void FireBulletAt(Vector3 targetPos)
        {
            if (_bulletPrefab == null) return;

            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            else dir.Normalize();

            // 산포 — yaw 로 ±spread/2 회전.
            if (_spreadRadians > 0f)
            {
                float yawDeg = (UnityEngine.Random.value - 0.5f) * _spreadRadians * Mathf.Rad2Deg;
                dir = Quaternion.Euler(0f, yawDeg, 0f) * dir;
            }

            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            var bullet = Instantiate(_bulletPrefab, transform.position, rot);
            if (bullet.TryGetComponent<DroneBullet>(out var db))
            {
                float dmg = _abilityData != null ? _abilityData.Damage : 0.8f;
                db.Initialize(dir, _bulletSpeed, dmg, _bulletLifetime, _bugLayer);
            }
            else
            {
                Debug.LogWarning("[DroneInstance] _bulletPrefab 에 DroneBullet 컴포넌트가 없습니다.");
                Destroy(bullet);
            }
        }

        // ─── 접촉 피해 ───────────────────────────────────────────

        private void ApplyContactDamage(float dt)
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, _contactRadius, _overlapBuffer, _bugLayer);
            if (hits <= 0) return;
            // v2: `dr.hp -= 0.5 * dt` 벌레 수만큼 누적. 0.5/frame = 30/sec 동등.
            _currentHp -= _contactDamagePerBug * hits * dt;
            if (_hpBar != null && _maxHp > 0f)
                _hpBar.SetHealth(Mathf.Max(0f, _currentHp) / _maxHp);
        }

        // ─── 파괴 ────────────────────────────────────────────────

        private void DestroySelf()
        {
            if (_rangeDecal != null)
            {
                _rangeDecal.transform.SetParent(null, worldPositionStays: true);
                _rangeDecal.Dispose();
                _rangeDecal = null;
            }
            if (_hpBar != null)
            {
                // HP 바는 drone 을 target 으로 LateUpdate 추적 중 — 드론 파괴되면 자체 Destroy 됨.
                // 명시적 Destroy 로 프레임 지연 없이 사라지게.
                Destroy(_hpBar.gameObject);
                _hpBar = null;
            }
            OnDestroyed?.Invoke();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // 외부 요인(씬 종료 등)으로 파괴된 경우에도 Runner 가 리스트 정리하도록 신호.
            // DestroySelf 경로와 중복 발행될 수 있지만 Runner 가 Remove 호출은 idempotent.
            OnDestroyed?.Invoke();
        }
    }
}

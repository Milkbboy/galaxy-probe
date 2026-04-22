using System;
using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;
using DrillCorp.Diagnostics;
using DrillCorp.Machine;
using DrillCorp.UI;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 지누스 드론 거미 본체 — AutoInterval 10초마다 자동 소환되는 **근접 추격형** 드론.
    /// v2 원본은 탄을 쏘는 것처럼 보이지만, 접근 속도가 공격적이라 타겟 위에 올라타 0거리 발사 →
    /// 실제 플레이어 체감은 "달라붙어 뜯는 근접 공격". Unity 포팅은 이 체감에 맞춰 **멜리 접촉 피해**로 구현.
    ///
    /// 동작:
    ///   1. lp += OrbitLpSpeed * dt
    ///   2. 사거리 내 최근접 벌레 탐색
    ///   3. 타겟 있음:
    ///      · yaw 회전 → 타겟 방향으로 접근. 멜리 반경에 도달하면 정지 (관통/통과 방지).
    ///      · 멜리 반경 내 벌레 모두에게 MeleeDps * dt 데미지
    ///   4. 타겟 없음: 머신 주위 선회 (v2 orbit 포팅)
    ///   5. HP 자연감쇠 — 0 이하면 Destroy
    /// </summary>
    public class SpiderDroneInstance : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("몸체 VFX 자식 Transform (SparkleOrbGreen 등). yaw 회전 적용 대상.")]
        [SerializeField] private Transform _bodyTransform;

        [Header("Combat — 근접 공격")]
        [Tooltip("드론 최대 HP. v2 원본 40.")]
        [SerializeField] private float _maxHp = 40f;

        [Tooltip("이동 속도(유닛/초). v2 원본 3/frame × 60 ÷ 10 = 18 u/sec.")]
        [SerializeField] private float _moveSpeed = 18f;

        [Tooltip("멜리 공격 반경(유닛). 이 반경 안에 있는 벌레는 dps 피해를 받는다. " +
                 "거미가 이 반경에 도달하면 접근을 멈추고 지속 피해만 가함.")]
        [SerializeField] private float _meleeRadius = 1.2f;

        [Tooltip("초당 멜리 데미지. 범위 내 벌레 각자에게 dps * dt 적용 (다중 타겟 가능).")]
        [SerializeField] private float _meleeDps = 3f;

        [Tooltip("HP 자연 감쇠(초당). v2 원본 0.005/dt × 60 = 0.3. 테스트 후 증량 가능.")]
        [SerializeField] private float _hpDecayPerSec = 0.3f;

        [Header("Orbit (타겟 없을 때 머신 주위 선회)")]
        [Tooltip("선회 기본 반경. v2 원본 60pix ÷ 10.")]
        [SerializeField] private float _orbitBase = 6f;

        [Tooltip("선회 반경 진폭(sin lp). v2 원본 20pix ÷ 10.")]
        [SerializeField] private float _orbitAmplitude = 2f;

        [Tooltip("선회 lp 증가 속도(rad/sec). v2 원본 0.05/frame × 60.")]
        [SerializeField] private float _orbitLpSpeed = 3f;

        [Tooltip("선회 각속도(rad/sec). v2 원본 0.03/frame × 60.")]
        [SerializeField] private float _orbitTurnRate = 1.8f;

        [Tooltip("선회 목표 위치 추적 lerp(1/sec). v2 원본 0.05/frame × 60.")]
        [SerializeField] private float _orbitLerp = 3f;

        [Header("HP Bar (3D Cube)")]
        [Tooltip("거미 위 HP 바 크기 (가로×높이×깊이).")]
        [SerializeField] private Vector3 _hpBarSize = new Vector3(1.2f, 0.15f, 0.2f);

        [Tooltip("거미 위 HP 바 오프셋. Z+ = 탑뷰 화면 위쪽.")]
        [SerializeField] private Vector3 _hpBarOffset = new Vector3(0f, 0.4f, 0.7f);

        [Header("Runtime Body (실루엣용)")]
        [Tooltip("런타임 구체 body 생성 여부. SparkleOrbGreen 내부 mesh 가 Y-up authoring 이라 탑뷰에서 뒤집혀 보이는 이슈 때문에 구체 primitive 를 대신 사용.")]
        [SerializeField] private bool _buildRuntimeBody = true;

        [Tooltip("런타임 구체 크기 (지름 유닛).")]
        [SerializeField] private float _bodySize = 0.9f;

        [Tooltip("구체 색상 (지누스 초록).")]
        [SerializeField] private Color _bodyColor = new Color(0.15f, 0.55f, 0.28f, 1f);

        [Tooltip("구체 Y 오프셋 — 지면에서 약간 띄움.")]
        [SerializeField] private float _bodyYOffset = 0.3f;

        // ─── runtime state ───
        private AbilityData _abilityData;
        private Transform _machineTransform;
        private LayerMask _bugLayer;
        private float _currentHp;
        private float _lp;  // 선회 진동용 내부 타이머
        private Hp3DBar _hpBar;

        // 타겟 락 — 사거리 이탈/사망 전까지 유지해 re-target thrashing 방지.
        private Collider _lockedTarget;
        private EntityId _lockedTargetId;

        // 모든 SpiderDroneInstance 공유 — 이미 다른 거미가 추적 중인 벌레 EntityId 집합.
        // 신규 타겟 선택 시 이 집합에 있는 벌레는 스킵 → 3거미가 3벌레에 분산됨.
        // 사거리 내에 미예약 벌레가 없으면 fallback 으로 예약된 벌레라도 선택 (starvation 방지).
        private static readonly HashSet<EntityId> _claimedTargets = new HashSet<EntityId>();

        private readonly Collider[] _overlapBuffer = new Collider[16];

        /// <summary>Destroy 시 발행. Runner 가 리스트에서 제거.</summary>
        public event Action OnDestroyed;

        public void Initialize(AbilityData data, Transform machine, LayerMask bugLayer)
        {
            _abilityData = data;
            _machineTransform = machine;
            _bugLayer = bugLayer;
            _currentHp = _maxHp;
            _lp = 0f;

            if (_buildRuntimeBody) BuildRuntimeBody();
            BuildHpBar();
        }

        private void BuildRuntimeBody()
        {
            // 탑뷰 실루엣 담당 — primitive Sphere 는 회전 대칭이라 "뒤집힘" 이슈 없음.
            // SparkleOrbGreen 내부 mesh(Euler -90°X 로 authoring) 는 탑뷰에서 orientation 깨짐 → 의존 제거.
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "RuntimeBody";
            if (body.TryGetComponent<Collider>(out var col)) Destroy(col);
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, _bodyYOffset, 0f);
            body.transform.localScale = Vector3.one * _bodySize;

            var r = body.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = "SpiderBody_Runtime" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", _bodyColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", _bodyColor);
            mat.color = _bodyColor;
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private void BuildHpBar()
        {
            _hpBar = Hp3DBar.Create(transform, _hpBarOffset, _hpBarSize);
            _hpBar.SetColors(
                full: new Color(0.32f, 0.81f, 0.4f, 1f),
                low:  new Color(0.95f, 0.28f, 0.22f, 1f),
                lowThreshold: 0.3f);
            _hpBar.SetHealth(1f);
        }

        private void Update()
        {
            if (_currentHp <= 0f) return;

            using var _perf = PerfMarkers.Spider_Update.Auto();

            float dt = Time.deltaTime;
            _lp += _orbitLpSpeed * dt;

            float range = _abilityData != null ? _abilityData.Range : 0f;
            Collider target = AcquireOrRefreshTarget(range);

            if (target != null)
            {
                UpdateChaseAndMelee(target.transform.position, dt);
            }
            else
            {
                UpdateOrbit(dt);
            }

            // HP 자연 감쇠
            _currentHp -= _hpDecayPerSec * dt;
            if (_hpBar != null && _maxHp > 0f)
                _hpBar.SetHealth(Mathf.Max(0f, _currentHp) / _maxHp);

            if (_currentHp <= 0f) DestroySelf();
        }

        // ─── 타겟팅 & 근접 ──────────────────────────────────────

        /// <summary>현재 락 타겟을 유지/재선택. 락 유효하면 그대로, 사거리 이탈·사망 시 재탐색.</summary>
        private Collider AcquireOrRefreshTarget(float range)
        {
            // 현재 락 타겟 유효성 검사
            if (_lockedTarget != null)
            {
                // Unity 파괴된 오브젝트는 == null 로 검출됨 (operator overload).
                Vector3 delta = _lockedTarget.transform.position - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= range * range) return _lockedTarget;
            }
            // 락 해제 후 신규 탐색.
            SetTarget(FindBestBugExcludingClaimed(range));
            return _lockedTarget;
        }

        /// <summary>다른 거미가 추적 중이 아닌 최근접 벌레. 전부 예약돼 있으면 가장 가까운 벌레(fallback).</summary>
        private Collider FindBestBugExcludingClaimed(float range)
        {
            if (range <= 0f) return null;
            int hits;
            using (PerfMarkers.Spider_OverlapSphere.Auto())
                hits = Physics.OverlapSphereNonAlloc(transform.position, range, _overlapBuffer, _bugLayer);
            if (hits <= 0) return null;

            Collider bestUnclaimed = null;
            float bestUnclaimedDistSqr = float.MaxValue;
            Collider bestAny = null;
            float bestAnyDistSqr = float.MaxValue;

            for (int i = 0; i < hits; i++)
            {
                var c = _overlapBuffer[i];
                if (c == null) continue;
                Vector3 delta = c.transform.position - transform.position;
                delta.y = 0f;
                float d = delta.sqrMagnitude;

                if (d < bestAnyDistSqr) { bestAnyDistSqr = d; bestAny = c; }

                if (!_claimedTargets.Contains(c.GetEntityId()) && d < bestUnclaimedDistSqr)
                { bestUnclaimedDistSqr = d; bestUnclaimed = c; }
            }
            return bestUnclaimed != null ? bestUnclaimed : bestAny;
        }

        private void SetTarget(Collider newTarget)
        {
            // 이전 예약 해제 (EntityId 로 기억 — 벌레 파괴돼도 확실히 제거).
            if (_lockedTargetId != default)
            {
                _claimedTargets.Remove(_lockedTargetId);
                _lockedTargetId = default;
            }
            _lockedTarget = newTarget;
            if (newTarget != null)
            {
                _lockedTargetId = newTarget.GetEntityId();
                _claimedTargets.Add(_lockedTargetId);
            }
        }

        private void UpdateChaseAndMelee(Vector3 targetPos, float dt)
        {
            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist < 0.0001f) return;
            dir /= dist;

            // yaw 회전 — 루트 Transform.
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // 멜리 반경 밖이면 접근, 안이면 정지 (통과 방지 + "달라붙는" 느낌).
            if (dist > _meleeRadius)
                transform.position += dir * (_moveSpeed * dt);

            // 범위 내 벌레 모두에게 dps 적용 — 보통 1마리(타겟) 이지만 떼 붙으면 다중 히트.
            ApplyMeleeDamage(dt);
        }

        private void ApplyMeleeDamage(float dt)
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, _meleeRadius, _overlapBuffer, _bugLayer);
            if (hits <= 0) return;
            float dmg = _meleeDps * dt;
            for (int i = 0; i < hits; i++)
            {
                var c = _overlapBuffer[i];
                if (c == null) continue;
                var dmgTarget = c.GetComponent<IDamageable>() ?? c.GetComponentInParent<IDamageable>();
                dmgTarget?.TakeDamage(dmg);
            }
        }

        private void UpdateOrbit(float dt)
        {
            if (_machineTransform == null) return;

            Vector3 mp = _machineTransform.position; mp.y = 0f;
            Vector3 pos = transform.position; pos.y = 0f;

            // 현재 머신→거미 각도 + 각속도 * dt (XZ 평면에서 atan2(z, x) 사용 — v2 의 atan2(y, x) 와 동일)
            Vector3 rel = pos - mp;
            float curA = Mathf.Atan2(rel.z, rel.x);
            float newA = curA + _orbitTurnRate * dt;
            float newR = _orbitBase + Mathf.Sin(_lp) * _orbitAmplitude;

            // 목표 선회 위치
            Vector3 orbitTarget = new Vector3(
                mp.x + Mathf.Cos(newA) * newR,
                0f,
                mp.z + Mathf.Sin(newA) * newR);

            // lerp 접근 — v2 `sd.x += (target-sd.x) * 0.05` (per-frame 0.05) → per-sec 3 대입.
            // 프레임 독립을 위해 `t = 1 - exp(-k*dt)` 로 치환 (k = OrbitLerp).
            float t = 1f - Mathf.Exp(-_orbitLerp * dt);
            Vector3 next = Vector3.Lerp(pos, orbitTarget, t);
            next.y = transform.position.y;
            transform.position = next;
        }

        // ─── 파괴 ────────────────────────────────────────────────

        private void DestroySelf()
        {
            SetTarget(null); // 예약 해제
            if (_hpBar != null)
            {
                Destroy(_hpBar.gameObject);
                _hpBar = null;
            }
            OnDestroyed?.Invoke();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // 외부 요인(씬 종료 등) 대응 — 예약 누수 방지.
            SetTarget(null);
            OnDestroyed?.Invoke();
        }
    }
}

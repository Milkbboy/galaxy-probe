using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 사라 1번 어빌리티 — 블랙홀.
    /// 마우스 위치에 지속형 중력 존을 1개 생성. 반경 내 벌레를 중심으로 끌어당김. 데미지 없음(CC 전용).
    ///
    /// v2.html:994~1000 / 1055 포팅.
    ///
    /// 단위 해석:
    ///   · AbilityData.Range          = 흡인 반경 (v2 180 → Unity 18)
    ///   · AbilityData.DurationSec    = 존 지속 시간 (10s)
    ///   · AbilityData.CooldownSec    = 재사용 쿨다운 (30s)
    ///   · AbilityData.Damage         = 사용 안 함 (흡인 속도는 Runner 상수)
    ///   · AbilityData.VfxScale       = VFX 크기 배율 (기본 1). 데칼·판정엔 영향 없음.
    ///
    /// 동시 존재 1개 — 활성 중 재발동 불가(v2 `!bh.active` 가드 포팅).
    ///
    /// 에디터 라이브 튜닝:
    ///   AbilityData.OnValidated 이벤트를 구독 — 인스펙터에서 Range/VfxScale 을
    ///   변경하는 즉시 활성 존의 판정 반경·데칼·VFX 스케일이 갱신된다. 빌드 포함 X.
    /// </summary>
    public class BlackHoleRunner : IAbilityRunner
    {
        public AbilityType Type => AbilityType.BlackHole;

        // ─── 튜닝 상수 ───
        // v2 pF=0.9/frame × 60 = 54 units/sec (픽셀 기준) → Unity 스케일(÷10) = 5.4.
        private const float PullSpeedPerSec = 5.4f;
        // 너무 가까우면 흡인 멈춤 — 벌레가 중심에 꼬치처럼 박히는 것 방지.
        // v2 d>4 픽셀 가드 → Unity 0.4.
        private const float InnerCutoff = 0.4f;
        // 데칼 Y 오프셋 — Z-fighting 방지.
        private const float DecalYOffset = 0.02f;
        // VortexPortal 프리펩이 XY 세로 authoring이면 바닥 평면화를 위해 90°X 회전 필요.
        // 런타임 실측 후 필요 여부에 따라 true/false 조정. 기본 true (Polygon Arsenal 다수가 Y+ forward).
        private const bool RotateVfxToLyFlat = true;
        // VortexPortalPurple 프리펩이 scale=1 일 때 파티클이 실제로 퍼지는 반경 (실측치).
        // 이 값을 기준으로 (Range / VortexReferenceRadius) 배율을 곱해 SO Range 에 맞춰 VFX 를 키움.
        // 프리펩 교체 시 이 상수도 재측정 필요.
        private const float VortexReferenceRadius = 2f;

        private AbilityData _data;
        private AbilityContext _ctx;
        private float _cooldown;
        private BlackHoleZone _activeZone;

        private readonly Collider[] _overlapBuffer = new Collider[64];

        public float CooldownNormalized =>
            (_data == null || _data.CooldownSec <= 0f)
                ? 0f
                : Mathf.Clamp01(_cooldown / _data.CooldownSec);

        public void Initialize(AbilityData data, AbilityContext ctx)
        {
            _data = data;
            _ctx = ctx;
            _cooldown = 0f;
            _activeZone = null;

#if UNITY_EDITOR
            if (_data != null)
            {
                // 중복 구독 방지 — 같은 SO 를 여러 Runner가 쓸 수 있고, 재진입도 가능.
                _data.OnValidated -= OnDataValidated;
                _data.OnValidated += OnDataValidated;
            }
#endif
        }

        public void Tick(float dt)
        {
            if (_cooldown > 0f) _cooldown = Mathf.Max(0f, _cooldown - dt);

            if (_activeZone != null)
            {
                if (!_activeZone.Tick(dt, _overlapBuffer, _ctx.BugLayer))
                {
                    _activeZone.Dispose();
                    _activeZone = null;
                }
            }
        }

        public bool TryUse(Vector3 aimPoint)
        {
            if (_cooldown > 0f) return false;
            if (_activeZone != null) return false; // 동시 1개 — v2 !bh.active 가드
            if (_data == null || _ctx == null) return false;

            Vector3 center = aimPoint;
            center.y = 0f;

            float radius = Mathf.Max(0.1f, _data.Range);

            _activeZone = new BlackHoleZone(
                center: center,
                radius: radius,
                life: _data.DurationSec,
                pullSpeedPerSec: PullSpeedPerSec,
                innerCutoff: InnerCutoff,
                vfxPrefab: _data.VfxPrefab,
                vfxScale: _data.VfxScale,
                vfxParent: _ctx.VfxParent
            );

            _cooldown = _data.CooldownSec;
            return true;
        }

#if UNITY_EDITOR
        private void OnDataValidated()
        {
            if (_activeZone == null || _data == null) return;
            _activeZone.ApplyLiveTuning(
                radius: Mathf.Max(0.1f, _data.Range),
                vfxScale: _data.VfxScale);
        }
#endif

        // ───────────────────────────────────────────────────────────
        // 단일 블랙홀 존 — 흡인 판정 + VFX 수명 관리.
        // ───────────────────────────────────────────────────────────
        private class BlackHoleZone
        {
            private Vector3 _center;
            private float _radius;
            private readonly float _pullSpeedPerSec;
            private readonly float _innerCutoff;

            private float _life;
            private GameObject _vfxWrapper;           // 부모 GO — 회전 보정
            private Transform _vfxInstanceTransform;  // 프리펩 인스턴스 — scale 조정 대상
            private Vector3 _vfxInstanceBaseScale;    // 프리펩 원본 localScale (스케일 기준점)
            private AbilityRangeDecal _decal;
            private Transform _decalTransform;        // 데칼 transform — localScale 로 반경 조절

            public BlackHoleZone(
                Vector3 center, float radius, float life,
                float pullSpeedPerSec, float innerCutoff,
                GameObject vfxPrefab, float vfxScale,
                Transform vfxParent)
            {
                _center = center;
                _radius = radius;
                _life = life;
                _pullSpeedPerSec = pullSpeedPerSec;
                _innerCutoff = innerCutoff;

                BuildVfx(vfxPrefab, vfxParent);
                BuildRangeDecal(vfxParent);
                ApplyScale(vfxScale);
            }

            private void BuildVfx(GameObject vfxPrefab, Transform vfxParent)
            {
                if (vfxPrefab == null) return;

                // wrapper GO — 바닥 평면화(필요 시) 및 VFX 정리 용도.
                _vfxWrapper = new GameObject("BlackHoleVfx");
                _vfxWrapper.transform.SetParent(vfxParent, false);
                _vfxWrapper.transform.position = _center;
                _vfxWrapper.transform.rotation = RotateVfxToLyFlat
                    ? Quaternion.Euler(90f, 0f, 0f)
                    : Quaternion.identity;

                var instance = Object.Instantiate(vfxPrefab, _vfxWrapper.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                _vfxInstanceTransform = instance.transform;
                _vfxInstanceBaseScale = instance.transform.localScale;
            }

            private void BuildRangeDecal(Transform vfxParent)
            {
                var decalGo = new GameObject("BlackHoleRangeDecal");
                decalGo.transform.SetParent(vfxParent, false);
                decalGo.transform.position = _center + new Vector3(0f, DecalYOffset, 0f);
                decalGo.transform.rotation = Quaternion.identity;
                _decalTransform = decalGo.transform;

                // Mesh 는 radius=1 로 고정 생성 → 스케일로 크기 조절. 매번 Mesh 재빌드 회피.
                _decal = decalGo.AddComponent<AbilityRangeDecal>();
                _decal.SetupMesh(AbilityDecalMeshBuilder.BuildCircle(1f, 48));
                // 사라 테마 보라 (#9c6fff)
                _decal.SetTint(new Color(0.61f, 0.43f, 1f, 1f));
            }

            // 데칼: _radius 에 맞춰 선형 스케일.
            // VFX: (판정 반경을 기준 반경에 맞추는 자동 스케일) × (SO 사용자 배율).
            // → VfxScale = 1 이면 데칼과 거의 같은 크기, 키우면 VFX만 커짐.
            private void ApplyScale(float vfxScale)
            {
                if (_decalTransform != null)
                {
                    _decalTransform.localScale = new Vector3(_radius, 1f, _radius);
                }

                if (_vfxInstanceTransform != null)
                {
                    float autoFit = Mathf.Max(0.01f, _radius / VortexReferenceRadius);
                    float userScale = Mathf.Max(0.01f, vfxScale);
                    _vfxInstanceTransform.localScale = _vfxInstanceBaseScale * (autoFit * userScale);
                }
            }

            /// <summary>에디터 라이브 튜닝 — Range/VfxScale 변경 시 Runner 가 호출.</summary>
            public void ApplyLiveTuning(float radius, float vfxScale)
            {
                _radius = Mathf.Max(0.1f, radius);
                ApplyScale(vfxScale);
            }

            /// <summary>수명이 남아있으면 true. false면 Runner가 Dispose + 제거.</summary>
            public bool Tick(float dt, Collider[] buffer, LayerMask bugLayer)
            {
                _life -= dt;
                if (_life <= 0f) return false;

                ApplyPull(dt, buffer, bugLayer);
                return true;
            }

            private void ApplyPull(float dt, Collider[] buffer, LayerMask bugLayer)
            {
                int hits = Physics.OverlapSphereNonAlloc(_center, _radius, buffer, bugLayer);
                float step = _pullSpeedPerSec * dt;

                for (int i = 0; i < hits; i++)
                {
                    var col = buffer[i];
                    if (col == null) continue;

                    Vector3 to = _center - col.transform.position;
                    to.y = 0f;
                    float d = to.magnitude;
                    if (d <= _innerCutoff) continue;

                    float move = Mathf.Min(step, d - _innerCutoff);
                    col.transform.position += (to / d) * move;
                }
            }

            public void Dispose()
            {
                if (_vfxWrapper != null) Object.Destroy(_vfxWrapper);
                _vfxWrapper = null;
                _vfxInstanceTransform = null;

                if (_decal != null) _decal.Dispose();
                _decal = null;
                _decalTransform = null;
            }
        }
    }
}

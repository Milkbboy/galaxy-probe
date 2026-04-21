using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;
using DrillCorp.Bug.Simple;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 사라 2번 어빌리티 — 충격파.
    /// 머신 중심에서 확장하는 링이 벌레를 밖으로 튕기고 3초간 50% 슬로우. 데미지 없음(CC).
    ///
    /// v2.html:1061~1080 / 1265~1295 포팅.
    ///
    /// 단위 해석:
    ///   · AbilityData.Range       = 링 최대 반경 (v2 360 → Unity 36)
    ///   · AbilityData.CooldownSec = 재사용 쿨다운 (50s)
    ///   · AbilityData.Damage      = 사용 안 함 (데미지 없음)
    ///   · AbilityData.VfxScale    = 중심 발동 VFX(FrostNova) 배율 (기본 1)
    ///   · 확장속도·푸시거리·슬로우는 Runner 내부 상수
    ///
    /// 순간형 — 발동하면 약 0.43초 내에 링이 maxR 에 도달 후 자연 종료.
    ///
    /// 에디터 라이브 튜닝:
    ///   AbilityData.OnValidated 이벤트를 구독 — 인스펙터에서 Range/VfxScale 변경 시
    ///   활성 링의 최대 반경과 VFX 스케일이 즉시 반영. 빌드 포함 X.
    /// </summary>
    public class ShockwaveRunner : IAbilityRunner
    {
        public AbilityType Type => AbilityType.Shockwave;

        // ─── 튜닝 상수 ───
        // v2 14/frame × 60 = 840 units/sec (픽셀) → Unity 스케일(÷10) = 84.
        private const float ExpandSpeedPerSec = 84f;
        // v2 pushDist 80 → Unity 8 유닛 순간 변위.
        private const float PushDistance = 8f;
        // 벌레가 링을 "지금 막 스쳐 지나감" 으로 인정하는 허용 오차 (v2 thickness 28 → Unity 2.8 의 여유치).
        private const float HitMargin = 0.3f;
        // 슬로우 50% × 3초 (v2 180 frame).
        private const float SlowStrength = 0.5f;
        private const float SlowDurationSec = 3f;
        // 데칼 Y 오프셋 — Z-fighting 방지.
        private const float DecalYOffset = 0.02f;
        // 임팩트 VFX 프리펩이 scale=1 일 때 퍼지는 대략적 반경 (실측).
        // 충격파는 머신 중심 임팩트 이펙트이므로 Range 전체에 맞출 필요는 없고, 어느 정도 시각적 비중만.
        // Range 전체 36 에 맞추면 너무 크고, 작게 유지하는 게 v2 원본에 가까움 → 기본 ~2 유닛 기준.
        // LightningWave/FrostNova 계열 공통 기본값.
        private const float ImpactVfxReferenceRadius = 2f;
        // 임팩트 VFX 프리펩이 XY 세로 authoring이면 바닥 평면화를 위해 90°X 회전 필요.
        // LightningWaveBlue / FrostNova 등 Polygon Arsenal Nova 다수가 Y+ forward 라 기본 true.
        private const bool RotateVfxToLyFlat = true;
        // 데칼 링 두께 = 외반경 대비 내반경 비율. 0.85 = 얇은 링, 0.5 = 두꺼운 링.
        private const float RingInnerRatio = 0.85f;

        private AbilityData _data;
        private AbilityContext _ctx;
        private float _cooldown;
        private ShockwaveRing _activeRing;

        private readonly Collider[] _overlapBuffer = new Collider[128];

        public float CooldownNormalized =>
            (_data == null || _data.CooldownSec <= 0f)
                ? 0f
                : Mathf.Clamp01(_cooldown / _data.CooldownSec);

        public void Initialize(AbilityData data, AbilityContext ctx)
        {
            _data = data;
            _ctx = ctx;
            _cooldown = 0f;
            _activeRing = null;

#if UNITY_EDITOR
            if (_data != null)
            {
                _data.OnValidated -= OnDataValidated;
                _data.OnValidated += OnDataValidated;
            }
#endif
        }

        public void Tick(float dt)
        {
            if (_cooldown > 0f) _cooldown = Mathf.Max(0f, _cooldown - dt);

            if (_activeRing != null)
            {
                if (!_activeRing.Tick(dt, _overlapBuffer, _ctx.BugLayer))
                {
                    _activeRing.Dispose();
                    _activeRing = null;
                }
            }
        }

        public bool TryUse(Vector3 aimPoint)
        {
            if (_cooldown > 0f) return false;
            if (_activeRing != null) return false; // 동시 1개
            if (_data == null || _ctx == null || _ctx.MachineTransform == null) return false;

            Vector3 center = _ctx.MachineTransform.position;
            center.y = 0f;

            float maxRadius = Mathf.Max(0.5f, _data.Range);

            _activeRing = new ShockwaveRing(
                center: center,
                maxRadius: maxRadius,
                expandSpeedPerSec: ExpandSpeedPerSec,
                pushDistance: PushDistance,
                hitMargin: HitMargin,
                slowStrength: SlowStrength,
                slowDurationSec: SlowDurationSec,
                impactVfxPrefab: _data.VfxPrefab,
                vfxScale: _data.VfxScale,
                vfxParent: _ctx.VfxParent
            );

            _cooldown = _data.CooldownSec;
            return true;
        }

#if UNITY_EDITOR
        private void OnDataValidated()
        {
            if (_activeRing == null || _data == null) return;
            _activeRing.ApplyLiveTuning(
                maxRadius: Mathf.Max(0.5f, _data.Range),
                vfxScale: _data.VfxScale);
        }
#endif

        // ───────────────────────────────────────────────────────────
        // 단일 충격파 링 — 확장 + 히트 판정 + VFX 수명 관리.
        // ───────────────────────────────────────────────────────────
        private class ShockwaveRing
        {
            private Vector3 _center;
            private float _maxRadius;
            private readonly float _expandSpeed;
            private readonly float _pushDistance;
            private readonly float _hitMargin;
            private readonly float _slowStrength;
            private readonly float _slowDurationSec;

            private float _radius;
            private readonly HashSet<Collider> _hitBugs = new HashSet<Collider>();

            private GameObject _impactVfxWrapper;       // 탑뷰 회전 보정용 부모 GO
            private Transform _impactVfxInstanceTransform; // 프리펩 인스턴스 (scale 조정 대상)
            private Vector3 _impactVfxBaseScale = Vector3.one;
            private AbilityRangeDecal _decal;
            private Transform _decalTransform;

            public ShockwaveRing(
                Vector3 center, float maxRadius,
                float expandSpeedPerSec, float pushDistance, float hitMargin,
                float slowStrength, float slowDurationSec,
                GameObject impactVfxPrefab, float vfxScale,
                Transform vfxParent)
            {
                _center = center;
                _maxRadius = maxRadius;
                _expandSpeed = expandSpeedPerSec;
                _pushDistance = pushDistance;
                _hitMargin = hitMargin;
                _slowStrength = slowStrength;
                _slowDurationSec = slowDurationSec;

                _radius = 0f;

                BuildImpactVfx(impactVfxPrefab, vfxParent);
                BuildRingDecal(vfxParent);
                ApplyScale(vfxScale);
            }

            private void BuildImpactVfx(GameObject prefab, Transform vfxParent)
            {
                if (prefab == null) return;

                // wrapper GO — 바닥 평면화(필요 시) 및 VFX 정리 용도.
                _impactVfxWrapper = new GameObject("ShockwaveImpactVfx");
                _impactVfxWrapper.transform.SetParent(vfxParent, false);
                _impactVfxWrapper.transform.position = _center;
                _impactVfxWrapper.transform.rotation = RotateVfxToLyFlat
                    ? Quaternion.Euler(90f, 0f, 0f)   // XY 세로 → XZ 바닥 평면
                    : Quaternion.identity;

                var instance = Object.Instantiate(prefab, _impactVfxWrapper.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                _impactVfxInstanceTransform = instance.transform;
                _impactVfxBaseScale = instance.transform.localScale;

                // LightningWaveBlue/FrostNova 등 Nova 계열 짧은 효과 — 수명 안전망으로 3초 후 wrapper 통째로 제거.
                // 링 수명(~0.43초) 보다 길게 유지해도 무방.
                Object.Destroy(_impactVfxWrapper, 3f);
            }

            private void BuildRingDecal(Transform vfxParent)
            {
                var decalGo = new GameObject("ShockwaveRingDecal");
                decalGo.transform.SetParent(vfxParent, false);
                decalGo.transform.position = _center + new Vector3(0f, DecalYOffset, 0f);
                decalGo.transform.rotation = Quaternion.identity;
                _decalTransform = decalGo.transform;

                // Ring Mesh는 inner=RingInnerRatio, outer=1 로 고정 생성 → localScale 로 확장.
                // 매 프레임 Mesh 재빌드 회피 (GC/mesh upload 비용 제거).
                _decal = decalGo.AddComponent<AbilityRangeDecal>();
                _decal.SetupMesh(AbilityDecalMeshBuilder.BuildRing(RingInnerRatio, 1f, 64));
                // 사라 테마 청록 (#4fc3f7)
                _decal.SetTint(new Color(0.31f, 0.76f, 0.97f, 1f));
            }

            // 현재 _radius + vfxScale 기준으로 데칼·VFX localScale 갱신.
            private void ApplyScale(float vfxScale)
            {
                if (_decalTransform != null)
                {
                    // 데칼은 현재 _radius (진행 중인 링의 외반경) 에 맞춰 XZ 스케일.
                    float r = Mathf.Max(0.01f, _radius);
                    _decalTransform.localScale = new Vector3(r, 1f, r);
                }

                if (_impactVfxInstanceTransform != null)
                {
                    float autoFit = Mathf.Max(0.01f, _maxRadius / ImpactVfxReferenceRadius);
                    float userScale = Mathf.Max(0.01f, vfxScale);
                    // 임팩트는 _radius 진행과 무관 — _maxRadius 기준으로 한 번만 맞추면 됨.
                    // 다만 ApplyScale 이 매 프레임 호출돼도 결과는 동일 (autoFit·userScale 변화 없으면 no-op).
                    _impactVfxInstanceTransform.localScale = _impactVfxBaseScale * (autoFit * userScale);
                }
            }

            /// <summary>에디터 라이브 튜닝 — Range/VfxScale 변경 시 Runner 가 호출.</summary>
            public void ApplyLiveTuning(float maxRadius, float vfxScale)
            {
                _maxRadius = Mathf.Max(0.5f, maxRadius);
                ApplyScale(vfxScale);
            }

            /// <summary>수명이 남아있으면 true. false면 Runner가 Dispose + 제거.</summary>
            public bool Tick(float dt, Collider[] buffer, LayerMask bugLayer)
            {
                float prevR = _radius;
                _radius += _expandSpeed * dt;
                if (_radius > _maxRadius) _radius = _maxRadius;

                ApplyHits(prevR, _radius, buffer, bugLayer);

                // 데칼 스케일만 매 프레임 갱신 (VFX 는 _maxRadius 기준이라 변화 없음).
                if (_decalTransform != null)
                {
                    float r = Mathf.Max(0.01f, _radius);
                    _decalTransform.localScale = new Vector3(r, 1f, r);
                }

                // maxR 도달하면 종료.
                return _radius < _maxRadius;
            }

            // 링이 prevR → currR 로 전진하는 동안 "현재 링에 스친" 벌레를 1회씩 히트.
            // v2 원본의 `sd >= prevR - sz && sd <= currR + sz` 판정 포팅 — hitMargin 으로 sz 대체.
            private void ApplyHits(float prevR, float currR, Collider[] buffer, LayerMask bugLayer)
            {
                // OverlapSphere 는 외반경 기준 — currR 범위로 한 번에 모두 수집 후 거리 필터.
                int hits = Physics.OverlapSphereNonAlloc(_center, currR + _hitMargin, buffer, bugLayer);
                for (int i = 0; i < hits; i++)
                {
                    var col = buffer[i];
                    if (col == null) continue;
                    if (_hitBugs.Contains(col)) continue;

                    Vector3 to = col.transform.position - _center;
                    to.y = 0f;
                    float d = to.magnitude;

                    if (d < prevR - _hitMargin) continue;   // 이미 링 안쪽 → 이번 프레임 아님
                    if (d > currR + _hitMargin) continue;   // 아직 링 밖 → 다음 프레임

                    _hitBugs.Add(col);

                    if (d > 0.01f)
                    {
                        Vector3 dir = to / d;
                        col.transform.position += dir * _pushDistance;
                    }
                    if (col.TryGetComponent<SimpleBug>(out var bug))
                        bug.ApplySlow(_slowStrength, _slowDurationSec);
                }
            }

            public void Dispose()
            {
                // 임팩트 VFX wrapper 는 자체 수명(Destroy 3s) 으로 끝남 — 강제 제거 안 함.
                if (_decal != null) _decal.Dispose();
                _decal = null;
                _decalTransform = null;
                _hitBugs.Clear();
            }
        }
    }
}

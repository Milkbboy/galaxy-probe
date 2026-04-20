using UnityEngine;
using DrillCorp.Aim;
using DrillCorp.Machine;

namespace DrillCorp.Weapon.Laser
{
    /// <summary>
    /// 자립형 레이저 빔 (Phase 4)
    /// - LaserWeapon 참조 없이 값만 보관 (무기 파괴돼도 수명까지 살아남음)
    /// - XZ 평면 마우스 추적, _tickInterval 간격 OverlapSphere 틱 데미지
    /// - 4겹 레이어(2×LineRenderer 링 + 2×SpriteRenderer 채움) + LifeArc 호
    ///   · OuterGlow/RingStroke: LineRenderer 링 (프로토 ctx.stroke 재현, 선명한 외곽)
    ///   · Core/Center: SpriteRenderer 채움 (프로토 ctx.fill 재현)
    ///   · LifeArc: LineRenderer 호 (life/maxLife로 짧아짐 — 호 길이만, 알파 고정)
    /// - 알파 = baseAlpha × pulse (Core/OuterGlow만 pulse) — 수명에 따른 페이드는 적용하지 않음
    ///   (의도적으로 프로토 alpha=life/maxLife 곱셈을 제외 — 빔이 끝까지 또렷하게 보이도록)
    /// 프로토 _.html L294(update) + L307(drawLasers) 충실 구현 (페이드 제외).
    /// </summary>
    public class LaserBeam : MonoBehaviour
    {
        [Header("Ring Layers — LineRenderer (프리펩에서 바인딩)")]
        [SerializeField] private LineRenderer _outerGlow;
        [SerializeField] private LineRenderer _ringStroke;
        [SerializeField] private LineRenderer _coreStroke;     // Core 외곽선 (프로토 ctx.stroke alpha 0.9)

        [Header("Fill Layers — SpriteRenderer (프리펩에서 바인딩)")]
        [SerializeField] private SpriteRenderer _core;
        [SerializeField] private SpriteRenderer _center;

        [Header("Crosshair — 분홍 가로/세로 십자 (프로토 L307 cr=r+4)")]
        [SerializeField] private LineRenderer _crosshairH;     // 가로
        [SerializeField] private LineRenderer _crosshairV;     // 세로
        [SerializeField] private float _crosshairOffset = 0.067f;  // 프로토 +4px → 0.067u

        [Header("Center Particles — 빔 영역 분홍 스파클 fx (프로토 L294)")]
        [SerializeField] private ParticleSystem _centerParticles;

        [Header("Life Arc (LineRenderer 자식, 프리펩에서 바인딩)")]
        [SerializeField] private LineRenderer _lifeArc;
        [SerializeField] private int _lifeArcSegments = 64;
        [SerializeField] private float _lifeArcRadiusOffset = 0.2f;

        // 색 상수 (프로토 _.html L307)
        private static readonly Color ColOuterGlow = new Color(1f, 0.09f, 0.267f, 1f);     // #ff1744
        private static readonly Color ColRingStroke = new Color(1f, 0.376f, 0.565f, 1f);   // #ff6090
        private static readonly Color ColCore = new Color(1f, 0.09f, 0.267f, 1f);          // #ff1744
        private static readonly Color ColCoreStroke = new Color(1f, 0.09f, 0.267f, 1f);    // #ff1744 (alpha 0.9 진한 외곽선)
        private static readonly Color ColCenter = new Color(1f, 0.784f, 0.823f, 1f);       // #ffc8d2
        private static readonly Color ColCrosshair = new Color(1f, 0.376f, 0.565f, 1f);    // #ff6090
        private static readonly Color ColLifeArc = new Color(1f, 0.09f, 0.267f, 1f);       // #ff1744

        // 기본 알파 (프로토 drawLasers 라인별)
        private const float BaseAlphaOuter = 0.12f;
        private const float BaseAlphaRing = 0.60f;
        private const float BaseAlphaCore = 0.35f;
        private const float BaseAlphaCoreStroke = 0.90f;   // 프로토 stroke alpha 0.9 — 빔 본체 윤곽 선명도 핵심
        private const float BaseAlphaCenter = 0.70f;
        private const float BaseAlphaCrosshair = 0.70f;
        private const float BaseAlphaLifeArc = 0.8f;

        // 링 해상도 (OuterGlow/RingStroke/CoreStroke — loop=true, 고정 positionCount)
        private const int RingSegments = 64;

        // 추적/피격 상태
        private float _life, _maxLife;
        private float _radius, _damage, _tickInterval, _speed, _stopDistance;
        private float _dmgTick;
        private int _bugLayer;
        private AimController _aim;

        // 공용 버퍼 (한 프레임에 여러 빔 처리 안전)
        private static readonly Collider[] _overlapBuffer = new Collider[64];

        public float LifeRatio => _maxLife > 0f ? Mathf.Clamp01(_life / _maxLife) : 0f;

        public void Initialize(AimController aim, LaserWeaponData data, LayerMask bugLayer)
            => Initialize(aim, data, bugLayer, data != null ? data.Damage : 0f,
                          data != null ? data.BeamRadius : 1f);

        /// <summary>WeaponUpgrade 보정된 damage만 받는 오버로드 — 반경은 data.BeamRadius.</summary>
        public void Initialize(AimController aim, LaserWeaponData data, LayerMask bugLayer, float effectiveDamage)
            => Initialize(aim, data, bugLayer, effectiveDamage,
                          data != null ? data.BeamRadius : 1f);

        /// <summary>WeaponUpgrade 보정된 damage + radius 를 모두 받는 오버로드 (v2 range 업그레이드).</summary>
        public void Initialize(AimController aim, LaserWeaponData data, LayerMask bugLayer,
            float effectiveDamage, float effectiveRadius)
        {
            _aim = aim;
            _life = _maxLife = data.BeamDuration;
            _radius = effectiveRadius;   // 업그레이드 반영된 반경
            _damage = effectiveDamage;
            _tickInterval = data.TickInterval;
            _speed = data.BeamSpeed;
            _stopDistance = data.StopDistance;
            _bugLayer = bugLayer;
            _dmgTick = _tickInterval;

            // 지면 평면 고정 (Y=0). 프리펩 회전(90,0,0)은 Instantiate가 이미 보존
            var p = transform.position;
            p.y = 0f;
            transform.position = p;

            // _radius 기반 각 레이어 크기 적용 (업그레이드 대응)
            ApplyLayerScales();
        }

        /// <summary>
        /// Core/Center는 SR localScale로, 링들은 LR positionCount 원주로, 십자는 LR 직선으로,
        /// ParticleSystem은 shape radius로 _radius 반영. PNG 지름 1 유닛 기준이라 SR scale 값 = 목표 지름.
        /// </summary>
        private void ApplyLayerScales()
        {
            SetLayerDiameter(_core, _radius * 2f);
            SetLayerDiameter(_center, (_radius * 0.45f) * 2f);

            RebuildRing(_outerGlow, _radius + 0.13f);
            RebuildRing(_ringStroke, _radius + 0.05f);
            RebuildRing(_coreStroke, _radius);    // Core fill과 같은 반경 (외곽선)

            // 십자 (프로토 L307: cr = lb.r + 4px → _radius + _crosshairOffset)
            float cr = _radius + _crosshairOffset;
            RebuildCrosshairLine(_crosshairH, horizontal: true, cr);
            RebuildCrosshairLine(_crosshairV, horizontal: false, cr);

            // 파티클 emission shape 반경
            if (_centerParticles != null)
            {
                var shape = _centerParticles.shape;
                shape.radius = _radius;
            }
        }

        private static void SetLayerDiameter(SpriteRenderer sr, float diameter)
        {
            if (sr == null) return;
            sr.transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        /// <summary>LineRenderer를 `radius` 반경 원(loop)로 재구성. positionCount 고정 64.</summary>
        private static void RebuildRing(LineRenderer lr, float radius)
        {
            if (lr == null) return;
            if (lr.positionCount != RingSegments)
                lr.positionCount = RingSegments;

            for (int i = 0; i < RingSegments; i++)
            {
                float a = (float)i / RingSegments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
        }

        /// <summary>십자 직선 1개 — 빔 local 좌표 (-cr~+cr) 양 끝 두 점.</summary>
        private static void RebuildCrosshairLine(LineRenderer lr, bool horizontal, float cr)
        {
            if (lr == null) return;
            if (lr.positionCount != 2) lr.positionCount = 2;

            // 빔 local: X = 가로(월드 X), Y = 세로(월드 Z, 화면 상하)
            if (horizontal)
            {
                lr.SetPosition(0, new Vector3(-cr, 0f, 0f));
                lr.SetPosition(1, new Vector3(+cr, 0f, 0f));
            }
            else
            {
                lr.SetPosition(0, new Vector3(0f, -cr, 0f));
                lr.SetPosition(1, new Vector3(0f, +cr, 0f));
            }
        }

        private void OnDestroy()
        {
            DrillCorp.Audio.AudioManager.Instance?.StopLaserBeam();
        }

        private void Update()
        {
            // 수명 감소 — 만료 시 자파괴 (자식 전부 함께 제거)
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            UpdateTracking();
            UpdateDamageTick();

            // 시각 — Core/OuterGlow 펄스(깜빡임) + LifeArc 호 길이 갱신
            // 빔 자체 알파는 고정 (수명 따라 페이드시키지 않음)
            float pulse = 0.7f + Mathf.Sin(Time.time * 20f) * 0.3f;   // 프로토 Math.sin(Date.now()*0.02)
            UpdateVisualAlpha(pulse);
            UpdateLifeArc(LifeRatio);
        }

        /// <summary>XZ 평면 마우스 추적 (프로토 L294). _stopDistance 이내면 이동 중단.</summary>
        private void UpdateTracking()
        {
            if (_aim == null) return;

            Vector3 target = _aim.AimPosition;
            target.y = transform.position.y;
            Vector3 delta = target - transform.position;
            delta.y = 0f;

            float d = new Vector2(delta.x, delta.z).magnitude;
            if (d <= _stopDistance) return;

            Vector3 step = delta / d * (_speed * Time.deltaTime);
            transform.position += step;
        }

        /// <summary>_tickInterval 간격 OverlapSphere → 범위 내 모든 벌레에 Damage (프로토 L294).</summary>
        private void UpdateDamageTick()
        {
            _dmgTick -= Time.deltaTime;
            if (_dmgTick > 0f) return;

            _dmgTick = _tickInterval;

            int hit = Physics.OverlapSphereNonAlloc(
                transform.position, _radius, _overlapBuffer, _bugLayer);

            for (int i = 0; i < hit; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;

                var dmg = col.GetComponent<IDamageable>()
                          ?? col.GetComponentInParent<IDamageable>();
                dmg?.TakeDamage(_damage);
            }
        }

        /// <summary>
        /// 4겹 레이어 알파 갱신. 기본 알파 × 펄스(코어/글로우만).
        /// 수명에 따른 페이드 곱셈은 적용하지 않음 — 빔이 끝까지 또렷하게 유지.
        /// SR는 color, LR는 start/endColor로 반영.
        /// </summary>
        private void UpdateVisualAlpha(float pulse)
        {
            SetLineAlpha(_outerGlow, ColOuterGlow, BaseAlphaOuter * pulse);
            SetLineAlpha(_ringStroke, ColRingStroke, BaseAlphaRing);
            SetSrAlpha(_core, ColCore, BaseAlphaCore * pulse);
            SetLineAlpha(_coreStroke, ColCoreStroke, BaseAlphaCoreStroke);
            SetSrAlpha(_center, ColCenter, BaseAlphaCenter);
            SetLineAlpha(_crosshairH, ColCrosshair, BaseAlphaCrosshair);
            SetLineAlpha(_crosshairV, ColCrosshair, BaseAlphaCrosshair);
        }

        private static void SetSrAlpha(SpriteRenderer sr, Color baseColor, float alpha)
        {
            if (sr == null) return;
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha));
        }

        private static void SetLineAlpha(LineRenderer lr, Color baseColor, float alpha)
        {
            if (lr == null) return;
            var c = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha));
            lr.startColor = c;
            lr.endColor = c;
        }

        /// <summary>
        /// 수명 호 재구성 — positionCount를 ratio에 비례 조정 + 각 점을 시계방향 원주 좌표로 설정.
        /// 빔 local 좌표계에서 XY 평면에 그림 → 프리펩 회전(90,0,0)에 의해 월드 XZ 평면에 누움.
        /// </summary>
        private void UpdateLifeArc(float ratio)
        {
            if (_lifeArc == null) return;

            int count = Mathf.Max(2, Mathf.RoundToInt(_lifeArcSegments * ratio) + 1);
            if (_lifeArc.positionCount != count)
                _lifeArc.positionCount = count;

            float r = _radius + _lifeArcRadiusOffset;
            float totalAngle = Mathf.PI * 2f * ratio;
            // Unity 월드 XZ 평면(탑뷰)에서 sin(+π/2) → 월드 Z+ 가 12시(위쪽).
            // 프로토는 2D 캔버스(Y 아래) 기준 -π/2가 12시지만, Unity 좌표계에선 부호 반전 필요.
            // AimWeaponRing.cs도 동일하게 +π/2 사용.
            float start = Mathf.PI * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (float)i / (count - 1) : 0f;
                float a = start - totalAngle * t;   // 각도 감소 = 시계방향 (12→3→6→9)
                _lifeArc.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
            }

            // 알파 고정 — 호 길이(positionCount)만 ratio로 줄어듦
            Color c = new Color(ColLifeArc.r, ColLifeArc.g, ColLifeArc.b, BaseAlphaLifeArc);
            _lifeArc.startColor = c;
            _lifeArc.endColor = c;
        }
    }
}

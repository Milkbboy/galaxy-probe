using UnityEngine;
using DrillCorp.Machine;
using DrillCorp.UI.Minimap;
using DrillCorp.Audio;
using DrillCorp.Core;

namespace DrillCorp.Bug.Simple
{
    /// <summary>
    /// 프로토타입(_.html) 스타일의 단순 벌레.
    /// 머신을 향해 XZ 평면에서 직진하며, 접촉 시 지속 피해를 준다.
    /// </summary>
    public class SimpleBug : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _contactDamagePerSecond = 16.8f; // 프로토타입 0.28/frame * 60fps
        [SerializeField] private float _contactRange = 0.73f;           // 프로토타입 44px 환산

        [Header("Hitbox (무기 감지용)")]
        [Tooltip("자동으로 SphereCollider 추가 + 'Bug' 레이어 지정")]
        [SerializeField] private bool _autoSetupHitbox = true;
        [SerializeField] private float _hitboxRadius = 0.5f;

        [Header("Minimap")]
        [SerializeField] private bool _showOnMinimap = true;
        [SerializeField] private float _minimapIconSize = 0.8f;
        [SerializeField] private MinimapIcon.IconShape _minimapShape = MinimapIcon.IconShape.Circle;

        [Header("VFX")]
        [Tooltip("VFX 스폰 위치 (빈 GameObject 자식으로 두고 여기서 위치 조정). null이면 벌레 중심")]
        [SerializeField] private Transform _fxSocket;
        [SerializeField] private GameObject _hitVfxPrefab;
        [SerializeField] private GameObject _deathVfxPrefab;
        [Tooltip("VFX 크기 = 프리펩 authored × 벌레 스케일 × 이 값. 1=벌레와 동일, 2=두 배")]
        [SerializeField] private float _vfxScaleMultiplier = 2f;

        [Header("Slow (충격파 등)")]
        [Tooltip("슬로우 시작 순간 한 번만 재생할 임팩트 VFX (예: ChainedFrost). " +
                 "지속 표시는 머티리얼 틴트로 처리 → 대량 벌레 동시 슬로우여도 렌더 부하 X. " +
                 "비우면 VFX 없이 틴트만.")]
        [SerializeField] private GameObject _slowVfxPrefab;
        [Tooltip("슬로우 VFX 의 바닥(XZ) 평면 정렬이 필요하면 on. Polygon Arsenal 다수가 XY authoring 이라 기본 true.")]
        [SerializeField] private bool _rotateSlowVfxFlat = true;
        [Tooltip(
            "슬로우 VFX 크기 배율. 벌레 localScale 을 상쇄해 월드 기준 일정 크기를 유지 + 추가 배율.\n" +
            "작은 벌레(Size=0.5)라도 VFX 는 동일한 시각 크기로 보이도록 보정."
        )]
        [Min(0.01f)]
        [SerializeField] private float _slowVfxScale = 3f;
        [Tooltip("임팩트 VFX 표시 시간(초). 0.5 권장 — 슬로우 지속 시간과 무관, 짧은 임팩트만 보여줌.")]
        [Min(0.1f)]
        [SerializeField] private float _slowVfxDuration = 0.5f;
        [Tooltip("슬로우 지속 중 벌레 몸체에 섞는 틴트 색 (청록). 알파=혼합 비율.")]
        [SerializeField] private Color _slowTint = new Color(0.31f, 0.76f, 0.97f, 0.55f);

        private SimpleBugData _data;
        private Transform _target;
        private float _hp;
        private float _maxHp;
        private float _speed;
        private float _score;
        private bool _isDead;

        // Slow runtime state
        private float _slowStrength;
        private float _slowTimer;
        private GameObject _slowVfxInstance;

        // 슬로우 틴트 — 벌레 MeshRenderer 머티리얼 캐싱. 슬로우 중 _Color/_BaseColor 를 _slowTint 와 Lerp.
        private Renderer[] _renderers;
        private Color[] _rendererBaseColors;
        private bool _tintApplied;

        public float CurrentHealth => _hp;
        public float MaxHealth => _maxHp;
        public bool IsDead => _isDead;
        public float Score => _score;
        public SimpleBugData Data => _data;

        public void Initialize(SimpleBugData data, Transform target, int wave)
        {
            _data = data;
            _target = target;
            _maxHp = data.GetHp(wave);
            _hp = _maxHp;
            _speed = data.GetSpeed(wave);
            _score = data.Score;
            _isDead = false;

            transform.localScale = Vector3.one * data.Size;

            if (_autoSetupHitbox) SetupHitbox();

            if (_showOnMinimap)
            {
                MinimapIcon.Create(transform, data.Tint, _minimapIconSize, _minimapShape);
            }
        }

        private void SetupHitbox()
        {
            int bugLayer = LayerMask.NameToLayer("Bug");
            if (bugLayer != -1) gameObject.layer = bugLayer;

            if (!TryGetComponent<Collider>(out _))
            {
                var col = gameObject.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = _hitboxRadius;
            }
        }

        private void Update()
        {
            if (_isDead || _target == null) return;

            // 슬로우 타이머 감소 (_isDead 상태 아닐 때만 흐름)
            if (_slowTimer > 0f)
            {
                _slowTimer -= Time.deltaTime;
                if (_slowTimer <= 0f)
                {
                    _slowTimer = 0f;
                    _slowStrength = 0f;
                    ClearSlowTint();
                    // VFX 는 이미 _slowVfxDuration 후 자동 Destroy — 여기선 처리 X.
                }
            }

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (dist < 0.001f) return;

            Vector3 dir = toTarget / dist;
            float speedMul = 1f - _slowStrength;  // 슬로우 감속 적용
            transform.position += dir * _speed * speedMul * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (dist < _contactRange && _target.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(_contactDamagePerSecond * Time.deltaTime);
            }
        }

        /// <summary>
        /// 타이머 기반 감속. 더 강한 슬로우만 덮어쓰고, 지속시간은 max(기존, 신규).
        /// 충격파 어빌리티 등이 호출.
        /// </summary>
        /// <param name="strength">감속 강도 0~0.9 (0.5 = 50% 감속)</param>
        /// <param name="durationSec">지속 시간(초)</param>
        public void ApplySlow(float strength, float durationSec)
        {
            if (_isDead) return;
            strength = Mathf.Clamp(strength, 0f, 0.9f);
            if (strength <= 0f || durationSec <= 0f) return;

            bool firstApply = !_tintApplied && _slowTimer <= durationSec;
            if (strength > _slowStrength) _slowStrength = strength;
            if (durationSec > _slowTimer) _slowTimer = durationSec;

            // 지속 표시 = 머티리얼 틴트 (거의 공짜)
            ApplySlowTint();

            // 임팩트 VFX 는 "처음 슬로우 걸린 순간" 에만 짧게 1회. 리프레시는 억제해 부하 제어.
            if (firstApply) SpawnSlowImpactVfx();
        }

        // 슬로우 시작 순간 한 번만 재생하는 짧은 임팩트 VFX. _slowVfxDuration 후 자동 Destroy.
        // wrapper GO 에 90°X 회전으로 탑뷰 평면화 (ChainedFrost 등 XY authoring 대응).
        // wrapper localScale 로 벌레 scale 을 상쇄해 월드 기준 일정 크기 유지 + _slowVfxScale 배율 적용.
        private void SpawnSlowImpactVfx()
        {
            if (_slowVfxPrefab == null) return;

            _slowVfxInstance = new GameObject("SlowVfx");
            _slowVfxInstance.transform.SetParent(transform, false);
            _slowVfxInstance.transform.localPosition = Vector3.zero;
            _slowVfxInstance.transform.localRotation = _rotateSlowVfxFlat
                ? Quaternion.Euler(90f, 0f, 0f)
                : Quaternion.identity;

            // 벌레 localScale 의 역수 × 사용자 배율 → 부모가 작아도 VFX 는 일정 월드 크기.
            Vector3 parentScale = transform.localScale;
            float invX = parentScale.x > 0.001f ? 1f / parentScale.x : 1f;
            float invY = parentScale.y > 0.001f ? 1f / parentScale.y : 1f;
            float invZ = parentScale.z > 0.001f ? 1f / parentScale.z : 1f;
            _slowVfxInstance.transform.localScale = new Vector3(
                invX * _slowVfxScale,
                invY * _slowVfxScale,
                invZ * _slowVfxScale);

            var fx = Instantiate(_slowVfxPrefab, _slowVfxInstance.transform);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = Quaternion.identity;

            // 짧은 임팩트만 — _slowVfxDuration 후 wrapper 통째로 제거. 지속 슬로우는 틴트가 담당.
            Destroy(_slowVfxInstance, _slowVfxDuration);
            // 참조는 유지되지만 Destroy 가 예약됐고, ClearSlowVfxImmediate 에서만 직접 제거.
        }

        // 죽거나 슬로우 해제 시 VFX 잔상 강제 정리.
        private void ClearSlowVfxImmediate()
        {
            if (_slowVfxInstance != null)
            {
                Destroy(_slowVfxInstance);
                _slowVfxInstance = null;
            }
        }

        // ─── Slow Tint (지속 표시) ───

        private void ApplySlowTint()
        {
            if (_tintApplied) return;
            CacheRenderers();
            if (_renderers == null) return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                var mat = r.material; // instance — 공유 머티리얼 오염 없음
                Color baseColor = _rendererBaseColors[i];
                Color tinted = Color.Lerp(baseColor, new Color(_slowTint.r, _slowTint.g, _slowTint.b, baseColor.a), _slowTint.a);
                SetMaterialColor(mat, tinted);
            }
            _tintApplied = true;
        }

        private void ClearSlowTint()
        {
            if (!_tintApplied) return;
            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    var r = _renderers[i];
                    if (r == null) continue;
                    SetMaterialColor(r.material, _rendererBaseColors[i]);
                }
            }
            _tintApplied = false;
        }

        // 처음 한 번만 — Renderer 배열과 기본 색 캐싱. material 접근은 instance 를 만들므로 최초에 1회.
        private void CacheRenderers()
        {
            if (_renderers != null) return;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _rendererBaseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) { _rendererBaseColors[i] = Color.white; continue; }
                var mat = r.material;
                _rendererBaseColors[i] = ReadMaterialColor(mat);
            }
        }

        // URP/Built-in 호환 — _BaseColor 우선, 없으면 _Color.
        private static Color ReadMaterialColor(Material mat)
        {
            if (mat == null) return Color.white;
            if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
            if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
            return Color.white;
        }

        private static void SetMaterialColor(Material mat, Color c)
        {
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        }

        public void TakeDamage(float damage)
        {
            if (_isDead) return;
            _hp -= damage;
            AudioManager.Instance?.PlayBugHit();

            if (_hp <= 0f)
            {
                _hp = 0f;
                _isDead = true;
                ClearSlowVfxImmediate();   // 죽을 때 슬로우 VFX 정리 (자식 Destroy 에 포함되지만 명시적으로)
                PlayDeathVfx();   // 치명타 — 사망 VFX만
                GameEvents.OnBugKilled?.Invoke(_data != null ? (int)_data.Kind : 0);
                GameEvents.OnBugScoreEarned?.Invoke(_score);   // v2 — sessionOre += score*0.5
                bool isElite = _data != null && _data.Kind == SimpleBugData.BugKind.Elite;
                GameEvents.OnBugDied?.Invoke(transform.position, isElite);
                Destroy(gameObject);
            }
            else
            {
                PlayHitVfx();     // 생존 — 피격 VFX만
            }
        }

        private void PlayHitVfx()
        {
            if (_hitVfxPrefab != null) SpawnScaledVfx(_hitVfxPrefab);
        }

        private void PlayDeathVfx()
        {
            if (_deathVfxPrefab != null) SpawnScaledVfx(_deathVfxPrefab);
        }

        // VFX 스폰 — 프리펩 authored 회전·스케일 보존 + 벌레 크기에 비례 스케일링
        private void SpawnScaledVfx(GameObject prefab)
        {
            GameObject vfx = Instantiate(prefab);
            vfx.transform.position = _fxSocket != null ? _fxSocket.position : transform.position;
            vfx.transform.localScale = Vector3.Scale(
                vfx.transform.localScale,
                transform.localScale * _vfxScaleMultiplier);

            var ps = vfx.GetComponent<ParticleSystem>();
            Destroy(vfx, ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f);
        }

        public void Heal(float amount)
        {
            if (_isDead) return;
            _hp = Mathf.Min(_maxHp, _hp + amount);
        }
    }
}

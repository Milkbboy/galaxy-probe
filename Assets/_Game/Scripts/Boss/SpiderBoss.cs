using UnityEngine;
using TMPro;
using DrillCorp.Machine;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.Bug.Simple;
using DrillCorp.UI;
using DrillCorp.VFX.Pool;

namespace DrillCorp.Boss
{
    /// <summary>
    /// v2 거미 보스 — 6각 perch 자리에서 점프하며 머신을 압박.
    /// 근거: docs/V2-prototype.html spawnBoss/tickBoss/damageBoss
    /// 좌표계: 탑다운 — perch는 머신 중심 XZ 평면에 배치, Y(높이)는 점프 시에만 변동.
    ///
    /// 행동 사이클: Jumping → Attacking(소환) → Walking → Idle → 다음 점프 (또는 Telegraphing 분기).
    /// 튜닝 수치는 BossData SO 에서 일괄 관리 (시트 'BossData' 1행 ↔ Boss_Spider.asset).
    /// </summary>
    public class SpiderBoss : MonoBehaviour, IDamageable
    {
        // ─── v2 상수 (V2-prototype.html line 715~717) — 시각/물리 상수, 튜닝 X ───
        private const int PERCH_COUNT = 6;
        private const float PERCH_RADIUS = 15f;          // v2 BOSS_PERCH_R=200 (px) → Unity 8m
        private const float JUMP_HEIGHT = 3.6f;         // v2 sin*90 (px) → Unity 3.6m

        private enum BossState { Walking, Idle, Jumping, Attacking, Telegraphing, Flinching, Pouncing }

        [Header("Data (시트 ↔ SO)")]
        [Tooltip("거미 보스 튜닝 SO (Stats / Movement / Attack / Telegraph). 비우면 폴백 기본값 사용.")]
        [SerializeField] private BossData _data;

        [Header("References")]
        [SerializeField] private Transform _machine;

        [Header("Boss Children (착지 시 소환)")]
        [Tooltip("새끼 거미 SimpleBugData (Swift 베이스 + IsBossChild=true 권장)")]
        [SerializeField] private SimpleBugData _childBugData;

        [Header("HP Bar (3D 월드)")]
        [SerializeField] private bool _showHpBar = true;
        [Tooltip("HP 바를 거미 머리 위로 띄우는 오프셋 (Y=높이, Z=화면 위쪽)")]
        [SerializeField] private Vector3 _hpBarOffset = new Vector3(0f, 4f, 0f);
        [Tooltip("HP 바 크기 (가로, 두께, 깊이)")]
        [SerializeField] private Vector3 _hpBarSize = new Vector3(4f, 0.4f, 0.5f);
        [Tooltip("바 위에 'current / max' 숫자 라벨 표시")]
        [SerializeField] private bool _hpBarShowLabel = true;

        [Header("VFX")]
        [Tooltip("VFX 스폰 위치 (빈 GameObject 자식으로 두고 여기서 위치 조정). null 이면 거미 중심.")]
        [SerializeField] private Transform _fxSocket;
        [Tooltip("피격 시마다 짧게 재생 (보라/검보라 권장)")]
        [SerializeField] private GameObject _hitVfxPrefab;
        [Tooltip("HP 0 사망 시 1회 재생 (큰 폭발 권장)")]
        [SerializeField] private GameObject _deathVfxPrefab;
        [Tooltip("VFX 크기 배율 (보스 거대해서 1보다 크게 권장)")]
        [Min(0.1f)]
        [SerializeField] private float _vfxScale = 2f;
        [Tooltip("Hit VFX 최소 재생 간격 (초). 기관총 연사로 매 프레임 터지는 부하 방지.")]
        [Min(0f)]
        [SerializeField] private float _hitVfxMinInterval = 0.1f;

        [Header("Telegraph 시각 (튜닝 수치는 BossData)")]
        [Tooltip("! 아이콘 머리 위 오프셋 (Y=높이, Z=화면 위쪽).")]
        [SerializeField] private Vector3 _warningIconOffset = new Vector3(0f, 5.5f, 0f);
        [Tooltip("! 아이콘 폰트 크기.")]
        [SerializeField] private float _warningIconFontSize = 12f;

        [Header("Animator Triggers")]
        [SerializeField] private string _attackTrigger = "TriggerAttack";
        [SerializeField] private string _deathTrigger = "TriggerDeath";
        [SerializeField] private string _speedParam = "Speed";

        // ─── BossData 가 비었을 때만 쓰는 폴백 (편의성). 실배포에선 항상 SO 바인딩.
        private static BossData _fallback;
        private BossData Data
        {
            get
            {
                if (_data != null) return _data;
                if (_fallback == null) _fallback = ScriptableObject.CreateInstance<BossData>();
                return _fallback;
            }
        }

        private Animator _animator;
        private float _hp;
        private float _maxHp;             // Initialize 시 Data 에서 캐시 (HP 바 갱신용)
        private bool _isDead;
        private Hp3DBar _hpBar;
        private float _hitVfxNextAllowedTime;

        // 행동 상태
        private BossState _state;
        private float _stateTimer;
        private int _perchIdx;
        private Vector3 _perchAnchor;
        private Vector3 _walkTarget;

        // 점프 보간 상태
        private float _jumpT;
        private float _jumpDuration;
        private Vector3 _jumpFrom;
        private Vector3 _jumpTo;

        // 공격 상태
        private bool _attackSpawnedThisCycle;

        // 텔레그래프 상태
        private int _cyclesSinceTelegraph;
        private bool _hadFirstIdle;
        private int _telegraphHitsTaken;
        private float _telegraphPulseTime;
        private Vector3 _baseScale;
        private GameObject _warningIconGo;
        private TextMeshPro _warningIconLabel;

        // 외부 호환
        private bool IsJumping => _state == BossState.Jumping || _state == BossState.Pouncing;

        public float CurrentHealth => _hp;
        public float MaxHealth => _maxHp;
        public bool IsDead => _isDead;
        public BossData BossData => _data;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();

            if (_machine == null)
            {
                var mc = FindAnyObjectByType<MachineController>();
                if (mc != null) _machine = mc.transform;
            }
        }

        public void Initialize(Vector3 spawnPos)
        {
            _maxHp = Data.MaxHp;
            _hp = _maxHp;
            _isDead = false;
            _baseScale = transform.localScale;

            _perchIdx = Random.Range(0, PERCH_COUNT);
            _perchAnchor = GetPerchPosition(_perchIdx);
            transform.position = _perchAnchor;
            EnterIdle();

            FaceMachine();

            CreateHpBar();
            CreateWarningIcon();

            GameEvents.OnBossSpawned?.Invoke(transform.position);
        }

        private void CreateHpBar()
        {
            if (!_showHpBar) return;
            if (_hpBar != null) return;

            _hpBar = Hp3DBar.Create(transform, _hpBarOffset, _hpBarSize, _hpBarShowLabel);
            _hpBar.SetColors(
                new Color(0.78f, 0.40f, 1f, 1f),     // #c060ff
                new Color(1f, 0.13f, 0.27f, 1f),     // #ff2244
                lowThreshold: Data.HpBarLowThreshold);
            _hpBar.SetHealth(_hp, _maxHp);
        }

        private void CreateWarningIcon()
        {
            if (_warningIconGo != null) return;

            _warningIconGo = new GameObject("BossWarningIcon");
            _warningIconGo.transform.SetParent(transform, false);
            _warningIconGo.transform.localPosition = _warningIconOffset;

            _warningIconLabel = _warningIconGo.AddComponent<TextMeshPro>();
            TMPFontHelper.ApplyDefaultFont(_warningIconLabel);
            _warningIconLabel.text = "!";
            _warningIconLabel.fontSize = _warningIconFontSize;
            _warningIconLabel.color = new Color(1f, 0.2f, 0.2f, 1f);
            _warningIconLabel.fontStyle = FontStyles.Bold;
            _warningIconLabel.alignment = TextAlignmentOptions.Center;
            _warningIconLabel.rectTransform.sizeDelta = new Vector2(2f, 2f);
            _warningIconLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _warningIconLabel.enableAutoSizing = false;

            _warningIconGo.SetActive(false);
        }

        private void Update()
        {
            if (_isDead || _machine == null) return;

            switch (_state)
            {
                case BossState.Walking:       UpdateWalking();       break;
                case BossState.Idle:          UpdateIdle();          break;
                case BossState.Jumping:       UpdateJump();          break;
                case BossState.Attacking:     UpdateAttacking();     break;
                case BossState.Telegraphing:  UpdateTelegraphing();  break;
                case BossState.Flinching:     UpdateFlinching();     break;
                case BossState.Pouncing:      UpdatePouncing();      break;
            }

            UpdateContactDamage();
        }

        private void LateUpdate()
        {
            if (_warningIconGo != null && _warningIconGo.activeSelf)
            {
                _warningIconGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        // ─── 상태 진입 ────────────────────────────────────────────

        private void EnterWalking()
        {
            var d = Data;
            if (d.WalkDuration <= 0f || d.WalkRadius <= 0f)
            {
                EnterIdle();
                return;
            }

            _state = BossState.Walking;
            _stateTimer = d.WalkDuration;
            PickNewWalkTarget();
            if (_animator != null) _animator.SetFloat(_speedParam, 1f);
        }

        private void EnterIdle()
        {
            _state = BossState.Idle;
            _stateTimer = Data.IdleDuration;
            if (_animator != null) _animator.SetFloat(_speedParam, 0f);

            if (_hadFirstIdle) _cyclesSinceTelegraph++;
            _hadFirstIdle = true;
        }

        private void EnterJumping(bool retreatToFar = false)
        {
            int next = _perchIdx;
            if (retreatToFar)
            {
                next = (_perchIdx + PERCH_COUNT / 2) % PERCH_COUNT;
            }
            else
            {
                while (next == _perchIdx) next = Random.Range(0, PERCH_COUNT);
            }

            var d = Data;
            _jumpFrom = transform.position;
            _perchAnchor = GetPerchPosition(next) + RandomXzInsideRadius(d.PerchJitter);
            _jumpTo = _perchAnchor;
            _perchIdx = next;
            _jumpT = 0f;
            float minD = Mathf.Max(0.1f, d.JumpDurationMin);
            float maxD = Mathf.Max(minD, d.JumpDurationMax);
            _jumpDuration = Random.Range(minD, maxD);

            _state = BossState.Jumping;
            if (_animator != null) _animator.SetFloat(_speedParam, 1f);
        }

        private void EnterAttacking()
        {
            _state = BossState.Attacking;
            _stateTimer = Data.AttackDuration;
            _attackSpawnedThisCycle = false;
            if (_animator != null)
            {
                _animator.SetFloat(_speedParam, 0f);
                _animator.SetTrigger(_attackTrigger);
            }
        }

        // ─── 상태 업데이트 ────────────────────────────────────────

        private void UpdateWalking()
        {
            _stateTimer -= Time.deltaTime;

            Vector3 toTarget = _walkTarget - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (dist < 0.1f)
            {
                PickNewWalkTarget();
            }
            else
            {
                Vector3 dir = toTarget / dist;
                float step = Mathf.Min(dist, Data.WalkSpeed * Time.deltaTime);
                transform.position += dir * step;
            }

            FaceMachine();

            if (_stateTimer <= 0f) EnterIdle();
        }

        private void UpdateIdle()
        {
            FaceMachine();

            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
            {
                if (Data.TelegraphCooldownCycles > 0 && _cyclesSinceTelegraph >= Data.TelegraphCooldownCycles)
                    EnterTelegraphing();
                else
                    EnterJumping();
            }
        }

        private void UpdateJump()
        {
            _jumpT = Mathf.Min(1f, _jumpT + Time.deltaTime / _jumpDuration);

            Vector3 flat = Vector3.Lerp(_jumpFrom, _jumpTo, _jumpT);
            float hop = Mathf.Sin(_jumpT * Mathf.PI) * JUMP_HEIGHT;
            transform.position = new Vector3(flat.x, _jumpFrom.y + hop, flat.z);

            FaceMachine();

            if (_jumpT >= 1f)
            {
                transform.position = _jumpTo;
                EnterAttacking();
            }
        }

        private void UpdateAttacking()
        {
            _stateTimer -= Time.deltaTime;
            FaceMachine();

            float duration = Data.AttackDuration;
            float elapsedFraction = duration > 0f ? 1f - Mathf.Clamp01(_stateTimer / duration) : 1f;
            if (!_attackSpawnedThisCycle && elapsedFraction >= Data.AttackSpawnFraction)
            {
                SpawnChildren();
                _attackSpawnedThisCycle = true;
            }

            if (_stateTimer <= 0f)
            {
                if (!_attackSpawnedThisCycle)
                {
                    SpawnChildren();
                    _attackSpawnedThisCycle = true;
                }
                EnterWalking();
            }
        }

        // ─── Telegraph / Flinch / Pounce ──────────────────────────

        private void EnterTelegraphing()
        {
            _state = BossState.Telegraphing;
            _stateTimer = Data.TelegraphDuration;
            _telegraphHitsTaken = 0;
            _telegraphPulseTime = 0f;
            _cyclesSinceTelegraph = 0;

            if (_warningIconGo != null) _warningIconGo.SetActive(true);
            if (_animator != null) _animator.SetFloat(_speedParam, 0f);
            FaceMachine();
        }

        private void UpdateTelegraphing()
        {
            var d = Data;
            _stateTimer -= Time.deltaTime;
            _telegraphPulseTime += Time.deltaTime;
            FaceMachine();

            float wave = (Mathf.Sin(_telegraphPulseTime * d.TelegraphPulseFreq * Mathf.PI * 2f) + 1f) * 0.5f;
            transform.localScale = _baseScale * (1f + wave * d.TelegraphScalePulse);

            if (_telegraphHitsTaken >= d.InterruptHitsRequired)
            {
                EnterFlinching();
                return;
            }

            if (_stateTimer <= 0f)
            {
                EnterPouncing();
            }
        }

        private void EnterFlinching()
        {
            _state = BossState.Flinching;
            _stateTimer = Data.FlinchDuration;
            transform.localScale = _baseScale;
            if (_warningIconGo != null) _warningIconGo.SetActive(false);
            if (_animator != null) _animator.SetFloat(_speedParam, 0f);
        }

        private void UpdateFlinching()
        {
            _stateTimer -= Time.deltaTime;
            FaceMachine();

            float shake = Mathf.Sin(Time.time * 30f) * 0.04f;
            var p = transform.position;
            p.x += shake * Time.deltaTime * 10f;
            transform.position = p;

            if (_stateTimer <= 0f)
            {
                EnterJumping(retreatToFar: true);
            }
        }

        private void EnterPouncing()
        {
            transform.localScale = _baseScale;
            if (_warningIconGo != null) _warningIconGo.SetActive(false);

            int next = _perchIdx;
            while (next == _perchIdx) next = Random.Range(0, PERCH_COUNT);

            var d = Data;
            _jumpFrom = transform.position;
            _perchAnchor = GetPerchPosition(next, d.PounceRadiusMultiplier);
            _jumpTo = _perchAnchor;
            _perchIdx = next;
            _jumpT = 0f;
            float minD = Mathf.Max(0.1f, d.JumpDurationMin);
            float maxD = Mathf.Max(minD, d.JumpDurationMax);
            _jumpDuration = Random.Range(minD, maxD);

            _state = BossState.Pouncing;
            if (_animator != null) _animator.SetFloat(_speedParam, 1f);
        }

        private void UpdatePouncing()
        {
            _jumpT = Mathf.Min(1f, _jumpT + Time.deltaTime / _jumpDuration);

            Vector3 flat = Vector3.Lerp(_jumpFrom, _jumpTo, _jumpT);
            float hop = Mathf.Sin(_jumpT * Mathf.PI) * JUMP_HEIGHT;
            transform.position = new Vector3(flat.x, _jumpFrom.y + hop, flat.z);

            FaceMachine();

            if (_jumpT >= 1f)
            {
                transform.position = _jumpTo;
                ApplyPounceImpact();
                EnterAttacking();
            }
        }

        private void ApplyPounceImpact()
        {
            float dmg = Data.PounceImpactDamage;
            if (dmg <= 0f) return;
            if (_machine == null) return;
            if (_machine.TryGetComponent<IDamageable>(out var dmgTarget))
                dmgTarget.TakeDamage(dmg);
        }

        // ─── walk 헬퍼 ────────────────────────────────────────────

        private void PickNewWalkTarget()
        {
            _walkTarget = _perchAnchor + RandomXzInsideRadius(Data.WalkRadius);
        }

        private static Vector3 RandomXzInsideRadius(float r)
        {
            if (r <= 0f) return Vector3.zero;
            Vector2 p = Random.insideUnitCircle * r;
            return new Vector3(p.x, 0f, p.y);
        }

        private void UpdateContactDamage()
        {
            if (IsJumping) return;

            Vector3 toMachine = _machine.position - transform.position;
            toMachine.y = 0f;
            if (toMachine.magnitude < Data.ContactRange &&
                _machine.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(Data.ContactDamagePerSecond * Time.deltaTime);
            }
        }

        public void TakeDamage(float damage)
        {
            if (_isDead) return;
            _hp -= damage;

            if (_state == BossState.Telegraphing)
                _telegraphHitsTaken++;

            if (_hpBar != null) _hpBar.SetHealth(_hp, _maxHp);

            if (_hp <= 0f)
            {
                _hp = 0f;
                _isDead = true;
                if (_warningIconGo != null) _warningIconGo.SetActive(false);
                if (_baseScale != Vector3.zero) transform.localScale = _baseScale;

                if (_animator != null) _animator.SetTrigger(_deathTrigger);
                PlayDeathVfx();
                GameEvents.OnBossKilled?.Invoke();
                if (_hpBar != null) Destroy(_hpBar.gameObject);
                Destroy(gameObject, 4f);
            }
            else
            {
                PlayHitVfx();
            }
        }

        private void PlayHitVfx()
        {
            if (_hitVfxPrefab == null) return;
            if (Time.time < _hitVfxNextAllowedTime) return;
            _hitVfxNextAllowedTime = Time.time + _hitVfxMinInterval;

            SpawnVfx(_hitVfxPrefab);
        }

        private void PlayDeathVfx()
        {
            if (_deathVfxPrefab == null) return;
            SpawnVfx(_deathVfxPrefab);
        }

        // VFX 스폰 — _fxSocket 위치(또는 거미 중심)에 띄움. SimpleBug 동일 패턴.
        // VFX 본체는 풀(_poolRoot)의 자식으로 두어 거미 회전과 분리 (world 고정).
        private void SpawnVfx(GameObject prefab)
        {
            Vector3 pos = _fxSocket != null ? _fxSocket.position : transform.position;

            GameObject vfx = null;
            if (prefab.GetComponent<ParticleSystem>() != null)
            {
                vfx = VfxPool.Get(prefab, pos, Quaternion.identity);
                if (vfx == null) return;
                vfx.transform.localScale = prefab.transform.localScale * _vfxScale;
            }
            else
            {
                vfx = Instantiate(prefab, pos, Quaternion.identity);
                vfx.transform.localScale = prefab.transform.localScale * _vfxScale;
                Destroy(vfx, 5f);
            }
        }

        public void Heal(float amount) { }

        private void SpawnChildren()
        {
            var d = Data;
            if (_childBugData == null || _childBugData.Prefab == null) return;
            if (d.ChildCountPerLanding <= 0) return;

            var spawner = FindAnyObjectByType<SimpleBugSpawner>();
            if (spawner == null) return;

            for (int i = 0; i < d.ChildCountPerLanding; i++)
            {
                Vector2 jitter2D = Random.insideUnitCircle * d.ChildSpawnJitter;
                Vector3 spawnPos = new Vector3(
                    transform.position.x + jitter2D.x,
                    0f,
                    transform.position.z + jitter2D.y);
                spawner.SpawnAt(_childBugData, spawnPos);
            }
        }

        private void FaceMachine()
        {
            if (_machine == null) return;
            Vector3 to = _machine.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        private Vector3 GetPerchPosition(int idx, float radiusMul = 1f)
        {
            float angle = (60f + idx * 60f) * Mathf.Deg2Rad;
            Vector3 center = _machine != null ? _machine.position : Vector3.zero;
            float r = PERCH_RADIUS * radiusMul;
            return new Vector3(
                center.x + Mathf.Cos(angle) * r,
                0f,
                center.z + Mathf.Sin(angle) * r
            );
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = _machine != null ? _machine.position : transform.position;
            // jitter/walk 반경은 Data 가 있을 때만 표시 — 에디터 시점에 SO null 가능.
            float jitter = _data != null ? _data.PerchJitter : 0f;
            float walkR  = _data != null ? _data.WalkRadius : 0f;
            float walkD  = _data != null ? _data.WalkDuration : 0f;

            for (int i = 0; i < PERCH_COUNT; i++)
            {
                float angle = (60f + i * 60f) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(
                    center.x + Mathf.Cos(angle) * PERCH_RADIUS,
                    0f,
                    center.z + Mathf.Sin(angle) * PERCH_RADIUS);
                Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.6f);
                Gizmos.DrawWireSphere(p, 0.5f);
                if (jitter > 0f)
                {
                    Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.18f);
                    Gizmos.DrawWireSphere(p, jitter);
                }
                if (walkR > 0f && walkD > 0f)
                {
                    Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.25f);
                    Gizmos.DrawWireSphere(p, walkR);
                }
            }

            Vector3 vfxPos = _fxSocket != null ? _fxSocket.position : transform.position;
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(vfxPos, _vfxScale);
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
            Gizmos.DrawLine(transform.position, vfxPos);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(vfxPos, 0.15f);
        }
#endif
    }
}

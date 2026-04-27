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
    /// 행동 사이클: Jumping(0.67s) → Walking(_walkDuration) → Idle(_idleDuration) → Jumping ...
    /// - perch 도착 위치에 _perchJitter 만큼 랜덤 오프셋 (매번 같은 자리 X)
    /// - Walking: perch 중심 _walkRadius 반경 안에서 랜덤 목표 어슬렁
    /// - Idle: 머신 응시·정지
    /// </summary>
    public class SpiderBoss : MonoBehaviour, IDamageable
    {
        // ─── v2 상수 (V2-prototype.html line 715~717) ───
        private const int PERCH_COUNT = 6;
        private const float PERCH_RADIUS = 15f;          // v2 BOSS_PERCH_R=200 (px) → Unity 8m
        private const float JUMP_HEIGHT = 3.6f;         // v2 sin*90 (px) → Unity 3.6m

        // 상태 머신 — 점프 → 공격(소환) → 짧은 walk → 정지 → 다음 점프.
        // Telegraphing/Flinching/Pouncing: 인터럽트 가능 압박 패턴 (Idle 종료 시 N사이클마다 발동).
        private enum BossState { Walking, Idle, Jumping, Attacking, Telegraphing, Flinching, Pouncing }

        [Header("Stats")]
        [SerializeField] private float _maxHp = 500f;
        [SerializeField] private float _contactDamagePerSecond = 30f;
        [SerializeField] private float _contactRange = 1.2f;

        [Header("Movement (자연스러운 행동)")]
        [Tooltip("perch 도달 후 주위를 어슬렁거리는 시간(초). 0 이면 바로 정지.")]
        [Min(0f)]
        [SerializeField] private float _walkDuration = 1.5f;
        [Tooltip("perch 중심에서 walk 가능한 최대 반경. 너무 크면 perch 패턴 흐려짐.")]
        [Min(0f)]
        [SerializeField] private float _walkRadius = 2.5f;
        [Tooltip("walk 속도 (유닛/초). 빠르면 부산해 보임.")]
        [Min(0f)]
        [SerializeField] private float _walkSpeed = 2.0f;
        [Tooltip("walk 끝난 후 다음 점프까지 대기 시간(초). v2 = 5 frame ≈ 5초.")]
        [Min(0f)]
        [SerializeField] private float _idleDuration = 2.0f;
        [Tooltip("perch 도착 위치에 추가되는 랜덤 jitter 반경. 0 이면 정확히 perch 자리.")]
        [Min(0f)]
        [SerializeField] private float _perchJitter = 1.5f;
        [Tooltip("점프 한 번의 최소 비행 시간(초). 매 점프마다 [Min, Max] 사이 랜덤. v2 기본은 0.667초.")]
        [Min(0.1f)]
        [SerializeField] private float _jumpDurationMin = 0.5f;
        [Tooltip("점프 한 번의 최대 비행 시간(초). Min 보다 작거나 같으면 고정값.")]
        [Min(0.1f)]
        [SerializeField] private float _jumpDurationMax = 1.0f;

        [Header("Attack (착지 후 새끼 소환)")]
        [Tooltip("공격 모션 전체 시간(초). 클립 길이에 맞추기 — 너무 짧으면 모션 잘림. " +
                 "Animator Attack state 의 default speed=1 기준.")]
        [Min(0.1f)]
        [SerializeField] private float _attackDuration = 2.0f;
        [Tooltip("공격 모션 중 어느 시점에 새끼를 소환할지 (0=시작 직후, 0.5=중간, 1=거의 끝). " +
                 "찌르기 임팩트 프레임에 맞추는 게 game-feel 가장 좋음.")]
        [Range(0f, 1f)]
        [SerializeField] private float _attackSpawnFraction = 0.5f;

        [Header("Telegraph (인터럽트 가능 압박 패턴)")]
        [Tooltip("정상 사이클 N번 완료마다 텔레그래프 발동 (0=비활성). 너무 잦으면 패턴화됨.")]
        [Min(0)]
        [SerializeField] private int _telegraphCooldownCycles = 2;
        [Tooltip("텔레그래프 지속 시간 — 이 시간 안에 인터럽트 명중 못 채우면 Pounce 발동.")]
        [Min(0.1f)]
        [SerializeField] private float _telegraphDuration = 2f;
        [Tooltip("텔레그래프 인터럽트에 필요한 명중 수. 무기 종류 무관, 단순 hit count.")]
        [Min(1)]
        [SerializeField] private int _interruptHitsRequired = 8;
        [Tooltip("Pounce 시 가까운 perch 반경 비율 (0.5 = 머신에서 절반 거리). 점프 후 머신에 더 가까이 압박.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _pounceRadiusMultiplier = 0.5f;
        [Tooltip("Pounce 착지 시 머신에 가하는 임팩트 데미지 (절대값).")]
        [Min(0f)]
        [SerializeField] private float _pounceImpactDamage = 50f;
        [Tooltip("인터럽트 성공 시 거미가 잠깐 흠칫하는 시간. 끝나면 가장 먼 perch 로 후퇴 점프.")]
        [Min(0f)]
        [SerializeField] private float _flinchDuration = 0.6f;
        [Tooltip("텔레그래프 시 스케일 펄스 진폭 (0.1 = 10% 커졌다 작아짐).")]
        [Min(0f)]
        [SerializeField] private float _telegraphScalePulse = 0.1f;
        [Tooltip("스케일 펄스 주기 (Hz).")]
        [Min(0.1f)]
        [SerializeField] private float _telegraphPulseFreq = 4f;
        [Tooltip("! 아이콘 머리 위 오프셋 (Y=높이, Z=화면 위쪽).")]
        [SerializeField] private Vector3 _warningIconOffset = new Vector3(0f, 5.5f, 0f);
        [Tooltip("! 아이콘 폰트 크기.")]
        [SerializeField] private float _warningIconFontSize = 12f;

        [Header("References")]
        [SerializeField] private Transform _machine;

        [Header("Boss Children (착지 시 소환)")]
        [Tooltip("새끼 거미 SimpleBugData (Swift 권장)")]
        [SerializeField] private SimpleBugData _childBugData;
        [SerializeField] private int _childCountPerLanding = 3;
        [SerializeField] private float _childSpawnJitter = 1.5f;

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


        [Header("Animator Triggers")]
        [SerializeField] private string _attackTrigger = "TriggerAttack";
        [SerializeField] private string _deathTrigger = "TriggerDeath";
        [SerializeField] private string _speedParam = "Speed";

        private Animator _animator;
        private float _hp;
        private bool _isDead;
        private Hp3DBar _hpBar;
        private float _hitVfxNextAllowedTime;

        // 행동 상태
        private BossState _state;
        private float _stateTimer;             // 현재 상태 남은 시간 (Walk/Idle 용)
        private int _perchIdx;
        private Vector3 _perchAnchor;          // 현재 perch 중심 (jitter 후 실제 도착 자리)
        private Vector3 _walkTarget;           // walk 목표 좌표

        // 점프 보간 상태
        private float _jumpT;
        private float _jumpDuration;            // 이번 점프의 비행 시간 (Min~Max 랜덤)
        private Vector3 _jumpFrom;
        private Vector3 _jumpTo;

        // 공격 상태 — 모션 중간에 1회 소환하기 위한 플래그·타이머
        private bool _attackSpawnedThisCycle;

        // 텔레그래프 사이클 트래킹·시각 효과
        private int _cyclesSinceTelegraph;     // EnterIdle 시 증가, EnterTelegraphing 시 0 리셋
        private bool _hadFirstIdle;            // Initialize 첫 Idle 진입은 카운트 X
        private int _telegraphHitsTaken;       // Telegraphing 상태에서 받은 hit 수
        private float _telegraphPulseTime;     // 스케일 펄스 sin 누적 시간
        private Vector3 _baseScale;            // 펄스 베이스 (Initialize 시점 localScale)
        private GameObject _warningIconGo;     // ! 아이콘 root
        private TextMeshPro _warningIconLabel;

        // 외부에서 점프 중인지 알아야 하는 곳(접촉 피해 등) 호환성용
        // Pouncing 도 공중이라 접촉 피해 면제 대상.
        private bool IsJumping => _state == BossState.Jumping || _state == BossState.Pouncing;

        public float CurrentHealth => _hp;
        public float MaxHealth => _maxHp;
        public bool IsDead => _isDead;

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
            _hp = _maxHp;
            _isDead = false;
            _baseScale = transform.localScale;

            // 가장 가까운 perch 인덱스부터 시작 — 등장 직후엔 idle 부터 (점프 즉시 X)
            _perchIdx = Random.Range(0, PERCH_COUNT);
            _perchAnchor = GetPerchPosition(_perchIdx);
            transform.position = _perchAnchor;
            EnterIdle();

            FaceMachine();   // 등장 즉시 머신 바라봄

            CreateHpBar();
            CreateWarningIcon();

            GameEvents.OnBossSpawned?.Invoke(transform.position);
        }

        private void CreateHpBar()
        {
            if (!_showHpBar) return;
            if (_hpBar != null) return;

            _hpBar = Hp3DBar.Create(transform, _hpBarOffset, _hpBarSize, _hpBarShowLabel);
            // 보스 테마색 — 보라(가득) → 빨강(낮음)
            _hpBar.SetColors(
                new Color(0.78f, 0.40f, 1f, 1f),     // #c060ff
                new Color(1f, 0.13f, 0.27f, 1f),     // #ff2244
                lowThreshold: 0.25f);
            _hpBar.SetHealth(_hp, _maxHp);
        }

        // ! 경고 아이콘 — Telegraphing 상태에서만 표시.
        // 거미 본체의 child 로 두되 LateUpdate 에서 회전을 강제 보정해 부모 회전 무시.
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

        // ! 아이콘은 거미 본체의 회전을 따라가면 글자가 비틀어짐 — 항상 카메라(-Y) 방향 정면 고정.
        // 탑뷰 카메라 기준 Euler(90, 0, 0) 가 XZ 바닥에 누워서 +Y 위로 향함 → 카메라가 정면으로 봄.
        private void LateUpdate()
        {
            if (_warningIconGo != null && _warningIconGo.activeSelf)
            {
                _warningIconGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        // ─── 상태 진입 ────────────────────────────────────────────

        // perch 도착 직후 — 짧게 주변 어슬렁 후 Idle 로 전환.
        // _walkDuration 0 이면 즉시 Idle.
        private void EnterWalking()
        {
            if (_walkDuration <= 0f || _walkRadius <= 0f)
            {
                EnterIdle();
                return;
            }

            _state = BossState.Walking;
            _stateTimer = _walkDuration;
            PickNewWalkTarget();
            if (_animator != null) _animator.SetFloat(_speedParam, 1f);
        }

        private void EnterIdle()
        {
            _state = BossState.Idle;
            _stateTimer = _idleDuration;
            if (_animator != null) _animator.SetFloat(_speedParam, 0f);

            // 첫 Idle (Initialize 호출) 은 사이클 카운트 X — 그 이후 사이클이 끝났을 때만 카운트.
            if (_hadFirstIdle) _cyclesSinceTelegraph++;
            _hadFirstIdle = true;
        }

        // 일반 점프(retreatToFar=false): 현재 perch 와 다른 랜덤 perch.
        // 후퇴 점프(retreatToFar=true): 현재 perch 의 정반대(가장 먼) perch — Flinch 후 호출.
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

            _jumpFrom = transform.position;
            // perch 위치에 약간의 jitter 적용 — 매번 같은 자리 안 오게
            _perchAnchor = GetPerchPosition(next) + RandomXzInsideRadius(_perchJitter);
            _jumpTo = _perchAnchor;
            _perchIdx = next;
            _jumpT = 0f;
            // 매 점프마다 비행 시간 랜덤 — 단조로운 패턴 깨기
            float minD = Mathf.Max(0.1f, _jumpDurationMin);
            float maxD = Mathf.Max(minD, _jumpDurationMax);
            _jumpDuration = Random.Range(minD, maxD);

            _state = BossState.Jumping;
            if (_animator != null) _animator.SetFloat(_speedParam, 1f);
        }

        // 착지 직후 진입 — 공격 모션 재생 + 모션 중간에 새끼 소환.
        // 공격 끝나야 비로소 walk 로 전환되어 다음 점프 사이클 시작.
        private void EnterAttacking()
        {
            _state = BossState.Attacking;
            _stateTimer = _attackDuration;
            _attackSpawnedThisCycle = false;
            if (_animator != null)
            {
                _animator.SetFloat(_speedParam, 0f);  // Walk 트랜지션 막기
                _animator.SetTrigger(_attackTrigger);
            }
        }

        // ─── 상태 업데이트 ────────────────────────────────────────

        private void UpdateWalking()
        {
            _stateTimer -= Time.deltaTime;

            // 목표 방향으로 이동 (XZ 평면)
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
                float step = Mathf.Min(dist, _walkSpeed * Time.deltaTime);
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
                // N사이클마다 텔레그래프 분기, 그 외엔 정상 점프.
                if (_telegraphCooldownCycles > 0 && _cyclesSinceTelegraph >= _telegraphCooldownCycles)
                    EnterTelegraphing();
                else
                    EnterJumping();
            }
        }

        private void UpdateJump()
        {
            _jumpT = Mathf.Min(1f, _jumpT + Time.deltaTime / _jumpDuration);

            // XZ 평면 보간
            Vector3 flat = Vector3.Lerp(_jumpFrom, _jumpTo, _jumpT);
            // Y 포물선 (탑다운에서 Y는 높이축, sin으로 솟구침)
            float hop = Mathf.Sin(_jumpT * Mathf.PI) * JUMP_HEIGHT;
            transform.position = new Vector3(flat.x, _jumpFrom.y + hop, flat.z);

            FaceMachine();

            if (_jumpT >= 1f)
            {
                transform.position = _jumpTo;
                EnterAttacking();   // 공격 모션 → (중간에 소환) → walk 로 전환
            }
        }

        // 공격 모션 진행 중 — 정해진 시점(_attackSpawnFraction)에 새끼 1회 소환.
        // 모션 끝나면 EnterWalking 으로 walk 전환 (Speed=1 → Animator 가 Attack→Walk 트랜지션).
        private void UpdateAttacking()
        {
            _stateTimer -= Time.deltaTime;
            FaceMachine();

            // 경과 비율 — 0 (방금 시작) ~ 1 (다 끝남)
            float elapsedFraction = 1f - Mathf.Clamp01(_stateTimer / _attackDuration);
            if (!_attackSpawnedThisCycle && elapsedFraction >= _attackSpawnFraction)
            {
                SpawnChildren();
                _attackSpawnedThisCycle = true;
            }

            if (_stateTimer <= 0f)
            {
                // 미처 소환 안 됐으면 안전망으로 1회 호출 (fraction 1.0 케이스 등)
                if (!_attackSpawnedThisCycle)
                {
                    SpawnChildren();
                    _attackSpawnedThisCycle = true;
                }
                EnterWalking();
            }
        }

        // ─── Telegraph / Flinch / Pounce (인터럽트 가능 압박 패턴) ─────────

        // perch 에서 ! 표시·스케일 펄스로 위협 신호. 플레이어가 N발 명중하면 Flinch (성공),
        // 시간 다 지나도록 못 막으면 Pounce (실패) — 가까운 perch 로 점프 + 머신에 임팩트 데미지.
        private void EnterTelegraphing()
        {
            _state = BossState.Telegraphing;
            _stateTimer = _telegraphDuration;
            _telegraphHitsTaken = 0;
            _telegraphPulseTime = 0f;
            _cyclesSinceTelegraph = 0;   // 다음 트리거까지 카운트 리셋

            if (_warningIconGo != null) _warningIconGo.SetActive(true);
            if (_animator != null) _animator.SetFloat(_speedParam, 0f);   // 정지 자세
            FaceMachine();
        }

        private void UpdateTelegraphing()
        {
            _stateTimer -= Time.deltaTime;
            _telegraphPulseTime += Time.deltaTime;
            FaceMachine();

            // 스케일 펄스 — sin 으로 base ↔ base*(1+pulse) 진동.
            float wave = (Mathf.Sin(_telegraphPulseTime * _telegraphPulseFreq * Mathf.PI * 2f) + 1f) * 0.5f;
            transform.localScale = _baseScale * (1f + wave * _telegraphScalePulse);

            // 인터럽트 성공 — 플레이어가 N발 명중
            if (_telegraphHitsTaken >= _interruptHitsRequired)
            {
                EnterFlinching();
                return;
            }

            // 인터럽트 실패 — 시간 만료, Pounce 발동
            if (_stateTimer <= 0f)
            {
                EnterPouncing();
            }
        }

        // 인터럽트 성공 직후 — 짧게 흠칫(스턴) → 가장 먼 perch 로 후퇴 점프.
        private void EnterFlinching()
        {
            _state = BossState.Flinching;
            _stateTimer = _flinchDuration;
            transform.localScale = _baseScale;
            if (_warningIconGo != null) _warningIconGo.SetActive(false);
            if (_animator != null) _animator.SetFloat(_speedParam, 0f);
        }

        private void UpdateFlinching()
        {
            _stateTimer -= Time.deltaTime;
            FaceMachine();

            // 스턴 시각화 — 좌우로 미세 흔들흔들
            float shake = Mathf.Sin(Time.time * 30f) * 0.04f;
            var p = transform.position;
            p.x += shake * Time.deltaTime * 10f;
            transform.position = p;

            if (_stateTimer <= 0f)
            {
                EnterJumping(retreatToFar: true);
            }
        }

        // 인터럽트 실패 — 머신과 가까운 perch(반경 _pounceRadiusMultiplier)로 점프해 압박 + 착지 시 임팩트 데미지.
        private void EnterPouncing()
        {
            transform.localScale = _baseScale;
            if (_warningIconGo != null) _warningIconGo.SetActive(false);

            int next = _perchIdx;
            while (next == _perchIdx) next = Random.Range(0, PERCH_COUNT);

            _jumpFrom = transform.position;
            // 일반 perch 보다 가까운 거리 — jitter 없이 정확히 압박 자리.
            _perchAnchor = GetPerchPosition(next, _pounceRadiusMultiplier);
            _jumpTo = _perchAnchor;
            _perchIdx = next;
            _jumpT = 0f;
            float minD = Mathf.Max(0.1f, _jumpDurationMin);
            float maxD = Mathf.Max(minD, _jumpDurationMax);
            _jumpDuration = Random.Range(minD, maxD);

            _state = BossState.Pouncing;
            if (_animator != null) _animator.SetFloat(_speedParam, 1f);
        }

        // UpdateJump 와 본문은 같지만 착지 시 머신에 임팩트 데미지를 가한 뒤 정상 Attack 사이클로 이행.
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
                EnterAttacking();   // 새끼 소환 + Attack 모션 정상 진행
            }
        }

        private void ApplyPounceImpact()
        {
            if (_pounceImpactDamage <= 0f) return;
            if (_machine == null) return;
            if (_machine.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(_pounceImpactDamage);
        }

        // ─── walk 헬퍼 ────────────────────────────────────────────

        private void PickNewWalkTarget()
        {
            _walkTarget = _perchAnchor + RandomXzInsideRadius(_walkRadius);
        }

        // XZ 평면 반경 r 안의 랜덤 오프셋 (Y=0).
        private static Vector3 RandomXzInsideRadius(float r)
        {
            if (r <= 0f) return Vector3.zero;
            Vector2 p = Random.insideUnitCircle * r;
            return new Vector3(p.x, 0f, p.y);
        }

        private void UpdateContactDamage()
        {
            // 점프 중엔 공중이라 접촉 피해 없음. Walk/Idle 상태에서만.
            if (IsJumping) return;

            Vector3 toMachine = _machine.position - transform.position;
            toMachine.y = 0f;
            if (toMachine.magnitude < _contactRange &&
                _machine.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(_contactDamagePerSecond * Time.deltaTime);
            }
        }

        public void TakeDamage(float damage)
        {
            if (_isDead) return;
            _hp -= damage;

            // 텔레그래프 인터럽트 — TakeDamage 호출 1회당 1 hit 로 카운트 (무기 종류 무관).
            if (_state == BossState.Telegraphing)
                _telegraphHitsTaken++;

            if (_hpBar != null) _hpBar.SetHealth(_hp, _maxHp);

            if (_hp <= 0f)
            {
                _hp = 0f;
                _isDead = true;
                // 텔레그래프 중 사망 시 ! 아이콘·펄스 즉시 정리
                if (_warningIconGo != null) _warningIconGo.SetActive(false);
                if (_baseScale != Vector3.zero) transform.localScale = _baseScale;

                if (_animator != null) _animator.SetTrigger(_deathTrigger);
                PlayDeathVfx();
                GameEvents.OnBossKilled?.Invoke();
                // HP 바는 보스 본체 사망과 동시에 사라지면 자연스러움
                if (_hpBar != null) Destroy(_hpBar.gameObject);
                // 게임 클리어 처리는 MachineController 또는 GameManager가 OnBossKilled 구독해서 처리
                // 사망 애니메이션 끝까지 보여준 후 Destroy (Death 클립 4초)
                Destroy(gameObject, 4f);
            }
            else
            {
                PlayHitVfx();
            }
        }

        // 피격 VFX — 풀링 + 짧은 인터벌 throttle 로 연사 부하 방지
        private void PlayHitVfx()
        {
            if (_hitVfxPrefab == null) return;
            if (Time.time < _hitVfxNextAllowedTime) return;
            _hitVfxNextAllowedTime = Time.time + _hitVfxMinInterval;

            SpawnVfx(_hitVfxPrefab);
        }

        // 사망 VFX — 1회만, 풀링 (큰 폭발 권장)
        private void PlayDeathVfx()
        {
            if (_deathVfxPrefab == null) return;
            SpawnVfx(_deathVfxPrefab);
        }

        // VFX 스폰 헬퍼 — 풀링 + 보스 크기에 비례한 _vfxScale 적용
        // (SimpleBug.SpawnScaledVfx 패턴 차용)
        // VFX 가 거미를 자식으로 따라가도록 parent=transform 지정 → 거미 점프해도 VFX 분리되지 않음.
        // 단, 풀링 반환 시 parent 가 _poolRoot 로 복귀하므로 파티클 수명만큼만 따라감.
        // VFX 스폰 — _fxSocket 위치(또는 거미 중심)에 띄움. SimpleBug 동일 패턴.
        // VFX 본체는 풀(_poolRoot)의 자식으로 두어 거미 회전과 분리 (world 고정).
        // 거미가 점프해도 spawn 시점 위치에 그대로 남음 — Hit VFX 수명 짧아 자연스러움.
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

        // 거미 보스는 회복 안 함 (인터페이스 호환용)
        public void Heal(float amount) { }

        // 착지 시 새끼 거미 N마리 소환. 씬의 SimpleBugSpawner를 통해 _alive 리스트 등록까지 일관 처리.
        private void SpawnChildren()
        {
            if (_childBugData == null || _childBugData.Prefab == null) return;
            if (_childCountPerLanding <= 0) return;

            var spawner = FindAnyObjectByType<SimpleBugSpawner>();
            if (spawner == null) return;

            for (int i = 0; i < _childCountPerLanding; i++)
            {
                Vector2 jitter2D = Random.insideUnitCircle * _childSpawnJitter;
                Vector3 spawnPos = new Vector3(
                    transform.position.x + jitter2D.x,
                    0f,
                    transform.position.z + jitter2D.y);
                spawner.SpawnAt(_childBugData, spawnPos);
            }
        }

        // 머신을 향해 회전 (XZ 평면, Y는 변경 안 함)
        private void FaceMachine()
        {
            if (_machine == null) return;
            Vector3 to = _machine.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        // radiusMul=1 이면 정상 perch, <1 이면 머신에 더 가까운 압박 자리(Pounce 용).
        private Vector3 GetPerchPosition(int idx, float radiusMul = 1f)
        {
            // v2: 60° 간격 6개 — 시작 각도 60° (offset)
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
            // ─── Perch 6자리 (보라) + jitter 반경 (옅은) ───
            Vector3 center = _machine != null ? _machine.position : transform.position;
            for (int i = 0; i < PERCH_COUNT; i++)
            {
                float angle = (60f + i * 60f) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(
                    center.x + Mathf.Cos(angle) * PERCH_RADIUS,
                    0f,
                    center.z + Mathf.Sin(angle) * PERCH_RADIUS);
                // 정확한 perch 점
                Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.6f);
                Gizmos.DrawWireSphere(p, 0.5f);
                // jitter 반경 (착지 가능 범위)
                if (_perchJitter > 0f)
                {
                    Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.18f);
                    Gizmos.DrawWireSphere(p, _perchJitter);
                }
                // walk 반경 (어슬렁거리는 범위)
                if (_walkRadius > 0f && _walkDuration > 0f)
                {
                    Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.25f);
                    Gizmos.DrawWireSphere(p, _walkRadius);
                }
            }

            // ─── VFX Socket 시각화 ───
            // FxSocket 자식 Transform 위치 (없으면 거미 중심).
            Vector3 vfxPos = _fxSocket != null ? _fxSocket.position : transform.position;
            // 노랑 wire sphere = VFX 추정 반경
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(vfxPos, _vfxScale);
            // 거미 root → VFX 위치 직선
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
            Gizmos.DrawLine(transform.position, vfxPos);
            // 작은 점으로 정확한 위치 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(vfxPos, 0.15f);
        }
#endif
    }
}

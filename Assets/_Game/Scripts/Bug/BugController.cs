using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.Machine;
using DrillCorp.Bug.Behaviors;
using DrillCorp.Bug.Behaviors.Data;
using DrillCorp.Bug.Behaviors.Movement;
using DrillCorp.Bug.Behaviors.Attack;
using DrillCorp.Bug.Behaviors.Passive;
using DrillCorp.Bug.Behaviors.Skill;
using DrillCorp.Bug.Behaviors.Trigger;
using DrillCorp.Bug.Pool;
using DrillCorp.VFX;
using DrillCorp.UI.Minimap;
using DrillCorp.Audio;

namespace DrillCorp.Bug
{
    /// <summary>
    /// 벌레 컨트롤러 - 행동 조합 기반
    /// BugData(스탯) + BugBehaviorData(행동)을 조합하여 동작
    /// </summary>
    public class BugController : MonoBehaviour, IDamageable
    {
        [Header("Data")]
        [SerializeField] private BugData _bugData;
        [SerializeField] private BugBehaviorData _behaviorData;

        [Header("VFX")]
        [SerializeField] private Transform _fxSocket;
        [SerializeField] private GameObject _hitVfxPrefab;     // 비어있으면 SimpleVFX.PlayBugHit 폴백
        [SerializeField] private GameObject _deathVfxPrefab;
        [SerializeField] private float _hitFlashDuration = 0.1f;
        [Tooltip("VFX 크기 = 프리펩 authored × 벌레 스케일 × 이 값. 1=벌레와 동일, 2=두 배")]
        [SerializeField] private float _vfxScaleMultiplier = 2f;

        // === Stats ===
        private int _bugId;
        private float _maxHealth;
        private float _currentHealth;
        private float _moveSpeed;
        private float _attackDamage;
        private float _attackCooldown;

        // === State ===
        private Transform _target;
        private float _aliveTime;
        private int _hitCount;
        private int _attackCount;
        private bool _justAttacked;
        private float _justAttackedTimer;
        private bool _allyJustDied;
        private bool _isDead;
        private bool _isInvulnerable;

        // === Formation Control ===
        private bool _movementExternallyControlled;

        // === Buff ===
        private Dictionary<object, BuffInfo> _activeBuffs = new Dictionary<object, BuffInfo>();
        private float _buffedDamageMultiplier = 1f;
        private float _buffedSpeedMultiplier = 1f;

        // === Behaviors ===
        private IMovementBehavior _currentMovement;
        private IMovementBehavior _defaultMovement;
        private List<ConditionalBehavior<IMovementBehavior>> _conditionalMovements = new List<ConditionalBehavior<IMovementBehavior>>();

        private IAttackBehavior _currentAttack;
        private IAttackBehavior _defaultAttack;
        private List<ConditionalBehavior<IAttackBehavior>> _conditionalAttacks = new List<ConditionalBehavior<IAttackBehavior>>();

        private List<ISkillBehavior> _skills = new List<ISkillBehavior>();
        private List<IPassiveBehavior> _passives = new List<IPassiveBehavior>();
        private List<ITriggerBehavior> _triggers = new List<ITriggerBehavior>();

        // === Visual ===
        private BugHpBar _hpBar;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private bool _isFlashing;

        // === Properties ===
        public int BugId => _bugId;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float CurrentHp => _currentHealth; // HealAllySkill 용 alias
        public float MaxHp => _maxHealth; // HealAllySkill 용 alias
        public float HealthPercent => _maxHealth > 0 ? (_currentHealth / _maxHealth) * 100f : 0f;
        public bool IsDead => _isDead;
        public float MoveSpeed => _moveSpeed * _buffedSpeedMultiplier;
        public float AttackDamage => _attackDamage * _buffedDamageMultiplier;
        public float AttackCooldown => _attackCooldown;
        /// <summary>
        /// 현재 공격 사거리 (Attack SO에서 설정)
        /// </summary>
        public float AttackRange => _currentAttack?.AttackRange ?? 0f;
        public float AliveTime => _aliveTime;
        public int HitCount => _hitCount;
        public int AttackCount => _attackCount;
        public bool JustAttacked => _justAttacked;
        public bool AllyJustDied => _allyJustDied;
        public Transform Target => _target;
        public BugData BugData => _bugData;
        public IMovementBehavior CurrentMovement => _currentMovement;
        public IAttackBehavior CurrentAttack => _currentAttack;
        public IReadOnlyList<IPassiveBehavior> Passives => _passives;
        public bool IsInvulnerable => _isInvulnerable;
        public bool MovementExternallyControlled => _movementExternallyControlled;

        /// <summary>
        /// 외부 시스템(Formation 등)이 이동을 제어하는지 설정
        /// true: BugController는 자체 Movement 비활성, 외부가 Transform 제어
        /// false: 기본 개별 Movement 동작
        /// </summary>
        public void SetMovementExternallyControlled(bool controlled)
        {
            _movementExternallyControlled = controlled;
        }

        #region Unity Lifecycle

        private void Awake()
        {
            CacheRenderers();
            FindFxSocket();
            EnsureCollider();
            SetBugLayer();
        }

        private void EnsureCollider()
        {
            if (GetComponent<Collider>() == null)
            {
                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = 1f;
                capsule.radius = 0.3f;
                capsule.center = new Vector3(0f, 0.5f, 0f);
            }
        }

        private void SetBugLayer()
        {
            int bugLayer = LayerMask.NameToLayer("Bug");
            if (bugLayer != -1)
            {
                gameObject.layer = bugLayer;
            }
        }

        private void Start()
        {
            ApplyBugData();
            InitializeBehaviors();
            FindTarget();
            CreateHpBar();
        }

        private MinimapIcon _minimapIcon;
        private void OnEnable()
        {
            if (_minimapIcon == null)
                _minimapIcon = MinimapIcon.Create(transform, new Color(1f, 0.3f, 0.3f), 1f, MinimapIcon.IconShape.Circle);
            else
                _minimapIcon.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (_minimapIcon != null)
                _minimapIcon.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_minimapIcon != null)
                Destroy(_minimapIcon.gameObject);
        }

        private void Update()
        {
            if (_isDead) return;

            float deltaTime = Time.deltaTime;
            _aliveTime += deltaTime;

            // JustAttacked 타이머
            if (_justAttacked)
            {
                _justAttackedTimer -= deltaTime;
                if (_justAttackedTimer <= 0f)
                    _justAttacked = false;
            }

            // 패시브 업데이트 (Burrow 상태 체크를 위해 먼저 실행)
            foreach (var passive in _passives)
            {
                passive.UpdatePassive(deltaTime);
            }

            // 버로우 상태 체크
            var burrow = GetBurrowPassive();
            bool isBurrowed = burrow != null && !burrow.CanBurrow; // Idle이 아니면 버로우 중

            // 버로우 중이 아닐 때만 행동
            if (!isBurrowed)
            {
                // 조건부 행동 체크 및 전환
                UpdateConditionalBehaviors();

                // 이동 (Formation이 제어 중이면 스킵)
                if (!_movementExternallyControlled)
                {
                    _currentMovement?.UpdateMovement(_target);
                }

                // 기본 공격
                if (_currentAttack != null && _target != null)
                {
                    // Cleave 범위 표시 업데이트
                    if (_currentAttack is CleaveAttack cleaveAttack)
                    {
                        cleaveAttack.UpdateRangeIndicator(_target);
                    }

                    // Beam 공격 업데이트 (지속 데미지)
                    if (_currentAttack is BeamAttack beamAttack)
                    {
                        beamAttack.UpdateBeam(deltaTime);
                    }

                    float distance = GetDistanceTo(_target);
                    if (distance <= _currentAttack.AttackRange)
                    {
                        // Beam이 활성 중이면 새 공격 시도 안 함
                        bool canAttack = !((_currentAttack is BeamAttack beam) && beam.IsBeamActive);

                        if (canAttack && _currentAttack.TryAttack(_target))
                        {
                            SetJustAttacked();
                        }
                    }
                }

                // 스킬 업데이트
                foreach (var skill in _skills)
                {
                    skill.UpdateCooldown(deltaTime);

                    // Nova 범위 표시 업데이트
                    if (skill is NovaSkill novaSkill)
                    {
                        novaSkill.UpdateRangeIndicator();
                    }

                    // BuffAlly Aura 업데이트
                    if (skill is BuffAllySkill buffAllySkill)
                    {
                        buffAllySkill.UpdateAura();
                    }

                    // HealAlly Aura 업데이트
                    if (skill is HealAllySkill healAllySkill)
                    {
                        healAllySkill.UpdateHealAura(deltaTime);
                    }

                    if (skill.IsReady && _target != null)
                    {
                        skill.TryUse(_target);
                    }
                }
            }

            // 트리거 체크 (버로우 중에도 체크 - PanicBurrow 등)
            foreach (var trigger in _triggers)
            {
                if (!trigger.TriggerOnDeath)
                {
                    trigger.CheckAndTrigger();
                }
            }

            // AllyJustDied 리셋
            _allyJustDied = false;
        }

        #endregion

        #region Initialization

        private void ApplyBugData()
        {
            if (_bugData != null)
            {
                _bugId = _bugData.BugId;
                _maxHealth = _bugData.MaxHealth;
                _moveSpeed = _bugData.MoveSpeed;
                _attackDamage = _bugData.AttackDamage;
                _attackCooldown = _bugData.AttackCooldown;

                if (_bugData.Scale != 1f)
                {
                    transform.localScale = Vector3.one * _bugData.Scale;
                }
            }

            _currentHealth = _maxHealth;
        }

        private void InitializeBehaviors()
        {
            if (_behaviorData == null)
            {
                // 기본 행동 설정 (BehaviorData 없을 때)
                SetupDefaultBehaviors();
                return;
            }

            if (_behaviorData.UseRuntimeData)
            {
                // 런타임 데이터 사용 (Google Sheets Import)
                InitializeFromRuntimeData(_behaviorData.RuntimeData);
            }
            else
            {
                // ScriptableObject 참조 사용
                InitializeFromScriptableObjects();
            }
        }

        private void SetupDefaultBehaviors()
        {
            // 기본: Linear 이동 + Melee 공격
            _defaultMovement = new LinearMovement();
            _defaultMovement.Initialize(this);
            _currentMovement = _defaultMovement;

            _defaultAttack = new MeleeAttack();
            _defaultAttack.Initialize(this);
            _currentAttack = _defaultAttack;
        }

        private void InitializeFromScriptableObjects()
        {
            // Movement
            if (_behaviorData.DefaultMovement != null)
            {
                var movData = _behaviorData.DefaultMovement;
                _defaultMovement = MovementBehaviorBase.Create(
                    movData.Type, movData.Param1, movData.Param2, movData.EffectPrefab,
                    movData.IdleType, movData.IdleParam
                );
                _defaultMovement?.Initialize(this);
            }
            else
            {
                _defaultMovement = new LinearMovement();
                _defaultMovement.Initialize(this);
            }
            _currentMovement = _defaultMovement;

            // Conditional Movements
            foreach (var condMov in _behaviorData.ConditionalMovements)
            {
                if (condMov.Behavior == null) continue;

                var behavior = MovementBehaviorBase.Create(
                    condMov.Behavior.Type,
                    condMov.Behavior.Param1,
                    condMov.Behavior.Param2,
                    condMov.Behavior.EffectPrefab,
                    condMov.Behavior.IdleType,
                    condMov.Behavior.IdleParam
                );
                behavior?.Initialize(this);

                _conditionalMovements.Add(new ConditionalBehavior<IMovementBehavior>
                {
                    Condition = BehaviorCondition.Parse(condMov.Condition),
                    Behavior = behavior,
                    Duration = condMov.Duration
                });
            }

            // Attack
            if (_behaviorData.DefaultAttack != null)
            {
                var atkData = _behaviorData.DefaultAttack;
                _defaultAttack = AttackBehaviorBase.Create(
                    atkData.Type,
                    atkData.Param1,
                    atkData.Param2,
                    atkData.ProjectilePrefab,
                    atkData.HitVfxPrefab,
                    atkData.Range
                );
                _defaultAttack?.Initialize(this);
            }
            else
            {
                _defaultAttack = new MeleeAttack();
                _defaultAttack.Initialize(this);
            }
            _currentAttack = _defaultAttack;

            // Conditional Attacks
            foreach (var condAtk in _behaviorData.ConditionalAttacks)
            {
                if (condAtk.Behavior == null) continue;

                var behavior = AttackBehaviorBase.Create(
                    condAtk.Behavior.Type,
                    condAtk.Behavior.Param1,
                    condAtk.Behavior.Param2,
                    condAtk.Behavior.ProjectilePrefab,
                    condAtk.Behavior.HitVfxPrefab,
                    condAtk.Behavior.Range
                );
                behavior?.Initialize(this);

                _conditionalAttacks.Add(new ConditionalBehavior<IAttackBehavior>
                {
                    Condition = BehaviorCondition.Parse(condAtk.Condition),
                    Behavior = behavior
                });
            }

            // Passives
            for (int i = 0; i < _behaviorData.Passives.Count; i++)
            {
                var passiveData = _behaviorData.Passives[i];
                if (passiveData == null)
                {
                    Debug.LogWarning($"[BugController] {name}: Passives[{i}] is null in {_behaviorData.name}");
                    continue;
                }

                var passive = PassiveBehaviorBase.Create(
                    passiveData.Type,
                    passiveData.Param1,
                    passiveData.Param2,
                    passiveData.EffectPrefab,
                    passiveData.EffectPrefab2
                );
                if (passive != null)
                {
                    passive.Initialize(this);
                    _passives.Add(passive);
                }
            }

            // Skills
            for (int i = 0; i < _behaviorData.Skills.Count; i++)
            {
                var skillData = _behaviorData.Skills[i];
                if (skillData == null)
                {
                    Debug.LogWarning($"[BugController] {name}: Skills[{i}] is null in {_behaviorData.name}");
                    continue;
                }

                var skill = SkillBehaviorBase.Create(
                    skillData.Type,
                    skillData.Cooldown,
                    skillData.Param1,
                    skillData.Param2,
                    skillData.SpawnPrefab,
                    skillData.EffectPrefab
                );
                if (skill != null)
                {
                    skill.Initialize(this);
                    _skills.Add(skill);
                }
            }

            // Triggers
            for (int i = 0; i < _behaviorData.Triggers.Count; i++)
            {
                var triggerData = _behaviorData.Triggers[i];
                if (triggerData == null)
                {
                    Debug.LogWarning($"[BugController] {name}: Triggers[{i}] is null in {_behaviorData.name}");
                    continue;
                }

                var trigger = TriggerBehaviorBase.Create(
                    triggerData.Type,
                    triggerData.Param1,
                    triggerData.Param2,
                    triggerData.Param3,
                    triggerData.EffectPrefab
                );
                if (trigger != null)
                {
                    trigger.Initialize(this);
                    _triggers.Add(trigger);
                }
            }
        }

        private void InitializeFromRuntimeData(RuntimeBehaviorSet data)
        {
            if (data == null)
            {
                SetupDefaultBehaviors();
                return;
            }

            // Movement
            _defaultMovement = MovementBehaviorBase.Create(data.MovementType, data.MovementParam1, data.MovementParam2);
            _defaultMovement?.Initialize(this);
            _currentMovement = _defaultMovement;

            // Conditional Movements
            foreach (var condMov in data.ConditionalMovements)
            {
                var behavior = MovementBehaviorBase.Create(condMov.Type, condMov.Param1, condMov.Param2);
                behavior?.Initialize(this);

                _conditionalMovements.Add(new ConditionalBehavior<IMovementBehavior>
                {
                    Condition = BehaviorCondition.Parse(condMov.Condition),
                    Behavior = behavior,
                    Duration = condMov.Duration
                });
            }

            // Attack
            _defaultAttack = AttackBehaviorBase.Create(data.AttackType, data.AttackParam1, data.AttackParam2, null);
            _defaultAttack?.Initialize(this);
            _currentAttack = _defaultAttack;

            // Conditional Attacks
            foreach (var condAtk in data.ConditionalAttacks)
            {
                var behavior = AttackBehaviorBase.Create(condAtk.Type, condAtk.Param1, condAtk.Param2, null);
                behavior?.Initialize(this);

                _conditionalAttacks.Add(new ConditionalBehavior<IAttackBehavior>
                {
                    Condition = BehaviorCondition.Parse(condAtk.Condition),
                    Behavior = behavior
                });
            }

            // Passives
            foreach (var passiveData in data.Passives)
            {
                var passive = PassiveBehaviorBase.Create(passiveData.Type, passiveData.Param1, passiveData.Param2);
                if (passive != null)
                {
                    passive.Initialize(this);
                    _passives.Add(passive);
                }
            }

            // TODO: Skills, Triggers 초기화 (Phase 2)
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void FindFxSocket()
        {
            if (_fxSocket == null)
            {
                Transform socket = transform.Find("FX_Socket");
                if (socket != null)
                    _fxSocket = socket;
            }
        }

        private void FindTarget()
        {
            var machine = FindAnyObjectByType<Machine.MachineController>();
            if (machine != null)
            {
                _target = machine.transform;
            }
        }

        private void CreateHpBar()
        {
            Vector3 offset = (_bugData != null && _bugData.HpBarOffset != Vector3.zero)
                ? _bugData.HpBarOffset
                : CalculateHpBarOffset();

            _hpBar = GetComponentInChildren<BugHpBar>();
            if (_hpBar != null)
            {
                _hpBar.Initialize(transform, offset);
            }
            else
            {
                _hpBar = BugHpBar.Create(transform, offset);
            }
        }

        private Vector3 CalculateHpBarOffset()
        {
            float topZ = 0.5f;

            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Vector3 localMax = transform.InverseTransformPoint(renderer.bounds.max);
                topZ = localMax.z + 0.2f;
            }
            else
            {
                var col = GetComponent<Collider>();
                if (col != null)
                {
                    Vector3 localMax = transform.InverseTransformPoint(col.bounds.max);
                    topZ = localMax.z + 0.2f;
                }
            }

            return new Vector3(0f, 0.1f, topZ);
        }

        #endregion

        #region Behavior Management

        private void UpdateConditionalBehaviors()
        {
            // Movement 조건부 전환
            IMovementBehavior newMovement = _defaultMovement;
            foreach (var conditional in _conditionalMovements)
            {
                bool result = conditional.Condition.Evaluate(this, _target);
                // Debug.Log($"[{name}] Movement Condition: {conditional.Condition.Type} {conditional.Condition.Value}, HP%={HealthPercent:F1}, Result={result}");
                if (result)
                {
                    newMovement = conditional.Behavior;
                    break;
                }
            }
            if (newMovement != _currentMovement)
            {
                Debug.Log($"[{name}] Movement changed: {_currentMovement?.GetType().Name} → {newMovement?.GetType().Name}");
                _currentMovement = newMovement;
            }

            // Attack 조건부 전환
            IAttackBehavior newAttack = _defaultAttack;
            foreach (var conditional in _conditionalAttacks)
            {
                bool result = conditional.Condition.Evaluate(this, _target);
                // Debug.Log($"[{name}] Attack Condition: {conditional.Condition.Type} {conditional.Condition.Value}, HP%={HealthPercent:F1}, Result={result}");
                if (result)
                {
                    newAttack = conditional.Behavior;
                    break;
                }
            }
            if (newAttack != _currentAttack)
            {
                Debug.Log($"[{name}] Attack changed: {_currentAttack?.GetType().Name} → {newAttack?.GetType().Name}");
                _currentAttack = newAttack;
            }
        }

        private void SetJustAttacked()
        {
            _justAttacked = true;
            _justAttackedTimer = 0.5f; // 0.5초간 유지
            _attackCount++;
        }

        /// <summary>
        /// 아군 사망 알림 (Swarm 등에서 호출)
        /// </summary>
        public void NotifyAllyDead()
        {
            _allyJustDied = true;
        }

        #endregion

        #region Damage & Health

        public void TakeDamage(float damage)
        {
            if (_isDead) return;
            if (_isInvulnerable) return; // 무적 상태면 데미지 무시

            _hitCount++;

            // 패시브로 데미지 처리
            foreach (var passive in _passives)
            {
                damage = passive.ProcessIncomingDamage(damage);
                if (damage <= 0) return; // 완전 흡수/회피
            }

            _currentHealth -= damage;
            _currentHealth = Mathf.Max(0f, _currentHealth);

            UpdateHpBar();
            PlayHitFlash();
            AudioManager.Instance?.PlayBugHit();

            if (_currentHealth <= 0f)
            {
                Die();            // 치명타 — Die() 내부에서 PlayDeathVfx만 실행
            }
            else
            {
                PlayHitVfx();     // 생존 — 피격 VFX만
            }
        }

        public void Heal(float amount)
        {
            if (_isDead) return;

            _currentHealth += amount;
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            UpdateHpBar();
        }

        private void UpdateHpBar()
        {
            if (_hpBar != null && _maxHealth > 0f)
            {
                _hpBar.SetHealth(_currentHealth / _maxHealth);
            }
        }

        /// <summary>
        /// 무적 상태 설정 (Burrow 등에서 사용)
        /// </summary>
        public void SetInvulnerable(bool invulnerable)
        {
            _isInvulnerable = invulnerable;
        }

        /// <summary>
        /// Burrow 패시브 가져오기 (Trigger에서 사용)
        /// </summary>
        public BurrowPassive GetBurrowPassive()
        {
            foreach (var passive in _passives)
            {
                if (passive is BurrowPassive burrow)
                    return burrow;
            }
            return null;
        }

        #endregion

        #region Buff

        /// <summary>
        /// 버프 정보
        /// </summary>
        private struct BuffInfo
        {
            public float DamageMultiplier;
            public float SpeedMultiplier;
        }

        /// <summary>
        /// 버프 적용 (Aura 등에서 호출)
        /// </summary>
        public void ApplyBuff(object source, float damageMultiplier, float speedMultiplier)
        {
            _activeBuffs[source] = new BuffInfo
            {
                DamageMultiplier = damageMultiplier,
                SpeedMultiplier = speedMultiplier
            };
            RecalculateBuffs();
        }

        /// <summary>
        /// 버프 해제
        /// </summary>
        public void RemoveBuff(object source)
        {
            if (_activeBuffs.Remove(source))
            {
                RecalculateBuffs();
            }
        }

        /// <summary>
        /// 모든 활성 버프를 종합하여 최종 배율 계산
        /// </summary>
        private void RecalculateBuffs()
        {
            _buffedDamageMultiplier = 1f;
            _buffedSpeedMultiplier = 1f;

            foreach (var buff in _activeBuffs.Values)
            {
                _buffedDamageMultiplier *= buff.DamageMultiplier;
                _buffedSpeedMultiplier *= buff.SpeedMultiplier;
            }

            // 버프 텍스트 업데이트
            UpdateBuffLabel();
        }

        /// <summary>
        /// 버프 활성 여부
        /// </summary>
        public bool HasBuff => _activeBuffs.Count > 0;

        // 버프 텍스트 UI
        private BugLabel _buffLabel;

        /// <summary>
        /// 버프 텍스트 업데이트
        /// </summary>
        private void UpdateBuffLabel()
        {
            bool hasBuff = _activeBuffs.Count > 0;

            if (!hasBuff)
            {
                // 버프 없으면 라벨 제거
                if (_buffLabel != null)
                {
                    Destroy(_buffLabel.gameObject);
                    _buffLabel = null;
                }
                // 아웃라인 끄기
                SetBuffOutline(false);
                return;
            }

            // 버프 있으면 라벨 생성/업데이트
            if (_buffLabel == null)
            {
                // HP바 위에 표시
                Vector3 offset = (_bugData != null && _bugData.HpBarOffset != Vector3.zero)
                    ? _bugData.HpBarOffset + new Vector3(0f, 0f, 0.3f)
                    : new Vector3(0f, 0.1f, 1.0f);
                _buffLabel = BugLabel.Create(transform, "", Color.yellow, offset);
            }

            // 텍스트 구성
            string buffText = "";
            if (_buffedDamageMultiplier > 1f)
            {
                buffText += $"ATK {_buffedDamageMultiplier:F1}x";
            }
            if (_buffedSpeedMultiplier > 1f)
            {
                if (!string.IsNullOrEmpty(buffText)) buffText += "\n";
                buffText += $"SPD {_buffedSpeedMultiplier:F1}x";
            }
            _buffLabel.SetText(buffText);

            // 아웃라인 켜기
            SetBuffOutline(true);
        }

        // 버프 아웃라인 쉐이더 프로퍼티 ID
        private static readonly int OutlineEnabledId = Shader.PropertyToID("_OutlineEnabled");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        /// <summary>
        /// 버프 아웃라인 쉐이더 On/Off
        /// </summary>
        private void SetBuffOutline(bool enabled)
        {
            if (_renderers == null) return;

            float value = enabled ? 1f : 0f;
            Color outlineColor = new Color(1f, 0.85f, 0.2f, 1f); // 황금색

            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;

                // MaterialPropertyBlock으로 인스턴스별 설정
                renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(OutlineEnabledId, value);
                if (enabled)
                {
                    _propBlock.SetColor(OutlineColorId, outlineColor);
                }
                renderer.SetPropertyBlock(_propBlock);
            }
        }

        #endregion

        #region Death

        private void Die()
        {
            _isDead = true;

            // 사망 트리거 실행
            foreach (var trigger in _triggers)
            {
                if (trigger.TriggerOnDeath)
                {
                    trigger.OnDeath();
                }
            }

            // HP바 제거
            if (_hpBar != null)
            {
                Destroy(_hpBar.gameObject);
            }

            // 버프 라벨 제거
            if (_buffLabel != null)
            {
                Destroy(_buffLabel.gameObject);
            }

            // 데스 VFX
            PlayDeathVfx();

            // 이벤트 발생
            GameEvents.OnBugKilled?.Invoke(_bugId);

            // 행동 정리
            CleanupBehaviors();

            // Pool 반환 or Destroy
            var pooled = GetComponent<PooledBug>();
            if (pooled != null && pooled.IsPooled)
            {
                ResetForPool();
                pooled.ReturnToPool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 풀 복귀 전 상태 초기화 (다음 Get() 시 재사용 가능)
        /// </summary>
        private void ResetForPool()
        {
            _isDead = false;
            _isInvulnerable = false;
            _movementExternallyControlled = false;
            _aliveTime = 0f;
            _hitCount = 0;
            _attackCount = 0;
            _justAttacked = false;
            _justAttackedTimer = 0f;
            _allyJustDied = false;
            _activeBuffs.Clear();
            _buffedDamageMultiplier = 1f;
            _buffedSpeedMultiplier = 1f;

            _currentMovement = null;
            _defaultMovement = null;
            _conditionalMovements.Clear();
            _currentAttack = null;
            _defaultAttack = null;
            _conditionalAttacks.Clear();
            _skills.Clear();
            _passives.Clear();
            _triggers.Clear();

            _hpBar = null;
            _buffLabel = null;
        }

        private void CleanupBehaviors()
        {
            _currentMovement?.Cleanup();
            _defaultMovement?.Cleanup();
            _currentAttack?.Cleanup();
            _defaultAttack?.Cleanup();

            // 조건부 행동들도 정리 (VFX, Indicator 등 남지 않도록)
            foreach (var cond in _conditionalMovements)
            {
                cond?.Behavior?.Cleanup();
            }
            foreach (var cond in _conditionalAttacks)
            {
                cond?.Behavior?.Cleanup();
            }

            foreach (var skill in _skills) skill?.Cleanup();
            foreach (var passive in _passives) passive?.Cleanup();
            foreach (var trigger in _triggers) trigger?.Cleanup();
        }

        private void PlayDeathVfx()
        {
            if (_deathVfxPrefab == null) return;
            Vector3 spawnPos = _fxSocket != null ? _fxSocket.position : transform.position;
            SpawnScaledVfx(_deathVfxPrefab, spawnPos);
        }

        private void PlayHitVfx()
        {
            Vector3 hitPos = _fxSocket != null ? _fxSocket.position : transform.position;

            if (_hitVfxPrefab != null)
            {
                SpawnScaledVfx(_hitVfxPrefab, hitPos);
                return;
            }

            SimpleVFX.PlayBugHit(hitPos);
        }

        // VFX 스폰 — 프리펩 authored 회전·스케일을 유지하고, 벌레 크기에 비례 스케일링
        private void SpawnScaledVfx(GameObject prefab, Vector3 worldPos)
        {
            GameObject vfx = Instantiate(prefab);
            vfx.transform.position = worldPos;
            // 프리펩 authored localScale × 벌레 월드 스케일 × multiplier — 벌레 크기에 비례해 임팩트 크기 조정
            vfx.transform.localScale = Vector3.Scale(
                vfx.transform.localScale,
                transform.localScale * _vfxScaleMultiplier);

            var ps = vfx.GetComponent<ParticleSystem>();
            Destroy(vfx, ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f);
        }

        #endregion

        #region Visual Effects

        private void PlayHitFlash()
        {
            if (!_isFlashing && _renderers != null && _renderers.Length > 0)
            {
                StartCoroutine(HitFlashCoroutine());
            }
        }

        private System.Collections.IEnumerator HitFlashCoroutine()
        {
            _isFlashing = true;

            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", Color.white);
                _propBlock.SetColor("_Color", Color.white);
                renderer.SetPropertyBlock(_propBlock);
            }

            yield return new WaitForSeconds(_hitFlashDuration);

            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_propBlock);
                _propBlock.Clear();
                renderer.SetPropertyBlock(_propBlock);
            }

            _isFlashing = false;
        }

        #endregion

        #region Utility

        /// <summary>
        /// XZ 평면 거리 계산 (콜라이더 경계 기준)
        /// </summary>
        public float GetDistanceTo(Transform target)
        {
            if (target == null) return float.MaxValue;

            Vector3 myPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPos = new Vector3(target.position.x, 0f, target.position.z);
            float centerDistance = Vector3.Distance(myPos, targetPos);

            // 타겟의 콜라이더 반경 빼기 (머신 스케일 고려)
            float targetRadius = GetColliderRadius(target);
            float myRadius = GetColliderRadius(transform);

            return Mathf.Max(0f, centerDistance - targetRadius - myRadius);
        }

        /// <summary>
        /// 콜라이더의 XZ 평면 반경 계산
        /// </summary>
        private float GetColliderRadius(Transform t)
        {
            var collider = t.GetComponent<Collider>();
            if (collider == null) return 0f;

            // Bounds의 XZ 크기에서 반경 추정
            Vector3 size = collider.bounds.size;
            return Mathf.Max(size.x, size.z) * 0.5f;
        }

        /// <summary>
        /// 외부에서 초기화 (스포너에서 사용) - 프리펩의 BehaviorData 유지
        /// </summary>
        public void Initialize(BugData data, float healthMult = 1f, float damageMult = 1f, float speedMult = 1f)
        {
            _bugData = data;
            // _behaviorData는 프리펩에 설정된 값 유지

            ApplyStats(data, healthMult, damageMult, speedMult);
            InitializeBehaviors();
        }

        /// <summary>
        /// 외부에서 초기화 (BehaviorData 지정)
        /// </summary>
        public void Initialize(BugData data, BugBehaviorData behaviorData,
            float healthMult = 1f, float damageMult = 1f, float speedMult = 1f)
        {
            _bugData = data;

            // behaviorData가 지정되면 덮어쓰기, null이면 프리펩 설정 유지
            if (behaviorData != null)
            {
                _behaviorData = behaviorData;
            }

            ApplyStats(data, healthMult, damageMult, speedMult);
            InitializeBehaviors();
        }

        private void ApplyStats(BugData data, float healthMult, float damageMult, float speedMult)
        {
            _bugId = data.BugId;
            _maxHealth = data.MaxHealth * healthMult;
            _currentHealth = _maxHealth;
            _moveSpeed = data.MoveSpeed * speedMult;
            _attackDamage = data.AttackDamage * damageMult;
            _attackCooldown = data.AttackCooldown;

            if (data.Scale != 1f)
            {
                transform.localScale = Vector3.one * data.Scale;
            }
        }

        #endregion
    }
}

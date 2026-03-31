using UnityEngine;
using System;
using System.Collections.Generic;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.Machine;
using DrillCorp.Bug.Behaviors;
using DrillCorp.Bug.Behaviors.Data;
using DrillCorp.Bug.Behaviors.Movement;
using DrillCorp.Bug.Behaviors.Attack;
using DrillCorp.Bug.Behaviors.Passive;
using DrillCorp.VFX;

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
        [SerializeField] private GameObject _deathVfxPrefab;
        [SerializeField] private float _hitFlashDuration = 0.1f;

        // === Stats ===
        private int _bugId;
        private float _maxHealth;
        private float _currentHealth;
        private float _moveSpeed;
        private float _attackDamage;
        private float _attackCooldown;
        private float _attackRange;

        // === State ===
        private Transform _target;
        private float _aliveTime;
        private int _hitCount;
        private bool _justAttacked;
        private float _justAttackedTimer;
        private bool _allyJustDied;
        private bool _isDead;

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
        public float HealthPercent => _maxHealth > 0 ? (_currentHealth / _maxHealth) * 100f : 0f;
        public bool IsDead => _isDead;
        public float MoveSpeed => _moveSpeed;
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float AttackRange => _attackRange;
        public float AliveTime => _aliveTime;
        public int HitCount => _hitCount;
        public bool JustAttacked => _justAttacked;
        public bool AllyJustDied => _allyJustDied;
        public Transform Target => _target;
        public BugData BugData => _bugData;

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

            // 조건부 행동 체크 및 전환
            UpdateConditionalBehaviors();

            // 이동
            _currentMovement?.UpdateMovement(_target);

            // 기본 공격
            if (_currentAttack != null && _target != null)
            {
                float distance = GetDistanceTo(_target);
                // Debug.Log($"[BugController] {name} distance: {distance:F2}, attackRange: {_currentAttack.AttackRange:F2}");
                if (distance <= _currentAttack.AttackRange)
                {
                    if (_currentAttack.TryAttack(_target))
                    {
                        SetJustAttacked();
                    }
                }
            }

            // 스킬 업데이트
            foreach (var skill in _skills)
            {
                skill.UpdateCooldown(deltaTime);
                if (skill.IsReady && _target != null)
                {
                    skill.TryUse(_target);
                }
            }

            // 패시브 업데이트
            foreach (var passive in _passives)
            {
                passive.UpdatePassive(deltaTime);
            }

            // 트리거 체크
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
                _attackRange = _bugData.AttackRange;

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
                _defaultMovement = MovementBehaviorBase.Create(movData.Type, movData.Param1, movData.Param2);
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
                    condMov.Behavior.Param2
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
                    atkData.HitVfxPrefab
                );
                _defaultAttack?.Initialize(this);
            }
            else
            {
                _defaultAttack = new MeleeAttack();
                _defaultAttack.Initialize(this);
            }
            _currentAttack = _defaultAttack;

            // Passives
            foreach (var passiveData in _behaviorData.Passives)
            {
                var passive = PassiveBehaviorBase.Create(
                    passiveData.Type,
                    passiveData.Param1,
                    passiveData.Param2
                );
                if (passive != null)
                {
                    passive.Initialize(this);
                    _passives.Add(passive);
                }
            }

            // TODO: Skills, Triggers 초기화 (Phase 2)
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
                if (conditional.Condition.Evaluate(this, _target))
                {
                    newMovement = conditional.Behavior;
                    break;
                }
            }
            if (newMovement != _currentMovement)
            {
                _currentMovement = newMovement;
            }

            // Attack 조건부 전환
            IAttackBehavior newAttack = _defaultAttack;
            foreach (var conditional in _conditionalAttacks)
            {
                if (conditional.Condition.Evaluate(this, _target))
                {
                    newAttack = conditional.Behavior;
                    break;
                }
            }
            if (newAttack != _currentAttack)
            {
                _currentAttack = newAttack;
            }
        }

        private void SetJustAttacked()
        {
            _justAttacked = true;
            _justAttackedTimer = 0.5f; // 0.5초간 유지
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
            PlayHitVfx();

            if (_currentHealth <= 0f)
            {
                Die();
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
            if (_hpBar != null)
            {
                _hpBar.SetHealth(_currentHealth / _maxHealth);
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

            // 데스 VFX
            PlayDeathVfx();

            // 이벤트 발생
            GameEvents.OnBugKilled?.Invoke(_bugId);

            // 행동 정리
            CleanupBehaviors();

            Destroy(gameObject);
        }

        private void CleanupBehaviors()
        {
            _currentMovement?.Cleanup();
            _defaultMovement?.Cleanup();
            _currentAttack?.Cleanup();
            _defaultAttack?.Cleanup();

            foreach (var skill in _skills) skill.Cleanup();
            foreach (var passive in _passives) passive.Cleanup();
            foreach (var trigger in _triggers) trigger.Cleanup();
        }

        private void PlayDeathVfx()
        {
            if (_deathVfxPrefab == null) return;

            Vector3 spawnPos = _fxSocket != null ? _fxSocket.position : transform.position;
            Quaternion spawnRot = _fxSocket != null ? _fxSocket.rotation : Quaternion.identity;

            GameObject vfx = Instantiate(_deathVfxPrefab, spawnPos, spawnRot);

            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(vfx, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(vfx, 2f);
            }
        }

        private void PlayHitVfx()
        {
            Vector3 hitPos = _fxSocket != null ? _fxSocket.position : transform.position;
            SimpleVFX.PlayBugHit(hitPos);
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
            _attackRange = data.AttackRange;

            if (data.Scale != 1f)
            {
                transform.localScale = Vector3.one * data.Scale;
            }
        }

        #endregion
    }
}

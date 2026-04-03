using UnityEngine;
using System.Collections;
using DrillCorp.Core;
using DrillCorp.Machine;
using DrillCorp.Data;

namespace DrillCorp.Bug
{
    public abstract class BugBase : MonoBehaviour, IDamageable
    {
        [Header("Data")]
        [SerializeField] protected BugData _bugData;

        [Header("Stats (Override or from BugData)")]
        [SerializeField] protected int _bugId;
        [SerializeField] protected float _maxHealth = 10f;
        [SerializeField] protected float _moveSpeed = 2f;
        [SerializeField] protected float _attackDamage = 5f;
        [SerializeField] protected float _attackCooldown = 1f;
        [SerializeField] protected float _attackRange = 1f;

        [Header("VFX")]
        [SerializeField] protected Transform _fxSocket;
        [SerializeField] protected GameObject _deathVfxPrefab;
        [SerializeField] protected float _hitFlashDuration = 0.1f;

        protected float _currentHealth;
        protected Transform _target;
        protected float _lastAttackTime;
        protected BugHpBar _hpBar;
        protected Renderer[] _renderers;
        protected MaterialPropertyBlock _propBlock;
        protected bool _isFlashing;

        public int BugId => _bugId;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsDead => _currentHealth <= 0f;

        protected virtual void Awake()
        {
            ApplyBugData();
            _currentHealth = _maxHealth;
            EnsureCollider();
            SetBugLayer();
            CacheRenderers();
            FindFxSocket();
        }

        /// <summary>
        /// BugData가 있으면 데이터를 적용
        /// </summary>
        protected virtual void ApplyBugData()
        {
            if (_bugData != null)
            {
                _bugId = _bugData.BugId;
                _maxHealth = _bugData.MaxHealth;
                _moveSpeed = _bugData.MoveSpeed;
                _attackDamage = _bugData.AttackDamage;
                _attackCooldown = _bugData.AttackCooldown;
                // _attackRange는 기본값(1f) 또는 Inspector에서 설정된 값 사용

                // Scale 적용
                if (_bugData.Scale != 1f)
                {
                    transform.localScale = Vector3.one * _bugData.Scale;
                }
            }
        }

        protected virtual void EnsureCollider()
        {
            // Collider가 없으면 자동으로 추가
            if (GetComponent<Collider>() == null)
            {
                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = 1f;
                capsule.radius = 0.3f;
                capsule.center = new Vector3(0f, 0.5f, 0f);
            }
        }

        protected virtual void SetBugLayer()
        {
            // "Bug" 레이어가 있으면 설정
            int bugLayer = LayerMask.NameToLayer("Bug");
            if (bugLayer != -1)
            {
                gameObject.layer = bugLayer;
            }
        }

        protected virtual void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        protected virtual void FindFxSocket()
        {
            if (_fxSocket == null)
            {
                // FX_Socket 자식 오브젝트 찾기
                Transform socket = transform.Find("FX_Socket");
                if (socket != null)
                {
                    _fxSocket = socket;
                }
            }
        }

        protected virtual void Start()
        {
            FindTarget();
            CreateHpBar();
        }

        protected virtual void CreateHpBar()
        {
            // BugData에 offset이 있으면 사용, 없으면 자동 계산
            Vector3 offset = (_bugData != null && _bugData.HpBarOffset != Vector3.zero)
                ? _bugData.HpBarOffset
                : CalculateHpBarOffset();

            // 이미 자식에 HpBar가 있으면 사용
            _hpBar = GetComponentInChildren<BugHpBar>();
            if (_hpBar != null)
            {
                _hpBar.Initialize(transform, offset);
                return;
            }

            // 없으면 동적 생성
            _hpBar = BugHpBar.Create(transform, offset);
        }

        /// <summary>
        /// Renderer 또는 Collider 기준으로 HP바 위치 자동 계산
        /// </summary>
        protected virtual Vector3 CalculateHpBarOffset()
        {
            float topZ = 0.5f; // 기본값

            // Renderer bounds 확인
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // 로컬 좌표로 변환하여 Z 상단 계산
                Vector3 localMax = transform.InverseTransformPoint(renderer.bounds.max);
                topZ = localMax.z + 0.2f; // 약간의 여유
            }
            // Collider bounds 확인
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

        protected virtual void Update()
        {
            if (IsDead || _target == null) return;

            MoveToTarget();
            TryAttack();
        }

        protected virtual void FindTarget()
        {
            var machine = FindAnyObjectByType<MachineController>();
            if (machine != null)
            {
                _target = machine.transform;
            }
        }

        protected virtual void MoveToTarget()
        {
            if (_target == null) return;

            // XZ 평면에서의 거리 계산
            Vector3 myPosXZ = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPosXZ = new Vector3(_target.position.x, 0f, _target.position.z);
            float distance = Vector3.Distance(myPosXZ, targetPosXZ);

            // 공격 범위 내에 들어오면 이동 중지
            if (distance <= GetAttackRange())
            {
                return;
            }

            // XZ 평면에서 이동 (Y는 높이)
            Vector3 targetPos = new Vector3(_target.position.x, transform.position.y, _target.position.z);
            Vector3 direction = (targetPos - transform.position).normalized;
            transform.position += direction * _moveSpeed * Time.deltaTime;

            // 이동 방향으로 회전 (Y축 기준)
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        protected virtual void TryAttack()
        {
            if (_target == null) return;

            // XZ 평면에서의 거리 계산 (Y축 무시)
            Vector3 myPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPos = new Vector3(_target.position.x, 0f, _target.position.z);
            float distance = Vector3.Distance(myPos, targetPos);

            if (distance <= GetAttackRange() && Time.time >= _lastAttackTime + _attackCooldown)
            {
                Attack();
                _lastAttackTime = Time.time;
            }
        }

        protected virtual float GetAttackRange()
        {
            return _attackRange;
        }

        protected virtual void Attack()
        {
            var damageable = _target.GetComponent<IDamageable>();
            damageable?.TakeDamage(_attackDamage);
        }

        public virtual void TakeDamage(float damage)
        {
            if (IsDead) return;

            _currentHealth -= damage;
            _currentHealth = Mathf.Max(0f, _currentHealth);

            UpdateHpBar();
            PlayHitFlash();

            if (IsDead)
            {
                Die();
            }
        }

        /// <summary>
        /// 피격 시 흰색 깜빡임 효과
        /// </summary>
        protected virtual void PlayHitFlash()
        {
            if (!_isFlashing && _renderers != null && _renderers.Length > 0)
            {
                StartCoroutine(HitFlashCoroutine());
            }
        }

        protected virtual IEnumerator HitFlashCoroutine()
        {
            _isFlashing = true;

            // 흰색으로 변경
            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", Color.white);
                _propBlock.SetColor("_Color", Color.white); // Fallback for different shaders
                renderer.SetPropertyBlock(_propBlock);
            }

            yield return new WaitForSeconds(_hitFlashDuration);

            // 원래 색상으로 복원
            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_propBlock);
                _propBlock.Clear();
                renderer.SetPropertyBlock(_propBlock);
            }

            _isFlashing = false;
        }

        protected virtual void UpdateHpBar()
        {
            if (_hpBar != null)
            {
                _hpBar.SetHealth(_currentHealth / _maxHealth);
            }
        }

        public virtual void Heal(float amount)
        {
            if (IsDead) return;

            _currentHealth += amount;
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        }

        protected virtual void Die()
        {
            // HP바 제거
            if (_hpBar != null)
            {
                Destroy(_hpBar.gameObject);
            }

            // 데스 VFX 재생
            PlayDeathVfx();

            GameEvents.OnBugKilled?.Invoke(_bugId);
            Destroy(gameObject);
        }

        /// <summary>
        /// 죽을 때 FX_Socket 위치에 데스 이펙트 생성
        /// </summary>
        protected virtual void PlayDeathVfx()
        {
            if (_deathVfxPrefab == null) return;

            Vector3 spawnPos = _fxSocket != null ? _fxSocket.position : transform.position;
            Quaternion spawnRot = _fxSocket != null ? _fxSocket.rotation : Quaternion.identity;

            GameObject vfx = Instantiate(_deathVfxPrefab, spawnPos, spawnRot);

            // VFX 자동 파괴 (ParticleSystem 기준)
            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(vfx, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(vfx, 2f); // 기본 2초 후 파괴
            }
        }

        public void Initialize(int id, float health, float speed, float damage)
        {
            _bugId = id;
            _maxHealth = health;
            _currentHealth = health;
            _moveSpeed = speed;
            _attackDamage = damage;
        }

        /// <summary>
        /// BugData로 초기화 (스포너에서 사용)
        /// </summary>
        public void Initialize(BugData data, float healthMult = 1f, float damageMult = 1f, float speedMult = 1f)
        {
            _bugData = data;
            _bugId = data.BugId;
            _maxHealth = data.MaxHealth * healthMult;
            _currentHealth = _maxHealth;
            _moveSpeed = data.MoveSpeed * speedMult;
            _attackDamage = data.AttackDamage * damageMult;
            _attackCooldown = data.AttackCooldown;
            // _attackRange는 기본값(1f) 또는 Inspector에서 설정된 값 사용

            if (data.Scale != 1f)
            {
                transform.localScale = Vector3.one * data.Scale;
            }
        }

        public BugData BugData => _bugData;
    }
}

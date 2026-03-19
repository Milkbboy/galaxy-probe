using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Machine;

namespace DrillCorp.Bug
{
    public abstract class BugBase : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] protected int _bugId;
        [SerializeField] protected float _maxHealth = 10f;
        [SerializeField] protected float _moveSpeed = 2f;
        [SerializeField] protected float _attackDamage = 5f;
        [SerializeField] protected float _attackCooldown = 1f;

        [Header("Health Bar")]
        [SerializeField] protected bool _showHealthBar = true;

        protected float _currentHealth;
        protected Transform _target;
        protected float _lastAttackTime;
        protected BugHealthBar _healthBar;

        public int BugId => _bugId;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool IsDead => _currentHealth <= 0f;

        protected virtual void Awake()
        {
            _currentHealth = _maxHealth;
            EnsureCollider();
            SetBugLayer();
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

        protected virtual void Start()
        {
            FindTarget();
            CreateHealthBar();
        }

        protected virtual void CreateHealthBar()
        {
            if (_showHealthBar)
            {
                _healthBar = BugHealthBar.Create(transform);
            }
        }

        protected virtual void Update()
        {
            if (IsDead || _target == null) return;

            MoveToTarget();
            TryAttack();
        }

        protected virtual void FindTarget()
        {
            var machine = FindFirstObjectByType<MachineController>();
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
            return 1f;
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

            UpdateHealthBar();

            if (IsDead)
            {
                Die();
            }
        }

        protected virtual void UpdateHealthBar()
        {
            if (_healthBar != null)
            {
                _healthBar.SetHealth(_currentHealth / _maxHealth);
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
            if (_healthBar != null)
            {
                Destroy(_healthBar.gameObject);
            }
            GameEvents.OnBugKilled?.Invoke(_bugId);
            Destroy(gameObject);
        }

        public void Initialize(int id, float health, float speed, float damage)
        {
            _bugId = id;
            _maxHealth = health;
            _currentHealth = health;
            _moveSpeed = speed;
            _attackDamage = damage;
        }
    }
}

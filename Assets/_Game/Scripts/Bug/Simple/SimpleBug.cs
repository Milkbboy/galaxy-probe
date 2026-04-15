using UnityEngine;
using DrillCorp.Machine;
using DrillCorp.UI.Minimap;

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

        private SimpleBugData _data;
        private Transform _target;
        private float _hp;
        private float _maxHp;
        private float _speed;
        private float _score;
        private bool _isDead;

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

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (dist < 0.001f) return;

            Vector3 dir = toTarget / dist;
            transform.position += dir * _speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (dist < _contactRange && _target.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(_contactDamagePerSecond * Time.deltaTime);
            }
        }

        public void TakeDamage(float damage)
        {
            if (_isDead) return;
            _hp -= damage;
            if (_hp <= 0f)
            {
                _hp = 0f;
                _isDead = true;
                Destroy(gameObject);
            }
        }

        public void Heal(float amount)
        {
            if (_isDead) return;
            _hp = Mathf.Min(_maxHp, _hp + amount);
        }
    }
}

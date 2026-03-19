using UnityEngine;

namespace DrillCorp.Data
{
    [CreateAssetMenu(fileName = "Bug_New", menuName = "Drill-Corp/Bug Data", order = 1)]
    public class BugData : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] private int _bugId;
        [SerializeField] private string _bugName;
        [SerializeField] private string _description;

        [Header("Stats")]
        [SerializeField] private float _maxHealth = 10f;
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _attackDamage = 5f;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _attackRange = 1f;

        [Header("Visuals")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Color _tintColor = Color.white;
        [SerializeField] private float _scale = 1f;

        [Header("Rewards")]
        [SerializeField] private int _currencyReward = 1;
        [SerializeField] private float _dropChance = 1f;

        // Properties
        public int BugId => _bugId;
        public string BugName => _bugName;
        public string Description => _description;
        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float AttackRange => _attackRange;
        public GameObject Prefab => _prefab;
        public Color TintColor => _tintColor;
        public float Scale => _scale;
        public int CurrencyReward => _currencyReward;
        public float DropChance => _dropChance;
    }
}

using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Data
{
    /// <summary>
    /// 기본 공격 타입
    /// </summary>
    public enum AttackType
    {
        None,       // 공격 없음 (자폭형 등)
        Melee,      // 근접
        Cleave,     // 범위 근접
        Projectile, // 원거리
        Spread,     // 다발 발사
        Homing,     // 유도탄
        Beam,       // 레이저
        Lob         // 포물선
    }

    /// <summary>
    /// 기본 공격 행동 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "Attack_New", menuName = "Drill-Corp/Bug Behaviors/Attack")]
    public class AttackBehaviorData : ScriptableObject
    {
        [Header("Basic")]
        [SerializeField] private AttackType _type = AttackType.Melee;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Header("Parameters")]
        [SerializeField] private float _param1;
        [SerializeField] private float _param2;

        [Header("Prefabs")]
        [SerializeField] private GameObject _projectilePrefab;

        public AttackType Type => _type;
        public string DisplayName => _displayName;
        public string Description => _description;
        public GameObject ProjectilePrefab => _projectilePrefab;

        /// <summary>
        /// 타입별 파라미터 의미:
        /// - Cleave: param1 = 각도
        /// - Projectile: param1 = 속도
        /// - Spread: param1 = 발수, param2 = 각도
        /// - Homing: param1 = 속도, param2 = 회전속도
        /// - Beam: param1 = 지속시간
        /// </summary>
        public float Param1 => _param1;
        public float Param2 => _param2;

        /// <summary>
        /// 문자열에서 파싱 (예: "Melee", "Cleave:90", "Spread:5:60")
        /// </summary>
        public static (AttackType type, float param1, float param2) Parse(string str)
        {
            if (string.IsNullOrEmpty(str))
                return (AttackType.Melee, 0f, 0f);

            string[] parts = str.Split(':');
            string typeName = parts[0].Trim();

            AttackType type = AttackType.Melee;
            float param1 = 0f;
            float param2 = 0f;

            // 타입 파싱
            if (System.Enum.TryParse(typeName, true, out AttackType parsedType))
            {
                type = parsedType;
            }

            // 파라미터 파싱
            if (parts.Length > 1 && float.TryParse(parts[1], out float p1))
                param1 = p1;
            if (parts.Length > 2 && float.TryParse(parts[2], out float p2))
                param2 = p2;

            return (type, param1, param2);
        }
    }
}

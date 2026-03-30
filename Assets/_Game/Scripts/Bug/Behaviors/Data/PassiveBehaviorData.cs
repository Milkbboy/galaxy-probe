using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Data
{
    /// <summary>
    /// 패시브 타입
    /// </summary>
    public enum PassiveType
    {
        // 방어
        Armor,          // 데미지 감소
        Shield,         // 데미지 흡수
        Regen,          // 체력 재생
        Dodge,          // 회피
        Reflect,        // 반사

        // 공격
        Lifesteal,      // 흡혈
        CritChance,     // 치명타
        PoisonAttack,   // 독 공격

        // 이동
        Fast            // 이속 증가
    }

    /// <summary>
    /// 패시브 행동 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "Passive_New", menuName = "Drill-Corp/Bug Behaviors/Passive")]
    public class PassiveBehaviorData : ScriptableObject
    {
        [Header("Basic")]
        [SerializeField] private PassiveType _type;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [Header("Parameters")]
        [SerializeField] private float _param1;
        [SerializeField] private float _param2;

        public PassiveType Type => _type;
        public string DisplayName => _displayName;
        public string Description => _description;

        /// <summary>
        /// 타입별 파라미터 의미:
        /// - Armor: param1 = 감소량
        /// - Shield: param1 = 흡수량, param2 = 재생쿨
        /// - Regen: param1 = 초당회복
        /// - Dodge: param1 = 확률%
        /// - Reflect: param1 = 비율%
        /// - Lifesteal: param1 = 비율%
        /// - CritChance: param1 = 확률%, param2 = 배율
        /// - PoisonAttack: param1 = 지속시간, param2 = 데미지
        /// - Fast: param1 = 이속배율
        /// </summary>
        public float Param1 => _param1;
        public float Param2 => _param2;

        /// <summary>
        /// 문자열에서 파싱 (예: "Armor:5", "Shield:50:30", "Dodge:20")
        /// </summary>
        public static (PassiveType type, float param1, float param2) Parse(string str)
        {
            if (string.IsNullOrEmpty(str))
                return (PassiveType.Armor, 0f, 0f);

            string[] parts = str.Split(':');
            string typeName = parts[0].Trim();

            PassiveType type = PassiveType.Armor;
            float param1 = 0f;
            float param2 = 0f;

            // 타입 파싱
            if (System.Enum.TryParse(typeName, true, out PassiveType parsedType))
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

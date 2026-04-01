using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Data
{
    /// <summary>
    /// 스킬 타입
    /// </summary>
    public enum SkillType
    {
        // 공격형
        Nova,           // 전방향 폭발
        Charge,         // 차지 공격
        Lunge,          // 돌진 공격

        // 소환형
        Spawn,          // 졸개 소환
        CallReinforce,  // 지원 요청

        // 버프형
        BuffAlly,       // 아군 강화
        HealAlly,       // 아군 회복
        SelfBuff,       // 자기 강화

        // 디버프형
        Slow,           // 감속
        Stun,           // 기절
        Poison          // 중독
    }

    /// <summary>
    /// 스킬 행동 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "Skill_New", menuName = "Drill-Corp/Bug Behaviors/Skill")]
    public class SkillBehaviorData : ScriptableObject
    {
        [SerializeField] private SkillType _type;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private float _cooldown = 10f;

        [SerializeField] private float _param1;
        [SerializeField] private float _param2;
        [SerializeField] private string _stringParam; // Spawn 시 BugName 등

        [SerializeField] private GameObject _effectPrefab;
        [SerializeField] private GameObject _spawnPrefab;

        public SkillType Type => _type;
        public string DisplayName => _displayName;
        public string Description => _description;
        public float Cooldown => _cooldown;
        public GameObject EffectPrefab => _effectPrefab;
        public GameObject SpawnPrefab => _spawnPrefab;

        /// <summary>
        /// 타입별 파라미터 의미:
        /// - Nova: (없음)
        /// - Charge: param1 = 데미지배율
        /// - Lunge: param1 = 거리
        /// - Spawn: stringParam = BugName, param1 = 수량
        /// - BuffAlly: param1 = 배율
        /// - HealAlly: param1 = 회복량
        /// - SelfBuff: param1 = 지속시간, param2 = 배율
        /// - Slow: param1 = 지속시간, param2 = 감속비율
        /// - Stun: param1 = 지속시간
        /// - Poison: param1 = 지속시간, param2 = 데미지
        /// </summary>
        public float Param1 => _param1;
        public float Param2 => _param2;
        public string StringParam => _stringParam;

        /// <summary>
        /// 문자열에서 파싱 (예: "Spawn:10:Beetle:2", "Nova:15", "BuffAlly:20:1.2")
        /// </summary>
        public static (SkillType type, float cooldown, float param1, float param2, string stringParam) Parse(string str)
        {
            if (string.IsNullOrEmpty(str))
                return (SkillType.Nova, 10f, 0f, 0f, "");

            string[] parts = str.Split(':');
            string typeName = parts[0].Trim();

            SkillType type = SkillType.Nova;
            float cooldown = 10f;
            float param1 = 0f;
            float param2 = 0f;
            string stringParam = "";

            // 타입 파싱
            if (System.Enum.TryParse(typeName, true, out SkillType parsedType))
            {
                type = parsedType;
            }

            // Spawn 특수 처리: Spawn:쿨:이름:수량
            if (type == SkillType.Spawn && parts.Length >= 4)
            {
                if (float.TryParse(parts[1], out float cd))
                    cooldown = cd;
                stringParam = parts[2];
                if (float.TryParse(parts[3], out float count))
                    param1 = count;
            }
            else
            {
                // 일반: Type:쿨:param1:param2
                if (parts.Length > 1 && float.TryParse(parts[1], out float cd))
                    cooldown = cd;
                if (parts.Length > 2 && float.TryParse(parts[2], out float p1))
                    param1 = p1;
                if (parts.Length > 3 && float.TryParse(parts[3], out float p2))
                    param2 = p2;
            }

            return (type, cooldown, param1, param2, stringParam);
        }
    }
}

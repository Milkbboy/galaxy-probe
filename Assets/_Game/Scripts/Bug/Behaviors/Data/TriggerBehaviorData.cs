using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Data
{
    /// <summary>
    /// 트리거 타입
    /// </summary>
    public enum TriggerType
    {
        // HP 기반
        Enrage,         // 광폭화
        LastStand,      // 최후의 저항
        Transform,      // 변신

        // 피격 기반
        ArmorBreak,     // 갑옷 파괴
        CounterAttack,  // 반격

        // 사망 기반
        ExplodeOnDeath, // 자폭
        SplitOnDeath,   // 분열
        DropHazard,     // 장판
        Revive,         // 부활

        // 시간 기반
        Grow            // 성장
    }

    /// <summary>
    /// 트리거 행동 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "Trigger_New", menuName = "Drill-Corp/Bug Behaviors/Trigger")]
    public class TriggerBehaviorData : ScriptableObject
    {
        [SerializeField] private TriggerType _type;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [SerializeField] private float _param1;
        [SerializeField] private float _param2;
        [SerializeField] private float _param3;
        [SerializeField] private string _stringParam; // SplitOnDeath 시 BugName 등

        [SerializeField] private GameObject _effectPrefab;

        public TriggerType Type => _type;
        public string DisplayName => _displayName;
        public string Description => _description;
        public GameObject EffectPrefab => _effectPrefab;

        /// <summary>
        /// 타입별 파라미터 의미:
        /// - Enrage: param1 = HP%, param2 = 공격배율
        /// - LastStand: param1 = HP%, param2 = 방어배율
        /// - Transform: param1 = HP%
        /// - ArmorBreak: param1 = 피격횟수
        /// - CounterAttack: param1 = 확률%
        /// - ExplodeOnDeath: param1 = 범위, param2 = 데미지
        /// - SplitOnDeath: stringParam = BugName, param1 = 수량
        /// - DropHazard: param1 = 범위, param2 = 지속시간, param3 = 데미지
        /// - Revive: param1 = HP%
        /// - Grow: param1 = 시간간격, param2 = 배율
        /// </summary>
        public float Param1 => _param1;
        public float Param2 => _param2;
        public float Param3 => _param3;
        public string StringParam => _stringParam;

        /// <summary>
        /// 사망 시 발동 트리거인지
        /// </summary>
        public bool IsTriggerOnDeath =>
            _type == TriggerType.ExplodeOnDeath ||
            _type == TriggerType.SplitOnDeath ||
            _type == TriggerType.DropHazard ||
            _type == TriggerType.Revive;

        /// <summary>
        /// 문자열에서 파싱 (예: "Enrage:30:2", "ExplodeOnDeath:3:50", "SplitOnDeath:MiniBeetle:3")
        /// </summary>
        public static (TriggerType type, float param1, float param2, float param3, string stringParam) Parse(string str)
        {
            if (string.IsNullOrEmpty(str))
                return (TriggerType.Enrage, 0f, 0f, 0f, "");

            string[] parts = str.Split(':');
            string typeName = parts[0].Trim();

            TriggerType type = TriggerType.Enrage;
            float param1 = 0f;
            float param2 = 0f;
            float param3 = 0f;
            string stringParam = "";

            // 타입 파싱
            if (System.Enum.TryParse(typeName, true, out TriggerType parsedType))
            {
                type = parsedType;
            }

            // SplitOnDeath 특수 처리: SplitOnDeath:이름:수량
            if (type == TriggerType.SplitOnDeath && parts.Length >= 3)
            {
                stringParam = parts[1];
                if (float.TryParse(parts[2], out float count))
                    param1 = count;
            }
            else
            {
                // 일반: Type:param1:param2:param3
                if (parts.Length > 1 && float.TryParse(parts[1], out float p1))
                    param1 = p1;
                if (parts.Length > 2 && float.TryParse(parts[2], out float p2))
                    param2 = p2;
                if (parts.Length > 3 && float.TryParse(parts[3], out float p3))
                    param3 = p3;
            }

            return (type, param1, param2, param3, stringParam);
        }
    }
}

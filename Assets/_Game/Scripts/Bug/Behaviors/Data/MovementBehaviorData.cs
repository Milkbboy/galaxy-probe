using UnityEngine;

namespace DrillCorp.Bug.Behaviors.Data
{
    /// <summary>
    /// 이동 행동 타입
    /// </summary>
    public enum MovementType
    {
        Linear,     // 직선 이동
        Hover,      // 공중 부유
        Burst,      // 멈췄다 돌진
        Ranged,     // 원거리 (사거리 유지 + 좌우 이동)
        Retreat,    // 후퇴
        SlowStart,  // 느린 시작
        Orbit,      // 선회
        Burrow,     // 땅속 이동
        Teleport,   // 순간이동
        Dive        // 급강하
    }

    /// <summary>
    /// 이동 행동 데이터 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "Movement_New", menuName = "Drill-Corp/Bug Behaviors/Movement")]
    public class MovementBehaviorData : ScriptableObject
    {
        [SerializeField] private MovementType _type = MovementType.Linear;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;

        [SerializeField] private float _param1;
        [SerializeField] private float _param2;

        [SerializeField] private GameObject _effectPrefab;

        public MovementType Type => _type;
        public string DisplayName => _displayName;
        public string Description => _description;

        /// <summary>
        /// 타입별 파라미터 의미:
        /// - Hover: param1 = 높이, param2 = 주기
        /// - Burst: param1 = 대기시간, param2 = 속도배율
        /// - Ranged: param1 = 유지거리(0=AttackRange), param2 = 좌우이동속도배율
        /// - Retreat: param1 = 지속시간
        /// - Orbit: param1 = 반경
        /// - Burrow: param1 = 지속시간
        /// - Teleport: param1 = 쿨다운
        /// </summary>
        public float Param1 => _param1;
        public float Param2 => _param2;
        public GameObject EffectPrefab => _effectPrefab;

        /// <summary>
        /// 문자열에서 파싱 (예: "Hover:0.5:2", "Burst:2:3", "Linear")
        /// </summary>
        public static (MovementType type, float param1, float param2) Parse(string str)
        {
            if (string.IsNullOrEmpty(str))
                return (MovementType.Linear, 0f, 0f);

            string[] parts = str.Split(':');
            string typeName = parts[0].Trim();

            MovementType type = MovementType.Linear;
            float param1 = 0f;
            float param2 = 0f;

            // 타입 파싱
            if (System.Enum.TryParse(typeName, true, out MovementType parsedType))
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

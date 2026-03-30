using UnityEngine;
using System;

namespace DrillCorp.Bug.Behaviors
{
    /// <summary>
    /// 행동 전환 조건 타입
    /// </summary>
    public enum ConditionType
    {
        None,
        HpBelow,        // HP < N%
        HpAbove,        // HP > N%
        DistanceBelow,  // Distance < N
        DistanceAbove,  // Distance > N
        AfterAttack,    // 공격 직후
        HitCountAbove,  // 피격 횟수 > N
        TimeAbove,      // 스폰 후 N초 경과
        AllyDead        // 아군 사망
    }

    /// <summary>
    /// 행동 전환 조건 데이터
    /// </summary>
    [Serializable]
    public class BehaviorCondition
    {
        public ConditionType Type;
        public float Value;

        /// <summary>
        /// 조건 문자열 파싱 (예: "HP<30", "Distance>5", "AfterAttack")
        /// </summary>
        public static BehaviorCondition Parse(string conditionStr)
        {
            if (string.IsNullOrEmpty(conditionStr))
                return null;

            var condition = new BehaviorCondition();
            conditionStr = conditionStr.Trim();

            // AfterAttack
            if (conditionStr.Equals("AfterAttack", StringComparison.OrdinalIgnoreCase))
            {
                condition.Type = ConditionType.AfterAttack;
                return condition;
            }

            // AllyDead
            if (conditionStr.Equals("AllyDead", StringComparison.OrdinalIgnoreCase))
            {
                condition.Type = ConditionType.AllyDead;
                return condition;
            }

            // HP<30, HP>50
            if (conditionStr.StartsWith("HP", StringComparison.OrdinalIgnoreCase))
            {
                if (conditionStr.Contains("<"))
                {
                    condition.Type = ConditionType.HpBelow;
                    condition.Value = ParseValue(conditionStr, '<');
                }
                else if (conditionStr.Contains(">"))
                {
                    condition.Type = ConditionType.HpAbove;
                    condition.Value = ParseValue(conditionStr, '>');
                }
                return condition;
            }

            // Distance<3, Distance>5
            if (conditionStr.StartsWith("Distance", StringComparison.OrdinalIgnoreCase))
            {
                if (conditionStr.Contains("<"))
                {
                    condition.Type = ConditionType.DistanceBelow;
                    condition.Value = ParseValue(conditionStr, '<');
                }
                else if (conditionStr.Contains(">"))
                {
                    condition.Type = ConditionType.DistanceAbove;
                    condition.Value = ParseValue(conditionStr, '>');
                }
                return condition;
            }

            // HitCount>5
            if (conditionStr.StartsWith("HitCount", StringComparison.OrdinalIgnoreCase))
            {
                condition.Type = ConditionType.HitCountAbove;
                condition.Value = ParseValue(conditionStr, '>');
                return condition;
            }

            // Time>10
            if (conditionStr.StartsWith("Time", StringComparison.OrdinalIgnoreCase))
            {
                condition.Type = ConditionType.TimeAbove;
                condition.Value = ParseValue(conditionStr, '>');
                return condition;
            }

            return null;
        }

        private static float ParseValue(string str, char separator)
        {
            int index = str.IndexOf(separator);
            if (index < 0 || index >= str.Length - 1)
                return 0f;

            string valueStr = str.Substring(index + 1).Trim();
            if (float.TryParse(valueStr, out float value))
                return value;

            return 0f;
        }

        /// <summary>
        /// 조건 평가
        /// </summary>
        public bool Evaluate(BugController bug, Transform target)
        {
            switch (Type)
            {
                case ConditionType.None:
                    return false;

                case ConditionType.HpBelow:
                    return bug.HealthPercent < Value;

                case ConditionType.HpAbove:
                    return bug.HealthPercent > Value;

                case ConditionType.DistanceBelow:
                    if (target == null) return false;
                    return bug.GetDistanceTo(target) < Value;

                case ConditionType.DistanceAbove:
                    if (target == null) return false;
                    return bug.GetDistanceTo(target) > Value;

                case ConditionType.AfterAttack:
                    return bug.JustAttacked;

                case ConditionType.HitCountAbove:
                    return bug.HitCount > (int)Value;

                case ConditionType.TimeAbove:
                    return bug.AliveTime > Value;

                case ConditionType.AllyDead:
                    return bug.AllyJustDied;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// 조건부 행동 데이터
    /// </summary>
    [Serializable]
    public class ConditionalBehavior<T> where T : class, IBehavior
    {
        public BehaviorCondition Condition;
        public T Behavior;
        public float Duration; // 조건 충족 후 지속 시간 (0이면 조건 유지 동안)

        [NonSerialized]
        public float RemainingDuration;
    }
}

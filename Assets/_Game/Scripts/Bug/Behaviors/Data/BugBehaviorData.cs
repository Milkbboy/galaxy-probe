using UnityEngine;
using System;
using System.Collections.Generic;

namespace DrillCorp.Bug.Behaviors.Data
{
    /// <summary>
    /// 조건부 행동 설정
    /// </summary>
    [Serializable]
    public class ConditionalMovementData
    {
        public string Condition;        // "HP<30", "AfterAttack" 등
        public MovementBehaviorData Behavior;
        public float Duration;          // 지속 시간 (0이면 조건 유지 동안)

        // 런타임 파싱용 (SO 없이 문자열만 있을 때)
        [NonSerialized] public MovementType RuntimeType;
        [NonSerialized] public float RuntimeParam1;
        [NonSerialized] public float RuntimeParam2;
    }

    [Serializable]
    public class ConditionalAttackData
    {
        public string Condition;
        public AttackBehaviorData Behavior;

        [NonSerialized] public AttackType RuntimeType;
        [NonSerialized] public float RuntimeParam1;
        [NonSerialized] public float RuntimeParam2;
    }

    /// <summary>
    /// 벌레의 모든 행동을 조합하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "BugBehavior_New", menuName = "Drill-Corp/Bug Behaviors/Bug Behavior Set")]
    public class BugBehaviorData : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private MovementBehaviorData _defaultMovement;
        [SerializeField] private List<ConditionalMovementData> _conditionalMovements = new List<ConditionalMovementData>();

        [Header("Basic Attack")]
        [SerializeField] private AttackBehaviorData _defaultAttack;
        [SerializeField] private List<ConditionalAttackData> _conditionalAttacks = new List<ConditionalAttackData>();

        [Header("Skills")]
        [SerializeField] private List<SkillBehaviorData> _skills = new List<SkillBehaviorData>();

        [Header("Passives")]
        [SerializeField] private List<PassiveBehaviorData> _passives = new List<PassiveBehaviorData>();

        [Header("Triggers")]
        [SerializeField] private List<TriggerBehaviorData> _triggers = new List<TriggerBehaviorData>();

        // Properties
        public MovementBehaviorData DefaultMovement => _defaultMovement;
        public IReadOnlyList<ConditionalMovementData> ConditionalMovements => _conditionalMovements;

        public AttackBehaviorData DefaultAttack => _defaultAttack;
        public IReadOnlyList<ConditionalAttackData> ConditionalAttacks => _conditionalAttacks;

        public IReadOnlyList<SkillBehaviorData> Skills => _skills;
        public IReadOnlyList<PassiveBehaviorData> Passives => _passives;
        public IReadOnlyList<TriggerBehaviorData> Triggers => _triggers;

        // === 런타임 데이터 (Google Sheets Import 시 사용) ===

        [NonSerialized] private bool _useRuntimeData = false;
        [NonSerialized] private RuntimeBehaviorSet _runtimeData;

        public bool UseRuntimeData => _useRuntimeData;

        /// <summary>
        /// 런타임 데이터 설정 (SO 없이 직접 파라미터 사용 시)
        /// </summary>
        public void SetRuntimeData(RuntimeBehaviorSet data)
        {
            _useRuntimeData = true;
            _runtimeData = data;
        }

        public RuntimeBehaviorSet RuntimeData => _runtimeData;
    }

    /// <summary>
    /// 런타임 행동 데이터 (SO 참조 없이 직접 파라미터 사용)
    /// Google Sheets에서 Import 시 사용
    /// </summary>
    [Serializable]
    public class RuntimeBehaviorSet
    {
        // Movement
        public MovementType MovementType = MovementType.Linear;
        public float MovementParam1;
        public float MovementParam2;
        public List<RuntimeConditionalMovement> ConditionalMovements = new List<RuntimeConditionalMovement>();

        // Attack
        public AttackType AttackType = AttackType.Melee;
        public float AttackParam1;
        public float AttackParam2;
        public List<RuntimeConditionalAttack> ConditionalAttacks = new List<RuntimeConditionalAttack>();

        // Skills
        public List<RuntimeSkill> Skills = new List<RuntimeSkill>();

        // Passives
        public List<RuntimePassive> Passives = new List<RuntimePassive>();

        // Triggers
        public List<RuntimeTrigger> Triggers = new List<RuntimeTrigger>();
    }

    [Serializable]
    public class RuntimeConditionalMovement
    {
        public string Condition;
        public MovementType Type;
        public float Param1;
        public float Param2;
        public float Duration;
    }

    [Serializable]
    public class RuntimeConditionalAttack
    {
        public string Condition;
        public AttackType Type;
        public float Param1;
        public float Param2;
    }

    [Serializable]
    public class RuntimeSkill
    {
        public SkillType Type;
        public float Cooldown;
        public float Param1;
        public float Param2;
        public string StringParam;
    }

    [Serializable]
    public class RuntimePassive
    {
        public PassiveType Type;
        public float Param1;
        public float Param2;
    }

    [Serializable]
    public class RuntimeTrigger
    {
        public TriggerType Type;
        public float Param1;
        public float Param2;
        public float Param3;
        public string StringParam;
    }
}

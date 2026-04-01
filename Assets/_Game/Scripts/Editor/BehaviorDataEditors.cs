using UnityEngine;
using UnityEditor;
using DrillCorp.Bug.Behaviors.Data;

namespace DrillCorp.Editor
{
    /// <summary>
    /// MovementBehaviorData Custom Editor
    /// 타입에 따라 param1, param2 라벨을 동적으로 표시
    /// </summary>
    [CustomEditor(typeof(MovementBehaviorData))]
    public class MovementBehaviorDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _type;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _param1;
        private SerializedProperty _param2;
        private SerializedProperty _effectPrefab;

        private void OnEnable()
        {
            _type = serializedObject.FindProperty("_type");
            _displayName = serializedObject.FindProperty("_displayName");
            _description = serializedObject.FindProperty("_description");
            _param1 = serializedObject.FindProperty("_param1");
            _param2 = serializedObject.FindProperty("_param2");
            _effectPrefab = serializedObject.FindProperty("_effectPrefab");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Basic
            EditorGUILayout.LabelField("Basic", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_type);
            EditorGUILayout.PropertyField(_displayName);

            // Description - 자동 높이 조절
            EditorGUILayout.LabelField("Description");
            _description.stringValue = EditorGUILayout.TextArea(_description.stringValue,
                GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 3));

            EditorGUILayout.Space(10);

            // Parameters - 타입에 따라 라벨 변경
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

            var movementType = (MovementType)_type.enumValueIndex;
            var (param1Label, param2Label) = GetParamLabels(movementType);

            if (!string.IsNullOrEmpty(param1Label))
            {
                EditorGUILayout.PropertyField(_param1, new GUIContent($"Param1 - {param1Label}"));
            }
            else
            {
                EditorGUILayout.PropertyField(_param1, new GUIContent("Param1 (미사용)"));
            }

            if (!string.IsNullOrEmpty(param2Label))
            {
                EditorGUILayout.PropertyField(_param2, new GUIContent($"Param2 - {param2Label}"));
            }
            else
            {
                EditorGUILayout.PropertyField(_param2, new GUIContent("Param2 (미사용)"));
            }

            // Prefabs (Teleport 타입만)
            if (movementType == MovementType.Teleport)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_effectPrefab, new GUIContent("Effect Prefab (순간이동 이펙트)"));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private (string, string) GetParamLabels(MovementType type)
        {
            switch (type)
            {
                case MovementType.Linear:
                    return (null, null);

                case MovementType.Hover:
                    return ("높이", "주기 (초)");

                case MovementType.Burst:
                    return ("대기시간 (초)", "속도배율");

                case MovementType.Ranged:
                    return ("유지거리 (0=AttackRange)", "좌우이동 속도배율");

                case MovementType.Retreat:
                    return ("후퇴 지속시간 (초)", "후퇴 속도배율");

                case MovementType.SlowStart:
                    return ("시작 속도비율 (0~1)", "최대속도 도달시간 (초)");

                case MovementType.Orbit:
                    return ("궤도 반경 (0=AttackRange)", "초당 회전각도 (도)");

                case MovementType.Teleport:
                    return ("텔레포트 쿨다운 (초)", "텔레포트 거리");

                case MovementType.Burrow:
                    return ("잠복 지속시간 (초)", "잠복 속도배율");

                case MovementType.Dive:
                    return ("급강하 높이", "급강하 속도배율");

                default:
                    return (null, null);
            }
        }
    }

    /// <summary>
    /// AttackBehaviorData Custom Editor
    /// </summary>
    [CustomEditor(typeof(AttackBehaviorData))]
    public class AttackBehaviorDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _type;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _param1;
        private SerializedProperty _param2;
        private SerializedProperty _projectilePrefab;
        private SerializedProperty _hitVfxPrefab;

        private void OnEnable()
        {
            _type = serializedObject.FindProperty("_type");
            _displayName = serializedObject.FindProperty("_displayName");
            _description = serializedObject.FindProperty("_description");
            _param1 = serializedObject.FindProperty("_param1");
            _param2 = serializedObject.FindProperty("_param2");
            _projectilePrefab = serializedObject.FindProperty("_projectilePrefab");
            _hitVfxPrefab = serializedObject.FindProperty("_hitVfxPrefab");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Basic
            EditorGUILayout.LabelField("Basic", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_type);
            EditorGUILayout.PropertyField(_displayName);

            // Description - 자동 높이 조절
            EditorGUILayout.LabelField("Description");
            _description.stringValue = EditorGUILayout.TextArea(_description.stringValue,
                GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 3));

            EditorGUILayout.Space(10);

            // Parameters
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

            var attackType = (AttackType)_type.enumValueIndex;
            var (param1Label, param2Label) = GetParamLabels(attackType);

            if (!string.IsNullOrEmpty(param1Label))
            {
                EditorGUILayout.PropertyField(_param1, new GUIContent($"Param1 - {param1Label}"));
            }
            else
            {
                EditorGUILayout.PropertyField(_param1, new GUIContent("Param1 (미사용)"));
            }

            if (!string.IsNullOrEmpty(param2Label))
            {
                EditorGUILayout.PropertyField(_param2, new GUIContent($"Param2 - {param2Label}"));
            }
            else
            {
                EditorGUILayout.PropertyField(_param2, new GUIContent("Param2 (미사용)"));
            }

            // Prefabs
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
            if (attackType == AttackType.Projectile || attackType == AttackType.Spread ||
                attackType == AttackType.Homing)
            {
                EditorGUILayout.PropertyField(_projectilePrefab);
            }
            EditorGUILayout.PropertyField(_hitVfxPrefab);

            serializedObject.ApplyModifiedProperties();
        }

        private (string, string) GetParamLabels(AttackType type)
        {
            switch (type)
            {
                case AttackType.Melee:
                    return (null, null);

                case AttackType.Projectile:
                    return ("투사체 속도", null);

                case AttackType.Cleave:
                    return ("부채꼴 각도 (도)", null);

                case AttackType.Spread:
                    return ("투사체 개수", "확산 각도 (도)");

                case AttackType.Homing:
                    return ("투사체 속도", "유도력");

                case AttackType.Beam:
                    return ("빔 지속시간 (초)", "초당 데미지 배율");

                default:
                    return (null, null);
            }
        }
    }

    /// <summary>
    /// PassiveBehaviorData Custom Editor
    /// </summary>
    [CustomEditor(typeof(PassiveBehaviorData))]
    public class PassiveBehaviorDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _type;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _param1;
        private SerializedProperty _param2;

        private void OnEnable()
        {
            _type = serializedObject.FindProperty("_type");
            _displayName = serializedObject.FindProperty("_displayName");
            _description = serializedObject.FindProperty("_description");
            _param1 = serializedObject.FindProperty("_param1");
            _param2 = serializedObject.FindProperty("_param2");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Basic
            EditorGUILayout.LabelField("Basic", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_type);
            EditorGUILayout.PropertyField(_displayName);

            // Description - 자동 높이 조절
            EditorGUILayout.LabelField("Description");
            _description.stringValue = EditorGUILayout.TextArea(_description.stringValue,
                GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 3));

            EditorGUILayout.Space(10);

            // Parameters
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

            var passiveType = (PassiveType)_type.enumValueIndex;
            var (param1Label, param2Label) = GetParamLabels(passiveType);

            if (!string.IsNullOrEmpty(param1Label))
            {
                EditorGUILayout.PropertyField(_param1, new GUIContent($"Param1 - {param1Label}"));
            }
            else
            {
                EditorGUILayout.PropertyField(_param1, new GUIContent("Param1 (미사용)"));
            }

            if (!string.IsNullOrEmpty(param2Label))
            {
                EditorGUILayout.PropertyField(_param2, new GUIContent($"Param2 - {param2Label}"));
            }
            else
            {
                EditorGUILayout.PropertyField(_param2, new GUIContent("Param2 (미사용)"));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private (string, string) GetParamLabels(PassiveType type)
        {
            switch (type)
            {
                case PassiveType.Armor:
                    return ("데미지 감소량", null);

                case PassiveType.Dodge:
                    return ("회피 확률 (%)", null);

                case PassiveType.Shield:
                    return ("보호막 흡수량", "재생 쿨다운 (초)");

                case PassiveType.Regen:
                    return ("초당 회복량", null);

                case PassiveType.PoisonAttack:
                    return ("독 지속시간 (초)", "초당 데미지");

                case PassiveType.Lifesteal:
                    return ("흡혈 비율 (%)", null);

                case PassiveType.Reflect:
                    return ("반사 비율 (%)", null);

                case PassiveType.Fast:
                    return ("이속 증가 (%)", null);

                default:
                    return (null, null);
            }
        }
    }

    /// <summary>
    /// SkillBehaviorData Custom Editor
    /// </summary>
    [CustomEditor(typeof(SkillBehaviorData))]
    public class SkillBehaviorDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _type;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _cooldown;
        private SerializedProperty _param1;
        private SerializedProperty _param2;
        private SerializedProperty _stringParam;
        private SerializedProperty _spawnPrefab;
        private SerializedProperty _effectPrefab;

        private void OnEnable()
        {
            _type = serializedObject.FindProperty("_type");
            _displayName = serializedObject.FindProperty("_displayName");
            _description = serializedObject.FindProperty("_description");
            _cooldown = serializedObject.FindProperty("_cooldown");
            _param1 = serializedObject.FindProperty("_param1");
            _param2 = serializedObject.FindProperty("_param2");
            _stringParam = serializedObject.FindProperty("_stringParam");
            _spawnPrefab = serializedObject.FindProperty("_spawnPrefab");
            _effectPrefab = serializedObject.FindProperty("_effectPrefab");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Basic
            EditorGUILayout.LabelField("Basic", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_type);
            EditorGUILayout.PropertyField(_displayName);

            // Description - 자동 높이 조절
            EditorGUILayout.LabelField("Description");
            _description.stringValue = EditorGUILayout.TextArea(_description.stringValue,
                GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 3));

            EditorGUILayout.Space(10);

            // Common
            EditorGUILayout.LabelField("Common", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_cooldown);

            EditorGUILayout.Space(10);

            // Parameters
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

            var skillType = (SkillType)_type.enumValueIndex;
            var (param1Label, param2Label, stringLabel) = GetParamLabels(skillType);

            if (!string.IsNullOrEmpty(param1Label))
            {
                EditorGUILayout.PropertyField(_param1, new GUIContent($"Param1 - {param1Label}"));
            }
            else
            {
                EditorGUILayout.PropertyField(_param1, new GUIContent("Param1 (미사용)"));
            }

            if (!string.IsNullOrEmpty(param2Label))
            {
                EditorGUILayout.PropertyField(_param2, new GUIContent($"Param2 - {param2Label}"));
            }
            else
            {
                EditorGUILayout.PropertyField(_param2, new GUIContent("Param2 (미사용)"));
            }

            // String Param
            EditorGUILayout.PropertyField(_stringParam, !string.IsNullOrEmpty(stringLabel)
                ? new GUIContent($"StringParam - {stringLabel}")
                : new GUIContent("StringParam (미사용)"));

            // Prefabs
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
            if (skillType == SkillType.Spawn)
            {
                EditorGUILayout.PropertyField(_spawnPrefab, new GUIContent("Spawn Prefab (소환할 버그)"));
            }
            EditorGUILayout.PropertyField(_effectPrefab);

            serializedObject.ApplyModifiedProperties();
        }

        private (string, string, string) GetParamLabels(SkillType type)
        {
            switch (type)
            {
                case SkillType.Nova:
                    return ("폭발 반경", "데미지 배율", null);

                case SkillType.Spawn:
                    return ("소환 개수", "소환 거리", null);

                case SkillType.BuffAlly:
                    return ("버프 배율", "버프 범위", null);

                case SkillType.HealAlly:
                    return ("회복량", "회복 범위", null);

                case SkillType.Slow:
                    return ("감속 비율 (%)", "감속 범위", null);

                case SkillType.Stun:
                    return ("기절 시간 (초)", "기절 범위", null);

                default:
                    return (null, null, null);
            }
        }
    }

    /// <summary>
    /// TriggerBehaviorData Custom Editor
    /// </summary>
    [CustomEditor(typeof(TriggerBehaviorData))]
    public class TriggerBehaviorDataEditor : UnityEditor.Editor
    {
        private SerializedProperty _type;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _param1;
        private SerializedProperty _param2;
        private SerializedProperty _param3;
        private SerializedProperty _stringParam;
        private SerializedProperty _effectPrefab;

        private void OnEnable()
        {
            _type = serializedObject.FindProperty("_type");
            _displayName = serializedObject.FindProperty("_displayName");
            _description = serializedObject.FindProperty("_description");
            _param1 = serializedObject.FindProperty("_param1");
            _param2 = serializedObject.FindProperty("_param2");
            _param3 = serializedObject.FindProperty("_param3");
            _stringParam = serializedObject.FindProperty("_stringParam");
            _effectPrefab = serializedObject.FindProperty("_effectPrefab");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Basic
            EditorGUILayout.LabelField("Basic", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_type);
            EditorGUILayout.PropertyField(_displayName);

            // Description - 자동 높이 조절
            EditorGUILayout.LabelField("Description");
            _description.stringValue = EditorGUILayout.TextArea(_description.stringValue,
                GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 3));

            EditorGUILayout.Space(10);

            // Parameters
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

            var triggerType = (TriggerType)_type.enumValueIndex;
            var (param1Label, param2Label, param3Label, stringLabel) = GetParamLabels(triggerType);

            if (!string.IsNullOrEmpty(param1Label))
            {
                EditorGUILayout.PropertyField(_param1, new GUIContent($"Param1 - {param1Label}"));
            }
            else
            {
                EditorGUILayout.PropertyField(_param1, new GUIContent("Param1 (미사용)"));
            }

            if (!string.IsNullOrEmpty(param2Label))
            {
                EditorGUILayout.PropertyField(_param2, new GUIContent($"Param2 - {param2Label}"));
            }
            else
            {
                EditorGUILayout.PropertyField(_param2, new GUIContent("Param2 (미사용)"));
            }

            if (!string.IsNullOrEmpty(param3Label))
            {
                EditorGUILayout.PropertyField(_param3, new GUIContent($"Param3 - {param3Label}"));
            }
            else
            {
                EditorGUILayout.PropertyField(_param3, new GUIContent("Param3 (미사용)"));
            }

            // String Param
            EditorGUILayout.PropertyField(_stringParam, !string.IsNullOrEmpty(stringLabel)
                ? new GUIContent($"StringParam - {stringLabel}")
                : new GUIContent("StringParam (미사용)"));

            // Prefabs
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_effectPrefab);

            serializedObject.ApplyModifiedProperties();
        }

        private (string, string, string, string) GetParamLabels(TriggerType type)
        {
            switch (type)
            {
                case TriggerType.Enrage:
                    return ("HP 임계값 (%)", "공격력 배율", "이속 배율", null);

                case TriggerType.ExplodeOnDeath:
                    return ("폭발 범위", "폭발 데미지", null, null);

                case TriggerType.SplitOnDeath:
                    return ("분열 개수", "분열체 체력비율 (%)", null, null);

                case TriggerType.Transform:
                    return ("HP 임계값 (%)", null, null, "변신 버그 ID");

                case TriggerType.Revive:
                    return ("부활 횟수", "부활 HP 비율 (%)", null, null);

                default:
                    return (null, null, null, null);
            }
        }
    }
}

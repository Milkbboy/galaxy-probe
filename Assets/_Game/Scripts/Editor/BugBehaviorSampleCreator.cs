using UnityEngine;
using UnityEditor;
using System.IO;
using DrillCorp.Bug.Behaviors.Data;

namespace DrillCorp.Editor
{
    /// <summary>
    /// Bug Behavior 샘플 ScriptableObject 생성 도구
    /// </summary>
    public static class BugBehaviorSampleCreator
    {
        private const string BasePath = "Assets/_Game/Data/BugBehaviors";

        [MenuItem("Tools/Bug Behaviors/샘플 전체 생성", priority = 100)]
        public static void CreateAllSamples()
        {
            CreateFolders();
            CreateMovementSamples();
            CreateAttackSamples();
            CreatePassiveSamples();
            CreateSkillSamples();
            CreateTriggerSamples();
            CreateBugBehaviorSetSamples();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BugBehaviorSampleCreator] 모든 샘플 생성 완료!");
            EditorUtility.DisplayDialog("완료", "Bug Behavior 샘플이 모두 생성되었습니다.\n\n경로: " + BasePath, "확인");
        }

        [MenuItem("Tools/Bug Behaviors/Movement 샘플 생성")]
        public static void CreateMovementSamples()
        {
            CreateFolders();
            string folder = BasePath + "/Movement";

            // Linear
            var linear = CreateAsset<MovementBehaviorData>(folder, "Movement_Linear");
            SetPrivateField(linear, "_type", MovementType.Linear);
            SetPrivateField(linear, "_displayName", "직선 이동");
            SetPrivateField(linear, "_description", "타겟을 향해 직선으로 이동합니다.\n가장 기본적인 이동 패턴으로, MoveSpeed에 따라 일정한 속도로 접근합니다.");

            // Hover
            var hover = CreateAsset<MovementBehaviorData>(folder, "Movement_Hover");
            SetPrivateField(hover, "_type", MovementType.Hover);
            SetPrivateField(hover, "_displayName", "부유 이동");
            SetPrivateField(hover, "_description", "공중에서 위아래로 떠다니며 타겟을 향해 이동합니다.\n비행형 벌레에 적합하며, 사인파로 Y축 높이가 변합니다.");
            SetPrivateField(hover, "_param1", 0.5f);
            SetPrivateField(hover, "_param2", 2f);

            // Burst
            var burst = CreateAsset<MovementBehaviorData>(folder, "Movement_Burst");
            SetPrivateField(burst, "_type", MovementType.Burst);
            SetPrivateField(burst, "_displayName", "돌진 이동");
            SetPrivateField(burst, "_description", "일정 시간 대기 후 빠른 속도로 돌진합니다.\n대기 중에는 정지, 돌진 중에는 속도배율만큼 빠르게 이동합니다.");
            SetPrivateField(burst, "_param1", 2f);
            SetPrivateField(burst, "_param2", 3f);

            // Ranged
            var ranged = CreateAsset<MovementBehaviorData>(folder, "Movement_Ranged");
            SetPrivateField(ranged, "_type", MovementType.Ranged);
            SetPrivateField(ranged, "_displayName", "원거리 이동");
            SetPrivateField(ranged, "_description", "타겟과 일정 거리를 유지하며 좌우로 이동합니다.\n원거리 공격 벌레에 적합합니다. 거리가 가까우면 후퇴, 멀면 접근합니다.");
            SetPrivateField(ranged, "_param1", 0f);
            SetPrivateField(ranged, "_param2", 0.5f);

            // Retreat (Phase 2)
            var retreat = CreateAsset<MovementBehaviorData>(folder, "Movement_Retreat");
            SetPrivateField(retreat, "_type", MovementType.Retreat);
            SetPrivateField(retreat, "_displayName", "후퇴 이동");
            SetPrivateField(retreat, "_description", "공격 후 일정 시간 동안 뒤로 물러납니다.\n근접 공격 후 도주하는 패턴에 적합합니다.");
            SetPrivateField(retreat, "_param1", 1f);
            SetPrivateField(retreat, "_param2", 1.5f);

            // SlowStart (Phase 2)
            var slowStart = CreateAsset<MovementBehaviorData>(folder, "Movement_SlowStart");
            SetPrivateField(slowStart, "_type", MovementType.SlowStart);
            SetPrivateField(slowStart, "_displayName", "점진 가속");
            SetPrivateField(slowStart, "_description", "스폰 후 천천히 속도가 증가합니다.\n시작 속도비율에서 시작해 도달시간 동안 최대 속도까지 가속합니다.");
            SetPrivateField(slowStart, "_param1", 0.2f);
            SetPrivateField(slowStart, "_param2", 2f);

            // Orbit (Phase 2)
            var orbit = CreateAsset<MovementBehaviorData>(folder, "Movement_Orbit");
            SetPrivateField(orbit, "_type", MovementType.Orbit);
            SetPrivateField(orbit, "_displayName", "궤도 이동");
            SetPrivateField(orbit, "_description", "타겟 주위를 원형으로 맴돌며 이동합니다.\n먼저 궤도까지 접근한 후, 일정 각속도로 회전합니다.");
            SetPrivateField(orbit, "_param1", 0f);
            SetPrivateField(orbit, "_param2", 60f);

            // Teleport (Phase 3)
            var teleport = CreateAsset<MovementBehaviorData>(folder, "Movement_Teleport");
            SetPrivateField(teleport, "_type", MovementType.Teleport);
            SetPrivateField(teleport, "_displayName", "순간이동");
            SetPrivateField(teleport, "_description", "일정 주기로 타겟 방향으로 순간이동합니다.\n쿨다운 중에는 정지하고 타겟 방향만 바라봅니다.\n공격 범위 내에서는 텔레포트하지 않습니다.");
            SetPrivateField(teleport, "_param1", 3f);
            SetPrivateField(teleport, "_param2", 3f);

            Debug.Log("[BugBehaviorSampleCreator] Movement 샘플 생성 완료");
        }

        [MenuItem("Tools/Bug Behaviors/Attack 샘플 생성")]
        public static void CreateAttackSamples()
        {
            CreateFolders();
            string folder = BasePath + "/Attack";

            // Melee
            var melee = CreateAsset<AttackBehaviorData>(folder, "Attack_Melee");
            SetPrivateField(melee, "_type", AttackType.Melee);
            SetPrivateField(melee, "_displayName", "근접 공격");
            SetPrivateField(melee, "_description", "공격 범위 내 타겟에게 직접 데미지를 줍니다.\n가장 기본적인 공격 패턴으로, AttackDamage만큼 데미지를 입힙니다.");

            // Projectile
            var projectile = CreateAsset<AttackBehaviorData>(folder, "Attack_Projectile");
            SetPrivateField(projectile, "_type", AttackType.Projectile);
            SetPrivateField(projectile, "_displayName", "원거리 공격");
            SetPrivateField(projectile, "_description", "타겟 방향으로 투사체를 발사합니다.\n투사체는 직선으로 날아가며, 충돌 시 AttackDamage만큼 데미지를 입힙니다.\n5초 후 자동 소멸됩니다.");
            SetPrivateField(projectile, "_param1", 10f);

            // Cleave (Phase 2)
            var cleave = CreateAsset<AttackBehaviorData>(folder, "Attack_Cleave");
            SetPrivateField(cleave, "_type", AttackType.Cleave);
            SetPrivateField(cleave, "_displayName", "범위 공격");
            SetPrivateField(cleave, "_description", "전방 부채꼴 범위 내 모든 대상에게 데미지를 줍니다.\n공격 범위(AttackRange)와 부채꼴 각도로 범위가 결정됩니다.\nLineRenderer로 범위가 표시됩니다.");
            SetPrivateField(cleave, "_param1", 90f);

            // Spread (Phase 2)
            var spread = CreateAsset<AttackBehaviorData>(folder, "Attack_Spread");
            SetPrivateField(spread, "_type", AttackType.Spread);
            SetPrivateField(spread, "_displayName", "다발 공격");
            SetPrivateField(spread, "_description", "여러 발의 투사체를 부채꼴로 발사합니다.\n각 투사체는 AttackDamage만큼 데미지를 입히며, 투사체 개수만큼 총 데미지가 증가할 수 있습니다.");
            SetPrivateField(spread, "_param1", 5f);
            SetPrivateField(spread, "_param2", 60f);

            // Beam (Phase 3)
            var beam = CreateAsset<AttackBehaviorData>(folder, "Attack_Beam");
            SetPrivateField(beam, "_type", AttackType.Beam);
            SetPrivateField(beam, "_displayName", "레이저 공격");
            SetPrivateField(beam, "_description", "타겟에게 지속적인 레이저 빔을 발사합니다.\n빔이 활성화된 동안 틱마다 데미지를 주며, 빔은 타겟을 추적합니다.\n빔 VFX가 없으면 붉은색 LineRenderer로 표시됩니다.");
            SetPrivateField(beam, "_param1", 2f);
            SetPrivateField(beam, "_param2", 0.5f);

            Debug.Log("[BugBehaviorSampleCreator] Attack 샘플 생성 완료");
        }

        [MenuItem("Tools/Bug Behaviors/Passive 샘플 생성")]
        public static void CreatePassiveSamples()
        {
            CreateFolders();
            string folder = BasePath + "/Passive";

            // Armor
            var armor = CreateAsset<PassiveBehaviorData>(folder, "Passive_Armor");
            SetPrivateField(armor, "_type", PassiveType.Armor);
            SetPrivateField(armor, "_displayName", "방어력");
            SetPrivateField(armor, "_description", "받는 데미지를 고정값만큼 감소시킵니다.\n최종 데미지 = 원래 데미지 - 감소량 (최소 1)");
            SetPrivateField(armor, "_param1", 5f);

            // Dodge
            var dodge = CreateAsset<PassiveBehaviorData>(folder, "Passive_Dodge");
            SetPrivateField(dodge, "_type", PassiveType.Dodge);
            SetPrivateField(dodge, "_displayName", "회피");
            SetPrivateField(dodge, "_description", "확률적으로 공격을 완전히 회피합니다.\n회피 성공 시 데미지 0, 회피 이펙트가 재생됩니다.");
            SetPrivateField(dodge, "_param1", 20f);

            // Shield (Phase 2)
            var shield = CreateAsset<PassiveBehaviorData>(folder, "Passive_Shield");
            SetPrivateField(shield, "_type", PassiveType.Shield);
            SetPrivateField(shield, "_displayName", "보호막");
            SetPrivateField(shield, "_description", "HP 대신 데미지를 흡수하는 보호막을 가집니다.\n보호막이 파괴되면 쿨다운 후 자동 재생됩니다.\n보호막 활성화 시 파란색 틴트가 적용됩니다.");
            SetPrivateField(shield, "_param1", 50f);
            SetPrivateField(shield, "_param2", 30f);

            // Regen (Phase 2)
            var regen = CreateAsset<PassiveBehaviorData>(folder, "Passive_Regen");
            SetPrivateField(regen, "_type", PassiveType.Regen);
            SetPrivateField(regen, "_displayName", "재생");
            SetPrivateField(regen, "_description", "매 초마다 체력을 회복합니다.\nMaxHealth를 초과하여 회복하지 않습니다.");
            SetPrivateField(regen, "_param1", 2f);

            // PoisonAttack (Phase 2)
            var poison = CreateAsset<PassiveBehaviorData>(folder, "Passive_PoisonAttack");
            SetPrivateField(poison, "_type", PassiveType.PoisonAttack);
            SetPrivateField(poison, "_displayName", "독 공격");
            SetPrivateField(poison, "_description", "공격 시 대상에게 독 효과를 적용합니다.\n독은 지속시간 동안 매 초 데미지를 주며, 대상이 녹색으로 변합니다.\nPoisonEffect 컴포넌트가 대상에 추가됩니다.");
            SetPrivateField(poison, "_param1", 3f);
            SetPrivateField(poison, "_param2", 5f);

            // Burrow (Phase 3)
            var burrow = CreateAsset<PassiveBehaviorData>(folder, "Passive_Burrow");
            SetPrivateField(burrow, "_type", PassiveType.Burrow);
            SetPrivateField(burrow, "_displayName", "땅속 숨기");
            SetPrivateField(burrow, "_description", "Trigger에 의해 발동되면 땅속으로 숨습니다.\n숨어있는 동안 무적 상태가 되며, 반투명으로 표시됩니다.\n지속시간이 지나면 다시 출현합니다.");
            SetPrivateField(burrow, "_param1", 2f);
            SetPrivateField(burrow, "_param2", 0.3f);

            Debug.Log("[BugBehaviorSampleCreator] Passive 샘플 생성 완료");
        }

        [MenuItem("Tools/Bug Behaviors/Skill 샘플 생성")]
        public static void CreateSkillSamples()
        {
            CreateFolders();
            string folder = BasePath + "/Skill";

            // Nova (Phase 2)
            var nova = CreateAsset<SkillBehaviorData>(folder, "Skill_Nova");
            SetPrivateField(nova, "_type", SkillType.Nova);
            SetPrivateField(nova, "_displayName", "폭발");
            SetPrivateField(nova, "_description", "자신을 중심으로 전방향 폭발을 일으킵니다.\n폭발 범위 내 모든 대상(벌레 제외)에게 데미지를 줍니다.\n원형 범위 인디케이터가 표시됩니다.");
            SetPrivateField(nova, "_cooldown", 15f);

            // Spawn (Phase 2)
            var spawn = CreateAsset<SkillBehaviorData>(folder, "Skill_Spawn");
            SetPrivateField(spawn, "_type", SkillType.Spawn);
            SetPrivateField(spawn, "_displayName", "소환");
            SetPrivateField(spawn, "_description", "졸개 벌레를 소환합니다.\n소환 위치는 소환 개수에 따라 좌/우/후방에 배치됩니다.\nSpawnPrefab에 소환할 버그 프리펩을 설정하세요.");
            SetPrivateField(spawn, "_cooldown", 10f);
            SetPrivateField(spawn, "_param1", 2f);
            SetPrivateField(spawn, "_stringParam", "Beetle");

            // BuffAlly (Phase 3) - Aura 방식
            var buff = CreateAsset<SkillBehaviorData>(folder, "Skill_BuffAlly");
            SetPrivateField(buff, "_type", SkillType.BuffAlly);
            SetPrivateField(buff, "_displayName", "아군 강화");
            SetPrivateField(buff, "_description", "주변 아군 벌레의 공격력과 이동속도를 증가시킵니다.\n범위 내에 있는 동안 버프가 적용되고, 범위를 벗어나면 해제됩니다.\n황금색 원형 인디케이터가 표시됩니다.");
            SetPrivateField(buff, "_cooldown", 1.2f);  // 이속 배율로 사용
            SetPrivateField(buff, "_param1", 5f);     // 범위
            SetPrivateField(buff, "_param2", 1.3f);   // 공격력 배율

            // HealAlly (Phase 3) - Aura 방식
            var heal = CreateAsset<SkillBehaviorData>(folder, "Skill_HealAlly");
            SetPrivateField(heal, "_type", SkillType.HealAlly);
            SetPrivateField(heal, "_displayName", "아군 회복");
            SetPrivateField(heal, "_description", "주변 아군 벌레의 체력을 주기적으로 회복시킵니다.\n범위 내에 있는 아군을 회복 주기마다 치유합니다.\n녹색 원형 인디케이터가 표시됩니다.");
            SetPrivateField(heal, "_cooldown", 1f);   // 회복 주기 (초)
            SetPrivateField(heal, "_param1", 5f);    // 범위
            SetPrivateField(heal, "_param2", 10f);   // 회복량 (회복 주기마다)

            Debug.Log("[BugBehaviorSampleCreator] Skill 샘플 생성 완료");
        }

        [MenuItem("Tools/Bug Behaviors/Trigger 샘플 생성")]
        public static void CreateTriggerSamples()
        {
            CreateFolders();
            string folder = BasePath + "/Trigger";

            // Enrage (Phase 2)
            var enrage = CreateAsset<TriggerBehaviorData>(folder, "Trigger_Enrage");
            SetPrivateField(enrage, "_type", TriggerType.Enrage);
            SetPrivateField(enrage, "_displayName", "광폭화");
            SetPrivateField(enrage, "_description", "HP가 임계값 이하로 떨어지면 광폭화 상태가 됩니다.\n광폭화 시 공격력과 이동속도가 증가하며, 붉은색 틴트가 적용됩니다.\n한번 발동되면 사망할 때까지 유지됩니다.");
            SetPrivateField(enrage, "_param1", 30f);
            SetPrivateField(enrage, "_param2", 2f);

            // ExplodeOnDeath (Phase 2)
            var explode = CreateAsset<TriggerBehaviorData>(folder, "Trigger_ExplodeOnDeath");
            SetPrivateField(explode, "_type", TriggerType.ExplodeOnDeath);
            SetPrivateField(explode, "_displayName", "사망 폭발");
            SetPrivateField(explode, "_description", "사망 시 폭발하여 주변에 데미지를 줍니다.\n폭발 범위 내 모든 IDamageable 대상(다른 버그 제외)에게 데미지를 입힙니다.\n폭발 이펙트가 재생됩니다.");
            SetPrivateField(explode, "_param1", 3f);
            SetPrivateField(explode, "_param2", 50f);

            // SplitOnDeath (Phase 3)
            var split = CreateAsset<TriggerBehaviorData>(folder, "Trigger_SplitOnDeath");
            SetPrivateField(split, "_type", TriggerType.SplitOnDeath);
            SetPrivateField(split, "_displayName", "분열");
            SetPrivateField(split, "_description", "사망 시 작은 벌레로 분열합니다.\n분열 개수만큼 SpawnPrefab이 소환되며, 체력비율만큼의 HP를 가집니다.");
            SetPrivateField(split, "_param1", 3f);
            SetPrivateField(split, "_stringParam", "MiniBeetle");

            // PanicBurrow (Phase 3)
            var panicBurrow = CreateAsset<TriggerBehaviorData>(folder, "Trigger_PanicBurrow");
            SetPrivateField(panicBurrow, "_type", TriggerType.PanicBurrow);
            SetPrivateField(panicBurrow, "_displayName", "위협 회피");
            SetPrivateField(panicBurrow, "_description", "HP가 임계값 이하일 때 피격 시 땅속으로 숨습니다.\nBurrow 패시브가 필요합니다.\n쿨다운 동안 재발동되지 않습니다.");
            SetPrivateField(panicBurrow, "_param1", 50f);
            SetPrivateField(panicBurrow, "_param2", 5f);

            Debug.Log("[BugBehaviorSampleCreator] Trigger 샘플 생성 완료");
        }

        [MenuItem("Tools/Bug Behaviors/BugBehavior Set 샘플 생성")]
        public static void CreateBugBehaviorSetSamples()
        {
            CreateFolders();
            string folder = BasePath;

            // === Beetle (기본) ===
            var beetle = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Beetle");
            // DefaultMovement, DefaultAttack은 None으로 두면 BugController가 기본값 사용

            // === Fly (부유) ===
            var fly = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Fly");
            var flyMovement = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>(BasePath + "/Movement/Movement_Hover.asset");
            SetPrivateField(fly, "_defaultMovement", flyMovement);

            // === Tank (방어형) ===
            var tank = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Tank");
            var armorPassive = AssetDatabase.LoadAssetAtPath<PassiveBehaviorData>(BasePath + "/Passive/Passive_Armor.asset");
            if (armorPassive != null)
            {
                var passives = new System.Collections.Generic.List<PassiveBehaviorData> { armorPassive };
                SetPrivateField(tank, "_passives", passives);
            }

            // === Spitter (원거리) ===
            var spitter = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Spitter");
            var projectileAttack = AssetDatabase.LoadAssetAtPath<AttackBehaviorData>(BasePath + "/Attack/Attack_Projectile.asset");
            SetPrivateField(spitter, "_defaultAttack", projectileAttack);

            // === Bomber (자폭) ===
            var bomber = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Bomber");
            var burstMovement = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>(BasePath + "/Movement/Movement_Burst.asset");
            SetPrivateField(bomber, "_defaultMovement", burstMovement);
            var explodeTrigger = AssetDatabase.LoadAssetAtPath<TriggerBehaviorData>(BasePath + "/Trigger/Trigger_ExplodeOnDeath.asset");
            if (explodeTrigger != null)
            {
                var triggers = new System.Collections.Generic.List<TriggerBehaviorData> { explodeTrigger };
                SetPrivateField(bomber, "_triggers", triggers);
            }

            Debug.Log("[BugBehaviorSampleCreator] BugBehavior Set 샘플 생성 완료");
        }

        private static void CreateFolders()
        {
            CreateFolderIfNotExists("Assets/_Game/Data");
            CreateFolderIfNotExists(BasePath);
            CreateFolderIfNotExists(BasePath + "/Movement");
            CreateFolderIfNotExists(BasePath + "/Attack");
            CreateFolderIfNotExists(BasePath + "/Passive");
            CreateFolderIfNotExists(BasePath + "/Skill");
            CreateFolderIfNotExists(BasePath + "/Trigger");
        }

        private static void CreateFolderIfNotExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static T CreateAsset<T>(string folder, string name) where T : ScriptableObject
        {
            string path = $"{folder}/{name}.asset";

            // 이미 존재하면 로드
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            // 새로 생성
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
                EditorUtility.SetDirty(obj as Object);
            }
        }
    }
}

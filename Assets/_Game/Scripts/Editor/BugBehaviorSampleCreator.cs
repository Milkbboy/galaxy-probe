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
            SetPrivateField(linear, "_description", "타겟을 향해 직선으로 이동합니다.");

            // Hover
            var hover = CreateAsset<MovementBehaviorData>(folder, "Movement_Hover");
            SetPrivateField(hover, "_type", MovementType.Hover);
            SetPrivateField(hover, "_displayName", "부유 이동");
            SetPrivateField(hover, "_description", "위아래로 떠다니며 이동합니다.");
            SetPrivateField(hover, "_param1", 0.5f);  // 높이
            SetPrivateField(hover, "_param2", 2f);    // 주기

            // Burst
            var burst = CreateAsset<MovementBehaviorData>(folder, "Movement_Burst");
            SetPrivateField(burst, "_type", MovementType.Burst);
            SetPrivateField(burst, "_displayName", "돌진 이동");
            SetPrivateField(burst, "_description", "잠시 멈췄다가 빠르게 돌진합니다.");
            SetPrivateField(burst, "_param1", 2f);    // 대기시간
            SetPrivateField(burst, "_param2", 3f);    // 속도배율

            // Ranged
            var ranged = CreateAsset<MovementBehaviorData>(folder, "Movement_Ranged");
            SetPrivateField(ranged, "_type", MovementType.Ranged);
            SetPrivateField(ranged, "_displayName", "원거리 이동");
            SetPrivateField(ranged, "_description", "사거리를 유지하며 좌우로 이동합니다.");
            SetPrivateField(ranged, "_param1", 0f);    // 유지거리 (0 = AttackRange 사용)
            SetPrivateField(ranged, "_param2", 0.5f);  // 좌우 이동 속도 배율

            // Retreat (Phase 2)
            var retreat = CreateAsset<MovementBehaviorData>(folder, "Movement_Retreat");
            SetPrivateField(retreat, "_type", MovementType.Retreat);
            SetPrivateField(retreat, "_displayName", "후퇴 이동");
            SetPrivateField(retreat, "_description", "공격 후 일정 시간 후퇴합니다.");
            SetPrivateField(retreat, "_param1", 1f);    // 후퇴 지속시간
            SetPrivateField(retreat, "_param2", 1.5f);  // 후퇴 속도배율

            // SlowStart (Phase 2)
            var slowStart = CreateAsset<MovementBehaviorData>(folder, "Movement_SlowStart");
            SetPrivateField(slowStart, "_type", MovementType.SlowStart);
            SetPrivateField(slowStart, "_displayName", "점진 가속");
            SetPrivateField(slowStart, "_description", "천천히 속도가 증가합니다.");
            SetPrivateField(slowStart, "_param1", 0.2f);  // 시작 속도 비율
            SetPrivateField(slowStart, "_param2", 2f);    // 최대속도 도달 시간

            // Orbit (Phase 2)
            var orbit = CreateAsset<MovementBehaviorData>(folder, "Movement_Orbit");
            SetPrivateField(orbit, "_type", MovementType.Orbit);
            SetPrivateField(orbit, "_displayName", "궤도 이동");
            SetPrivateField(orbit, "_description", "타겟 주위를 맴돌며 이동합니다.");
            SetPrivateField(orbit, "_param1", 0f);    // 궤도 반경 (0 = AttackRange 사용)
            SetPrivateField(orbit, "_param2", 60f);   // 초당 회전 각도 (60도 = 6초에 한바퀴)

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
            SetPrivateField(melee, "_description", "가까이 다가가서 공격합니다.");

            // Projectile
            var projectile = CreateAsset<AttackBehaviorData>(folder, "Attack_Projectile");
            SetPrivateField(projectile, "_type", AttackType.Projectile);
            SetPrivateField(projectile, "_displayName", "원거리 공격");
            SetPrivateField(projectile, "_description", "투사체를 발사합니다.");
            SetPrivateField(projectile, "_param1", 10f);  // 속도

            // Cleave (Phase 2)
            var cleave = CreateAsset<AttackBehaviorData>(folder, "Attack_Cleave");
            SetPrivateField(cleave, "_type", AttackType.Cleave);
            SetPrivateField(cleave, "_displayName", "범위 공격");
            SetPrivateField(cleave, "_description", "부채꼴 범위로 공격합니다.");
            SetPrivateField(cleave, "_param1", 90f);  // 각도

            // Spread (Phase 2)
            var spread = CreateAsset<AttackBehaviorData>(folder, "Attack_Spread");
            SetPrivateField(spread, "_type", AttackType.Spread);
            SetPrivateField(spread, "_displayName", "다발 공격");
            SetPrivateField(spread, "_description", "여러 발의 투사체를 발사합니다.");
            SetPrivateField(spread, "_param1", 5f);   // 발수
            SetPrivateField(spread, "_param2", 60f);  // 각도

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
            SetPrivateField(armor, "_description", "받는 데미지를 고정값만큼 감소시킵니다.");
            SetPrivateField(armor, "_param1", 5f);  // 감소량

            // Dodge
            var dodge = CreateAsset<PassiveBehaviorData>(folder, "Passive_Dodge");
            SetPrivateField(dodge, "_type", PassiveType.Dodge);
            SetPrivateField(dodge, "_displayName", "회피");
            SetPrivateField(dodge, "_description", "확률적으로 공격을 회피합니다.");
            SetPrivateField(dodge, "_param1", 20f);  // 확률%

            // Shield (Phase 2)
            var shield = CreateAsset<PassiveBehaviorData>(folder, "Passive_Shield");
            SetPrivateField(shield, "_type", PassiveType.Shield);
            SetPrivateField(shield, "_displayName", "보호막");
            SetPrivateField(shield, "_description", "데미지를 흡수하는 보호막을 가집니다.");
            SetPrivateField(shield, "_param1", 50f);  // 흡수량
            SetPrivateField(shield, "_param2", 30f);  // 재생쿨

            // Regen (Phase 2)
            var regen = CreateAsset<PassiveBehaviorData>(folder, "Passive_Regen");
            SetPrivateField(regen, "_type", PassiveType.Regen);
            SetPrivateField(regen, "_displayName", "재생");
            SetPrivateField(regen, "_description", "초당 체력을 회복합니다.");
            SetPrivateField(regen, "_param1", 2f);  // 초당회복

            // PoisonAttack (Phase 2)
            var poison = CreateAsset<PassiveBehaviorData>(folder, "Passive_PoisonAttack");
            SetPrivateField(poison, "_type", PassiveType.PoisonAttack);
            SetPrivateField(poison, "_displayName", "독 공격");
            SetPrivateField(poison, "_description", "공격 시 대상에게 독을 적용합니다.");
            SetPrivateField(poison, "_param1", 3f);  // 지속시간
            SetPrivateField(poison, "_param2", 5f);  // 초당 데미지

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
            SetPrivateField(nova, "_description", "전방향으로 폭발합니다.");
            SetPrivateField(nova, "_cooldown", 15f);

            // Spawn (Phase 2)
            var spawn = CreateAsset<SkillBehaviorData>(folder, "Skill_Spawn");
            SetPrivateField(spawn, "_type", SkillType.Spawn);
            SetPrivateField(spawn, "_displayName", "소환");
            SetPrivateField(spawn, "_description", "졸개를 소환합니다.");
            SetPrivateField(spawn, "_cooldown", 10f);
            SetPrivateField(spawn, "_param1", 2f);  // 수량
            SetPrivateField(spawn, "_stringParam", "Beetle");

            // BuffAlly (Phase 3)
            var buff = CreateAsset<SkillBehaviorData>(folder, "Skill_BuffAlly");
            SetPrivateField(buff, "_type", SkillType.BuffAlly);
            SetPrivateField(buff, "_displayName", "아군 강화");
            SetPrivateField(buff, "_description", "주변 아군을 강화합니다.");
            SetPrivateField(buff, "_cooldown", 20f);
            SetPrivateField(buff, "_param1", 1.5f);  // 배율

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
            SetPrivateField(enrage, "_description", "HP가 낮아지면 공격력이 증가합니다.");
            SetPrivateField(enrage, "_param1", 30f);  // HP%
            SetPrivateField(enrage, "_param2", 2f);   // 공격배율

            // ExplodeOnDeath (Phase 2)
            var explode = CreateAsset<TriggerBehaviorData>(folder, "Trigger_ExplodeOnDeath");
            SetPrivateField(explode, "_type", TriggerType.ExplodeOnDeath);
            SetPrivateField(explode, "_displayName", "자폭");
            SetPrivateField(explode, "_description", "죽을 때 폭발합니다.");
            SetPrivateField(explode, "_param1", 3f);   // 범위
            SetPrivateField(explode, "_param2", 50f);  // 데미지

            // SplitOnDeath (Phase 3)
            var split = CreateAsset<TriggerBehaviorData>(folder, "Trigger_SplitOnDeath");
            SetPrivateField(split, "_type", TriggerType.SplitOnDeath);
            SetPrivateField(split, "_displayName", "분열");
            SetPrivateField(split, "_description", "죽을 때 작은 벌레로 분열합니다.");
            SetPrivateField(split, "_param1", 3f);  // 수량
            SetPrivateField(split, "_stringParam", "MiniBeetle");

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

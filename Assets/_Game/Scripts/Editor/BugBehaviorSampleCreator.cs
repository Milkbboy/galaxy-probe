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

        [MenuItem("Tools/Drill-Corp/1. 버그 설정/행동 (Behavior)/샘플 전체 생성", priority = 100)]
        public static void CreateAllSamples()
        {
            CreateFolders();

            // 1단계: 기본 행동 SO 생성 (Movement, Attack, Passive, Skill, Trigger)
            CreateMovementSamples();
            CreateAttackSamples();
            CreatePassiveSamples();
            CreateSkillSamples();
            CreateTriggerSamples();

            // 저장 및 새로고침하여 에셋 로드 가능하게 함
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.ReleaseCachedFileHandles();  // 파일 핸들 해제

            // 2단계: BugBehavior Set 생성 (위에서 만든 SO 참조)
            CreateBugBehaviorSetSamples();
            CreateTestBugBehaviorSamples();

            // 최종 저장
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BugBehaviorSampleCreator] 모든 샘플 생성 완료!");
            EditorUtility.DisplayDialog("완료", "Bug Behavior 샘플이 모두 생성되었습니다.\n\n경로: " + BasePath, "확인");
        }

        [MenuItem("Tools/Drill-Corp/1. 버그 설정/행동 (Behavior)/Movement 샘플 생성")]
        public static void CreateMovementSamples()
        {
            CreateFolders();
            string folder = BasePath + "/Movement";

            // 1단계: 빈 에셋 생성
            CreateAsset<MovementBehaviorData>(folder, "Movement_Linear");
            CreateAsset<MovementBehaviorData>(folder, "Movement_Hover");
            CreateAsset<MovementBehaviorData>(folder, "Movement_Burst");
            CreateAsset<MovementBehaviorData>(folder, "Movement_Ranged");
            CreateAsset<MovementBehaviorData>(folder, "Movement_Retreat");
            CreateAsset<MovementBehaviorData>(folder, "Movement_SlowStart");
            CreateAsset<MovementBehaviorData>(folder, "Movement_Orbit");
            CreateAsset<MovementBehaviorData>(folder, "Movement_Teleport");
            CreateAsset<MovementBehaviorData>(folder, "Movement_Linear_Strafe");
            CreateAsset<MovementBehaviorData>(folder, "Movement_Linear_Orbit");
            CreateAsset<MovementBehaviorData>(folder, "Movement_Linear_Retreat");
            CreateAsset<MovementBehaviorData>(folder, "Movement_Hover_Strafe");

            // 2단계: 저장 및 새로고침
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3단계: 에셋 다시 로드 후 값 설정
            var linear = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Linear.asset");
            SetPrivateField(linear, "_type", MovementType.Linear);
            SetPrivateField(linear, "_idleType", IdleType.Stop);
            SetPrivateField(linear, "_displayName", "직선 이동");
            SetPrivateField(linear, "_description", "타겟을 향해 직선으로 이동합니다.\n가장 기본적인 이동 패턴으로, MoveSpeed에 따라 일정한 속도로 접근합니다.\nIdleType으로 사거리 도달 후 행동을 설정할 수 있습니다.");

            var hover = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Hover.asset");
            SetPrivateField(hover, "_type", MovementType.Hover);
            SetPrivateField(hover, "_idleType", IdleType.Stop);
            SetPrivateField(hover, "_displayName", "부유 이동");
            SetPrivateField(hover, "_description", "공중에서 위아래로 떠다니며 타겟을 향해 이동합니다.\n비행형 벌레에 적합하며, 사인파로 Y축 높이가 변합니다.");
            SetPrivateField(hover, "_param1", 0.5f);
            SetPrivateField(hover, "_param2", 2f);

            var burst = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Burst.asset");
            SetPrivateField(burst, "_type", MovementType.Burst);
            SetPrivateField(burst, "_displayName", "돌진 이동");
            SetPrivateField(burst, "_description", "일정 시간 대기 후 빠른 속도로 돌진합니다.\n대기 중에는 정지, 돌진 중에는 속도배율만큼 빠르게 이동합니다.");
            SetPrivateField(burst, "_param1", 2f);
            SetPrivateField(burst, "_param2", 3f);

            var ranged = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Ranged.asset");
            SetPrivateField(ranged, "_type", MovementType.Ranged);
            SetPrivateField(ranged, "_displayName", "[Deprecated] 원거리 이동");
            SetPrivateField(ranged, "_description", "[Deprecated] Linear + IdleType.Strafe 사용을 권장합니다.\n타겟과 일정 거리를 유지하며 좌우로 이동합니다.");
            SetPrivateField(ranged, "_param1", 0f);
            SetPrivateField(ranged, "_param2", 0.5f);

            var retreat = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Retreat.asset");
            SetPrivateField(retreat, "_type", MovementType.Retreat);
            SetPrivateField(retreat, "_displayName", "후퇴 이동");
            SetPrivateField(retreat, "_description", "공격 후 일정 시간 동안 뒤로 물러납니다.\n근접 공격 후 도주하는 패턴에 적합합니다.");
            SetPrivateField(retreat, "_param1", 1f);
            SetPrivateField(retreat, "_param2", 1.5f);

            var slowStart = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_SlowStart.asset");
            SetPrivateField(slowStart, "_type", MovementType.SlowStart);
            SetPrivateField(slowStart, "_displayName", "점진 가속");
            SetPrivateField(slowStart, "_description", "스폰 후 천천히 속도가 증가합니다.\n시작 속도비율에서 시작해 도달시간 동안 최대 속도까지 가속합니다.");
            SetPrivateField(slowStart, "_param1", 0.2f);
            SetPrivateField(slowStart, "_param2", 2f);

            var orbit = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Orbit.asset");
            SetPrivateField(orbit, "_type", MovementType.Orbit);
            SetPrivateField(orbit, "_displayName", "궤도 이동");
            SetPrivateField(orbit, "_description", "타겟 주위를 원형으로 맴돌며 이동합니다.\n먼저 궤도까지 접근한 후, 일정 각속도로 회전합니다.");
            SetPrivateField(orbit, "_param1", 0f);
            SetPrivateField(orbit, "_param2", 60f);

            var teleport = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Teleport.asset");
            SetPrivateField(teleport, "_type", MovementType.Teleport);
            SetPrivateField(teleport, "_displayName", "순간이동");
            SetPrivateField(teleport, "_description", "일정 주기로 타겟 방향으로 순간이동합니다.\n쿨다운 중에는 정지하고 타겟 방향만 바라봅니다.\n공격 범위 내에서는 텔레포트하지 않습니다.");
            SetPrivateField(teleport, "_param1", 3f);
            SetPrivateField(teleport, "_param2", 3f);

            // === IdleType 조합 샘플 ===
            var linearStrafe = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Linear_Strafe.asset");
            SetPrivateField(linearStrafe, "_type", MovementType.Linear);
            SetPrivateField(linearStrafe, "_idleType", IdleType.Strafe);
            SetPrivateField(linearStrafe, "_idleParam", 0.5f);
            SetPrivateField(linearStrafe, "_displayName", "직선 + 좌우이동");
            SetPrivateField(linearStrafe, "_description", "타겟을 향해 직선으로 접근 후, 사거리 도달 시 좌우로 이동합니다.\n원거리 공격 벌레에 적합합니다.");

            var linearOrbit = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Linear_Orbit.asset");
            SetPrivateField(linearOrbit, "_type", MovementType.Linear);
            SetPrivateField(linearOrbit, "_idleType", IdleType.Orbit);
            SetPrivateField(linearOrbit, "_idleParam", 45f);
            SetPrivateField(linearOrbit, "_displayName", "직선 + 선회");
            SetPrivateField(linearOrbit, "_description", "타겟을 향해 직선으로 접근 후, 사거리 도달 시 타겟 주위를 선회합니다.\n초당 회전각도로 속도를 조절합니다.");

            var linearRetreat = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Linear_Retreat.asset");
            SetPrivateField(linearRetreat, "_type", MovementType.Linear);
            SetPrivateField(linearRetreat, "_idleType", IdleType.Retreat);
            SetPrivateField(linearRetreat, "_idleParam", 1f);
            SetPrivateField(linearRetreat, "_displayName", "직선 + 후퇴");
            SetPrivateField(linearRetreat, "_description", "타겟을 향해 직선으로 접근 후, 사거리 도달 시 후퇴했다가 다시 접근합니다.\n치고 빠지기 패턴에 적합합니다.");

            var hoverStrafe = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Hover_Strafe.asset");
            SetPrivateField(hoverStrafe, "_type", MovementType.Hover);
            SetPrivateField(hoverStrafe, "_idleType", IdleType.Strafe);
            SetPrivateField(hoverStrafe, "_idleParam", 0.5f);
            SetPrivateField(hoverStrafe, "_param1", 0.5f);
            SetPrivateField(hoverStrafe, "_param2", 2f);
            SetPrivateField(hoverStrafe, "_displayName", "부유 + 좌우이동");
            SetPrivateField(hoverStrafe, "_description", "공중에서 부유하며 접근 후, 사거리 도달 시 좌우로 이동합니다.\n비행형 원거리 벌레에 적합합니다.");

            SaveAllAssets();
            Debug.Log("[BugBehaviorSampleCreator] Movement 샘플 생성 완료");
        }

        [MenuItem("Tools/Drill-Corp/1. 버그 설정/행동 (Behavior)/Attack 샘플 생성")]
        public static void CreateAttackSamples()
        {
            CreateFolders();
            string folder = BasePath + "/Attack";

            // 1단계: 빈 에셋 생성
            CreateAsset<AttackBehaviorData>(folder, "Attack_Melee");
            CreateAsset<AttackBehaviorData>(folder, "Attack_Projectile");
            CreateAsset<AttackBehaviorData>(folder, "Attack_Cleave");
            CreateAsset<AttackBehaviorData>(folder, "Attack_Spread");
            CreateAsset<AttackBehaviorData>(folder, "Attack_Beam");

            // 2단계: 저장 및 새로고침
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3단계: 에셋 다시 로드 후 값 설정
            var melee = AssetDatabase.LoadAssetAtPath<AttackBehaviorData>($"{folder}/Attack_Melee.asset");
            SetPrivateField(melee, "_type", AttackType.Melee);
            SetPrivateField(melee, "_range", 1.5f);
            SetPrivateField(melee, "_displayName", "근접 공격");
            SetPrivateField(melee, "_description", "공격 범위 내 타겟에게 직접 데미지를 줍니다.\n가장 기본적인 공격 패턴으로, AttackDamage만큼 데미지를 입힙니다.");

            var projectile = AssetDatabase.LoadAssetAtPath<AttackBehaviorData>($"{folder}/Attack_Projectile.asset");
            SetPrivateField(projectile, "_type", AttackType.Projectile);
            SetPrivateField(projectile, "_range", 8f);
            SetPrivateField(projectile, "_displayName", "원거리 공격");
            SetPrivateField(projectile, "_description", "타겟 방향으로 투사체를 발사합니다.\n투사체는 직선으로 날아가며, 충돌 시 AttackDamage만큼 데미지를 입힙니다.\n5초 후 자동 소멸됩니다.");
            SetPrivateField(projectile, "_param1", 10f);

            var cleave = AssetDatabase.LoadAssetAtPath<AttackBehaviorData>($"{folder}/Attack_Cleave.asset");
            SetPrivateField(cleave, "_type", AttackType.Cleave);
            SetPrivateField(cleave, "_range", 2f);
            SetPrivateField(cleave, "_displayName", "범위 공격");
            SetPrivateField(cleave, "_description", "전방 부채꼴 범위 내 모든 대상에게 데미지를 줍니다.\n공격 범위와 부채꼴 각도로 범위가 결정됩니다.\nLineRenderer로 범위가 표시됩니다.");
            SetPrivateField(cleave, "_param1", 90f);

            var spread = AssetDatabase.LoadAssetAtPath<AttackBehaviorData>($"{folder}/Attack_Spread.asset");
            SetPrivateField(spread, "_type", AttackType.Spread);
            SetPrivateField(spread, "_range", 6f);
            SetPrivateField(spread, "_displayName", "다발 공격");
            SetPrivateField(spread, "_description", "여러 발의 투사체를 부채꼴로 발사합니다.\n각 투사체는 AttackDamage만큼 데미지를 입히며, 투사체 개수만큼 총 데미지가 증가할 수 있습니다.");
            SetPrivateField(spread, "_param1", 5f);
            SetPrivateField(spread, "_param2", 60f);

            var beam = AssetDatabase.LoadAssetAtPath<AttackBehaviorData>($"{folder}/Attack_Beam.asset");
            SetPrivateField(beam, "_type", AttackType.Beam);
            SetPrivateField(beam, "_range", 10f);
            SetPrivateField(beam, "_displayName", "레이저 공격");
            SetPrivateField(beam, "_description", "타겟에게 지속적인 레이저 빔을 발사합니다.\n빔이 활성화된 동안 틱마다 데미지를 주며, 빔은 타겟을 추적합니다.\n빔 VFX가 없으면 붉은색 LineRenderer로 표시됩니다.");
            SetPrivateField(beam, "_param1", 2f);
            SetPrivateField(beam, "_param2", 0.5f);

            SaveAllAssets();
            Debug.Log("[BugBehaviorSampleCreator] Attack 샘플 생성 완료");
        }

        [MenuItem("Tools/Drill-Corp/1. 버그 설정/행동 (Behavior)/Passive 샘플 생성")]
        public static void CreatePassiveSamples()
        {
            CreateFolders();
            string folder = BasePath + "/Passive";

            // 1단계: 빈 에셋 생성
            CreateAsset<PassiveBehaviorData>(folder, "Passive_Armor");
            CreateAsset<PassiveBehaviorData>(folder, "Passive_Dodge");
            CreateAsset<PassiveBehaviorData>(folder, "Passive_Shield");
            CreateAsset<PassiveBehaviorData>(folder, "Passive_Regen");
            CreateAsset<PassiveBehaviorData>(folder, "Passive_PoisonAttack");
            CreateAsset<PassiveBehaviorData>(folder, "Passive_Burrow");

            // 2단계: 저장 및 새로고침
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3단계: 에셋 다시 로드 후 값 설정
            var armor = AssetDatabase.LoadAssetAtPath<PassiveBehaviorData>($"{folder}/Passive_Armor.asset");
            SetPrivateField(armor, "_type", PassiveType.Armor);
            SetPrivateField(armor, "_displayName", "방어력");
            SetPrivateField(armor, "_description", "받는 데미지를 고정값만큼 감소시킵니다.\n최종 데미지 = 원래 데미지 - 감소량 (최소 1)");
            SetPrivateField(armor, "_param1", 5f);

            var dodge = AssetDatabase.LoadAssetAtPath<PassiveBehaviorData>($"{folder}/Passive_Dodge.asset");
            SetPrivateField(dodge, "_type", PassiveType.Dodge);
            SetPrivateField(dodge, "_displayName", "회피");
            SetPrivateField(dodge, "_description", "확률적으로 공격을 완전히 회피합니다.\n회피 성공 시 데미지 0, 회피 이펙트가 재생됩니다.");
            SetPrivateField(dodge, "_param1", 20f);

            var shield = AssetDatabase.LoadAssetAtPath<PassiveBehaviorData>($"{folder}/Passive_Shield.asset");
            SetPrivateField(shield, "_type", PassiveType.Shield);
            SetPrivateField(shield, "_displayName", "보호막");
            SetPrivateField(shield, "_description", "HP 대신 데미지를 흡수하는 보호막을 가집니다.\n보호막이 파괴되면 쿨다운 후 자동 재생됩니다.\n보호막 활성화 시 파란색 틴트가 적용됩니다.");
            SetPrivateField(shield, "_param1", 50f);
            SetPrivateField(shield, "_param2", 30f);

            var regen = AssetDatabase.LoadAssetAtPath<PassiveBehaviorData>($"{folder}/Passive_Regen.asset");
            SetPrivateField(regen, "_type", PassiveType.Regen);
            SetPrivateField(regen, "_displayName", "재생");
            SetPrivateField(regen, "_description", "매 초마다 체력을 회복합니다.\nMaxHealth를 초과하여 회복하지 않습니다.");
            SetPrivateField(regen, "_param1", 2f);

            var poison = AssetDatabase.LoadAssetAtPath<PassiveBehaviorData>($"{folder}/Passive_PoisonAttack.asset");
            SetPrivateField(poison, "_type", PassiveType.PoisonAttack);
            SetPrivateField(poison, "_displayName", "독 공격");
            SetPrivateField(poison, "_description", "공격 시 대상에게 독 효과를 적용합니다.\n독은 지속시간 동안 매 초 데미지를 주며, 대상이 녹색으로 변합니다.\nPoisonEffect 컴포넌트가 대상에 추가됩니다.");
            SetPrivateField(poison, "_param1", 3f);
            SetPrivateField(poison, "_param2", 5f);

            var burrow = AssetDatabase.LoadAssetAtPath<PassiveBehaviorData>($"{folder}/Passive_Burrow.asset");
            SetPrivateField(burrow, "_type", PassiveType.Burrow);
            SetPrivateField(burrow, "_displayName", "땅속 숨기");
            SetPrivateField(burrow, "_description", "Trigger에 의해 발동되면 땅속으로 숨습니다.\n숨어있는 동안 무적 상태가 되며, 반투명으로 표시됩니다.\n지속시간이 지나면 다시 출현합니다.");
            SetPrivateField(burrow, "_param1", 2f);
            SetPrivateField(burrow, "_param2", 0.3f);

            SaveAllAssets();
            Debug.Log("[BugBehaviorSampleCreator] Passive 샘플 생성 완료");
        }

        [MenuItem("Tools/Drill-Corp/1. 버그 설정/행동 (Behavior)/Skill 샘플 생성")]
        public static void CreateSkillSamples()
        {
            CreateFolders();
            string folder = BasePath + "/Skill";

            // 1단계: 빈 에셋 생성
            CreateAsset<SkillBehaviorData>(folder, "Skill_Nova");
            CreateAsset<SkillBehaviorData>(folder, "Skill_Spawn");
            CreateAsset<SkillBehaviorData>(folder, "Skill_BuffAlly");
            CreateAsset<SkillBehaviorData>(folder, "Skill_HealAlly");

            // 2단계: 저장 및 새로고침
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3단계: 에셋 다시 로드 후 값 설정
            var nova = AssetDatabase.LoadAssetAtPath<SkillBehaviorData>($"{folder}/Skill_Nova.asset");
            SetPrivateField(nova, "_type", SkillType.Nova);
            SetPrivateField(nova, "_displayName", "폭발");
            SetPrivateField(nova, "_description", "자신을 중심으로 전방향 폭발을 일으킵니다.\n폭발 범위 내 모든 대상(벌레 제외)에게 데미지를 줍니다.\n원형 범위 인디케이터가 표시됩니다.");
            SetPrivateField(nova, "_cooldown", 15f);

            var spawn = AssetDatabase.LoadAssetAtPath<SkillBehaviorData>($"{folder}/Skill_Spawn.asset");
            SetPrivateField(spawn, "_type", SkillType.Spawn);
            SetPrivateField(spawn, "_displayName", "소환");
            SetPrivateField(spawn, "_description", "졸개 벌레를 소환합니다.\n소환 위치는 소환 개수에 따라 좌/우/후방에 배치됩니다.\nSpawnPrefab에 소환할 버그 프리펩을 설정하세요.");
            SetPrivateField(spawn, "_cooldown", 10f);
            SetPrivateField(spawn, "_param1", 2f);
            SetPrivateField(spawn, "_stringParam", "Beetle");

            var buff = AssetDatabase.LoadAssetAtPath<SkillBehaviorData>($"{folder}/Skill_BuffAlly.asset");
            SetPrivateField(buff, "_type", SkillType.BuffAlly);
            SetPrivateField(buff, "_displayName", "아군 강화");
            SetPrivateField(buff, "_description", "주변 아군 벌레의 공격력과 이동속도를 증가시킵니다.\n범위 내에 있는 동안 버프가 적용되고, 범위를 벗어나면 해제됩니다.\n황금색 원형 인디케이터가 표시됩니다.");
            SetPrivateField(buff, "_cooldown", 1.2f);  // 이속 배율로 사용
            SetPrivateField(buff, "_param1", 5f);     // 범위
            SetPrivateField(buff, "_param2", 1.3f);   // 공격력 배율

            var heal = AssetDatabase.LoadAssetAtPath<SkillBehaviorData>($"{folder}/Skill_HealAlly.asset");
            SetPrivateField(heal, "_type", SkillType.HealAlly);
            SetPrivateField(heal, "_displayName", "아군 회복");
            SetPrivateField(heal, "_description", "주변 아군 벌레의 체력을 주기적으로 회복시킵니다.\n범위 내에 있는 아군을 회복 주기마다 치유합니다.\n녹색 원형 인디케이터가 표시됩니다.");
            SetPrivateField(heal, "_cooldown", 1f);   // 회복 주기 (초)
            SetPrivateField(heal, "_param1", 5f);    // 범위
            SetPrivateField(heal, "_param2", 10f);   // 회복량 (회복 주기마다)

            SaveAllAssets();
            Debug.Log("[BugBehaviorSampleCreator] Skill 샘플 생성 완료");
        }

        [MenuItem("Tools/Drill-Corp/1. 버그 설정/행동 (Behavior)/Trigger 샘플 생성")]
        public static void CreateTriggerSamples()
        {
            CreateFolders();
            string folder = BasePath + "/Trigger";

            // 1단계: 빈 에셋 생성
            CreateAsset<TriggerBehaviorData>(folder, "Trigger_Enrage");
            CreateAsset<TriggerBehaviorData>(folder, "Trigger_ExplodeOnDeath");
            CreateAsset<TriggerBehaviorData>(folder, "Trigger_SplitOnDeath");
            CreateAsset<TriggerBehaviorData>(folder, "Trigger_PanicBurrow");

            // 2단계: 저장 및 새로고침
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3단계: 에셋 다시 로드 후 값 설정
            var enrage = AssetDatabase.LoadAssetAtPath<TriggerBehaviorData>($"{folder}/Trigger_Enrage.asset");
            SetPrivateField(enrage, "_type", TriggerType.Enrage);
            SetPrivateField(enrage, "_displayName", "광폭화");
            SetPrivateField(enrage, "_description", "HP가 임계값 이하로 떨어지면 광폭화 상태가 됩니다.\n광폭화 시 공격력과 이동속도가 증가하며, 붉은색 틴트가 적용됩니다.\n한번 발동되면 사망할 때까지 유지됩니다.");
            SetPrivateField(enrage, "_param1", 30f);
            SetPrivateField(enrage, "_param2", 2f);

            var explode = AssetDatabase.LoadAssetAtPath<TriggerBehaviorData>($"{folder}/Trigger_ExplodeOnDeath.asset");
            SetPrivateField(explode, "_type", TriggerType.ExplodeOnDeath);
            SetPrivateField(explode, "_displayName", "사망 폭발");
            SetPrivateField(explode, "_description", "사망 시 폭발하여 주변에 데미지를 줍니다.\n폭발 범위 내 모든 IDamageable 대상(다른 버그 제외)에게 데미지를 입힙니다.\n폭발 이펙트가 재생됩니다.");
            SetPrivateField(explode, "_param1", 3f);
            SetPrivateField(explode, "_param2", 50f);

            var split = AssetDatabase.LoadAssetAtPath<TriggerBehaviorData>($"{folder}/Trigger_SplitOnDeath.asset");
            SetPrivateField(split, "_type", TriggerType.SplitOnDeath);
            SetPrivateField(split, "_displayName", "분열");
            SetPrivateField(split, "_description", "사망 시 작은 벌레로 분열합니다.\n분열 개수만큼 SpawnPrefab이 소환되며, 체력비율만큼의 HP를 가집니다.");
            SetPrivateField(split, "_param1", 3f);
            SetPrivateField(split, "_param2", 0.5f);  // 체력비율 50%
            SetPrivateField(split, "_stringParam", "MiniBeetle");

            var panicBurrow = AssetDatabase.LoadAssetAtPath<TriggerBehaviorData>($"{folder}/Trigger_PanicBurrow.asset");
            SetPrivateField(panicBurrow, "_type", TriggerType.PanicBurrow);
            SetPrivateField(panicBurrow, "_displayName", "위협 회피");
            SetPrivateField(panicBurrow, "_description", "HP가 임계값 이하일 때 피격 시 땅속으로 숨습니다.\nBurrow 패시브가 필요합니다.\n쿨다운 동안 재발동되지 않습니다.");
            SetPrivateField(panicBurrow, "_param1", 50f);
            SetPrivateField(panicBurrow, "_param2", 5f);

            SaveAllAssets();
            Debug.Log("[BugBehaviorSampleCreator] Trigger 샘플 생성 완료");
        }

        [MenuItem("Tools/Drill-Corp/1. 버그 설정/행동 (Behavior)/BugBehavior Set 샘플 생성")]
        public static void CreateBugBehaviorSetSamples()
        {
            CreateFolders();
            string folder = BasePath;

            // 1단계: 빈 BugBehavior 에셋 먼저 생성
            var beetle = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Beetle");
            var fly = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Fly");
            var tank = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Tank");
            var spitter = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Spitter");
            var bomber = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Bomber");

            // 2단계: 저장 및 새로고침
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3단계: 참조할 에셋 로드
            var linearMovement = LoadAssetWithLog<MovementBehaviorData>(BasePath + "/Movement/Movement_Linear.asset");
            var hoverMovement = LoadAssetWithLog<MovementBehaviorData>(BasePath + "/Movement/Movement_Hover.asset");
            var burstMovement = LoadAssetWithLog<MovementBehaviorData>(BasePath + "/Movement/Movement_Burst.asset");
            var linearStrafeMovement = LoadAssetWithLog<MovementBehaviorData>(BasePath + "/Movement/Movement_Linear_Strafe.asset");

            var meleeAttack = LoadAssetWithLog<AttackBehaviorData>(BasePath + "/Attack/Attack_Melee.asset");
            var projectileAttack = LoadAssetWithLog<AttackBehaviorData>(BasePath + "/Attack/Attack_Projectile.asset");

            var armorPassive = LoadAssetWithLog<PassiveBehaviorData>(BasePath + "/Passive/Passive_Armor.asset");
            var explodeTrigger = LoadAssetWithLog<TriggerBehaviorData>(BasePath + "/Trigger/Trigger_ExplodeOnDeath.asset");

            // 4단계: 생성한 에셋 다시 로드 후 참조 설정
            beetle = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Beetle.asset");
            fly = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Fly.asset");
            tank = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Tank.asset");
            spitter = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Spitter.asset");
            bomber = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Bomber.asset");

            // === Beetle (기본 근접) ===
            SetPrivateField(beetle, "_defaultMovement", linearMovement);
            SetPrivateField(beetle, "_defaultAttack", meleeAttack);

            // === Fly (부유 근접) ===
            SetPrivateField(fly, "_defaultMovement", hoverMovement);
            SetPrivateField(fly, "_defaultAttack", meleeAttack);

            // === Tank (방어형 근접) ===
            SetPrivateField(tank, "_defaultMovement", linearMovement);
            SetPrivateField(tank, "_defaultAttack", meleeAttack);
            if (armorPassive != null)
            {
                var passives = new System.Collections.Generic.List<PassiveBehaviorData> { armorPassive };
                SetPrivateField(tank, "_passives", passives);
            }

            // === Spitter (원거리) ===
            SetPrivateField(spitter, "_defaultMovement", linearStrafeMovement);
            SetPrivateField(spitter, "_defaultAttack", projectileAttack);

            // === Bomber (자폭) ===
            SetPrivateField(bomber, "_defaultMovement", burstMovement);
            SetPrivateField(bomber, "_defaultAttack", meleeAttack);
            if (explodeTrigger != null)
            {
                var triggers = new System.Collections.Generic.List<TriggerBehaviorData> { explodeTrigger };
                SetPrivateField(bomber, "_triggers", triggers);
            }

            SaveAllAssets();
            Debug.Log("[BugBehaviorSampleCreator] BugBehavior Set 샘플 생성 완료");
        }

        [MenuItem("Tools/Drill-Corp/1. 버그 설정/행동 (Behavior)/Test BugBehavior 샘플 생성")]
        public static void CreateTestBugBehaviorSamples()
        {
            CreateFolders();
            string folder = BasePath + "/Test";

            // 1단계: 빈 BugBehavior 에셋 먼저 생성
            var testCleave = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Test_Cleave");
            var testSpread = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Test_Spread");
            var testOrbit = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Test_Orbit");
            var testRetreat = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Test_Retreat");
            var testShield = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Test_Shield");
            var testRegen = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Test_Regen");
            var testPoison = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Test_Poison");
            var testEnrage = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Test_Enrage");
            var testExplode = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Test_Explode");
            var testOrbitPoison = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Test_OrbitPoison");
            var testEnrageExplode = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Test_EnrageExplode");

            // 2단계: 저장 및 새로고침
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3단계: 참조할 에셋 로드
            var linearMovement = LoadAssetWithLog<MovementBehaviorData>(BasePath + "/Movement/Movement_Linear.asset");
            var hoverMovement = LoadAssetWithLog<MovementBehaviorData>(BasePath + "/Movement/Movement_Hover.asset");
            var orbitMovement = LoadAssetWithLog<MovementBehaviorData>(BasePath + "/Movement/Movement_Orbit.asset");
            var retreatMovement = LoadAssetWithLog<MovementBehaviorData>(BasePath + "/Movement/Movement_Retreat.asset");

            var meleeAttack = LoadAssetWithLog<AttackBehaviorData>(BasePath + "/Attack/Attack_Melee.asset");
            var projectileAttack = LoadAssetWithLog<AttackBehaviorData>(BasePath + "/Attack/Attack_Projectile.asset");
            var cleaveAttack = LoadAssetWithLog<AttackBehaviorData>(BasePath + "/Attack/Attack_Cleave.asset");
            var spreadAttack = LoadAssetWithLog<AttackBehaviorData>(BasePath + "/Attack/Attack_Spread.asset");

            var armorPassive = LoadAssetWithLog<PassiveBehaviorData>(BasePath + "/Passive/Passive_Armor.asset");
            var dodgePassive = LoadAssetWithLog<PassiveBehaviorData>(BasePath + "/Passive/Passive_Dodge.asset");
            var shieldPassive = LoadAssetWithLog<PassiveBehaviorData>(BasePath + "/Passive/Passive_Shield.asset");
            var regenPassive = LoadAssetWithLog<PassiveBehaviorData>(BasePath + "/Passive/Passive_Regen.asset");
            var poisonPassive = LoadAssetWithLog<PassiveBehaviorData>(BasePath + "/Passive/Passive_PoisonAttack.asset");

            var enrageTrigger = LoadAssetWithLog<TriggerBehaviorData>(BasePath + "/Trigger/Trigger_Enrage.asset");
            var explodeTrigger = LoadAssetWithLog<TriggerBehaviorData>(BasePath + "/Trigger/Trigger_ExplodeOnDeath.asset");

            // 4단계: 생성한 에셋 다시 로드 후 참조 설정
            testCleave = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Test_Cleave.asset");
            testSpread = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Test_Spread.asset");
            testOrbit = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Test_Orbit.asset");
            testRetreat = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Test_Retreat.asset");
            testShield = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Test_Shield.asset");
            testRegen = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Test_Regen.asset");
            testPoison = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Test_Poison.asset");
            testEnrage = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Test_Enrage.asset");
            testExplode = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Test_Explode.asset");
            testOrbitPoison = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Test_OrbitPoison.asset");
            testEnrageExplode = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Test_EnrageExplode.asset");

            // === Test_Cleave (범위 공격 테스트) ===
            SetPrivateField(testCleave, "_defaultMovement", linearMovement);
            SetPrivateField(testCleave, "_defaultAttack", cleaveAttack);

            // === Test_Spread (다발 공격 테스트) ===
            SetPrivateField(testSpread, "_defaultMovement", linearMovement);
            SetPrivateField(testSpread, "_defaultAttack", spreadAttack);

            // === Test_Orbit (궤도 이동 테스트) ===
            SetPrivateField(testOrbit, "_defaultMovement", orbitMovement);
            SetPrivateField(testOrbit, "_defaultAttack", meleeAttack);

            // === Test_Retreat (후퇴 이동 테스트) ===
            SetPrivateField(testRetreat, "_defaultMovement", retreatMovement);
            SetPrivateField(testRetreat, "_defaultAttack", meleeAttack);

            // === Test_Shield (보호막 테스트) ===
            SetPrivateField(testShield, "_defaultMovement", linearMovement);
            SetPrivateField(testShield, "_defaultAttack", meleeAttack);
            if (shieldPassive != null)
            {
                var passives = new System.Collections.Generic.List<PassiveBehaviorData> { shieldPassive };
                SetPrivateField(testShield, "_passives", passives);
            }

            // === Test_Regen (재생 테스트) ===
            SetPrivateField(testRegen, "_defaultMovement", linearMovement);
            SetPrivateField(testRegen, "_defaultAttack", meleeAttack);
            if (regenPassive != null)
            {
                var passives = new System.Collections.Generic.List<PassiveBehaviorData> { regenPassive };
                SetPrivateField(testRegen, "_passives", passives);
            }

            // === Test_Poison (독 공격 테스트) ===
            SetPrivateField(testPoison, "_defaultMovement", linearMovement);
            SetPrivateField(testPoison, "_defaultAttack", meleeAttack);
            if (poisonPassive != null)
            {
                var passives = new System.Collections.Generic.List<PassiveBehaviorData> { poisonPassive };
                SetPrivateField(testPoison, "_passives", passives);
            }

            // === Test_Enrage (광폭화 테스트) ===
            SetPrivateField(testEnrage, "_defaultMovement", linearMovement);
            SetPrivateField(testEnrage, "_defaultAttack", meleeAttack);
            if (enrageTrigger != null)
            {
                var triggers = new System.Collections.Generic.List<TriggerBehaviorData> { enrageTrigger };
                SetPrivateField(testEnrage, "_triggers", triggers);
            }

            // === Test_Explode (사망 폭발 테스트) ===
            SetPrivateField(testExplode, "_defaultMovement", linearMovement);
            SetPrivateField(testExplode, "_defaultAttack", meleeAttack);
            if (explodeTrigger != null)
            {
                var triggers = new System.Collections.Generic.List<TriggerBehaviorData> { explodeTrigger };
                SetPrivateField(testExplode, "_triggers", triggers);
            }

            // === Test_OrbitPoison (궤도 + 독 조합 테스트) ===
            SetPrivateField(testOrbitPoison, "_defaultMovement", orbitMovement);
            SetPrivateField(testOrbitPoison, "_defaultAttack", meleeAttack);
            if (poisonPassive != null)
            {
                var passives = new System.Collections.Generic.List<PassiveBehaviorData> { poisonPassive };
                SetPrivateField(testOrbitPoison, "_passives", passives);
            }

            // === Test_EnrageExplode (광폭화 + 사망폭발 조합 테스트) ===
            SetPrivateField(testEnrageExplode, "_defaultMovement", linearMovement);
            SetPrivateField(testEnrageExplode, "_defaultAttack", meleeAttack);
            if (enrageTrigger != null && explodeTrigger != null)
            {
                var triggers = new System.Collections.Generic.List<TriggerBehaviorData> { enrageTrigger, explodeTrigger };
                SetPrivateField(testEnrageExplode, "_triggers", triggers);
            }

            SaveAllAssets();
            Debug.Log("[BugBehaviorSampleCreator] Test BugBehavior 샘플 생성 완료");
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
            CreateFolderIfNotExists(BasePath + "/Test");
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

        private static T CreateAsset<T>(string folder, string name, bool overwrite = true) where T : ScriptableObject
        {
            string path = $"{folder}/{name}.asset";

            // 이미 존재하면 삭제 후 재생성 (overwrite 모드)
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                if (overwrite)
                {
                    // 기존 에셋 삭제 후 Refresh
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.Refresh();
                }
                else
                {
                    return existing;
                }
            }

            // 새로 생성
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();  // 즉시 저장
            return asset;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            if (obj is not Object unityObj)
                return;

            // null 값은 설정하지 않음 (ObjectReference의 경우)
            if (value == null && (fieldName.Contains("Movement") || fieldName.Contains("Attack") || fieldName.Contains("Passive") || fieldName.Contains("Trigger")))
            {
                Debug.LogWarning($"[BugBehaviorSampleCreator] {unityObj.name}.{fieldName}에 null 값 설정 시도 - 스킵됨");
                return;
            }

            // SerializedObject를 통해 직렬화 보장
            var serializedObj = new SerializedObject(unityObj);
            var prop = serializedObj.FindProperty(fieldName);

            if (prop != null)
            {
                // 타입에 따라 적절한 방식으로 값 설정
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        prop.intValue = System.Convert.ToInt32(value);
                        break;
                    case SerializedPropertyType.Float:
                        prop.floatValue = System.Convert.ToSingle(value);
                        break;
                    case SerializedPropertyType.String:
                        prop.stringValue = value?.ToString() ?? "";
                        break;
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = System.Convert.ToBoolean(value);
                        break;
                    case SerializedPropertyType.Enum:
                        prop.enumValueIndex = System.Convert.ToInt32(value);
                        break;
                    case SerializedPropertyType.ObjectReference:
                        var objRef = value as Object;
                        if (objRef != null)
                        {
                            prop.objectReferenceValue = objRef;
                        }
                        break;
                    default:
                        // List 등 복잡한 타입은 리플렉션 사용
                        SetPrivateFieldByReflection(obj, fieldName, value);
                        serializedObj.Dispose();
                        return;
                }

                serializedObj.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                // SerializedProperty로 찾지 못하면 리플렉션 사용
                SetPrivateFieldByReflection(obj, fieldName, value);
            }

            serializedObj.Dispose();
            EditorUtility.SetDirty(unityObj);
        }

        private static void SetPrivateFieldByReflection(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
                if (obj is Object unityObj)
                {
                    EditorUtility.SetDirty(unityObj);
                }
            }
        }

        /// <summary>
        /// 모든 변경사항을 디스크에 저장
        /// 각 샘플 그룹 생성 후 호출하여 안정적으로 저장
        /// </summary>
        private static void SaveAllAssets()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 에셋 로드 + null 체크 로그
        /// </summary>
        private static T LoadAssetWithLog<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[BugBehaviorSampleCreator] 에셋 로드 실패: {path}");
            }
            return asset;
        }
    }
}

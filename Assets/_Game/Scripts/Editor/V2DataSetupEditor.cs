// v2 데이터 에셋 자동 생성
// ─────────────────────────────────────────────────────────────
// 생성 대상:
//   • CharacterData 3개 (Victor, Sara, Jinus)
//   • AbilityData 9개 (캐릭터별 3종)
//   • WeaponUpgradeData 15개 (5무기 × 3강화)
//   • UpgradeData 3개 (MiningTarget, GemDrop, GemSpeed — 기존 Upgrades 폴더에 추가)
//
// 기존 에셋이 있으면 덮어쓰지 않고 스킵. 전부 지우고 재생성하려면
// Assets/_Game/Data/Characters/, Abilities/, WeaponUpgrades/ 폴더 삭제 후 재실행.
//
// 참고: docs/CharacterAbilitySystem.md §5, docs/GoogleSheetsGuide_v2Addendum.md
// ─────────────────────────────────────────────────────────────

using System.IO;
using UnityEditor;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Editor
{
    public static class V2DataSetupEditor
    {
        const string DATA_ROOT = "Assets/_Game/Data";
        const string CHAR_DIR  = DATA_ROOT + "/Characters";
        const string ABIL_DIR  = DATA_ROOT + "/Abilities";
        const string WUPG_DIR  = DATA_ROOT + "/WeaponUpgrades";
        const string UPG_DIR   = DATA_ROOT + "/Upgrades";

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/Title/4. v2 Data Assets 생성")]
        public static void CreateAllV2Assets()
        {
            EnsureFolders();

            CreateAbilities();        // 1) 어빌리티 먼저 (CharacterData가 참조)
            LinkAbilityRequirements();// 2) req 체인 연결
            CreateCharacters();       // 3) 캐릭터 + Abilities[] 배열 링크
            CreateWeaponUpgrades();   // 4) 무기 강화 15종
            CreateV2Upgrades();       // 5) 굴착기 강화 신규 3종

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[V2DataSetup] 완료. Characters/Abilities/WeaponUpgrades/Upgrades 폴더를 확인하세요.");
        }

        // ═════════════════════════════════════════════════════
        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(DATA_ROOT))
                AssetDatabase.CreateFolder("Assets/_Game", "Data");
            if (!AssetDatabase.IsValidFolder(CHAR_DIR))
                AssetDatabase.CreateFolder(DATA_ROOT, "Characters");
            if (!AssetDatabase.IsValidFolder(ABIL_DIR))
                AssetDatabase.CreateFolder(DATA_ROOT, "Abilities");
            if (!AssetDatabase.IsValidFolder(WUPG_DIR))
                AssetDatabase.CreateFolder(DATA_ROOT, "WeaponUpgrades");
            if (!AssetDatabase.IsValidFolder(UPG_DIR))
                AssetDatabase.CreateFolder(DATA_ROOT, "Upgrades");
        }

        // ═════════════════════════════════════════════════════
        // 1) AbilityData 9개
        // ═════════════════════════════════════════════════════
        static void CreateAbilities()
        {
            // Victor
            MakeAbility("Ability_Victor_Napalm", "victor_napalm", "victor",
                "네이팜 탄", "🔥", 1, AbilityType.Napalm, AbilityTrigger.Manual,
                cooldownSec: 40f, durationSec: 20f, autoIntervalSec: 0f,
                damage: 0.5f, range: 42f, angle: 0f, maxInstances: 1, gemCost: 30);

            MakeAbility("Ability_Victor_Flame", "victor_flame", "victor",
                "화염방사기", "🔆", 2, AbilityType.Flame, AbilityTrigger.Manual,
                cooldownSec: 20f, durationSec: 5f, autoIntervalSec: 0f,
                damage: 10.8f, range: 18f, angle: 0.35f, maxInstances: 1, gemCost: 30);

            MakeAbility("Ability_Victor_Mine", "victor_mine", "victor",
                "폭발 지뢰", "💣", 3, AbilityType.Mine, AbilityTrigger.Manual,
                cooldownSec: 10f, durationSec: 0f, autoIntervalSec: 0f,
                damage: 0f, range: 0f, angle: 0f, maxInstances: 5, gemCost: 30);

            // Sara
            MakeAbility("Ability_Sara_BlackHole", "sara_blackhole", "sara",
                "블랙홀", "🌀", 1, AbilityType.BlackHole, AbilityTrigger.Manual,
                cooldownSec: 30f, durationSec: 10f, autoIntervalSec: 0f,
                damage: 0f, range: 18f, angle: 0f, maxInstances: 1, gemCost: 30);

            MakeAbility("Ability_Sara_Shockwave", "sara_shockwave", "sara",
                "충격파", "💥", 2, AbilityType.Shockwave, AbilityTrigger.Manual,
                cooldownSec: 50f, durationSec: 0f, autoIntervalSec: 0f,
                damage: 0f, range: 36f, angle: 0f, maxInstances: 1, gemCost: 30);

            MakeAbility("Ability_Sara_Meteor", "sara_meteor", "sara",
                "반중력 메테오", "☄", 3, AbilityType.Meteor, AbilityTrigger.AutoInterval,
                cooldownSec: 0f, durationSec: 15f, autoIntervalSec: 10f,
                damage: 0.5f, range: 5.5f, angle: 0f, maxInstances: 999, gemCost: 30);

            // Jinus
            MakeAbility("Ability_Jinus_Drone", "jinus_drone", "jinus",
                "드론 포탑", "🚁", 1, AbilityType.Drone, AbilityTrigger.Manual,
                cooldownSec: 20f, durationSec: 0f, autoIntervalSec: 0f,
                damage: 0.8f, range: 10f, angle: 0f, maxInstances: 5, gemCost: 30);

            MakeAbility("Ability_Jinus_MiningDrone", "jinus_mining_drone", "jinus",
                "채굴 드론", "⛏", 2, AbilityType.MiningDrone, AbilityTrigger.Manual,
                cooldownSec: 30f, durationSec: 10f, autoIntervalSec: 0f,
                damage: 0f, range: 0f, angle: 0f, maxInstances: 1, gemCost: 30);

            MakeAbility("Ability_Jinus_SpiderDrone", "jinus_spider_drone", "jinus",
                "드론 거미", "🕷", 3, AbilityType.SpiderDrone, AbilityTrigger.AutoInterval,
                cooldownSec: 0f, durationSec: 0f, autoIntervalSec: 10f,
                damage: 0f, range: 12f, angle: 0f, maxInstances: 3, gemCost: 30);
        }

        static void MakeAbility(string fileName, string abilityId, string characterId,
            string displayName, string emoji, int slotKey, AbilityType type, AbilityTrigger trigger,
            float cooldownSec, float durationSec, float autoIntervalSec,
            float damage, float range, float angle, int maxInstances, int gemCost)
        {
            string path = $"{ABIL_DIR}/{fileName}.asset";
            if (File.Exists(path)) { Debug.Log($"[V2DataSetup] {fileName} 이미 존재 — 스킵"); return; }

            var a = ScriptableObject.CreateInstance<AbilityData>();
            var so = new SerializedObject(a);
            so.FindProperty("_abilityId").stringValue = abilityId;
            so.FindProperty("_characterId").stringValue = characterId;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_iconEmoji").stringValue = emoji;
            so.FindProperty("_slotKey").intValue = slotKey;
            so.FindProperty("_abilityType").enumValueIndex = (int)type;
            so.FindProperty("_trigger").enumValueIndex = (int)trigger;
            so.FindProperty("_cooldownSec").floatValue = cooldownSec;
            so.FindProperty("_durationSec").floatValue = durationSec;
            so.FindProperty("_autoIntervalSec").floatValue = autoIntervalSec;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_angle").floatValue = angle;
            so.FindProperty("_maxInstances").intValue = maxInstances;
            so.FindProperty("_unlockGemCost").intValue = gemCost;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(a, path);
        }

        // ═════════════════════════════════════════════════════
        // 2) Ability 간 req 체인 연결
        // ═════════════════════════════════════════════════════
        static void LinkAbilityRequirements()
        {
            LinkReq("Ability_Victor_Flame", "Ability_Victor_Napalm");
            LinkReq("Ability_Victor_Mine",  "Ability_Victor_Napalm");
            LinkReq("Ability_Sara_Shockwave", "Ability_Sara_BlackHole");
            LinkReq("Ability_Sara_Meteor",    "Ability_Sara_Shockwave");
            LinkReq("Ability_Jinus_MiningDrone", "Ability_Jinus_Drone");
            LinkReq("Ability_Jinus_SpiderDrone", "Ability_Jinus_MiningDrone");
        }

        static void LinkReq(string childFile, string parentFile)
        {
            var child  = AssetDatabase.LoadAssetAtPath<AbilityData>($"{ABIL_DIR}/{childFile}.asset");
            var parent = AssetDatabase.LoadAssetAtPath<AbilityData>($"{ABIL_DIR}/{parentFile}.asset");
            if (child == null || parent == null)
            {
                Debug.LogWarning($"[V2DataSetup] req 연결 실패: {childFile} -> {parentFile}");
                return;
            }
            var so = new SerializedObject(child);
            so.FindProperty("_requiredAbility").objectReferenceValue = parent;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ═════════════════════════════════════════════════════
        // 3) CharacterData 3개 + Abilities[] 링크
        // ═════════════════════════════════════════════════════
        static void CreateCharacters()
        {
            MakeCharacter("Character_Victor", "victor", "빅터", "중장비 전문가",
                "네이팜·화염방사기·폭발지뢰로 화력을 극대화하는 전문가",
                new Color32(0xF4, 0xA4, 0x23, 0xFF), "Machine_Default",
                new[] { "Ability_Victor_Napalm", "Ability_Victor_Flame", "Ability_Victor_Mine" });

            MakeCharacter("Character_Sara", "sara", "사라", "방어 전문가",
                "블랙홀·충격파·반중력 메테오로 전장을 제어하는 제어 전문가",
                new Color32(0x4F, 0xC3, 0xF7, 0xFF), "Machine_Heavy",
                new[] { "Ability_Sara_BlackHole", "Ability_Sara_Shockwave", "Ability_Sara_Meteor" });

            MakeCharacter("Character_Jinus", "jinus", "지누스", "채굴 전문가",
                "드론 포탑·채굴 드론·드론 거미로 전장을 장악하는 자원 전문가",
                new Color32(0x51, 0xCF, 0x66, 0xFF), "Machine_Speed",
                new[] { "Ability_Jinus_Drone", "Ability_Jinus_MiningDrone", "Ability_Jinus_SpiderDrone" });
        }

        static void MakeCharacter(string fileName, string characterId, string displayName, string title,
            string description, Color themeColor, string machineFileName, string[] abilityFileNames)
        {
            string path = $"{CHAR_DIR}/{fileName}.asset";
            if (File.Exists(path)) { Debug.Log($"[V2DataSetup] {fileName} 이미 존재 — 스킵"); return; }

            var c = ScriptableObject.CreateInstance<CharacterData>();
            var so = new SerializedObject(c);
            so.FindProperty("_characterId").stringValue = characterId;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_title").stringValue = title;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_themeColor").colorValue = themeColor;

            // 기본 머신 연결
            var machine = AssetDatabase.LoadAssetAtPath<MachineData>(
                $"{DATA_ROOT}/Machines/{machineFileName}.asset");
            so.FindProperty("_defaultMachine").objectReferenceValue = machine;

            // Abilities[3] 연결
            var abilitiesProp = so.FindProperty("_abilities");
            abilitiesProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var ability = AssetDatabase.LoadAssetAtPath<AbilityData>(
                    $"{ABIL_DIR}/{abilityFileNames[i]}.asset");
                abilitiesProp.GetArrayElementAtIndex(i).objectReferenceValue = ability;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(c, path);
        }

        // ═════════════════════════════════════════════════════
        // 4) WeaponUpgradeData 15개
        // ═════════════════════════════════════════════════════
        static void CreateWeaponUpgrades()
        {
            // sniper
            MakeWpnUpg("WeaponUpgrade_Sniper_Damage",   "sniper_dmg",   "sniper", "저격총 데미지",
                WeaponUpgradeStat.Damage, 5, 0.25f, true, WeaponUpgradeOp.Multiply, 40, 2, 2.0f, 2.0f);
            MakeWpnUpg("WeaponUpgrade_Sniper_Range",    "sniper_range", "sniper", "저격총 범위",
                WeaponUpgradeStat.Range,  3, 0.20f, true, WeaponUpgradeOp.Multiply, 55, 3, 2.1f, 2.0f);
            MakeWpnUpg("WeaponUpgrade_Sniper_Cooldown", "sniper_cd",    "sniper", "저격총 연사",
                WeaponUpgradeStat.Cooldown, 4, -0.20f, true, WeaponUpgradeOp.Multiply, 45, 2, 2.1f, 2.2f);

            // bomb
            MakeWpnUpg("WeaponUpgrade_Bomb_Damage",   "bomb_dmg",    "bomb", "폭탄 데미지",
                WeaponUpgradeStat.Damage, 4, 0.30f, true, WeaponUpgradeOp.Multiply, 60, 4, 2.0f, 2.0f);
            MakeWpnUpg("WeaponUpgrade_Bomb_Radius",   "bomb_radius", "bomb", "폭탄 범위",
                WeaponUpgradeStat.Radius, 4, 0.20f, true, WeaponUpgradeOp.Multiply, 55, 3, 2.1f, 2.0f);
            MakeWpnUpg("WeaponUpgrade_Bomb_Cooldown", "bomb_cd",     "bomb", "폭탄 쿨타임",
                WeaponUpgradeStat.Cooldown, 4, -0.15f, true, WeaponUpgradeOp.Multiply, 45, 3, 2.0f, 2.0f);

            // gun
            MakeWpnUpg("WeaponUpgrade_Gun_Damage", "gun_dmg",    "gun", "기관총 데미지",
                WeaponUpgradeStat.Damage, 5, 0.25f, true, WeaponUpgradeOp.Multiply, 70, 5, 2.0f, 2.0f);
            MakeWpnUpg("WeaponUpgrade_Gun_Ammo",   "gun_ammo",   "gun", "탄창 증가",
                WeaponUpgradeStat.AmmoBonus, 4, 10f, false, WeaponUpgradeOp.Add, 55, 4, 2.1f, 2.0f);
            MakeWpnUpg("WeaponUpgrade_Gun_Reload", "gun_reload", "gun", "재장전 단축",
                WeaponUpgradeStat.ReloadTime, 4, -0.20f, true, WeaponUpgradeOp.Multiply, 50, 3, 2.1f, 2.2f);

            // laser
            MakeWpnUpg("WeaponUpgrade_Laser_Damage",   "laser_dmg",   "laser", "레이저 데미지",
                WeaponUpgradeStat.Damage, 5, 0.25f, true, WeaponUpgradeOp.Multiply, 85, 6, 2.0f, 2.0f);
            MakeWpnUpg("WeaponUpgrade_Laser_Range",    "laser_range", "laser", "레이저 범위",
                WeaponUpgradeStat.Range,  4, 0.20f, true, WeaponUpgradeOp.Multiply, 70, 5, 2.1f, 2.0f);
            MakeWpnUpg("WeaponUpgrade_Laser_Cooldown", "laser_cd",    "laser", "레이저 쿨타임",
                WeaponUpgradeStat.Cooldown, 4, -0.15f, true, WeaponUpgradeOp.Multiply, 60, 4, 2.1f, 2.2f);

            // saw
            MakeWpnUpg("WeaponUpgrade_Saw_Damage", "saw_dmg",    "saw", "톱날 데미지",
                WeaponUpgradeStat.Damage, 5, 0.20f, true, WeaponUpgradeOp.Multiply, 85, 7, 2.0f, 2.0f);
            MakeWpnUpg("WeaponUpgrade_Saw_Radius", "saw_radius", "saw", "톱날 사거리",
                WeaponUpgradeStat.Radius, 4, 0.25f, true, WeaponUpgradeOp.Multiply, 80, 6, 2.1f, 2.0f);
            MakeWpnUpg("WeaponUpgrade_Saw_Slow",   "saw_slow",   "saw", "슬로우 강화",
                WeaponUpgradeStat.SlowBonus, 3, 0.20f, false, WeaponUpgradeOp.Add, 95, 8, 2.1f, 2.0f);
        }

        static void MakeWpnUpg(string fileName, string upgradeId, string weaponId, string displayName,
            WeaponUpgradeStat stat, int maxLv, float valuePerLv, bool isPercentage, WeaponUpgradeOp op,
            int baseOre, int baseGem, float oreMult, float gemMult)
        {
            string path = $"{WUPG_DIR}/{fileName}.asset";
            if (File.Exists(path)) { Debug.Log($"[V2DataSetup] {fileName} 이미 존재 — 스킵"); return; }

            var u = ScriptableObject.CreateInstance<WeaponUpgradeData>();
            var so = new SerializedObject(u);
            so.FindProperty("_upgradeId").stringValue = upgradeId;
            so.FindProperty("_weaponId").stringValue = weaponId;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_targetStat").enumValueIndex = (int)stat;
            so.FindProperty("_maxLevel").intValue = maxLv;
            so.FindProperty("_valuePerLevel").floatValue = valuePerLv;
            so.FindProperty("_isPercentage").boolValue = isPercentage;
            so.FindProperty("_operation").enumValueIndex = (int)op;
            so.FindProperty("_baseCostOre").intValue = baseOre;
            so.FindProperty("_baseCostGem").intValue = baseGem;
            so.FindProperty("_oreCostMultiplier").floatValue = oreMult;
            so.FindProperty("_gemCostMultiplier").floatValue = gemMult;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(u, path);
        }

        // ═════════════════════════════════════════════════════
        // 5) UpgradeData 3개 (굴착기 강화 v2 신규)
        // ═════════════════════════════════════════════════════
        static void CreateV2Upgrades()
        {
            MakeUpgrade("Upgrade_MiningTarget", "mine_target", "목표량 확장",
                "세션 목표 채굴량 +50", UpgradeType.MiningTarget,
                maxLv: 5, valuePerLv: 50f, isPercent: false,
                baseOre: 100, baseGem: 0, costMult: 2.0f);

            MakeUpgrade("Upgrade_GemDrop", "gem_drop", "보석 출현 확률",
                "벌레 처치 시 보석 드랍 확률 +2%", UpgradeType.GemDropRate,
                maxLv: 5, valuePerLv: 0.02f, isPercent: false,
                baseOre: 0, baseGem: 15, costMult: 2.0f);

            MakeUpgrade("Upgrade_GemSpeed", "gem_speed", "보석 채집 속도",
                "마우스 호버 채집 속도 +20%", UpgradeType.GemCollectSpeed,
                maxLv: 5, valuePerLv: 0.20f, isPercent: true,
                baseOre: 0, baseGem: 10, costMult: 2.2f);
        }

        static void MakeUpgrade(string fileName, string upgradeId, string displayName, string desc,
            UpgradeType type, int maxLv, float valuePerLv, bool isPercent,
            int baseOre, int baseGem, float costMult)
        {
            string path = $"{UPG_DIR}/{fileName}.asset";
            if (File.Exists(path)) { Debug.Log($"[V2DataSetup] {fileName} 이미 존재 — 스킵"); return; }

            var u = ScriptableObject.CreateInstance<UpgradeData>();
            var so = new SerializedObject(u);
            so.FindProperty("_upgradeId").stringValue = upgradeId;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_upgradeType").enumValueIndex = (int)type;
            so.FindProperty("_maxLevel").intValue = maxLv;
            so.FindProperty("_baseValue").floatValue = 0f;
            so.FindProperty("_valuePerLevel").floatValue = valuePerLv;
            so.FindProperty("_isPercentage").boolValue = isPercent;
            so.FindProperty("_baseCost").intValue = baseOre;
            so.FindProperty("_baseCostGem").intValue = baseGem;
            so.FindProperty("_costMultiplier").floatValue = costMult;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(u, path);
        }
    }
}

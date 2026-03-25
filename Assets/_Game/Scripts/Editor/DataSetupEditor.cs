using UnityEngine;
using UnityEditor;
using DrillCorp.Data;
using System.IO;

namespace DrillCorp.Editor
{
    public class DataSetupEditor : EditorWindow
    {
        [MenuItem("Tools/Drill-Corp/Setup Data Assets")]
        public static void SetupDataAssets()
        {
            CreateFolders();
            CreateBugAssets();
            CreateWaveAssets();
            CreateMachineAssets();
            CreateUpgradeAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DataSetupEditor] All data assets created successfully!");
        }

        private static void CreateFolders()
        {
            string dataPath = "Assets/_Game/Data";

            if (!AssetDatabase.IsValidFolder(dataPath))
                AssetDatabase.CreateFolder("Assets/_Game", "Data");

            if (!AssetDatabase.IsValidFolder(dataPath + "/Bugs"))
                AssetDatabase.CreateFolder(dataPath, "Bugs");

            if (!AssetDatabase.IsValidFolder(dataPath + "/Waves"))
                AssetDatabase.CreateFolder(dataPath, "Waves");

            if (!AssetDatabase.IsValidFolder(dataPath + "/Machines"))
                AssetDatabase.CreateFolder(dataPath, "Machines");

            if (!AssetDatabase.IsValidFolder(dataPath + "/Upgrades"))
                AssetDatabase.CreateFolder(dataPath, "Upgrades");
        }

        private static void CreateBugAssets()
        {
            string path = "Assets/_Game/Data/Bugs/";

            // Bug_Beetle
            if (!File.Exists(path + "Bug_Beetle.asset"))
            {
                var beetle = ScriptableObject.CreateInstance<BugData>();
                SetBugData(beetle, 1, "Beetle", "Basic melee bug",
                    maxHealth: 15f, moveSpeed: 2f, attackDamage: 5f, attackCooldown: 1f, attackRange: 1f, currencyReward: 1);
                AssetDatabase.CreateAsset(beetle, path + "Bug_Beetle.asset");
            }

            // Bug_Fly
            if (!File.Exists(path + "Bug_Fly.asset"))
            {
                var fly = ScriptableObject.CreateInstance<BugData>();
                SetBugData(fly, 2, "Fly", "Fast but weak",
                    maxHealth: 8f, moveSpeed: 4f, attackDamage: 3f, attackCooldown: 0.5f, attackRange: 1f, currencyReward: 1);
                AssetDatabase.CreateAsset(fly, path + "Bug_Fly.asset");
            }

            // Bug_Centipede
            if (!File.Exists(path + "Bug_Centipede.asset"))
            {
                var centipede = ScriptableObject.CreateInstance<BugData>();
                SetBugData(centipede, 3, "Centipede", "Tanky and slow",
                    maxHealth: 40f, moveSpeed: 1f, attackDamage: 10f, attackCooldown: 2f, attackRange: 1.5f, currencyReward: 3);
                AssetDatabase.CreateAsset(centipede, path + "Bug_Centipede.asset");
            }

            // Bug_Spider
            if (!File.Exists(path + "Bug_Spider.asset"))
            {
                var spider = ScriptableObject.CreateInstance<BugData>();
                SetBugData(spider, 4, "Spider", "Balanced stats",
                    maxHealth: 20f, moveSpeed: 2.5f, attackDamage: 7f, attackCooldown: 1.2f, attackRange: 1.2f, currencyReward: 2);
                AssetDatabase.CreateAsset(spider, path + "Bug_Spider.asset");
            }

            // Bug_Wasp
            if (!File.Exists(path + "Bug_Wasp.asset"))
            {
                var wasp = ScriptableObject.CreateInstance<BugData>();
                SetBugData(wasp, 5, "Wasp", "High damage, low health",
                    maxHealth: 12f, moveSpeed: 3f, attackDamage: 12f, attackCooldown: 1.5f, attackRange: 1f, currencyReward: 2);
                AssetDatabase.CreateAsset(wasp, path + "Bug_Wasp.asset");
            }

            Debug.Log("[DataSetupEditor] Bug assets created");
        }

        private static void SetBugData(BugData bug, int id, string name, string desc,
            float maxHealth, float moveSpeed, float attackDamage, float attackCooldown, float attackRange, int currencyReward)
        {
            var so = new SerializedObject(bug);
            so.FindProperty("_bugId").intValue = id;
            so.FindProperty("_bugName").stringValue = name;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_maxHealth").floatValue = maxHealth;
            so.FindProperty("_moveSpeed").floatValue = moveSpeed;
            so.FindProperty("_attackDamage").floatValue = attackDamage;
            so.FindProperty("_attackCooldown").floatValue = attackCooldown;
            so.FindProperty("_attackRange").floatValue = attackRange;
            so.FindProperty("_currencyReward").intValue = currencyReward;
            so.FindProperty("_scale").floatValue = 1f;
            so.FindProperty("_dropChance").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateWaveAssets()
        {
            string bugPath = "Assets/_Game/Data/Bugs/";
            string wavePath = "Assets/_Game/Data/Waves/";

            // Load bug data
            var beetle = AssetDatabase.LoadAssetAtPath<BugData>(bugPath + "Bug_Beetle.asset");
            var fly = AssetDatabase.LoadAssetAtPath<BugData>(bugPath + "Bug_Fly.asset");
            var centipede = AssetDatabase.LoadAssetAtPath<BugData>(bugPath + "Bug_Centipede.asset");
            var spider = AssetDatabase.LoadAssetAtPath<BugData>(bugPath + "Bug_Spider.asset");
            var wasp = AssetDatabase.LoadAssetAtPath<BugData>(bugPath + "Bug_Wasp.asset");

            // Wave 1 - Tutorial
            if (!File.Exists(wavePath + "Wave_01.asset"))
            {
                var wave1 = ScriptableObject.CreateInstance<WaveData>();
                SetWaveData(wave1, 1, "First Contact",
                    new SpawnGroup[] {
                        new SpawnGroup { BugData = beetle, Count = 5, StartDelay = 0f, SpawnInterval = 2f }
                    },
                    waveDuration: 30f, healthMult: 1f, damageMult: 1f, speedMult: 1f);
                AssetDatabase.CreateAsset(wave1, wavePath + "Wave_01.asset");
            }

            // Wave 2 - Mix
            if (!File.Exists(wavePath + "Wave_02.asset"))
            {
                var wave2 = ScriptableObject.CreateInstance<WaveData>();
                SetWaveData(wave2, 2, "Swarm Incoming",
                    new SpawnGroup[] {
                        new SpawnGroup { BugData = beetle, Count = 5, StartDelay = 0f, SpawnInterval = 1.5f },
                        new SpawnGroup { BugData = fly, Count = 3, StartDelay = 5f, SpawnInterval = 1f }
                    },
                    waveDuration: 40f, healthMult: 1f, damageMult: 1f, speedMult: 1f);
                AssetDatabase.CreateAsset(wave2, wavePath + "Wave_02.asset");
            }

            // Wave 3 - Tank
            if (!File.Exists(wavePath + "Wave_03.asset"))
            {
                var wave3 = ScriptableObject.CreateInstance<WaveData>();
                SetWaveData(wave3, 3, "Heavy Hitters",
                    new SpawnGroup[] {
                        new SpawnGroup { BugData = beetle, Count = 4, StartDelay = 0f, SpawnInterval = 1.5f },
                        new SpawnGroup { BugData = centipede, Count = 2, StartDelay = 3f, SpawnInterval = 5f }
                    },
                    waveDuration: 45f, healthMult: 1.1f, damageMult: 1f, speedMult: 1f);
                AssetDatabase.CreateAsset(wave3, wavePath + "Wave_03.asset");
            }

            // Wave 4 - Rush
            if (!File.Exists(wavePath + "Wave_04.asset"))
            {
                var wave4 = ScriptableObject.CreateInstance<WaveData>();
                SetWaveData(wave4, 4, "Speed Rush",
                    new SpawnGroup[] {
                        new SpawnGroup { BugData = fly, Count = 8, StartDelay = 0f, SpawnInterval = 0.8f },
                        new SpawnGroup { BugData = spider, Count = 3, StartDelay = 8f, SpawnInterval = 2f }
                    },
                    waveDuration: 50f, healthMult: 1.1f, damageMult: 1.1f, speedMult: 1.1f);
                AssetDatabase.CreateAsset(wave4, wavePath + "Wave_04.asset");
            }

            // Wave 5 - Boss Wave
            if (!File.Exists(wavePath + "Wave_05.asset"))
            {
                var wave5 = ScriptableObject.CreateInstance<WaveData>();
                SetWaveData(wave5, 5, "Final Stand",
                    new SpawnGroup[] {
                        new SpawnGroup { BugData = beetle, Count = 6, StartDelay = 0f, SpawnInterval = 1f },
                        new SpawnGroup { BugData = wasp, Count = 4, StartDelay = 5f, SpawnInterval = 2f },
                        new SpawnGroup { BugData = centipede, Count = 3, StartDelay = 10f, SpawnInterval = 4f }
                    },
                    waveDuration: 60f, healthMult: 1.2f, damageMult: 1.2f, speedMult: 1f);
                AssetDatabase.CreateAsset(wave5, wavePath + "Wave_05.asset");
            }

            Debug.Log("[DataSetupEditor] Wave assets created");
        }

        private static void SetWaveData(WaveData wave, int number, string name, SpawnGroup[] groups,
            float waveDuration, float healthMult, float damageMult, float speedMult)
        {
            var so = new SerializedObject(wave);
            so.FindProperty("_waveNumber").intValue = number;
            so.FindProperty("_waveName").stringValue = name;
            so.FindProperty("_waveDuration").floatValue = waveDuration;
            so.FindProperty("_delayBeforeNextWave").floatValue = 3f;
            so.FindProperty("_healthMultiplier").floatValue = healthMult;
            so.FindProperty("_damageMultiplier").floatValue = damageMult;
            so.FindProperty("_speedMultiplier").floatValue = speedMult;

            var groupsProp = so.FindProperty("_spawnGroups");
            groupsProp.arraySize = groups.Length;
            for (int i = 0; i < groups.Length; i++)
            {
                var element = groupsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("BugData").objectReferenceValue = groups[i].BugData;
                element.FindPropertyRelative("Count").intValue = groups[i].Count;
                element.FindPropertyRelative("StartDelay").floatValue = groups[i].StartDelay;
                element.FindPropertyRelative("SpawnInterval").floatValue = groups[i].SpawnInterval;
                element.FindPropertyRelative("RandomPosition").boolValue = true;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateMachineAssets()
        {
            string path = "Assets/_Game/Data/Machines/";

            // Machine_Default
            if (!File.Exists(path + "Machine_Default.asset"))
            {
                var machine = ScriptableObject.CreateInstance<MachineData>();
                SetMachineData(machine, 1, "Drill-01", "Standard mining drill",
                    maxHealth: 100f, maxFuel: 60f, fuelRate: 1f,
                    miningRate: 10f, attackDamage: 20f, attackCooldown: 0.5f, attackRange: 3f);
                AssetDatabase.CreateAsset(machine, path + "Machine_Default.asset");
            }

            // Machine_Heavy
            if (!File.Exists(path + "Machine_Heavy.asset"))
            {
                var heavy = ScriptableObject.CreateInstance<MachineData>();
                SetMachineData(heavy, 2, "Drill-Heavy", "Armored mining drill with more health",
                    maxHealth: 150f, maxFuel: 50f, fuelRate: 1.2f,
                    miningRate: 8f, attackDamage: 25f, attackCooldown: 0.7f, attackRange: 2.5f);
                var so = new SerializedObject(heavy);
                so.FindProperty("_armor").floatValue = 15f;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(heavy, path + "Machine_Heavy.asset");
            }

            // Machine_Speed
            if (!File.Exists(path + "Machine_Speed.asset"))
            {
                var speed = ScriptableObject.CreateInstance<MachineData>();
                SetMachineData(speed, 3, "Drill-Rapid", "Fast mining, fast shooting",
                    maxHealth: 80f, maxFuel: 45f, fuelRate: 1.5f,
                    miningRate: 15f, attackDamage: 15f, attackCooldown: 0.3f, attackRange: 3.5f);
                AssetDatabase.CreateAsset(speed, path + "Machine_Speed.asset");
            }

            Debug.Log("[DataSetupEditor] Machine assets created");
        }

        private static void SetMachineData(MachineData machine, int id, string name, string desc,
            float maxHealth, float maxFuel, float fuelRate, float miningRate,
            float attackDamage, float attackCooldown, float attackRange)
        {
            var so = new SerializedObject(machine);
            so.FindProperty("_machineId").intValue = id;
            so.FindProperty("_machineName").stringValue = name;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_maxHealth").floatValue = maxHealth;
            so.FindProperty("_maxFuel").floatValue = maxFuel;
            so.FindProperty("_fuelConsumeRate").floatValue = fuelRate;
            so.FindProperty("_miningRate").floatValue = miningRate;
            so.FindProperty("_attackDamage").floatValue = attackDamage;
            so.FindProperty("_attackCooldown").floatValue = attackCooldown;
            so.FindProperty("_attackRange").floatValue = attackRange;
            so.FindProperty("_healthRegen").floatValue = 0f;
            so.FindProperty("_armor").floatValue = 0f;
            so.FindProperty("_miningBonus").floatValue = 0f;
            so.FindProperty("_critChance").floatValue = 0f;
            so.FindProperty("_critMultiplier").floatValue = 1.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateUpgradeAssets()
        {
            string path = "Assets/_Game/Data/Upgrades/";

            // Upgrade_MaxHealth
            if (!File.Exists(path + "Upgrade_MaxHealth.asset"))
            {
                var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
                SetUpgradeData(upgrade, "max_health", "Max HP", "Increase maximum health",
                    UpgradeType.MaxHealth, maxLevel: 10, baseValue: 0f, valuePerLevel: 10f,
                    isPercent: false, baseCost: 100, costMult: 1.5f);
                AssetDatabase.CreateAsset(upgrade, path + "Upgrade_MaxHealth.asset");
            }

            // Upgrade_Armor
            if (!File.Exists(path + "Upgrade_Armor.asset"))
            {
                var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
                SetUpgradeData(upgrade, "armor", "Armor", "Reduce damage taken",
                    UpgradeType.Armor, maxLevel: 10, baseValue: 0f, valuePerLevel: 5f,
                    isPercent: false, baseCost: 150, costMult: 1.6f);
                AssetDatabase.CreateAsset(upgrade, path + "Upgrade_Armor.asset");
            }

            // Upgrade_MiningRate
            if (!File.Exists(path + "Upgrade_MiningRate.asset"))
            {
                var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
                SetUpgradeData(upgrade, "mining_rate", "Mining", "Increase mining rate",
                    UpgradeType.MiningRate, maxLevel: 10, baseValue: 0f, valuePerLevel: 5f,
                    isPercent: true, baseCost: 100, costMult: 1.4f);
                AssetDatabase.CreateAsset(upgrade, path + "Upgrade_MiningRate.asset");
            }

            // Upgrade_AttackDamage
            if (!File.Exists(path + "Upgrade_AttackDamage.asset"))
            {
                var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
                SetUpgradeData(upgrade, "attack_damage", "Damage", "Increase attack damage",
                    UpgradeType.AttackDamage, maxLevel: 10, baseValue: 0f, valuePerLevel: 5f,
                    isPercent: true, baseCost: 120, costMult: 1.5f);
                AssetDatabase.CreateAsset(upgrade, path + "Upgrade_AttackDamage.asset");
            }

            // Upgrade_AttackSpeed
            if (!File.Exists(path + "Upgrade_AttackSpeed.asset"))
            {
                var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
                SetUpgradeData(upgrade, "attack_speed", "Attack Speed", "Increase attack speed",
                    UpgradeType.AttackSpeed, maxLevel: 10, baseValue: 0f, valuePerLevel: 5f,
                    isPercent: true, baseCost: 120, costMult: 1.5f);
                AssetDatabase.CreateAsset(upgrade, path + "Upgrade_AttackSpeed.asset");
            }

            // Upgrade_FuelEfficiency
            if (!File.Exists(path + "Upgrade_FuelEfficiency.asset"))
            {
                var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
                SetUpgradeData(upgrade, "fuel_efficiency", "Fuel Efficiency", "Reduce fuel consumption",
                    UpgradeType.FuelEfficiency, maxLevel: 10, baseValue: 0f, valuePerLevel: 3f,
                    isPercent: true, baseCost: 80, costMult: 1.4f);
                AssetDatabase.CreateAsset(upgrade, path + "Upgrade_FuelEfficiency.asset");
            }

            Debug.Log("[DataSetupEditor] Upgrade assets created");
        }

        private static void SetUpgradeData(UpgradeData upgrade, string id, string displayName, string desc,
            UpgradeType type, int maxLevel, float baseValue, float valuePerLevel,
            bool isPercent, int baseCost, float costMult)
        {
            var so = new SerializedObject(upgrade);
            so.FindProperty("_upgradeId").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_upgradeType").enumValueIndex = (int)type;
            so.FindProperty("_maxLevel").intValue = maxLevel;
            so.FindProperty("_baseValue").floatValue = baseValue;
            so.FindProperty("_valuePerLevel").floatValue = valuePerLevel;
            so.FindProperty("_isPercentage").boolValue = isPercent;
            so.FindProperty("_baseCost").intValue = baseCost;
            so.FindProperty("_costMultiplier").floatValue = costMult;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

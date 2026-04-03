using UnityEngine;
using UnityEditor;
using DrillCorp.Bug;
using DrillCorp.Bug.Behaviors.Data;
using TMPro;

namespace DrillCorp.Editor
{
    /// <summary>
    /// 행동 테스트용 Bug 프리펩 생성 도구
    /// 각 행동 조합을 테스트할 수 있는 Bug 프리펩을 생성합니다.
    /// </summary>
    public static class BugTestPrefabCreator
    {
        private const string PrefabPath = "Assets/_Game/Prefabs/Bugs/Test";
        private const string BehaviorDataPath = "Assets/_Game/Data/BugBehaviors";
        private const string BugDataPath = "Assets/_Game/Data/Bugs";
        private const string HpBarPrefabPath = "Assets/_Game/Prefabs/UI/BugHpBar.prefab";

        /// <summary>
        /// 테스트 Bug 정의
        /// </summary>
        private const string ModelsPath = "Assets/_Game/Models";

        private static readonly TestBugDefinition[] TestBugs = new TestBugDefinition[]
        {
            // Movement 테스트
            new TestBugDefinition
            {
                Name = "Test_Linear",
                Label = "Linear\nMelee",
                MovementType = "Linear",
                AttackType = "Melee",
                Passives = new string[0],
                Color = new Color(1f, 0.3f, 0.3f), // 빨강
                ModelName = "SM_Bug_A_01",
                Scale = 0.2f
            },
            new TestBugDefinition
            {
                Name = "Test_Hover",
                Label = "Hover\nMelee",
                MovementType = "Hover",
                AttackType = "Melee",
                Passives = new string[0],
                Color = new Color(0.3f, 0.5f, 1f), // 파랑
                ModelName = "SM_Bug_B_01",
                Scale = 0.22f
            },
            new TestBugDefinition
            {
                Name = "Test_Burst",
                Label = "Burst\nMelee",
                MovementType = "Burst",
                AttackType = "Melee",
                Passives = new string[0],
                Color = new Color(0.3f, 1f, 0.3f), // 초록
                ModelName = "SM_Bug_C_01",
                Scale = 0.18f
            },
            new TestBugDefinition
            {
                Name = "Test_Ranged",
                Label = "Ranged\nProjectile",
                MovementType = "Ranged",
                AttackType = "Projectile",
                Passives = new string[0],
                Color = new Color(1f, 1f, 0.3f), // 노랑
                ModelName = "SM_Bug_A_01",
                Scale = 0.25f
            },

            // Passive 테스트
            new TestBugDefinition
            {
                Name = "Test_Armor",
                Label = "Linear\nArmor",
                MovementType = "Linear",
                AttackType = "Melee",
                Passives = new string[] { "Armor" },
                Color = new Color(0.6f, 0.6f, 0.6f), // 회색
                ModelName = "SM_Bug_B_01",
                Scale = 0.3f // Armor는 크게
            },
            new TestBugDefinition
            {
                Name = "Test_Dodge",
                Label = "Linear\nDodge",
                MovementType = "Linear",
                AttackType = "Melee",
                Passives = new string[] { "Dodge" },
                Color = new Color(0f, 1f, 1f), // 청록
                ModelName = "SM_Bug_C_01",
                Scale = 0.15f // Dodge는 작게
            },
            new TestBugDefinition
            {
                Name = "Test_ArmorDodge",
                Label = "Linear\nArmor+Dodge",
                MovementType = "Linear",
                AttackType = "Melee",
                Passives = new string[] { "Armor", "Dodge" },
                Color = new Color(0.8f, 0.3f, 1f), // 보라
                ModelName = "SM_Bug_A_01",
                Scale = 0.28f
            },

            // 복합 테스트
            new TestBugDefinition
            {
                Name = "Test_RangedArmor",
                Label = "Ranged\nProj+Armor",
                MovementType = "Ranged",
                AttackType = "Projectile",
                Passives = new string[] { "Armor" },
                Color = new Color(1f, 0.5f, 0f), // 주황
                ModelName = "SM_Bug_B_01",
                Scale = 0.35f // 원거리 Armor는 제일 크게
            },

            // ===== Phase 2 테스트 =====

            // Movement Phase 2
            new TestBugDefinition
            {
                Name = "Test_Retreat",
                Label = "Retreat\nMelee",
                MovementType = "Retreat",
                AttackType = "Melee",
                Color = new Color(0.8f, 0.4f, 0.6f), // 분홍
                ModelName = "SM_Bug_C_01",
                Scale = 0.2f
            },
            new TestBugDefinition
            {
                Name = "Test_Orbit",
                Label = "Orbit\nProjectile",
                MovementType = "Orbit",
                AttackType = "Projectile",
                Color = new Color(0.4f, 0.8f, 0.6f), // 민트
                ModelName = "SM_Bug_A_01",
                Scale = 0.22f
            },

            // Attack Phase 2
            new TestBugDefinition
            {
                Name = "Test_Cleave",
                Label = "Linear\nCleave",
                MovementType = "Linear",
                AttackType = "Cleave",
                Color = new Color(0.9f, 0.2f, 0.2f), // 진한 빨강
                ModelName = "SM_Bug_B_01",
                Scale = 0.28f
            },
            new TestBugDefinition
            {
                Name = "Test_Spread",
                Label = "Ranged\nSpread",
                MovementType = "Ranged",
                AttackType = "Spread",
                Color = new Color(0.9f, 0.6f, 0.2f), // 황금색
                ModelName = "SM_Bug_A_01",
                Scale = 0.25f
            },

            // Passive Phase 2
            new TestBugDefinition
            {
                Name = "Test_Shield",
                Label = "Linear\nShield",
                MovementType = "Linear",
                AttackType = "Melee",
                Passives = new string[] { "Shield" },
                Color = new Color(0.3f, 0.7f, 1f), // 하늘색
                ModelName = "SM_Bug_B_01",
                Scale = 0.26f
            },
            new TestBugDefinition
            {
                Name = "Test_Regen",
                Label = "Linear\nRegen",
                MovementType = "Linear",
                AttackType = "Melee",
                Passives = new string[] { "Regen" },
                Color = new Color(0.3f, 1f, 0.5f), // 밝은 초록
                ModelName = "SM_Bug_C_01",
                Scale = 0.22f
            },
            new TestBugDefinition
            {
                Name = "Test_Poison",
                Label = "Linear\nPoison",
                MovementType = "Linear",
                AttackType = "Melee",
                Passives = new string[] { "PoisonAttack" },
                Color = new Color(0.5f, 0.2f, 0.8f), // 보라
                ModelName = "SM_Bug_A_01",
                Scale = 0.2f
            },

            // Trigger Phase 2
            new TestBugDefinition
            {
                Name = "Test_Enrage",
                Label = "Linear\nEnrage",
                MovementType = "Linear",
                AttackType = "Melee",
                Triggers = new string[] { "Enrage" },
                Color = new Color(1f, 0.3f, 0.1f), // 붉은 주황
                ModelName = "SM_Bug_B_01",
                Scale = 0.24f
            },
            new TestBugDefinition
            {
                Name = "Test_Explode",
                Label = "Burst\nExplode",
                MovementType = "Burst",
                AttackType = "Melee",
                Triggers = new string[] { "ExplodeOnDeath" },
                Color = new Color(1f, 0.8f, 0.2f), // 노란 주황
                ModelName = "SM_Bug_C_01",
                Scale = 0.3f
            },

            // 복합 Phase 2
            new TestBugDefinition
            {
                Name = "Test_OrbitPoison",
                Label = "Orbit\nProj+Poison",
                MovementType = "Orbit",
                AttackType = "Projectile",
                Passives = new string[] { "PoisonAttack" },
                Color = new Color(0.6f, 0.1f, 0.6f), // 진보라
                ModelName = "SM_Bug_A_01",
                Scale = 0.25f
            },
            new TestBugDefinition
            {
                Name = "Test_EnrageExplode",
                Label = "Linear\nEnrage+Explode",
                MovementType = "Linear",
                AttackType = "Melee",
                Triggers = new string[] { "Enrage", "ExplodeOnDeath" },
                Color = new Color(0.9f, 0.1f, 0.1f), // 강렬한 빨강
                ModelName = "SM_Bug_B_01",
                Scale = 0.32f
            },

            // ===== Phase 3 테스트 =====

            // Movement Phase 3
            new TestBugDefinition
            {
                Name = "Test_Teleport",
                Label = "Teleport\nMelee",
                MovementType = "Teleport",
                AttackType = "Melee",
                Color = new Color(0.6f, 0.2f, 1f), // 진한 보라 (순간이동)
                ModelName = "SM_Bug_C_01",
                Scale = 0.22f
            },
        };

        [MenuItem("Tools/Drill-Corp/1. 버그 설정/테스트/1. 테스트용 Bug 전체 생성 (Data + Prefab)", priority = 200)]
        public static void CreateAllTestPrefabs()
        {
            // 폴더 생성
            CreateFolders();

            // BehaviorData 생성
            CreateTestBehaviorData();

            // BugData 생성
            CreateTestBugDataInternal();

            // 프리펩 생성
            foreach (var def in TestBugs)
            {
                CreateTestBugPrefab(def);
            }

            // BugData에 Prefab 연결
            ConnectPrefabsToBugData();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BugTestPrefabCreator] {TestBugs.Length}개의 테스트 Bug 생성 완료!");
            EditorUtility.DisplayDialog("완료",
                $"{TestBugs.Length}개의 테스트용 Bug가 생성되었습니다.\n\n" +
                $"BugData: {BugDataPath}\n" +
                $"Prefab: {PrefabPath}\n" +
                $"BehaviorData: {BehaviorDataPath}/Test",
                "확인");
        }

        /// <summary>
        /// BugData만 개별 생성 (메뉴)
        /// </summary>
        [MenuItem("Tools/Drill-Corp/1. 버그 설정/테스트/2. 테스트용 BugData만 생성", priority = 201)]
        public static void CreateTestBugData()
        {
            CreateTestBugDataInternal();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("완료",
                $"테스트용 BugData가 생성되었습니다.\n\n경로: {BugDataPath}",
                "확인");
        }

        /// <summary>
        /// BugData 생성 (내부용)
        /// </summary>
        private static void CreateTestBugDataInternal()
        {
            string dataPath = BugDataPath;

            // 폴더 확인
            if (!AssetDatabase.IsValidFolder(dataPath))
            {
                string parent = System.IO.Path.GetDirectoryName(dataPath).Replace("\\", "/");
                string folderName = System.IO.Path.GetFileName(dataPath);
                AssetDatabase.CreateFolder(parent, folderName);
            }

            for (int i = 0; i < TestBugs.Length; i++)
            {
                var def = TestBugs[i];
                string assetPath = $"{dataPath}/Bug_{def.Name}.asset";

                if (System.IO.File.Exists(assetPath))
                {
                    Debug.Log($"[BugTestPrefabCreator] Bug_{def.Name}.asset already exists, skipping.");
                    continue;
                }

                var bugData = ScriptableObject.CreateInstance<DrillCorp.Data.BugData>();

                // 기본 스탯 설정 (리플렉션 사용)
                SetPrivateField(bugData, "_bugId", 100 + i);
                SetPrivateField(bugData, "_bugName", def.Name);
                SetPrivateField(bugData, "_description", def.Label.Replace("\n", " / "));
                SetPrivateField(bugData, "_maxHealth", 100f);
                SetPrivateField(bugData, "_moveSpeed", 2f);
                SetPrivateField(bugData, "_attackDamage", 10f);
                SetPrivateField(bugData, "_attackCooldown", 1f);
                SetPrivateField(bugData, "_tintColor", def.Color);

                // Ranged 타입은 사거리 늘림, 밀리는 거의 붙어서 공격
                float attackRange = def.AttackType == "Projectile" ? 5f : 0.05f;
                SetPrivateField(bugData, "_attackRange", attackRange);

                AssetDatabase.CreateAsset(bugData, assetPath);
                Debug.Log($"[BugTestPrefabCreator] Created: Bug_{def.Name}.asset (ID: {100 + i})");
            }
        }

        /// <summary>
        /// BugData에 Prefab 연결
        /// </summary>
        private static void ConnectPrefabsToBugData()
        {
            foreach (var def in TestBugs)
            {
                string bugDataPath = $"{BugDataPath}/Bug_{def.Name}.asset";
                string prefabPath = $"{PrefabPath}/Bug_{def.Name}.prefab";

                var bugData = AssetDatabase.LoadAssetAtPath<DrillCorp.Data.BugData>(bugDataPath);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (bugData == null || prefab == null) continue;

                // BugData에 Prefab 연결
                SetPrivateField(bugData, "_prefab", prefab);

                // Prefab의 BugController에 BugData 연결
                var bugController = prefab.GetComponent<BugController>();
                if (bugController != null)
                {
                    var so = new SerializedObject(bugController);
                    so.FindProperty("_bugData").objectReferenceValue = bugData;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                Debug.Log($"[BugTestPrefabCreator] Connected: Bug_{def.Name}.asset <-> Bug_{def.Name}.prefab");
            }
        }

        /// <summary>
        /// 테스트용 Wave 생성 - 모든 테스트 Bug가 등장
        /// </summary>
        [MenuItem("Tools/Drill-Corp/1. 버그 설정/테스트/3. 테스트용 Wave 생성", priority = 202)]
        public static void CreateTestWave()
        {
            string wavePath = "Assets/_Game/Data/Waves";

            // 폴더 확인
            if (!AssetDatabase.IsValidFolder(wavePath))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Waves");
            }

            string assetPath = $"{wavePath}/Wave_Test_AllBehaviors.asset";

            // 이미 존재하면 삭제 후 재생성
            if (System.IO.File.Exists(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var waveData = ScriptableObject.CreateInstance<DrillCorp.Data.WaveData>();

            // Wave 기본 설정
            SetPrivateField(waveData, "_waveNumber", 99);
            SetPrivateField(waveData, "_waveName", "Test - All Behaviors");
            SetPrivateField(waveData, "_waveDuration", 120f);
            SetPrivateField(waveData, "_delayBeforeNextWave", 5f);
            SetPrivateField(waveData, "_healthMultiplier", 1f);
            SetPrivateField(waveData, "_damageMultiplier", 1f);
            SetPrivateField(waveData, "_speedMultiplier", 1f);

            // SpawnGroup 생성 - 각 테스트 Bug 1마리씩
            var spawnGroups = new DrillCorp.Data.SpawnGroup[TestBugs.Length];

            for (int i = 0; i < TestBugs.Length; i++)
            {
                var def = TestBugs[i];
                string bugDataPath = $"{BugDataPath}/Bug_{def.Name}.asset";
                var bugData = AssetDatabase.LoadAssetAtPath<DrillCorp.Data.BugData>(bugDataPath);

                spawnGroups[i] = new DrillCorp.Data.SpawnGroup
                {
                    BugData = bugData,
                    Count = 1,
                    StartDelay = i * 2f,  // 2초 간격으로 순차 스폰
                    SpawnInterval = 0f,
                    RandomPosition = true
                };
            }

            SetPrivateField(waveData, "_spawnGroups", spawnGroups);

            AssetDatabase.CreateAsset(waveData, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = waveData;

            Debug.Log($"[BugTestPrefabCreator] Created: Wave_Test_AllBehaviors.asset");
            EditorUtility.DisplayDialog("완료",
                $"테스트용 Wave가 생성되었습니다.\n\n" +
                $"경로: {assetPath}\n\n" +
                $"총 {TestBugs.Length}종의 Bug가 2초 간격으로 스폰됩니다.",
                "확인");
        }

        private static void CreateFolders()
        {
            // Prefabs/Bugs/Test
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs/Bugs"))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "Bugs");
            if (!AssetDatabase.IsValidFolder(PrefabPath))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs/Bugs", "Test");

            // BugBehaviors/Test
            if (!AssetDatabase.IsValidFolder(BehaviorDataPath))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "BugBehaviors");
            }
            if (!AssetDatabase.IsValidFolder(BehaviorDataPath + "/Test"))
            {
                AssetDatabase.CreateFolder(BehaviorDataPath, "Test");
            }
        }

        private static void CreateTestBehaviorData()
        {
            string folder = BehaviorDataPath + "/Test";

            foreach (var def in TestBugs)
            {
                string assetPath = $"{folder}/BugBehavior_{def.Name}.asset";

                if (System.IO.File.Exists(assetPath))
                {
                    continue;
                }

                var behaviorData = ScriptableObject.CreateInstance<BugBehaviorData>();

                // Movement 연결
                string movementPath = $"{BehaviorDataPath}/Movement/Movement_{def.MovementType}.asset";
                var movement = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>(movementPath);
                if (movement != null)
                {
                    SetPrivateField(behaviorData, "_defaultMovement", movement);
                }

                // Attack 연결
                string attackPath = $"{BehaviorDataPath}/Attack/Attack_{def.AttackType}.asset";
                var attack = AssetDatabase.LoadAssetAtPath<AttackBehaviorData>(attackPath);
                if (attack != null)
                {
                    SetPrivateField(behaviorData, "_defaultAttack", attack);
                }

                // Passives 연결
                if (def.Passives != null && def.Passives.Length > 0)
                {
                    var passiveList = new System.Collections.Generic.List<PassiveBehaviorData>();
                    foreach (string passiveName in def.Passives)
                    {
                        string passivePath = $"{BehaviorDataPath}/Passive/Passive_{passiveName}.asset";
                        var passive = AssetDatabase.LoadAssetAtPath<PassiveBehaviorData>(passivePath);
                        if (passive != null)
                        {
                            passiveList.Add(passive);
                        }
                    }
                    SetPrivateField(behaviorData, "_passives", passiveList);
                }

                // Triggers 연결 (Phase 2)
                if (def.Triggers != null && def.Triggers.Length > 0)
                {
                    var triggerList = new System.Collections.Generic.List<TriggerBehaviorData>();
                    foreach (string triggerName in def.Triggers)
                    {
                        string triggerPath = $"{BehaviorDataPath}/Trigger/Trigger_{triggerName}.asset";
                        var trigger = AssetDatabase.LoadAssetAtPath<TriggerBehaviorData>(triggerPath);
                        if (trigger != null)
                        {
                            triggerList.Add(trigger);
                        }
                    }
                    SetPrivateField(behaviorData, "_triggers", triggerList);
                }

                AssetDatabase.CreateAsset(behaviorData, assetPath);
                Debug.Log($"[BugTestPrefabCreator] Created: BugBehavior_{def.Name}.asset");
            }
        }

        private static void CreateTestBugPrefab(TestBugDefinition def)
        {
            string fullPath = $"{PrefabPath}/Bug_{def.Name}.prefab";

            if (System.IO.File.Exists(fullPath))
            {
                Debug.Log($"[BugTestPrefabCreator] Bug_{def.Name}.prefab already exists, skipping.");
                return;
            }

            // Root 오브젝트
            GameObject bugObj = new GameObject($"Bug_{def.Name}");

            // BugController 컴포넌트 추가
            BugController bugController = bugObj.AddComponent<BugController>();

            // BehaviorData 연결
            string behaviorPath = $"{BehaviorDataPath}/Test/BugBehavior_{def.Name}.asset";
            var behaviorData = AssetDatabase.LoadAssetAtPath<BugBehaviorData>(behaviorPath);
            if (behaviorData != null)
            {
                var so = new SerializedObject(bugController);
                so.FindProperty("_behaviorData").objectReferenceValue = behaviorData;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Visual (FBX 모델 사용)
            GameObject visual = null;
            string modelPath = $"{ModelsPath}/{def.ModelName}.fbx";
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

            if (modelPrefab != null)
            {
                // FBX 모델 인스턴스화
                visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
                visual.name = "Visual";
                visual.transform.SetParent(bugObj.transform);
                visual.transform.localPosition = Vector3.zero;
                // 탑뷰용 회전: FBX 기본 -90도 → 추가로 -90도 해서 바닥에 눕힘
                visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                visual.transform.localScale = Vector3.one * def.Scale;

                // FBX에 있는 Collider 제거
                foreach (var col in visual.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(col);
                }

                // 모델 크기에 맞는 SphereCollider 추가 (Root에)
                var collider = bugObj.AddComponent<SphereCollider>();
                collider.center = new Vector3(0f, 0.3f, 0f);
                collider.radius = 0.5f * def.Scale;
            }
            else
            {
                // 폴백: 모델이 없으면 Capsule 사용
                Debug.LogWarning($"[BugTestPrefabCreator] Model not found: {modelPath}, using Capsule fallback");
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                visual.transform.SetParent(bugObj.transform);
                visual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f) * def.Scale;

                // Collider 제거
                Object.DestroyImmediate(visual.GetComponent<Collider>());
            }

            // Visual 색상 설정 (모든 Renderer에 적용)
            var renderers = visual.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length > 0)
            {
                // Material 생성/로드
                string matPath = $"{PrefabPath}/Bug_{def.Name}_Mat.mat";
                Material mat;

                if (!System.IO.File.Exists(matPath))
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = def.Color;
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                else
                {
                    mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                }

                // 모든 Renderer에 Material 적용
                foreach (var renderer in renderers)
                {
                    renderer.sharedMaterial = mat;
                }
            }

            // FX_Socket 추가
            GameObject fxSocket = new GameObject("FX_Socket");
            fxSocket.transform.SetParent(bugObj.transform);
            fxSocket.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            // BugController에 FX_Socket 연결
            var controllerSo = new SerializedObject(bugController);
            controllerSo.FindProperty("_fxSocket").objectReferenceValue = fxSocket.transform;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            // HpBar 프리팹 인스턴스 추가 (스케일에 맞게 위치 조정)
            float hpBarZ = 0.6f + (def.Scale * 0.4f); // 스케일에 비례하여 Z 위치
            GameObject hpBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HpBarPrefabPath);
            if (hpBarPrefab != null)
            {
                GameObject hpBar = (GameObject)PrefabUtility.InstantiatePrefab(hpBarPrefab);
                hpBar.transform.SetParent(bugObj.transform);
                hpBar.transform.localPosition = new Vector3(0f, 0.1f, hpBarZ);
            }

            // 라벨 추가 (BugLabel 컴포넌트 사용 - 월드 좌표 고정)
            float labelZ = hpBarZ + 0.4f; // HP 바 위에 라벨
            GameObject labelObj = new GameObject("BugLabel");
            labelObj.transform.SetParent(bugObj.transform);

            var bugLabel = labelObj.AddComponent<DrillCorp.Bug.BugLabel>();

            // BugLabel 필드 설정 (SerializedObject 사용)
            var bugLabelSo = new SerializedObject(bugLabel);
            bugLabelSo.FindProperty("_offset").vector3Value = new Vector3(0f, 0.1f, labelZ);
            bugLabelSo.ApplyModifiedPropertiesWithoutUndo();

            // TextMeshPro 추가
            TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = def.Label;
            tmp.fontSize = 2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.sortingOrder = 150;

            // D2Coding 폰트 적용
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset");
            if (font != null)
            {
                tmp.font = font;
            }

            // RectTransform 크기 조정
            RectTransform rectTransform = labelObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(3f, 2f);

            // BugLabel의 _text 필드 연결
            var labelSo = new SerializedObject(bugLabel);
            labelSo.FindProperty("_text").objectReferenceValue = tmp;
            labelSo.ApplyModifiedPropertiesWithoutUndo();

            // 프리팹으로 저장
            var prefab = PrefabUtility.SaveAsPrefabAsset(bugObj, fullPath);

            // 씬의 임시 오브젝트 삭제
            Object.DestroyImmediate(bugObj);

            Debug.Log($"[BugTestPrefabCreator] Created: Bug_{def.Name}.prefab");
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

        /// <summary>
        /// 테스트 Bug 정의
        /// </summary>
        private class TestBugDefinition
        {
            public string Name;
            public string Label;
            public string MovementType;
            public string AttackType;
            public string[] Passives = new string[0];
            public string[] Triggers = new string[0];  // Phase 2: Trigger 지원
            public Color Color;
            public string ModelName; // SM_Bug_A_01, SM_Bug_B_01, SM_Bug_C_01
            public float Scale = 1f;
        }
    }
}

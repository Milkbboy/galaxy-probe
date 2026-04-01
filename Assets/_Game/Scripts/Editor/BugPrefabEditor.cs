using UnityEngine;
using UnityEditor;
using DrillCorp.Bug;
using DrillCorp.Bug.Behaviors.Attack;
using DrillCorp.Bug.Behaviors.Data;
using DrillCorp.Data;

namespace DrillCorp.Editor
{
    public class BugPrefabEditor : UnityEditor.Editor
    {
        private const string HpBarPrefabPath = "Assets/_Game/Prefabs/UI/BugHpBar.prefab";
        private const string BehaviorDataPath = "Assets/_Game/Data/BugBehaviors";
        private const string ProjectilePrefabPath = "Assets/_Game/Prefabs/Bugs/BugProjectile.prefab";

        [MenuItem("Tools/Drill-Corp/Bug/1. Create HpBar Prefab")]
        public static void CreateHpBarPrefab()
        {
            string prefabPath = "Assets/_Game/Prefabs/UI";

            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");

            if (!AssetDatabase.IsValidFolder(prefabPath))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "UI");

            string fullPath = $"{prefabPath}/BugHpBar.prefab";

            if (System.IO.File.Exists(fullPath))
            {
                Debug.Log("[BugPrefabEditor] BugHpBar.prefab already exists, skipping.");
                return;
            }

            // HpBar 오브젝트 생성
            GameObject hpBarObj = new GameObject("BugHpBar");
            BugHpBar hpBar = hpBarObj.AddComponent<BugHpBar>();

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(hpBarObj.transform);
            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SpriteRenderer bgRenderer = bgObj.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = CreateSquareSprite();
            bgRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            bgRenderer.sortingOrder = 100;
            bgObj.transform.localScale = new Vector3(1f, 0.15f, 1f);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(hpBarObj.transform);
            fillObj.transform.localPosition = Vector3.zero;
            fillObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SpriteRenderer fillRenderer = fillObj.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = CreateSquareSprite();
            fillRenderer.color = Color.green;
            fillRenderer.sortingOrder = 101;
            fillObj.transform.localScale = new Vector3(1f, 0.15f, 1f);

            // BugHpBar 컴포넌트 연결
            var so = new SerializedObject(hpBar);
            so.FindProperty("_backgroundRenderer").objectReferenceValue = bgRenderer;
            so.FindProperty("_fillRenderer").objectReferenceValue = fillRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 프리팹으로 저장
            var prefab = PrefabUtility.SaveAsPrefabAsset(hpBarObj, fullPath);

            // 씬의 임시 오브젝트 삭제
            Object.DestroyImmediate(hpBarObj);

            // Inspector 선택을 생성한 프리팹으로 변경
            Selection.activeObject = prefab;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BugPrefabEditor] Created: BugHpBar.prefab");
        }

        [MenuItem("Tools/Drill-Corp/Bug/2. Create Projectile Prefab")]
        public static void CreateProjectilePrefab()
        {
            string prefabPath = "Assets/_Game/Prefabs/Bugs";

            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");

            if (!AssetDatabase.IsValidFolder(prefabPath))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "Bugs");

            string fullPath = $"{prefabPath}/BugProjectile.prefab";

            if (System.IO.File.Exists(fullPath))
            {
                Debug.Log("[BugPrefabEditor] BugProjectile.prefab already exists, skipping.");
                return;
            }

            // Root 오브젝트
            GameObject projectileObj = new GameObject("BugProjectile");

            // BugProjectile 컴포넌트 추가
            BugProjectile projectile = projectileObj.AddComponent<BugProjectile>();

            // Rigidbody (Kinematic)
            Rigidbody rb = projectileObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            // SphereCollider (Trigger)
            SphereCollider collider = projectileObj.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.15f;

            // Visual (Sphere)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(projectileObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            // Visual의 Collider 제거
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            // Material 생성 (노란색)
            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(1f, 0.8f, 0f); // 노란색
                mat.SetFloat("_Smoothness", 0.8f);

                // Emission 설정 (발광)
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0f) * 2f);

                renderer.sharedMaterial = mat;

                // Material 저장
                string matPath = $"{prefabPath}/BugProjectile_Mat.mat";
                AssetDatabase.CreateAsset(mat, matPath);
            }

            // 프리팹으로 저장
            var prefab = PrefabUtility.SaveAsPrefabAsset(projectileObj, fullPath);

            // 씬의 임시 오브젝트 삭제
            Object.DestroyImmediate(projectileObj);

            // Inspector 선택
            Selection.activeObject = prefab;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BugPrefabEditor] Created: BugProjectile.prefab");

            // Attack_Projectile에 연결
            ConnectProjectileToAttackData();
        }

        private static void ConnectProjectileToAttackData()
        {
            string attackDataPath = $"{BehaviorDataPath}/Attack/Attack_Projectile.asset";
            var attackData = AssetDatabase.LoadAssetAtPath<AttackBehaviorData>(attackDataPath);
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);

            if (attackData != null && projectilePrefab != null)
            {
                var so = new SerializedObject(attackData);
                var prefabProp = so.FindProperty("_projectilePrefab");
                if (prefabProp != null)
                {
                    prefabProp.objectReferenceValue = projectilePrefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[BugPrefabEditor] Connected: BugProjectile -> Attack_Projectile.asset");
                }
            }
        }

        [MenuItem("Tools/Drill-Corp/Bug/3. Create Bug Prefabs (BugController)")]
        public static void CreateBugPrefabs()
        {
            // HpBar 프리팹 확인
            if (!System.IO.File.Exists(HpBarPrefabPath))
            {
                Debug.LogError("[BugPrefabEditor] BugHpBar.prefab not found! Run 'Tools > Drill-Corp > Bug > 1. Create HpBar Prefab' first.");
                return;
            }

            string prefabPath = "Assets/_Game/Prefabs/Bugs";

            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");

            if (!AssetDatabase.IsValidFolder(prefabPath))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "Bugs");

            // Bug 프리팹 생성 (BugController 사용)
            CreateBugPrefab(prefabPath, "Bug_Beetle", "BugBehavior_Beetle", Color.red);
            CreateBugPrefab(prefabPath, "Bug_Fly", "BugBehavior_Fly", Color.blue);
            CreateBugPrefab(prefabPath, "Bug_Centipede", null, Color.green);  // 기본 행동
            CreateBugPrefab(prefabPath, "Bug_Tank", "BugBehavior_Tank", new Color(0.5f, 0.5f, 0.5f));
            CreateBugPrefab(prefabPath, "Bug_Spitter", "BugBehavior_Spitter", Color.yellow);
            CreateBugPrefab(prefabPath, "Bug_Bomber", "BugBehavior_Bomber", new Color(1f, 0.5f, 0f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BugPrefabEditor] Bug prefabs created at: {prefabPath}");
        }

        [MenuItem("Tools/Drill-Corp/Bug/4. Connect BugData to Prefabs")]
        public static void ConnectBugDataToPrefabs()
        {
            string prefabPath = "Assets/_Game/Prefabs/Bugs";
            string dataPath = "Assets/_Game/Data/Bugs";

            // 기존 벌레
            ConnectDataToPrefab($"{dataPath}/Bug_Beetle.asset", $"{prefabPath}/Bug_Beetle.prefab", $"{BehaviorDataPath}/BugBehavior_Beetle.asset");
            ConnectDataToPrefab($"{dataPath}/Bug_Fly.asset", $"{prefabPath}/Bug_Fly.prefab", $"{BehaviorDataPath}/BugBehavior_Fly.asset");
            ConnectDataToPrefab($"{dataPath}/Bug_Centipede.asset", $"{prefabPath}/Bug_Centipede.prefab", null);

            // 새 벌레 (BugData가 있으면 연결)
            ConnectDataToPrefab($"{dataPath}/Bug_Tank.asset", $"{prefabPath}/Bug_Tank.prefab", $"{BehaviorDataPath}/BugBehavior_Tank.asset");
            ConnectDataToPrefab($"{dataPath}/Bug_Spitter.asset", $"{prefabPath}/Bug_Spitter.prefab", $"{BehaviorDataPath}/BugBehavior_Spitter.asset");
            ConnectDataToPrefab($"{dataPath}/Bug_Bomber.asset", $"{prefabPath}/Bug_Bomber.prefab", $"{BehaviorDataPath}/BugBehavior_Bomber.asset");

            AssetDatabase.SaveAssets();
            Debug.Log("[BugPrefabEditor] BugData connected to prefabs!");
        }

        [MenuItem("Tools/Drill-Corp/Bug/5. Upgrade Existing Prefabs to BugController")]
        public static void UpgradeExistingPrefabs()
        {
            string prefabPath = "Assets/_Game/Prefabs/Bugs";

            // 모든 Bug 프리팹 찾기
            string[] guids = AssetDatabase.FindAssets("Bug_", new[] { prefabPath });

            int upgraded = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab")) continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // 이미 BugController가 있으면 스킵
                if (prefab.GetComponent<BugController>() != null)
                {
                    Debug.Log($"[BugPrefabEditor] {prefab.name} already has BugController, skipping.");
                    continue;
                }

                // BugBase가 있으면 업그레이드
                BugBase bugBase = prefab.GetComponent<BugBase>();
                if (bugBase != null)
                {
                    // 프리팹 수정 모드 진입
                    string prefabFullPath = AssetDatabase.GetAssetPath(prefab);
                    GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabFullPath);

                    // 기존 BugBase에서 데이터 가져오기
                    BugBase oldBugBase = prefabRoot.GetComponent<BugBase>();
                    BugData bugData = oldBugBase?.BugData;

                    // BugBase 제거
                    Object.DestroyImmediate(oldBugBase);

                    // BugController 추가
                    BugController bugController = prefabRoot.AddComponent<BugController>();

                    // BugData 연결
                    if (bugData != null)
                    {
                        var so = new SerializedObject(bugController);
                        so.FindProperty("_bugData").objectReferenceValue = bugData;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }

                    // 프리팹 저장
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabFullPath);
                    PrefabUtility.UnloadPrefabContents(prefabRoot);

                    Debug.Log($"[BugPrefabEditor] Upgraded: {prefab.name}");
                    upgraded++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (upgraded > 0)
            {
                EditorUtility.DisplayDialog("완료", $"{upgraded}개의 프리팹이 BugController로 업그레이드되었습니다.", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("완료", "업그레이드할 프리팹이 없습니다.\n(이미 모두 BugController를 사용 중이거나 프리팹이 없음)", "확인");
            }
        }

        private static void CreateBugPrefab(string path, string name, string behaviorDataName, Color visualColor)
        {
            string fullPath = $"{path}/{name}.prefab";

            if (System.IO.File.Exists(fullPath))
            {
                Debug.Log($"[BugPrefabEditor] {name}.prefab already exists, skipping.");
                return;
            }

            // Root 오브젝트
            GameObject bugObj = new GameObject(name);

            // BugController 컴포넌트 추가
            BugController bugController = bugObj.AddComponent<BugController>();

            // BehaviorData 연결 (있으면)
            if (!string.IsNullOrEmpty(behaviorDataName))
            {
                var behaviorData = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{BehaviorDataPath}/{behaviorDataName}.asset");
                if (behaviorData != null)
                {
                    var so = new SerializedObject(bugController);
                    so.FindProperty("_behaviorData").objectReferenceValue = behaviorData;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Visual (임시 Capsule)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(bugObj.transform);
            visual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Collider 제거 (BugController에서 자동 추가)
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            // Visual 색상 설정
            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = visualColor;
                renderer.sharedMaterial = mat;

                // Material 저장
                string matPath = $"{path}/{name}_Mat.mat";
                if (!System.IO.File.Exists(matPath))
                {
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                else
                {
                    // 기존 머티리얼 사용
                    var existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (existingMat != null)
                    {
                        renderer.sharedMaterial = existingMat;
                    }
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

            // HpBar 프리팹 인스턴스 추가 (자식으로)
            GameObject hpBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HpBarPrefabPath);
            if (hpBarPrefab != null)
            {
                GameObject hpBar = (GameObject)PrefabUtility.InstantiatePrefab(hpBarPrefab);
                hpBar.transform.SetParent(bugObj.transform);
                hpBar.transform.localPosition = new Vector3(0f, 0.1f, 0.8f);
            }

            // 프리팹으로 저장
            var prefab = PrefabUtility.SaveAsPrefabAsset(bugObj, fullPath);

            // 씬의 임시 오브젝트 삭제
            Object.DestroyImmediate(bugObj);

            // Inspector 선택을 생성한 프리팹으로 변경
            Selection.activeObject = prefab;

            Debug.Log($"[BugPrefabEditor] Created: {name}.prefab (with BugController)");
        }

        private static Sprite CreateSquareSprite()
        {
            // 월드 스페이스용 스프라이트 사용 (PPU 4)
            string spritePath = "Assets/_Game/Sprites/UI/Square_White_World.png";
            Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existingSprite != null)
            {
                return existingSprite;
            }

            // 폴백: UI용 스프라이트
            spritePath = "Assets/_Game/Sprites/UI/Square_White.png";
            existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existingSprite != null)
            {
                return existingSprite;
            }

            // 없으면 새로 생성 (PPU 4로 월드 스페이스에 적합)
            Texture2D texture = new Texture2D(4, 4);
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private static void ConnectDataToPrefab(string dataPath, string prefabPath, string behaviorDataPath)
        {
            var bugData = AssetDatabase.LoadAssetAtPath<BugData>(dataPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (bugData == null)
            {
                Debug.LogWarning($"[BugPrefabEditor] BugData not found: {dataPath}");
                return;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[BugPrefabEditor] Prefab not found: {prefabPath}");
                return;
            }

            // BugData에 Prefab 연결
            var dataSo = new SerializedObject(bugData);
            var prefabProp = dataSo.FindProperty("_prefab");
            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = prefab;
                dataSo.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[BugPrefabEditor] Connected: {bugData.name} -> {prefab.name}");
            }

            // Prefab의 BugController에 BugData 연결
            var bugController = prefab.GetComponent<BugController>();
            if (bugController != null)
            {
                var bugSo = new SerializedObject(bugController);

                // BugData 연결
                var bugDataProp = bugSo.FindProperty("_bugData");
                if (bugDataProp != null)
                {
                    bugDataProp.objectReferenceValue = bugData;
                }

                // BehaviorData 연결 (있으면)
                if (!string.IsNullOrEmpty(behaviorDataPath))
                {
                    var behaviorData = AssetDatabase.LoadAssetAtPath<BugBehaviorData>(behaviorDataPath);
                    if (behaviorData != null)
                    {
                        var behaviorProp = bugSo.FindProperty("_behaviorData");
                        if (behaviorProp != null)
                        {
                            behaviorProp.objectReferenceValue = behaviorData;
                        }
                    }
                }

                bugSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // 기존 BugBase 지원 (호환성)
            var bugBase = prefab.GetComponent<BugBase>();
            if (bugBase != null)
            {
                var bugSo = new SerializedObject(bugBase);
                var bugDataProp = bugSo.FindProperty("_bugData");
                if (bugDataProp != null)
                {
                    bugDataProp.objectReferenceValue = bugData;
                    bugSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        [MenuItem("Tools/Drill-Corp/Bug/6. Create Teleport VFX Prefab")]
        public static void CreateTeleportVfxPrefab()
        {
            string prefabPath = "Assets/_Game/Prefabs/VFX";

            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");

            if (!AssetDatabase.IsValidFolder(prefabPath))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "VFX");

            string fullPath = $"{prefabPath}/VFX_Teleport.prefab";

            // 기존 파일 삭제 (재생성)
            if (System.IO.File.Exists(fullPath))
            {
                AssetDatabase.DeleteAsset(fullPath);
            }

            // Root 오브젝트
            GameObject vfxObj = new GameObject("VFX_Teleport");

            // 자동 삭제 컴포넌트
            var autoDestroy = vfxObj.AddComponent<DrillCorp.VFX.AutoDestroy>();
            var autoDestroySo = new SerializedObject(autoDestroy);
            autoDestroySo.FindProperty("_lifetime").floatValue = 0.5f;
            autoDestroySo.ApplyModifiedPropertiesWithoutUndo();

            // 텍스트 오브젝트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(vfxObj.transform);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // 탑뷰용

            // TextMeshPro 추가
            var tmp = textObj.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "뿅!";
            tmp.fontSize = 5f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.yellow; // 노란색 (눈에 잘 띔)
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.sortingOrder = 200;

            // D2Coding 폰트 적용 (Material 설정 전에 폰트부터 설정)
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset");
            if (font != null)
            {
                tmp.font = font;
            }

            // Outline Material 생성 및 설정
            // 폰트의 기본 Material을 복사하여 Outline 설정
            Material outlineMat = new Material(tmp.font.material);
            outlineMat.name = "VFX_Teleport_Mat";

            // Outline 활성화
            outlineMat.EnableKeyword("OUTLINE_ON");
            outlineMat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0.2f);
            outlineMat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, Color.black);

            // Face Color (텍스트 본체 색상)
            outlineMat.SetColor(TMPro.ShaderUtilities.ID_FaceColor, Color.yellow);

            // Material 에셋으로 저장
            string matPath = $"{prefabPath}/VFX_Teleport_Mat.mat";
            if (System.IO.File.Exists(matPath))
            {
                AssetDatabase.DeleteAsset(matPath);
            }
            AssetDatabase.CreateAsset(outlineMat, matPath);

            // TMP에 Material 적용
            tmp.fontSharedMaterial = outlineMat;

            // RectTransform 크기
            var rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(3f, 2f);

            // 프리팹으로 저장
            var prefab = PrefabUtility.SaveAsPrefabAsset(vfxObj, fullPath);

            // 씬의 임시 오브젝트 삭제
            Object.DestroyImmediate(vfxObj);

            // Inspector 선택
            Selection.activeObject = prefab;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BugPrefabEditor] Created: VFX_Teleport.prefab");

            // Movement_Teleport에 연결
            ConnectTeleportVfx(prefab);
        }

        private static void ConnectTeleportVfx(GameObject vfxPrefab)
        {
            string movementDataPath = $"{BehaviorDataPath}/Movement/Movement_Teleport.asset";
            var movementData = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>(movementDataPath);

            if (movementData != null && vfxPrefab != null)
            {
                var so = new SerializedObject(movementData);
                var effectProp = so.FindProperty("_effectPrefab");
                if (effectProp != null)
                {
                    effectProp.objectReferenceValue = vfxPrefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[BugPrefabEditor] Connected: VFX_Teleport -> Movement_Teleport.asset");
                }
            }
        }
    }
}

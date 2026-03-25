using UnityEngine;
using UnityEditor;
using DrillCorp.Bug;
using DrillCorp.Data;

namespace DrillCorp.Editor
{
    public class BugPrefabEditor : UnityEditor.Editor
    {
        private const string HealthBarPrefabPath = "Assets/_Game/Prefabs/UI/BugHealthBar.prefab";

        [MenuItem("Tools/Drill-Corp/Bug/1. Create HealthBar Prefab")]
        public static void CreateHealthBarPrefab()
        {
            string prefabPath = "Assets/_Game/Prefabs/UI";

            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");

            if (!AssetDatabase.IsValidFolder(prefabPath))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "UI");

            string fullPath = $"{prefabPath}/BugHealthBar.prefab";

            if (System.IO.File.Exists(fullPath))
            {
                Debug.Log("[BugPrefabEditor] BugHealthBar.prefab already exists, skipping.");
                return;
            }

            // HealthBar 오브젝트 생성
            GameObject hpBarObj = new GameObject("BugHealthBar");
            BugHealthBar healthBar = hpBarObj.AddComponent<BugHealthBar>();

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

            // BugHealthBar 컴포넌트 연결
            var so = new SerializedObject(healthBar);
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

            Debug.Log($"[BugPrefabEditor] Created: BugHealthBar.prefab");
        }

        [MenuItem("Tools/Drill-Corp/Bug/2. Create Bug Prefabs")]
        public static void CreateBugPrefabs()
        {
            // HealthBar 프리팹 확인
            if (!System.IO.File.Exists(HealthBarPrefabPath))
            {
                Debug.LogError("[BugPrefabEditor] BugHealthBar.prefab not found! Run 'Tools > Drill-Corp > Bug > 1. Create HealthBar Prefab' first.");
                return;
            }

            string prefabPath = "Assets/_Game/Prefabs/Bugs";

            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");

            if (!AssetDatabase.IsValidFolder(prefabPath))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "Bugs");

            // Bug 프리팹 생성
            CreateBugPrefab(prefabPath, "Bug_Beetle", typeof(BeetleBug), Color.red);
            CreateBugPrefab(prefabPath, "Bug_Fly", typeof(FlyBug), Color.blue);
            CreateBugPrefab(prefabPath, "Bug_Centipede", typeof(CentipedeBug), Color.green);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BugPrefabEditor] Bug prefabs created at: {prefabPath}");
        }

        [MenuItem("Tools/Drill-Corp/Bug/3. Connect BugData to Prefabs")]
        public static void ConnectBugDataToPrefabs()
        {
            string prefabPath = "Assets/_Game/Prefabs/Bugs";
            string dataPath = "Assets/_Game/Data/Bugs";

            // Beetle
            ConnectDataToPrefab(
                $"{dataPath}/Bug_Beetle.asset",
                $"{prefabPath}/Bug_Beetle.prefab"
            );

            // Fly
            ConnectDataToPrefab(
                $"{dataPath}/Bug_Fly.asset",
                $"{prefabPath}/Bug_Fly.prefab"
            );

            // Centipede
            ConnectDataToPrefab(
                $"{dataPath}/Bug_Centipede.asset",
                $"{prefabPath}/Bug_Centipede.prefab"
            );

            AssetDatabase.SaveAssets();
            Debug.Log("[BugPrefabEditor] BugData connected to prefabs!");
        }

        private static void CreateBugPrefab(string path, string name, System.Type bugType, Color visualColor)
        {
            string fullPath = $"{path}/{name}.prefab";

            if (System.IO.File.Exists(fullPath))
            {
                Debug.Log($"[BugPrefabEditor] {name}.prefab already exists, skipping.");
                return;
            }

            // Root 오브젝트
            GameObject bugObj = new GameObject(name);

            // BugBase 컴포넌트 추가
            bugObj.AddComponent(bugType);

            // Visual (임시 Capsule)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(bugObj.transform);
            visual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Collider 제거 (BugBase에서 자동 추가)
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
                AssetDatabase.CreateAsset(mat, matPath);
            }

            // HealthBar 프리팹 인스턴스 추가 (자식으로)
            GameObject healthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPrefabPath);
            if (healthBarPrefab != null)
            {
                GameObject healthBar = (GameObject)PrefabUtility.InstantiatePrefab(healthBarPrefab);
                healthBar.transform.SetParent(bugObj.transform);
                healthBar.transform.localPosition = new Vector3(0f, 0.1f, 0.8f);
            }


            // 프리팹으로 저장
            var prefab = PrefabUtility.SaveAsPrefabAsset(bugObj, fullPath);

            // 씬의 임시 오브젝트 삭제
            Object.DestroyImmediate(bugObj);

            // Inspector 선택을 생성한 프리팹으로 변경
            Selection.activeObject = prefab;

            Debug.Log($"[BugPrefabEditor] Created: {name}.prefab");
        }

        private static Sprite CreateSquareSprite()
        {
            // 기존 스프라이트가 있는지 확인
            string spritePath = "Assets/_Game/Sprites/UI/Square_White.png";
            Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existingSprite != null)
            {
                return existingSprite;
            }

            // 없으면 새로 생성
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

        private static void ConnectDataToPrefab(string dataPath, string prefabPath)
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

            // Prefab의 BugBase에 BugData 연결
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
    }
}

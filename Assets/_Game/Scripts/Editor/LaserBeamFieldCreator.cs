#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using DrillCorp.Weapon.Laser;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// 레이저 바닥 필드 프리펩 + 원형 스프라이트 자동 생성
    /// 탑뷰 XZ 평면에 눕혀진 상태 (rotation 90,0,0)
    /// </summary>
    public static class LaserBeamFieldCreator
    {
        private const string FolderPath = "Assets/_Game/Prefabs/Weapons";
        private const string PrefabPath = FolderPath + "/LaserBeamField.prefab";
        private const string SpritePath = FolderPath + "/LaserBeamFieldCircle.png";

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/5. 레이저 필드 프리펩 생성")]
        public static void Create()
        {
            EnsureFolders();

            bool spriteCreated = EnsureCirclePng();
            if (spriteCreated)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                Debug.LogError($"[LaserBeamFieldCreator] Sprite 로드 실패: {SpritePath}");
                return;
            }

            CreatePrefab(sprite);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");
            if (!AssetDatabase.IsValidFolder(FolderPath))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "Weapons");
        }

        private static bool EnsureCirclePng()
        {
            bool existed = File.Exists(SpritePath);
            if (!existed) CreateCirclePng();

            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null) return !existed;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (Mathf.Abs(importer.spritePixelsPerUnit - 100f) > 0.01f) { importer.spritePixelsPerUnit = 100f; changed = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }

            if (changed || !existed) importer.SaveAndReimport();
            return !existed;
        }

        private static void CreateCirclePng()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            float center = (size - 1) * 0.5f;
            float outer = size * 0.5f;
            float innerRing = size * 0.42f;
            float outerRing = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    Color c = new Color(0f, 0f, 0f, 0f);

                    // 중심 발광 (부드러운 그라데이션)
                    float coreT = 1f - Mathf.Clamp01(dist / (outer * 0.7f));
                    float coreAlpha = coreT * coreT * 0.35f;
                    c = new Color(1f, 0.6f, 0.3f, coreAlpha);

                    // 외곽 링 (강조)
                    if (dist >= innerRing && dist <= outerRing)
                    {
                        float ringT = 1f - Mathf.Abs(dist - (innerRing + outerRing) * 0.5f) / ((outerRing - innerRing) * 0.5f);
                        c = new Color(1f, 0.8f, 0.4f, Mathf.Clamp01(ringT));
                    }

                    // 바깥 페이드아웃
                    if (dist > outer)
                        c.a = 0f;

                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreatePrefab(Sprite sprite)
        {
            var root = new GameObject("LaserBeamField");
            try
            {
                root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                root.transform.localScale = new Vector3(1f, 1f, 1f);

                var sr = root.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(1f, 0.3f, 0.1f, 0.7f);
                sr.sortingOrder = 5;

                root.AddComponent<LaserBeamField>();

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success || prefab == null)
                {
                    Debug.LogError($"[LaserBeamFieldCreator] 프리펩 저장 실패: {PrefabPath}");
                    return;
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"[LaserBeamFieldCreator] ✅ 생성 완료: {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using DrillCorp.Weapon.LockOn;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// LockOn 마커 프리펩 자동 생성
    /// 간단한 빨간 십자선 SpriteRenderer 프리펩
    /// </summary>
    public static class LockOnMarkerCreator
    {
        private const string FolderPath = "Assets/_Game/Prefabs/Weapons";
        private const string PrefabPath = FolderPath + "/LockOnMarker.prefab";
        private const string SpritePath = FolderPath + "/LockOnCrosshair.png";

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/4. LockOn 마커 프리펩 생성")]
        public static void Create()
        {
            EnsureFolders();

            bool spriteCreated = EnsureCrosshairSprite();
            if (spriteCreated)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                Debug.LogError($"[LockOnMarkerCreator] Sprite 로드 실패: {SpritePath}\nTextureImporter 설정 실패 가능. 다시 한번 메뉴를 실행해보세요.");
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

        /// <summary>
        /// Sprite(PNG) 생성 + TextureImporter 설정
        /// 반환값: 신규 생성 여부
        /// </summary>
        private static bool EnsureCrosshairSprite()
        {
            bool existed = File.Exists(SpritePath);

            if (!existed)
            {
                CreateCrosshairPng();
            }

            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[LockOnMarkerCreator] TextureImporter 가져오기 실패: {SpritePath}");
                return !existed;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (Mathf.Abs(importer.spritePixelsPerUnit - 100f) > 0.01f)
            {
                importer.spritePixelsPerUnit = 100f;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (changed || !existed)
            {
                importer.SaveAndReimport();
            }

            return !existed;
        }

        private static void CreateCrosshairPng()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            int center = size / 2;
            int thickness = 3;
            int lineLength = 22;
            int gapFromCenter = 6;
            int ringInner = 26;
            int ringOuter = 30;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    Color c = new Color(0f, 0f, 0f, 0f);

                    int dx = x - center;
                    int dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist >= ringInner && dist <= ringOuter)
                        c = Color.white;

                    if (Mathf.Abs(dy) <= thickness / 2 &&
                        Mathf.Abs(dx) >= gapFromCenter && Mathf.Abs(dx) <= gapFromCenter + lineLength)
                        c = Color.white;

                    if (Mathf.Abs(dx) <= thickness / 2 &&
                        Mathf.Abs(dy) >= gapFromCenter && Mathf.Abs(dy) <= gapFromCenter + lineLength)
                        c = Color.white;

                    pixels[i] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(SpritePath, png);
            Object.DestroyImmediate(tex);
        }

        private static void CreatePrefab(Sprite sprite)
        {
            var root = new GameObject("LockOnMarker");

            try
            {
                root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                root.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

                var sr = root.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(1f, 0.2f, 0.2f, 0.9f);
                sr.sortingOrder = 10;

                root.AddComponent<LockOnMarker>();

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);

                if (!success || prefab == null)
                {
                    Debug.LogError($"[LockOnMarkerCreator] 프리펩 저장 실패: {PrefabPath}");
                    return;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"[LockOnMarkerCreator] ✅ 생성 완료: {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif

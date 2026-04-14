#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using DrillCorp.Weapon;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// 무기 게이지 프리펩 자동 생성 (배경 + 채움 구조, 세로 바)
    /// 탑뷰 XZ 평면에 눕힌 상태. 채움은 아래(Y-) 끝을 기준으로 위로 차오름
    /// Sprite pivot=BottomCenter, scale.y * baseFillHeight로 채움 길이 제어
    /// </summary>
    public static class WeaponGaugeCreator
    {
        private const string FolderPath = "Assets/_Game/Prefabs/Weapons";
        private const string PrefabPath = FolderPath + "/WeaponGauge.prefab";
        private const string SpritePath = FolderPath + "/WeaponGaugeStrip.png";

        private const float BarWidth = 0.2f;   // 가로 두께
        private const float BarHeight = 10.0f;  // 세로 길이

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/6. 무기 게이지 프리펩 생성")]
        public static void Create()
        {
            EnsureFolders();
            bool spriteCreated = EnsureStripPng();
            if (spriteCreated)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                Debug.LogError($"[WeaponGaugeCreator] Sprite 로드 실패: {SpritePath}");
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

        private static bool EnsureStripPng()
        {
            bool existed = File.Exists(SpritePath);
            if (!existed) CreateStripPng();

            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null) return !existed;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (Mathf.Abs(importer.spritePixelsPerUnit - 100f) > 0.01f) { importer.spritePixelsPerUnit = 100f; changed = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }

            // pivot을 아래 중앙으로 설정 (채움 바가 아래 기준으로 위로 늘어나도록)
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteAlignment != (int)SpriteAlignment.BottomCenter)
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                settings.spritePivot = new Vector2(0.5f, 0f);
                importer.SetTextureSettings(settings);
                changed = true;
            }

            if (changed || !existed) importer.SaveAndReimport();
            return !existed;
        }

        private static void CreateStripPng()
        {
            const int w = 100;
            const int h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreatePrefab(Sprite sprite)
        {
            var root = new GameObject("WeaponGauge");
            try
            {
                root.transform.rotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                // Sprite pivot=BottomCenter, 배경·채움 모두 root 원점(0,0,0)에 위치.
                // localScale.y = BarHeight일 때 sprite가 root 원점부터 위로 BarHeight만큼 뻗음.
                // → root 원점이 바의 "아래 끝" 기준점이 됨.
                //   세로 바: 외부에서 "게이지를 놓을 아래 끝 위치"를 root.position으로 주면 그 위로 뻗음.
                Vector3 anchor = Vector3.zero;

                // 배경
                var bgObj = new GameObject("Background");
                bgObj.transform.SetParent(root.transform, false);
                bgObj.transform.localPosition = anchor;
                bgObj.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);
                var bgSr = bgObj.AddComponent<SpriteRenderer>();
                bgSr.sprite = sprite;
                bgSr.color = new Color(0f, 0f, 0f, 0.5f);
                bgSr.sortingOrder = 100;

                // 채움 피벗 = 채움 자체가 아래 정렬이므로 피벗도 동일 위치. Scale Y는 런타임 조정.
                var pivotObj = new GameObject("FillPivot");
                pivotObj.transform.SetParent(root.transform, false);
                pivotObj.transform.localPosition = anchor + new Vector3(0f, 0f, -0.01f);
                pivotObj.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);

                // 채움 (sprite pivot=BottomCenter라 아래 정렬)
                var fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(pivotObj.transform, false);
                fillObj.transform.localPosition = Vector3.zero;
                fillObj.transform.localScale = Vector3.one;
                var fillSr = fillObj.AddComponent<SpriteRenderer>();
                fillSr.sprite = sprite;
                fillSr.color = new Color(0.3f, 1f, 0.4f, 0.9f);
                fillSr.sortingOrder = 101;

                // WeaponGauge 컴포넌트 (세로 모드)
                var gauge = root.AddComponent<WeaponGauge>();
                var so = new SerializedObject(gauge);
                so.FindProperty("_background").objectReferenceValue = bgSr;
                so.FindProperty("_fill").objectReferenceValue = fillSr;
                so.FindProperty("_fillPivot").objectReferenceValue = pivotObj.transform;
                so.FindProperty("_baseFillWidth").floatValue = BarWidth;
                so.FindProperty("_baseFillHeight").floatValue = BarHeight;
                so.FindProperty("_vertical").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success || prefab == null)
                {
                    Debug.LogError($"[WeaponGaugeCreator] 프리펩 저장 실패: {PrefabPath}");
                    return;
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"[WeaponGaugeCreator] ✅ 생성 완료: {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif

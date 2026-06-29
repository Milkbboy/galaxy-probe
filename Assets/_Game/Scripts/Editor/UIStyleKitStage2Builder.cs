#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// UI 2단계 공통 아이콘 + ResourceBar 생성기.
    ///
    /// 생성물:
    /// - Assets/_Game/Sprites/UI/Icons/*.png: 공통 UI 아이콘
    /// - Assets/_Game/Sprites/UI/Icons/ui_icon_contact_sheet.png: 생성 아이콘 확인용 시트
    /// - Assets/_Game/Prefabs/UI/Common/ResourceBar.prefab
    /// - Assets/_Game/Prefabs/UI/Common/IconButton.prefab
    ///
    /// 메뉴:
    /// Legacy generator. 실제 빌드 UI 작업에서는 메뉴에 노출하지 않는다.
    /// </summary>
    public static class UIStyleKitStage2Builder
    {
        private const string IconFolder = "Assets/_Game/Sprites/UI/Icons";
        private const string CommonSpriteFolder = "Assets/_Game/Sprites/UI/Common";
        private const string PrefabFolder = "Assets/_Game/Prefabs/UI/Common";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset";

        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
        private static readonly Color Text = Hex(0xE8, 0xE0, 0xD0, 0xFF);
        private static readonly Color Shadow = Hex(0x0A, 0x0A, 0x0D, 0xCC);
        private static readonly Color Ore = Hex(0xFF, 0xC4, 0x4D, 0xFF);
        private static readonly Color OreDark = Hex(0x8C, 0x5E, 0x2A, 0xFF);
        private static readonly Color Gem = Hex(0x78, 0xD8, 0xFF, 0xFF);
        private static readonly Color GemDark = Hex(0x2E, 0x67, 0x92, 0xFF);
        private static readonly Color Credit = Hex(0xE7, 0xB9, 0x60, 0xFF);
        private static readonly Color Green = Hex(0x7E, 0xD7, 0x8E, 0xFF);
        private static readonly Color Cyan = Hex(0x67, 0xE6, 0xE4, 0xFF);
        private static readonly Color Amber = Hex(0xFF, 0xC4, 0x5A, 0xFF);
        private static readonly Color Red = Hex(0xD4, 0x67, 0x5F, 0xFF);
        private static readonly Color Metal = Hex(0x8E, 0x8A, 0x76, 0xFF);

        public static void Build()
        {
            EnsureFolders();

            if (LoadCommonSprite("resource_slot") == null || LoadCommonSprite("metal_button_normal") == null)
            {
                EditorUtility.DisplayDialog(
                    "UI 2단계 생성 전 확인",
                    "1단계 공통 UI 키트 리소스가 없습니다.\n먼저 '1단계 공통 UI 키트 생성' 메뉴를 실행하세요.",
                    "확인");
                return;
            }

            var icons = new Dictionary<string, IconDef>
            {
                ["icon_ore"] = new IconDef(DrawOre),
                ["icon_gem"] = new IconDef(DrawGem),
                ["icon_credit"] = new IconDef(DrawCredit),
                ["icon_settings"] = new IconDef(DrawSettings),
                ["icon_exit"] = new IconDef(DrawExit),
                ["icon_upgrade"] = new IconDef(DrawUpgrade),
                ["icon_character"] = new IconDef(DrawCharacter),
                ["icon_crafting"] = new IconDef(DrawCrafting),
                ["icon_back"] = new IconDef(DrawBack),
                ["icon_lock"] = new IconDef(DrawLock),
                ["icon_check"] = new IconDef(DrawCheck),
                ["icon_display"] = new IconDef(DrawDisplay),
                ["icon_sound"] = new IconDef(DrawSound),
                ["icon_language"] = new IconDef(DrawLanguage),
                ["icon_accessibility"] = new IconDef(DrawAccessibility),
            };

            foreach (var pair in icons)
            {
                CreateIcon(pair.Key, pair.Value);
            }

            CreateContactSheet(icons);
            AssetDatabase.Refresh();

            CreateResourceBarPrefab();
            CreateIconButtonPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var resourceBar = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/ResourceBar.prefab");
            Selection.activeObject = resourceBar;
            if (resourceBar != null) EditorGUIUtility.PingObject(resourceBar);

            Debug.Log("[UIStyleKitStage2] 2단계 공통 아이콘·자원바 생성 완료. ResourceBar/IconButton 프리팹을 확인하세요.");
        }

        private static void CreateIcon(string name, IconDef def)
        {
            string path = $"{IconFolder}/{name}.png";
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, mipChain: false);
            Fill(tex, Clear);
            def.Draw(tex);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[UIStyleKitStage2] TextureImporter 로드 실패: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void CreateContactSheet(IReadOnlyDictionary<string, IconDef> icons)
        {
            const int cell = 64;
            const int cols = 5;
            int rows = Mathf.CeilToInt(icons.Count / (float)cols);
            var tex = new Texture2D(cols * cell, rows * cell, TextureFormat.RGBA32, mipChain: false);
            Fill(tex, Hex(0x10, 0x12, 0x1C, 0xFF));

            int i = 0;
            foreach (var pair in icons)
            {
                var sample = new Texture2D(32, 32, TextureFormat.RGBA32, mipChain: false);
                Fill(sample, Clear);
                pair.Value.Draw(sample);
                sample.Apply();

                int col = i % cols;
                int row = i / cols;
                int baseX = col * cell + 16;
                int baseY = tex.height - ((row + 1) * cell) + 16;
                DrawRect(tex, col * cell + 4, row * cell + 4, cell - 8, cell - 8, Hex(0x18, 0x1C, 0x24, 0xFF));
                DrawRectOutline(tex, col * cell + 4, row * cell + 4, cell - 8, cell - 8, Hex(0x35, 0x45, 0x47, 0xFF), 2);
                Blit(tex, sample, baseX, baseY);
                UnityEngine.Object.DestroyImmediate(sample);
                i++;
            }

            tex.Apply();
            string path = $"{IconFolder}/ui_icon_contact_sheet.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static void CreateResourceBarPrefab()
        {
            var root = CreateUiObject("ResourceBar", new Vector2(420, 56));
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateResourceSlot(root, "OreSlot", LoadIcon("icon_ore"), "광석", "0", Ore);
            CreateResourceSlot(root, "GemSlot", LoadIcon("icon_gem"), "보석", "0", Gem);
            CreateResourceSlot(root, "CreditSlot", LoadIcon("icon_credit"), "크레딧", "0", Credit);

            SavePrefab(root.gameObject, $"{PrefabFolder}/ResourceBar.prefab");
        }

        private static void CreateIconButtonPrefab()
        {
            var root = CreateUiObject("IconButton", new Vector2(220, 64));
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = LoadCommonSprite("metal_button_normal");
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.SpriteSwap;
            button.targetGraphic = bg;
            button.spriteState = new SpriteState
            {
                highlightedSprite = LoadCommonSprite("metal_button_hover"),
                pressedSprite = LoadCommonSprite("metal_button_pressed"),
                selectedSprite = LoadCommonSprite("metal_button_hover"),
                disabledSprite = LoadCommonSprite("metal_button_disabled"),
            };

            var icon = CreateChild(root, "Icon", new Vector2(30, 30));
            icon.anchorMin = new Vector2(0f, 0.5f);
            icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(24f, 0f);
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = LoadIcon("icon_upgrade");
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var label = CreateText(root, "Label", "BUTTON", 24, TextAlignmentOptions.MidlineLeft);
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.offsetMin = new Vector2(66f, 0f);
            label.rectTransform.offsetMax = new Vector2(-18f, 0f);

            SavePrefab(root.gameObject, $"{PrefabFolder}/IconButton.prefab");
        }

        private static void CreateResourceSlot(RectTransform parent, string name, Sprite icon, string label, string value, Color color)
        {
            var slot = CreateChild(parent, name, new Vector2(128, 44));
            var le = slot.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 128;
            le.preferredHeight = 44;

            var bg = slot.gameObject.AddComponent<Image>();
            bg.sprite = LoadCommonSprite("resource_slot");
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            var iconRt = CreateChild(slot, "Icon", new Vector2(24, 24));
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(10f, 0f);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = icon;
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var labelTmp = CreateText(slot, "Label", label, 13, TextAlignmentOptions.MidlineLeft);
            labelTmp.color = Hex(0xA8, 0xA0, 0x90, 0xFF);
            labelTmp.rectTransform.anchorMin = new Vector2(0f, 0f);
            labelTmp.rectTransform.anchorMax = new Vector2(1f, 1f);
            labelTmp.rectTransform.offsetMin = new Vector2(38f, 18f);
            labelTmp.rectTransform.offsetMax = new Vector2(-8f, -2f);

            var valueTmp = CreateText(slot, "Value", value, 20, TextAlignmentOptions.MidlineRight);
            valueTmp.color = color;
            valueTmp.rectTransform.anchorMin = new Vector2(0f, 0f);
            valueTmp.rectTransform.anchorMax = new Vector2(1f, 1f);
            valueTmp.rectTransform.offsetMin = new Vector2(38f, 0f);
            valueTmp.rectTransform.offsetMax = new Vector2(-8f, -15f);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment)
        {
            var rt = CreateChild(parent, name, new Vector2(120, 32));
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Text;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.outlineWidth = 0.16f;
            tmp.outlineColor = Color.black;
            ApplyD2Coding(tmp);
            return tmp;
        }

        private static RectTransform CreateUiObject(string name, Vector2 size)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            return rt;
        }

        private static RectTransform CreateChild(Transform parent, string name, Vector2 size)
        {
            var rt = CreateUiObject(name, size);
            rt.SetParent(parent, false);
            return rt;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static Sprite LoadIcon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{name}.png");
        }

        private static Sprite LoadCommonSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{CommonSpriteFolder}/{name}.png");
        }

        private static void ApplyD2Coding(TextMeshProUGUI tmp)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) tmp.font = font;
            else Debug.LogWarning($"[UIStyleKitStage2] D2Coding 폰트 로드 실패: {FontPath}");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(IconFolder);
            Directory.CreateDirectory(PrefabFolder);
        }

        private static void DrawOre(Texture2D tex)
        {
            DrawPoly(tex, new[] { P(16, 3), P(25, 14), P(21, 29), P(10, 29), P(6, 13) }, OreDark);
            DrawPoly(tex, new[] { P(16, 5), P(23, 14), P(19, 27), P(12, 27), P(8, 14) }, Ore);
            DrawRect(tex, 14, 7, 4, 20, Hex(0xFF, 0xE3, 0x81, 0xFF));
        }

        private static void DrawGem(Texture2D tex)
        {
            DrawPoly(tex, new[] { P(8, 8), P(24, 8), P(29, 14), P(16, 30), P(3, 14) }, GemDark);
            DrawPoly(tex, new[] { P(10, 10), P(22, 10), P(26, 14), P(16, 27), P(6, 14) }, Gem);
            DrawLine(tex, 16, 10, 16, 27, Color.white, 2);
            DrawLine(tex, 7, 14, 25, 14, Color.white, 1);
        }

        private static void DrawCredit(Texture2D tex)
        {
            DrawCircle(tex, 16, 16, 13, Hex(0x8A, 0x62, 0x35, 0xFF));
            DrawCircle(tex, 16, 16, 11, Credit);
            DrawCircle(tex, 16, 16, 7, Hex(0xB9, 0x7A, 0x37, 0xFF));
            DrawRect(tex, 14, 8, 4, 16, Hex(0xFF, 0xE5, 0x9A, 0xFF));
            DrawRect(tex, 11, 11, 10, 3, Hex(0xFF, 0xE5, 0x9A, 0xFF));
            DrawRect(tex, 11, 19, 10, 3, Hex(0xFF, 0xE5, 0x9A, 0xFF));
        }

        private static void DrawSettings(Texture2D tex)
        {
            DrawCircle(tex, 16, 16, 12, Metal);
            DrawCircle(tex, 16, 16, 6, Shadow);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                int x = Mathf.RoundToInt(16 + Mathf.Cos(a) * 12);
                int y = Mathf.RoundToInt(16 + Mathf.Sin(a) * 12);
                DrawRect(tex, x - 2, y - 2, 4, 4, Metal);
            }
            DrawCircle(tex, 16, 16, 3, Cyan);
        }

        private static void DrawExit(Texture2D tex)
        {
            DrawCircleOutline(tex, 16, 17, 11, Red, 3);
            DrawRect(tex, 14, 4, 4, 13, Red);
        }

        private static void DrawUpgrade(Texture2D tex)
        {
            DrawPoly(tex, new[] { P(16, 4), P(27, 15), P(20, 15), P(20, 28), P(12, 28), P(12, 15), P(5, 15) }, Green);
            DrawRect(tex, 7, 23, 18, 5, Metal);
            DrawCircle(tex, 23, 24, 5, Shadow);
            DrawCircleOutline(tex, 23, 24, 5, Metal, 2);
        }

        private static void DrawCharacter(Texture2D tex)
        {
            DrawCircle(tex, 16, 10, 7, Metal);
            DrawRect(tex, 9, 18, 14, 10, Metal);
            DrawRect(tex, 6, 24, 20, 5, Metal);
            DrawRect(tex, 12, 9, 2, 2, Shadow);
            DrawRect(tex, 18, 9, 2, 2, Shadow);
        }

        private static void DrawCrafting(Texture2D tex)
        {
            DrawLine(tex, 8, 25, 24, 9, Metal, 4);
            DrawLine(tex, 7, 8, 25, 26, Metal, 4);
            DrawRect(tex, 20, 5, 7, 5, Metal);
            DrawRect(tex, 5, 21, 6, 6, Metal);
            DrawRect(tex, 6, 6, 5, 5, Metal);
            DrawRect(tex, 22, 22, 5, 5, Metal);
        }

        private static void DrawBack(Texture2D tex)
        {
            DrawPoly(tex, new[] { P(6, 16), P(17, 6), P(17, 12), P(27, 12), P(27, 20), P(17, 20), P(17, 26) }, Text);
        }

        private static void DrawLock(Texture2D tex)
        {
            DrawRect(tex, 8, 14, 16, 13, Red);
            DrawRectOutline(tex, 10, 6, 12, 12, Red, 3);
            DrawRect(tex, 15, 18, 2, 6, Shadow);
        }

        private static void DrawCheck(Texture2D tex)
        {
            DrawLine(tex, 7, 16, 13, 24, Green, 4);
            DrawLine(tex, 13, 24, 26, 8, Green, 4);
        }

        private static void DrawDisplay(Texture2D tex)
        {
            DrawRect(tex, 5, 7, 22, 16, Metal);
            DrawRect(tex, 8, 10, 16, 10, Cyan);
            DrawRect(tex, 12, 24, 8, 3, Metal);
            DrawRect(tex, 9, 27, 14, 3, Metal);
        }

        private static void DrawSound(Texture2D tex)
        {
            DrawPoly(tex, new[] { P(5, 13), P(11, 13), P(20, 6), P(20, 26), P(11, 19), P(5, 19) }, Metal);
            DrawCircleOutline(tex, 20, 16, 7, Cyan, 2);
            DrawCircleOutline(tex, 20, 16, 11, Cyan, 2);
        }

        private static void DrawLanguage(Texture2D tex)
        {
            DrawCircleOutline(tex, 16, 16, 12, Cyan, 2);
            DrawLine(tex, 4, 16, 28, 16, Cyan, 2);
            DrawLine(tex, 16, 4, 16, 28, Cyan, 2);
            DrawCircleOutline(tex, 16, 16, 7, Cyan, 1);
        }

        private static void DrawAccessibility(Texture2D tex)
        {
            DrawCircle(tex, 16, 6, 3, Amber);
            DrawLine(tex, 6, 12, 26, 12, Amber, 3);
            DrawLine(tex, 16, 12, 16, 23, Amber, 3);
            DrawLine(tex, 16, 23, 9, 29, Amber, 3);
            DrawLine(tex, 16, 23, 23, 29, Amber, 3);
        }

        private static void Fill(Texture2D tex, Color color)
        {
            var pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
        }

        private static Vector2Int P(int x, int y) => new Vector2Int(x, y);

        private static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color color)
        {
            int x0 = Mathf.Clamp(x, 0, tex.width);
            int y0 = Mathf.Clamp(ToPixelY(tex, y + h - 1), 0, tex.height);
            int x1 = Mathf.Clamp(x + w, 0, tex.width);
            int y1 = Mathf.Clamp(ToPixelY(tex, y - 1), 0, tex.height);
            for (int yy = y0; yy < y1; yy++)
            {
                for (int xx = x0; xx < x1; xx++)
                {
                    tex.SetPixel(xx, yy, Blend(tex.GetPixel(xx, yy), color));
                }
            }
        }

        private static void DrawRectOutline(Texture2D tex, int x, int y, int w, int h, Color color, int thickness)
        {
            DrawRect(tex, x, y, w, thickness, color);
            DrawRect(tex, x, y + h - thickness, w, thickness, color);
            DrawRect(tex, x, y, thickness, h, color);
            DrawRect(tex, x + w - thickness, y, thickness, h, color);
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawRect(tex, x0 - thickness / 2, y0 - thickness / 2, thickness, thickness, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private static void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        int px = cx + x;
                        int py = ToPixelY(tex, cy + y);
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        {
                            tex.SetPixel(px, py, Blend(tex.GetPixel(px, py), color));
                        }
                    }
                }
            }
        }

        private static void DrawCircleOutline(Texture2D tex, int cx, int cy, int radius, Color color, int thickness)
        {
            int inner = Mathf.Max(0, radius - thickness);
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    int d = x * x + y * y;
                    if (d <= radius * radius && d >= inner * inner)
                    {
                        int px = cx + x;
                        int py = ToPixelY(tex, cy + y);
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        {
                            tex.SetPixel(px, py, Blend(tex.GetPixel(px, py), color));
                        }
                    }
                }
            }
        }

        private static void DrawPoly(Texture2D tex, Vector2Int[] points, Color color)
        {
            if (points == null || points.Length < 3) return;

            int minY = tex.height;
            int maxY = 0;
            foreach (var p in points)
            {
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }

            for (int y = minY; y <= maxY; y++)
            {
                var nodes = new List<int>();
                int j = points.Length - 1;
                for (int i = 0; i < points.Length; i++)
                {
                    if ((points[i].y < y && points[j].y >= y) || (points[j].y < y && points[i].y >= y))
                    {
                        int x = points[i].x + (y - points[i].y) * (points[j].x - points[i].x) / (points[j].y - points[i].y);
                        nodes.Add(x);
                    }
                    j = i;
                }

                nodes.Sort();
                for (int i = 0; i + 1 < nodes.Count; i += 2)
                {
                    DrawRect(tex, nodes[i], y, nodes[i + 1] - nodes[i] + 1, 1, color);
                }
            }
        }

        private static int ToPixelY(Texture2D tex, int topOriginY)
        {
            return tex.height - 1 - topOriginY;
        }

        private static void Blit(Texture2D dst, Texture2D src, int x, int y)
        {
            for (int yy = 0; yy < src.height; yy++)
            {
                for (int xx = 0; xx < src.width; xx++)
                {
                    int dx = x + xx;
                    int dy = y + yy;
                    if (dx < 0 || dx >= dst.width || dy < 0 || dy >= dst.height) continue;
                    dst.SetPixel(dx, dy, Blend(dst.GetPixel(dx, dy), src.GetPixel(xx, yy)));
                }
            }
        }

        private static Color Blend(Color dst, Color src)
        {
            float a = src.a + dst.a * (1f - src.a);
            if (a <= 0f) return Clear;
            return new Color(
                (src.r * src.a + dst.r * dst.a * (1f - src.a)) / a,
                (src.g * src.a + dst.g * dst.a * (1f - src.a)) / a,
                (src.b * src.a + dst.b * dst.a * (1f - src.a)) / a,
                a);
        }

        private static Color Hex(byte r, byte g, byte b, byte a) => new Color32(r, g, b, a);

        private readonly struct IconDef
        {
            public readonly Action<Texture2D> Draw;

            public IconDef(Action<Texture2D> draw)
            {
                Draw = draw;
            }
        }
    }
}
#endif

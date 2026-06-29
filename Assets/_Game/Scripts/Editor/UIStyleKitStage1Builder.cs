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
    /// UI 1단계 공통 스타일 키트 생성기.
    ///
    /// 생성물:
    /// - Assets/_Game/Sprites/UI/Common/*.png: 9-slice 가능한 공통 UI 스프라이트
    /// - Assets/_Game/Sprites/UI/Common/ui_common_contact_sheet.png: 생성 리소스 확인용 시트
    /// - Assets/_Game/Prefabs/UI/Common/MetalPanel.prefab
    /// - Assets/_Game/Prefabs/UI/Common/MetalButton.prefab
    /// - Assets/_Game/Prefabs/UI/Common/UIStyleKitPreview.prefab
    ///
    /// 메뉴:
    /// Legacy generator. 실제 빌드 UI 작업에서는 메뉴에 노출하지 않는다.
    /// </summary>
    public static class UIStyleKitStage1Builder
    {
        private const string SpriteFolder = "Assets/_Game/Sprites/UI/Common";
        private const string PrefabFolder = "Assets/_Game/Prefabs/UI/Common";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset";
        private const string SourceAtlasPath = "docs/image/ui-concepts/ui-kit-source-atlas-v3.png";
        private const bool UseSourceAtlasCrops = true;

        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
        private static readonly Color PanelDark = Hex(0x10, 0x17, 0x18, 0xF5);
        private static readonly Color PanelMid = Hex(0x26, 0x35, 0x2D, 0xFF);
        private static readonly Color PanelHi = Hex(0x4B, 0x5F, 0x4D, 0xFF);
        private static readonly Color MetalEdge = Hex(0x8B, 0x7D, 0x62, 0xFF);
        private static readonly Color MetalShadow = Hex(0x08, 0x0B, 0x0D, 0xFF);
        private static readonly Color Cyan = Hex(0x67, 0xE6, 0xE4, 0xFF);
        private static readonly Color Green = Hex(0x7E, 0xD7, 0x8E, 0xFF);
        private static readonly Color Amber = Hex(0xFF, 0xC4, 0x5A, 0xFF);
        private static readonly Color Red = Hex(0xD4, 0x67, 0x5F, 0xFF);
        private static readonly Color Text = Hex(0xE8, 0xE0, 0xD0, 0xFF);

        public static void Build()
        {
            EnsureFolders();

            if (UseSourceAtlasCrops && File.Exists(SourceAtlasPath))
            {
                CreateSpritesFromSourceAtlas();
                CreateContactSheetFromFiles();
                AssetDatabase.Refresh();

                CreateMetalPanelPrefab();
                CreateMetalButtonPrefab();
                CreatePreviewPrefab();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var previewFromAtlas = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/UIStyleKitPreview.prefab");
                Selection.activeObject = previewFromAtlas;
                if (previewFromAtlas != null) EditorGUIUtility.PingObject(previewFromAtlas);

                Debug.Log("[UIStyleKitStage1] 소스 아틀라스 기반 1단계 공통 UI 키트 생성 완료.");
                return;
            }

            var sprites = new Dictionary<string, SpriteDef>
            {
                ["metal_panel"] = new SpriteDef(128, 128, new Vector4(24, 24, 24, 24), DrawPanel),
                ["metal_panel_small"] = new SpriteDef(160, 56, new Vector4(24, 18, 24, 18), DrawThinPanel),
                ["popup_frame"] = new SpriteDef(160, 112, new Vector4(28, 28, 28, 28), DrawPopup),
                ["metal_button_normal"] = new SpriteDef(144, 48, new Vector4(24, 16, 24, 16), (tex) => DrawButton(tex, ButtonVisual.Normal)),
                ["metal_button_hover"] = new SpriteDef(144, 48, new Vector4(24, 16, 24, 16), (tex) => DrawButton(tex, ButtonVisual.Hover)),
                ["metal_button_pressed"] = new SpriteDef(144, 48, new Vector4(24, 16, 24, 16), (tex) => DrawButton(tex, ButtonVisual.Pressed)),
                ["metal_button_disabled"] = new SpriteDef(144, 48, new Vector4(24, 16, 24, 16), (tex) => DrawButton(tex, ButtonVisual.Disabled)),
                ["button_glow_cyan"] = new SpriteDef(144, 48, new Vector4(24, 16, 24, 16), (tex) => DrawGlowFrame(tex, Cyan)),
                ["button_glow_amber"] = new SpriteDef(144, 48, new Vector4(24, 16, 24, 16), (tex) => DrawGlowFrame(tex, Amber)),
                ["resource_slot"] = new SpriteDef(128, 40, new Vector4(22, 14, 22, 14), DrawResourceSlot),
                ["list_row"] = new SpriteDef(160, 40, new Vector4(22, 14, 22, 14), DrawListRow),
                ["card_frame"] = new SpriteDef(96, 144, new Vector4(22, 26, 22, 26), DrawCardFrame),
                ["node_frame"] = new SpriteDef(64, 64, new Vector4(16, 16, 16, 16), (tex) => DrawNode(tex, PanelHi)),
                ["node_frame_active"] = new SpriteDef(64, 64, new Vector4(16, 16, 16, 16), (tex) => DrawNode(tex, Green)),
                ["node_frame_selected"] = new SpriteDef(64, 64, new Vector4(16, 16, 16, 16), (tex) => DrawNode(tex, Amber)),
                ["node_frame_locked"] = new SpriteDef(64, 64, new Vector4(16, 16, 16, 16), DrawLockedNode),
                ["lock_overlay"] = new SpriteDef(64, 64, new Vector4(8, 8, 8, 8), DrawLockOverlay),
                ["selected_glow"] = new SpriteDef(72, 112, new Vector4(18, 18, 18, 18), (tex) => DrawGlowFrame(tex, Amber)),
                ["tooltip_frame"] = new SpriteDef(96, 44, new Vector4(12, 12, 12, 12), DrawTooltip),
                ["divider_line"] = new SpriteDef(48, 6, Vector4.zero, DrawDivider),
            };

            foreach (var pair in sprites)
            {
                CreateSprite(pair.Key, pair.Value);
            }

            CreateContactSheet(sprites);
            AssetDatabase.Refresh();

            CreateMetalPanelPrefab();
            CreateMetalButtonPrefab();
            CreatePreviewPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var preview = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/UIStyleKitPreview.prefab");
            Selection.activeObject = preview;
            if (preview != null) EditorGUIUtility.PingObject(preview);

            Debug.Log("[UIStyleKitStage1] 1단계 공통 UI 키트 생성 완료. UIStyleKitPreview.prefab을 열어 버튼/패널/노드 상태를 확인하세요.");
        }

        private static void CreateSpritesFromSourceAtlas()
        {
            byte[] bytes = File.ReadAllBytes(SourceAtlasPath);
            var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            atlas.LoadImage(bytes);

            var crops = new Dictionary<string, CropDef>
            {
                ["metal_panel"] = new CropDef(24, 36, 526, 350, new Vector4(72, 72, 72, 72), true),
                ["metal_panel_small"] = new CropDef(589, 55, 370, 74, new Vector4(62, 22, 62, 22), true),
                ["popup_frame"] = new CropDef(591, 160, 335, 246, new Vector4(58, 58, 58, 58), true),
                ["metal_button_normal"] = new CropDef(965, 55, 265, 86, new Vector4(52, 26, 52, 26), true),
                ["metal_button_hover"] = new CropDef(1233, 50, 275, 90, new Vector4(52, 26, 52, 26), true),
                ["metal_button_pressed"] = new CropDef(959, 174, 270, 86, new Vector4(52, 26, 52, 26), true),
                ["metal_button_disabled"] = new CropDef(959, 287, 270, 82, new Vector4(52, 26, 52, 26), true),
                ["button_glow_amber"] = new CropDef(1241, 174, 270, 90, new Vector4(52, 26, 52, 26), true),
                ["button_glow_cyan"] = new CropDef(1233, 50, 275, 90, new Vector4(52, 26, 52, 26), true),
                ["resource_slot"] = new CropDef(237, 479, 456, 68, new Vector4(58, 20, 58, 20), true),
                ["list_row"] = new CropDef(237, 584, 370, 68, new Vector4(58, 20, 58, 20), true),
                ["card_frame"] = new CropDef(24, 453, 186, 246, new Vector4(48, 48, 48, 48), true),
                ["node_frame"] = new CropDef(957, 445, 172, 145, new Vector4(40, 40, 40, 40), true),
                ["node_frame_active"] = new CropDef(1115, 445, 192, 145, new Vector4(40, 40, 40, 40), true),
                ["node_frame_selected"] = new CropDef(1356, 445, 145, 145, new Vector4(40, 40, 40, 40), true),
                ["node_frame_locked"] = new CropDef(1356, 608, 145, 140, new Vector4(40, 40, 40, 40), true),
                ["tooltip_frame"] = new CropDef(24, 775, 370, 164, new Vector4(58, 48, 58, 48), true),
                ["lock_overlay"] = new CropDef(982, 608, 150, 140, new Vector4(34, 34, 34, 34), true),
                ["selected_glow"] = new CropDef(724, 790, 278, 150, new Vector4(52, 44, 52, 44), true),
                ["divider_line"] = new CropDef(429, 846, 245, 18, Vector4.zero, true),
            };

            foreach (var pair in crops)
            {
                CreateSpriteFromAtlas(atlas, pair.Key, pair.Value);
            }

            UnityEngine.Object.DestroyImmediate(atlas);
        }

        private static void CreateSpriteFromAtlas(Texture2D atlas, string name, CropDef crop)
        {
            string path = $"{SpriteFolder}/{name}.png";
            var tex = new Texture2D(crop.Width, crop.Height, TextureFormat.RGBA32, mipChain: false);

            for (int y = 0; y < crop.Height; y++)
            {
                for (int x = 0; x < crop.Width; x++)
                {
                    int srcX = crop.X + x;
                    int srcY = atlas.height - 1 - (crop.Y + y);
                    tex.SetPixel(x, crop.Height - 1 - y, atlas.GetPixel(srcX, srcY));
                }
            }

            if (crop.RemoveBackground)
            {
                RemoveEdgeBackground(tex, 34);
            }
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[UIStyleKitStage1] TextureImporter 로드 실패: {path}");
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
            importer.spriteBorder = crop.Border;
            importer.SaveAndReimport();
        }

        private static void RemoveEdgeBackground(Texture2D tex, int tolerance)
        {
            int w = tex.width;
            int h = tex.height;
            var visited = new bool[w, h];
            var queue = new Queue<Vector2Int>();

            void Enqueue(int x, int y)
            {
                if (x < 0 || x >= w || y < 0 || y >= h || visited[x, y]) return;
                var c = tex.GetPixel(x, y);
                if (c.a <= 0f || Mathf.Max(c.r, c.g, c.b) > 0.38f) return;
                visited[x, y] = true;
                queue.Enqueue(new Vector2Int(x, y));
            }

            for (int x = 0; x < w; x++)
            {
                Enqueue(x, 0);
                Enqueue(x, h - 1);
            }
            for (int y = 0; y < h; y++)
            {
                Enqueue(0, y);
                Enqueue(w - 1, y);
            }

            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                var c = tex.GetPixel(p.x, p.y);
                c.a = 0f;
                tex.SetPixel(p.x, p.y, c);

                TryVisit(p.x + 1, p.y);
                TryVisit(p.x - 1, p.y);
                TryVisit(p.x, p.y + 1);
                TryVisit(p.x, p.y - 1);
            }

            void TryVisit(int x, int y)
            {
                if (x < 0 || x >= w || y < 0 || y >= h || visited[x, y]) return;
                var c = tex.GetPixel(x, y);
                if (c.a <= 0f) return;

                // Atlas background is a dark blue gradient. Only remove dark edge-connected pixels.
                int brightness = Mathf.RoundToInt(Mathf.Max(c.r, c.g, c.b) * 255f);
                if (brightness > 95 + tolerance / 4) return;

                visited[x, y] = true;
                queue.Enqueue(new Vector2Int(x, y));
            }
        }

        private static void CreateContactSheetFromFiles()
        {
            string[] names =
            {
                "metal_panel", "metal_panel_small", "popup_frame", "metal_button_normal",
                "metal_button_hover", "metal_button_pressed", "metal_button_disabled", "button_glow_amber",
                "button_glow_cyan", "resource_slot", "list_row", "card_frame",
                "node_frame", "node_frame_active", "node_frame_selected", "node_frame_locked",
                "tooltip_frame", "lock_overlay", "selected_glow", "divider_line",
            };

            const int cellW = 180;
            const int cellH = 130;
            const int cols = 4;
            int rows = Mathf.CeilToInt(names.Length / (float)cols);
            var sheet = new Texture2D(cellW * cols, cellH * rows, TextureFormat.RGBA32, mipChain: false);
            Fill(sheet, Hex(0x0F, 0x12, 0x18, 0xFF));

            for (int i = 0; i < names.Length; i++)
            {
                string path = $"{SpriteFolder}/{names[i]}.png";
                if (!File.Exists(path)) continue;

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
                tex.LoadImage(File.ReadAllBytes(path));
                float scale = Mathf.Min(150f / tex.width, 100f / tex.height, 1f);
                int dw = Mathf.Max(1, Mathf.RoundToInt(tex.width * scale));
                int dh = Mathf.Max(1, Mathf.RoundToInt(tex.height * scale));
                var scaled = ScaleNearest(tex, dw, dh);

                int col = i % cols;
                int row = i / cols;
                int x = col * cellW + (cellW - dw) / 2;
                int y = row * cellH + 20 + (90 - dh) / 2;
                Blit(sheet, scaled, x, sheet.height - y - dh);

                UnityEngine.Object.DestroyImmediate(tex);
                UnityEngine.Object.DestroyImmediate(scaled);
            }

            sheet.Apply();
            string outPath = $"{SpriteFolder}/ui_common_contact_sheet.png";
            File.WriteAllBytes(outPath, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
        }

        private static Texture2D ScaleNearest(Texture2D src, int width, int height)
        {
            var dst = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sx = Mathf.Clamp(Mathf.FloorToInt(x / (float)width * src.width), 0, src.width - 1);
                    int sy = Mathf.Clamp(Mathf.FloorToInt(y / (float)height * src.height), 0, src.height - 1);
                    dst.SetPixel(x, y, src.GetPixel(sx, sy));
                }
            }
            dst.Apply();
            return dst;
        }

        private static void CreateSprite(string name, SpriteDef def)
        {
            string path = $"{SpriteFolder}/{name}.png";
            var tex = new Texture2D(def.Width, def.Height, TextureFormat.RGBA32, mipChain: false);
            Fill(tex, Clear);
            def.Draw(tex);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[UIStyleKitStage1] TextureImporter 로드 실패: {path}");
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
            importer.spriteBorder = def.Border;
            importer.SaveAndReimport();
        }

        private static void CreateContactSheet(IReadOnlyDictionary<string, SpriteDef> sprites)
        {
            const int cellW = 180;
            const int cellH = 140;
            const int cols = 4;

            int rows = Mathf.CeilToInt(sprites.Count / (float)cols);
            var tex = new Texture2D(cellW * cols, cellH * rows, TextureFormat.RGBA32, mipChain: false);
            Fill(tex, Hex(0x0F, 0x11, 0x18, 0xFF));

            int i = 0;
            foreach (var pair in sprites)
            {
                var def = pair.Value;
                var sample = new Texture2D(def.Width, def.Height, TextureFormat.RGBA32, mipChain: false);
                Fill(sample, Clear);
                def.Draw(sample);
                sample.Apply();

                int col = i % cols;
                int row = i / cols;
                int baseX = col * cellW + 16;
                int baseY = tex.height - ((row + 1) * cellH) + 24;
                DrawRect(tex, col * cellW + 6, tex.height - ((row + 1) * cellH) + 6, cellW - 12, cellH - 12, Hex(0x18, 0x1C, 0x24, 0xFF));
                DrawRectOutline(tex, col * cellW + 6, tex.height - ((row + 1) * cellH) + 6, cellW - 12, cellH - 12, Hex(0x35, 0x45, 0x47, 0xFF), 2);
                Blit(tex, sample, baseX, baseY);
                UnityEngine.Object.DestroyImmediate(sample);
                i++;
            }

            tex.Apply();
            string path = $"{SpriteFolder}/ui_common_contact_sheet.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static void CreateMetalPanelPrefab()
        {
            var sprite = LoadSprite("metal_panel");
            var root = CreateUiObject("MetalPanel", new Vector2(360, 220));
            var image = root.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;

            SavePrefab(root.gameObject, $"{PrefabFolder}/MetalPanel.prefab");
        }

        private static void CreateMetalButtonPrefab()
        {
            var root = CreateUiObject("MetalButton", new Vector2(220, 64));
            var image = root.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite("metal_button_normal");
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.SpriteSwap;
            button.targetGraphic = image;
            button.spriteState = new SpriteState
            {
                highlightedSprite = LoadSprite("metal_button_hover"),
                pressedSprite = LoadSprite("metal_button_pressed"),
                selectedSprite = LoadSprite("metal_button_hover"),
                disabledSprite = LoadSprite("metal_button_disabled"),
            };

            var label = CreateText(root.transform, "Label", "BUTTON", 26, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);

            SavePrefab(root.gameObject, $"{PrefabFolder}/MetalButton.prefab");
        }

        private static void CreatePreviewPrefab()
        {
            var root = new GameObject("UIStyleKitPreview");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.vertexColorAlwaysGammaSpace = true;
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();

            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1920, 1080);

            var bg = CreateChild(root.transform, "Background", new Vector2(1920, 1080));
            Stretch(bg);
            var bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.color = Hex(0x10, 0x12, 0x1C, 0xFF);
            bgImage.raycastTarget = false;

            var panel = CreateChild(root.transform, "CommonPanel", new Vector2(1500, 820));
            panel.anchoredPosition = Vector2.zero;
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = LoadSprite("metal_panel");
            panelImage.type = Image.Type.Sliced;
            panelImage.raycastTarget = false;

            var title = CreateText(panel, "Title", "DRILL-CORP UI STYLE KIT - STAGE 1", 36, TextAlignmentOptions.Center);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -34f);
            title.rectTransform.sizeDelta = new Vector2(-80f, 56f);

            CreatePreviewButton(panel, "NormalButton", "NORMAL", new Vector2(-520, 230), true, false);
            CreatePreviewButton(panel, "HoverButton", "HOVER", new Vector2(-260, 230), true, false, "metal_button_hover");
            CreatePreviewButton(panel, "PressedButton", "PRESSED", new Vector2(0, 230), true, false, "metal_button_pressed");
            CreatePreviewButton(panel, "DisabledButton", "DISABLED", new Vector2(260, 230), false, false);
            CreatePreviewButton(panel, "SelectedButton", "SELECTED", new Vector2(520, 230), true, true);

            CreatePreviewPanel(panel, "PopupFrame", "POPUP FRAME", "popup_frame", new Vector2(-460, 20), new Vector2(360, 200));
            CreatePreviewPanel(panel, "ListRow", "UPGRADE LIST ROW", "list_row", new Vector2(0, 60), new Vector2(420, 58));
            CreatePreviewPanel(panel, "Tooltip", "TOOLTIP", "tooltip_frame", new Vector2(460, 60), new Vector2(320, 80));

            CreatePreviewNode(panel, "NodeNormal", "node_frame", new Vector2(-360, -190));
            CreatePreviewNode(panel, "NodeActive", "node_frame_active", new Vector2(-240, -190));
            CreatePreviewNode(panel, "NodeSelected", "node_frame_selected", new Vector2(-120, -190));
            CreatePreviewNode(panel, "NodeLocked", "node_frame_locked", new Vector2(0, -190));

            CreatePreviewCard(panel, new Vector2(310, -190));
            CreatePreviewResourceBar(panel, new Vector2(520, -250));

            SavePrefab(root, $"{PrefabFolder}/UIStyleKitPreview.prefab");
        }

        private static void CreatePreviewButton(RectTransform parent, string name, string label, Vector2 pos, bool interactable, bool selected, string overrideSprite = null)
        {
            var rt = CreateChild(parent, name, new Vector2(220, 64));
            rt.anchoredPosition = pos;
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(overrideSprite ?? "metal_button_normal");
            image.type = Image.Type.Sliced;
            var button = rt.gameObject.AddComponent<Button>();
            button.interactable = interactable;
            button.transition = Selectable.Transition.SpriteSwap;
            button.targetGraphic = image;
            button.spriteState = new SpriteState
            {
                highlightedSprite = LoadSprite("metal_button_hover"),
                pressedSprite = LoadSprite("metal_button_pressed"),
                selectedSprite = LoadSprite("metal_button_hover"),
                disabledSprite = LoadSprite("metal_button_disabled"),
            };

            if (selected)
            {
                var glow = CreateChild(rt, "SelectedGlow", new Vector2(236, 80));
                glow.SetAsFirstSibling();
                var glowImage = glow.gameObject.AddComponent<Image>();
                glowImage.sprite = LoadSprite("button_glow_amber");
                glowImage.type = Image.Type.Sliced;
                glowImage.raycastTarget = false;
            }

            var tmp = CreateText(rt, "Label", label, 24, TextAlignmentOptions.Center);
            Stretch(tmp.rectTransform);
        }

        private static void CreatePreviewPanel(RectTransform parent, string name, string label, string spriteName, Vector2 pos, Vector2 size)
        {
            var rt = CreateChild(parent, name, size);
            rt.anchoredPosition = pos;
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(spriteName);
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            var tmp = CreateText(rt, "Label", label, 22, TextAlignmentOptions.Center);
            Stretch(tmp.rectTransform);
        }

        private static void CreatePreviewNode(RectTransform parent, string name, string spriteName, Vector2 pos)
        {
            var rt = CreateChild(parent, name, new Vector2(78, 78));
            rt.anchoredPosition = pos;
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(spriteName);
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
        }

        private static void CreatePreviewCard(RectTransform parent, Vector2 pos)
        {
            var rt = CreateChild(parent, "CharacterCardFrame", new Vector2(150, 230));
            rt.anchoredPosition = pos;
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite("card_frame");
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;

            var selected = CreateChild(rt, "SelectedGlow", new Vector2(168, 248));
            selected.SetAsFirstSibling();
            var selectedImage = selected.gameObject.AddComponent<Image>();
            selectedImage.sprite = LoadSprite("selected_glow");
            selectedImage.type = Image.Type.Sliced;
            selectedImage.raycastTarget = false;

            var tmp = CreateText(rt, "Label", "CARD", 22, TextAlignmentOptions.Center);
            tmp.rectTransform.anchorMin = new Vector2(0f, 0f);
            tmp.rectTransform.anchorMax = new Vector2(1f, 0f);
            tmp.rectTransform.pivot = new Vector2(0.5f, 0f);
            tmp.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            tmp.rectTransform.sizeDelta = new Vector2(-20f, 40f);
        }

        private static void CreatePreviewResourceBar(RectTransform parent, Vector2 pos)
        {
            var rt = CreateChild(parent, "ResourceBarSample", new Vector2(360, 56));
            rt.anchoredPosition = pos;
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateResourceSlot(rt, Amber, "61");
            CreateResourceSlot(rt, Cyan, "0");
            CreateResourceSlot(rt, Green, "125");
        }

        private static void CreateResourceSlot(RectTransform parent, Color color, string value)
        {
            var slot = CreateChild(parent, "ResourceSlot", new Vector2(104, 44));
            var image = slot.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite("resource_slot");
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;

            var icon = CreateChild(slot, "Icon", new Vector2(22, 22));
            icon.anchorMin = new Vector2(0f, 0.5f);
            icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(12f, 0f);
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.color = color;
            iconImage.raycastTarget = false;

            var tmp = CreateText(slot, "Value", value, 21, TextAlignmentOptions.Right);
            tmp.rectTransform.anchorMin = new Vector2(0f, 0f);
            tmp.rectTransform.anchorMax = new Vector2(1f, 1f);
            tmp.rectTransform.offsetMin = new Vector2(38f, 0f);
            tmp.rectTransform.offsetMax = new Vector2(-10f, 0f);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment)
        {
            var rt = CreateChild(parent, name, new Vector2(120, 40));
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Text;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            ApplyD2Coding(tmp);
            tmp.outlineWidth = 0.18f;
            tmp.outlineColor = Color.black;
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

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{name}.png");
        }

        private static void ApplyD2Coding(TextMeshProUGUI tmp)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) tmp.font = font;
            else Debug.LogWarning($"[UIStyleKitStage1] D2Coding 폰트 로드 실패: {FontPath}");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(SpriteFolder);
            Directory.CreateDirectory(PrefabFolder);
        }

        private static void Fill(Texture2D tex, Color color)
        {
            var pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
        }

        private static void DrawPanel(Texture2D tex)
        {
            DrawIndustrialFrame(tex, PanelDark, MetalEdge, PanelHi, 7, true);
        }

        private static void DrawThinPanel(Texture2D tex)
        {
            DrawIndustrialFrame(tex, Hex(0x0F, 0x18, 0x17, 0xF8), MetalEdge, PanelHi, 5, false);
            DrawRect(tex, 18, tex.height - 12, tex.width - 36, 2, Hex(0x70, 0x86, 0x78, 0xCC));
            DrawRect(tex, 18, 10, tex.width - 36, 1, Hex(0x06, 0x09, 0x0C, 0xCC));
        }

        private static void DrawPopup(Texture2D tex)
        {
            DrawIndustrialFrame(tex, Hex(0x0D, 0x10, 0x15, 0xFA), MetalEdge, Amber, 8, true);
            DrawRect(tex, 18, tex.height - 21, tex.width - 36, 5, Hex(0x3C, 0x42, 0x35, 0xEE));
            DrawRect(tex, 18, 16, tex.width - 36, 4, Hex(0x34, 0x30, 0x27, 0xEE));
            DrawCornerCaps(tex, 10, Amber);
        }

        private static void DrawButton(Texture2D tex, ButtonVisual visual)
        {
            Color fill = visual switch
            {
                ButtonVisual.Hover => Hex(0x23, 0x3E, 0x3D, 0xFF),
                ButtonVisual.Pressed => Hex(0x10, 0x16, 0x15, 0xFF),
                ButtonVisual.Disabled => Hex(0x24, 0x27, 0x27, 0xDD),
                _ => Hex(0x18, 0x22, 0x1F, 0xFF)
            };
            Color accent = visual == ButtonVisual.Hover ? Cyan : visual == ButtonVisual.Pressed ? Amber : MetalEdge;

            DrawIndustrialFrame(tex, fill, accent, Hex(0x5D, 0x6D, 0x63, 0xFF), 5, false);
            DrawRect(tex, 18, tex.height - 12, tex.width - 36, 2, WithAlpha(accent, 0.72f));
            DrawRect(tex, 18, 10, tex.width - 36, 2, Hex(0x05, 0x07, 0x08, 0xDD));
            DrawRect(tex, 9, tex.height / 2 - 2, 4, 4, WithAlpha(accent, 0.9f));
            DrawRect(tex, tex.width - 13, tex.height / 2 - 2, 4, 4, WithAlpha(accent, 0.9f));

            if (visual == ButtonVisual.Hover) DrawGlowFrame(tex, Cyan);
            if (visual == ButtonVisual.Disabled) DrawRect(tex, 8, 8, tex.width - 16, tex.height - 16, Hex(0x00, 0x00, 0x00, 0x66));
        }

        private static void DrawResourceSlot(Texture2D tex)
        {
            DrawIndustrialFrame(tex, Hex(0x0F, 0x14, 0x16, 0xF8), Hex(0x66, 0x62, 0x52, 0xFF), Hex(0x4F, 0x64, 0x66, 0xFF), 4, false);
            DrawRect(tex, 11, 8, 1, tex.height - 16, Hex(0x3C, 0x45, 0x45, 0xCC));
            DrawRect(tex, 16, tex.height - 11, tex.width - 32, 2, Hex(0x55, 0x67, 0x69, 0xDD));
        }

        private static void DrawListRow(Texture2D tex)
        {
            DrawIndustrialFrame(tex, Hex(0x0F, 0x12, 0x18, 0xF6), Hex(0x45, 0x50, 0x55, 0xFF), Hex(0x2A, 0x36, 0x3E, 0xFF), 4, false);
            DrawRect(tex, 14, tex.height - 10, tex.width - 28, 2, Hex(0x35, 0x42, 0x4C, 0xE8));
            DrawRect(tex, 14, 9, tex.width - 28, 1, Hex(0x06, 0x08, 0x0D, 0xE8));
        }

        private static void DrawCardFrame(Texture2D tex)
        {
            DrawIndustrialFrame(tex, Hex(0x1D, 0x2B, 0x22, 0xF8), MetalEdge, Hex(0x65, 0x78, 0x66, 0xFF), 7, true);
            DrawRect(tex, 15, tex.height - 34, tex.width - 30, 16, Hex(0x2E, 0x46, 0x34, 0xF2));
            DrawRect(tex, 15, 17, tex.width - 30, 20, Hex(0x2A, 0x3E, 0x31, 0xF2));
            DrawRect(tex, 20, tex.height - 24, tex.width - 40, 2, Hex(0x8A, 0x9C, 0x78, 0xCC));
            DrawCornerCaps(tex, 11, MetalEdge);
        }

        private static void DrawNode(Texture2D tex, Color accent)
        {
            DrawGlowFrame(tex, accent);
            DrawIndustrialFrame(tex, Hex(0x12, 0x19, 0x19, 0xF6), accent, Hex(0x58, 0x66, 0x5E, 0xFF), 6, false);
            DrawRect(tex, tex.width / 2 - 8, tex.height - 15, 16, 2, WithAlpha(accent, 0.9f));
            DrawRect(tex, tex.width / 2 - 8, 13, 16, 2, Hex(0x04, 0x06, 0x07, 0xDD));
        }

        private static void DrawLockedNode(Texture2D tex)
        {
            DrawNode(tex, Hex(0x65, 0x69, 0x6C, 0xFF));
            DrawRect(tex, 10, 10, tex.width - 20, tex.height - 20, Hex(0x00, 0x00, 0x00, 0x88));
            DrawRect(tex, 21, 17, 10, 12, Hex(0x85, 0x8C, 0x90, 0xFF));
            DrawRectOutline(tex, 18, 27, 16, 12, Hex(0x85, 0x8C, 0x90, 0xFF), 3);
        }

        private static void DrawLockOverlay(Texture2D tex)
        {
            DrawRect(tex, 0, 0, tex.width, tex.height, Hex(0x00, 0x00, 0x00, 0x99));
            DrawRect(tex, 18, 20, 28, 24, Hex(0x7B, 0x35, 0x31, 0xE8));
            DrawRectOutline(tex, 16, 42, 32, 18, Red, 4);
        }

        private static void DrawTooltip(Texture2D tex)
        {
            DrawIndustrialFrame(tex, Hex(0x0B, 0x0F, 0x14, 0xF7), Hex(0x4B, 0x5D, 0x60, 0xFF), Cyan, 4, false);
            DrawRect(tex, 14, tex.height - 10, tex.width - 28, 2, Cyan);
        }

        private static void DrawDivider(Texture2D tex)
        {
            DrawRect(tex, 0, 2, tex.width, 1, Hex(0x1B, 0x20, 0x1C, 0xFF));
            DrawRect(tex, 4, 3, tex.width - 8, 2, Hex(0x77, 0x73, 0x5C, 0xFF));
            DrawRect(tex, tex.width / 2 - 8, 1, 16, 4, Hex(0xA1, 0x8D, 0x62, 0xFF));
        }

        private static void DrawGlowFrame(Texture2D tex, Color color)
        {
            DrawRectOutline(tex, 1, 1, tex.width - 2, tex.height - 2, WithAlpha(color, 0.18f), 5);
            DrawRectOutline(tex, 4, 4, tex.width - 8, tex.height - 8, WithAlpha(color, 0.42f), 3);
            DrawRectOutline(tex, 7, 7, tex.width - 14, tex.height - 14, WithAlpha(color, 0.85f), 2);
        }

        private static void DrawIndustrialFrame(Texture2D tex, Color fill, Color edge, Color highlight, int corner, bool texture)
        {
            DrawRect(tex, 2, 2, tex.width - 4, tex.height - 4, MetalShadow);
            DrawRect(tex, 5, 5, tex.width - 10, tex.height - 10, fill);
            DrawChamferMask(tex, corner);

            DrawRectOutline(tex, 4, 4, tex.width - 8, tex.height - 8, edge, 2);
            DrawRectOutline(tex, 8, 8, tex.width - 16, tex.height - 16, Hex(0x05, 0x08, 0x09, 0xCC), 2);
            DrawRectOutline(tex, 11, 11, tex.width - 22, tex.height - 22, WithAlpha(highlight, 0.45f), 1);

            DrawRect(tex, 16, tex.height - 12, Math.Max(4, tex.width / 5), 2, WithAlpha(highlight, 0.65f));
            DrawRect(tex, tex.width - 16 - Math.Max(4, tex.width / 5), tex.height - 12, Math.Max(4, tex.width / 5), 2, WithAlpha(highlight, 0.38f));
            DrawRect(tex, 16, 10, Math.Max(4, tex.width / 5), 2, Hex(0x04, 0x06, 0x07, 0xCC));

            if (texture)
            {
                DrawPanelTexture(tex);
            }

            DrawChamferMask(tex, corner);
            DrawCornerCaps(tex, corner + 4, edge);
        }

        private static void DrawPanelTexture(Texture2D tex)
        {
            for (int y = 16; y < tex.height - 16; y += 6)
            {
                for (int x = 16; x < tex.width - 16; x += 6)
                {
                    if (((x + y) / 6) % 2 == 0)
                    {
                        DrawRect(tex, x, y, 1, 1, Hex(0x4A, 0x52, 0x46, 0x38));
                    }
                    else
                    {
                        DrawRect(tex, x, y, 1, 1, Hex(0x02, 0x04, 0x05, 0x42));
                    }
                }
            }
        }

        private static void DrawChamferMask(Texture2D tex, int corner)
        {
            for (int i = 0; i < corner; i++)
            {
                int len = corner - i;
                ClearRect(tex, 0, i, len, 1);
                ClearRect(tex, i, 0, 1, len);
                ClearRect(tex, tex.width - len, i, len, 1);
                ClearRect(tex, tex.width - i - 1, 0, 1, len);
                ClearRect(tex, 0, tex.height - i - 1, len, 1);
                ClearRect(tex, i, tex.height - len, 1, len);
                ClearRect(tex, tex.width - len, tex.height - i - 1, len, 1);
                ClearRect(tex, tex.width - i - 1, tex.height - len, 1, len);
            }
        }

        private static void DrawCornerCaps(Texture2D tex, int inset, Color color)
        {
            DrawRect(tex, inset, tex.height - inset - 2, 18, 2, color);
            DrawRect(tex, inset, tex.height - inset - 8, 2, 8, color);
            DrawRect(tex, tex.width - inset - 18, tex.height - inset - 2, 18, 2, color);
            DrawRect(tex, tex.width - inset - 2, tex.height - inset - 8, 2, 8, color);
            DrawRect(tex, inset, inset, 18, 2, WithAlpha(color, 0.8f));
            DrawRect(tex, inset, inset, 2, 8, WithAlpha(color, 0.8f));
            DrawRect(tex, tex.width - inset - 18, inset, 18, 2, WithAlpha(color, 0.8f));
            DrawRect(tex, tex.width - inset - 2, inset, 2, 8, WithAlpha(color, 0.8f));
            DrawCornerBolts(tex, inset + 6, inset + 6);
        }

        private static void DrawCornerBolts(Texture2D tex, int insetX, int insetY)
        {
            DrawRect(tex, insetX, insetY, 4, 4, MetalEdge);
            DrawRect(tex, tex.width - insetX - 4, insetY, 4, 4, MetalEdge);
            DrawRect(tex, insetX, tex.height - insetY - 4, 4, 4, MetalEdge);
            DrawRect(tex, tex.width - insetX - 4, tex.height - insetY - 4, 4, 4, MetalEdge);
        }

        private static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color color)
        {
            int x0 = Mathf.Clamp(x, 0, tex.width);
            int y0 = Mathf.Clamp(y, 0, tex.height);
            int x1 = Mathf.Clamp(x + w, 0, tex.width);
            int y1 = Mathf.Clamp(y + h, 0, tex.height);
            for (int yy = y0; yy < y1; yy++)
            {
                for (int xx = x0; xx < x1; xx++)
                {
                    tex.SetPixel(xx, yy, Blend(tex.GetPixel(xx, yy), color));
                }
            }
        }

        private static void ClearRect(Texture2D tex, int x, int y, int w, int h)
        {
            int x0 = Mathf.Clamp(x, 0, tex.width);
            int y0 = Mathf.Clamp(y, 0, tex.height);
            int x1 = Mathf.Clamp(x + w, 0, tex.width);
            int y1 = Mathf.Clamp(y + h, 0, tex.height);
            for (int yy = y0; yy < y1; yy++)
            {
                for (int xx = x0; xx < x1; xx++)
                {
                    tex.SetPixel(xx, yy, Clear);
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

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private readonly struct SpriteDef
        {
            public readonly int Width;
            public readonly int Height;
            public readonly Vector4 Border;
            public readonly Action<Texture2D> Draw;

            public SpriteDef(int width, int height, Vector4 border, Action<Texture2D> draw)
            {
                Width = width;
                Height = height;
                Border = border;
                Draw = draw;
            }
        }

        private readonly struct CropDef
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Width;
            public readonly int Height;
            public readonly Vector4 Border;
            public readonly bool RemoveBackground;

            public CropDef(int x, int y, int width, int height, Vector4 border, bool removeBackground = false)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
                Border = border;
                RemoveBackground = removeBackground;
            }
        }

        private enum ButtonVisual
        {
            Normal,
            Hover,
            Pressed,
            Disabled,
        }
    }
}
#endif

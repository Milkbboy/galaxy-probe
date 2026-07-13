using System.IO;
using DrillCorp.UI;
using DrillCorp.OutGame;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DrillCorp.Editor
{
    public static class TitleLandingSetupEditor
    {
        private const string TitleScenePath = "Assets/_Game/Scenes/Title.unity";
        private const string SourceFolder = "docs/Title";
        private const string TitleSpriteFolder = "Assets/_Game/Sprites/UI/Title/Generated";
        private const string BackgroundSpritePath = TitleSpriteFolder + "/title_background_space_drill.png";
        private const string LogoSpritePath = TitleSpriteFolder + "/title_logo_drill_corp_swarm_survivor.png";
        private const string ButtonNormalSpritePath = TitleSpriteFolder + "/title_button_panel_normal.png";
        private const string ButtonHoverSpritePath = TitleSpriteFolder + "/title_button_panel_hover_green.png";
        private const string ButtonDisabledSpritePath = TitleSpriteFolder + "/title_button_panel_disabled.png";
        private const string BottomNavigationBaseSpritePath = TitleSpriteFolder + "/bottom_navigation_metal_base_1920x220.png";
        private const string BottomMetalFrameSpritePath = TitleSpriteFolder + "/common_metal_button_frame_600x140.png";
        private const string BottomHoverGlowSpritePath = TitleSpriteFolder + "/common_hover_glow_504x92.png";
        private const string UpgradesInnerPanelSpritePath = TitleSpriteFolder + "/upgrades_inner_panel_green_504x92.png";
        private const string CharacterInnerPanelSpritePath = TitleSpriteFolder + "/character_inner_panel_cyan_504x92.png";
        private const string CraftingInnerPanelSpritePath = TitleSpriteFolder + "/crafting_inner_panel_purple_504x92.png";
        private const string UpgradesIconSpritePath = TitleSpriteFolder + "/icon_upgrades_128.png";
        private const string CharacterIconSpritePath = TitleSpriteFolder + "/icon_character_128.png";
        private const string CraftingIconSpritePath = TitleSpriteFolder + "/icon_crafting_128.png";
        private const string PanelName = "TitleLandingPanel";
        private static readonly Vector4 ButtonBorder = new Vector4(56f, 36f, 56f, 36f);
        private static readonly Vector4 BottomMetalFrameBorder = new Vector4(48f, 40f, 48f, 40f);
        private static readonly Vector4 BottomPanelBorder = new Vector4(32f, 24f, 32f, 24f);
        private static readonly Vector2 BottomButtonSize = new Vector2(520f, 126f);
        // The fixed-size artwork is uniformly scaled from 600x140. Rendering these
        // layers as Simple images keeps the 536x104 panel inset proportional.
        private static readonly Vector2 BottomFrameSize = new Vector2(520f, 126f);
        private static readonly Vector2 BottomPanelSize = new Vector2(464.533f, 90.133f);
        private static readonly Vector2 BottomIconSize = new Vector2(104f, 104f);
        private static readonly Vector2 BottomTitleSize = new Vector2(310f, 42f);
        private static readonly Vector2 BottomSubtitleSize = new Vector2(310f, 24f);

        public static void ApplyTitleLandingScreen()
        {
            if (SceneManager.GetActiveScene().path != TitleScenePath)
                EditorSceneManager.OpenScene(TitleScenePath);

            EnsureTitleSpriteAssets();

            var canvas = FindOrCreateCanvas();
            var titleUI = canvas.GetComponent<TitleUI>();
            if (titleUI == null)
                titleUI = canvas.gameObject.AddComponent<TitleUI>();

            var panel = FindOrCreatePanel(canvas.transform);
            RebuildPanel(panel, titleUI);
            EnsureTMPFontHolder();
            ConnectTitleUI(titleUI, canvas.transform, panel);
            SetInitialPanelState(canvas.transform, panel);
            EnsureInputSystemEventSystem();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[TitleLandingSetup] Title landing screen applied and saved.");
        }

        private static void EnsureTitleSpriteAssets()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                return;

            EnsureFolder("Assets/_Game/Sprites/UI", "Title");
            EnsureFolder("Assets/_Game/Sprites/UI/Title", "Generated");

            CopySprite(projectRoot, "title_background_space_drill.png", BackgroundSpritePath, false, false);
            ProcessTransparentSprite(projectRoot, "title_logo_drill_corp_swarm_survivor.png", LogoSpritePath, false);
            ProcessTransparentSprite(projectRoot, "title_button_panel_normal.png", ButtonNormalSpritePath, true);
            ProcessTransparentSprite(projectRoot, "title_button_panel_hover_green.png", ButtonHoverSpritePath, true);
            ProcessTransparentSprite(projectRoot, "title_button_panel_disabled.png", ButtonDisabledSpritePath, true);
            CopySprite(projectRoot, "bottom_navigation_metal_base_1920x220.png", BottomNavigationBaseSpritePath, false, true);
            CopySprite(projectRoot, "common_metal_button_frame_600x140.png", BottomMetalFrameSpritePath, true, true, BottomMetalFrameBorder);
            CopySprite(projectRoot, "common_hover_glow_504x92.png", BottomHoverGlowSpritePath, true, true, BottomPanelBorder);
            CopySprite(projectRoot, "upgrades_inner_panel_green_504x92.png", UpgradesInnerPanelSpritePath, true, true, BottomPanelBorder);
            CopySprite(projectRoot, "character_inner_panel_cyan_504x92.png", CharacterInnerPanelSpritePath, true, true, BottomPanelBorder);
            CopySprite(projectRoot, "crafting_inner_panel_purple_504x92.png", CraftingInnerPanelSpritePath, true, true, BottomPanelBorder);
            CopySprite(projectRoot, "icon_upgrades_128.png", UpgradesIconSpritePath, false, true);
            CopySprite(projectRoot, "icon_character_128.png", CharacterIconSpritePath, false, true);
            CopySprite(projectRoot, "icon_crafting_128.png", CraftingIconSpritePath, false, true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void CopySprite(string projectRoot, string sourceName, string targetAssetPath, bool sliced, bool alpha)
        {
            CopySprite(projectRoot, sourceName, targetAssetPath, sliced, alpha, sliced ? ButtonBorder : Vector4.zero);
        }

        private static void CopySprite(string projectRoot, string sourceName, string targetAssetPath, bool sliced, bool alpha, Vector4 spriteBorder)
        {
            var sourceFullPath = Path.Combine(projectRoot, SourceFolder, sourceName);
            var targetFullPath = Path.Combine(projectRoot, targetAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFullPath));

            if (!File.Exists(sourceFullPath))
            {
                Debug.LogWarning($"[TitleLandingSetup] Source image not found: {sourceFullPath}");
                return;
            }

            if (!File.Exists(targetFullPath) || File.GetLastWriteTimeUtc(sourceFullPath) > File.GetLastWriteTimeUtc(targetFullPath))
                File.Copy(sourceFullPath, targetFullPath, true);

            ImportSprite(targetAssetPath, sliced, alpha, spriteBorder);
        }

        private static void ProcessTransparentSprite(string projectRoot, string sourceName, string targetAssetPath, bool sliced)
        {
            var sourceFullPath = Path.Combine(projectRoot, SourceFolder, sourceName);
            var targetFullPath = Path.Combine(projectRoot, targetAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFullPath));

            if (!File.Exists(sourceFullPath))
            {
                Debug.LogWarning($"[TitleLandingSetup] Source image not found: {sourceFullPath}");
                return;
            }

            var sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!sourceTexture.LoadImage(File.ReadAllBytes(sourceFullPath)))
            {
                Object.DestroyImmediate(sourceTexture);
                return;
            }

            ApplyCheckerTransparency(sourceTexture);
            var cropped = CropTransparent(sourceTexture, 2);
            File.WriteAllBytes(targetFullPath, cropped.EncodeToPNG());
            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(cropped);

            ImportSprite(targetAssetPath, sliced, true, sliced ? ButtonBorder : Vector4.zero);
        }

        private static void ApplyCheckerTransparency(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                int min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                if (max >= 238 && max - min <= 12)
                    c.a = 0;
                pixels[i] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private static Texture2D CropTransparent(Texture2D source, int padding)
        {
            var pixels = source.GetPixels32();
            int minX = source.width;
            int minY = source.height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < source.height; y++)
            {
                for (int x = 0; x < source.width; x++)
                {
                    if (pixels[y * source.width + x].a <= 8)
                        continue;

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
                return Object.Instantiate(source);

            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(source.width - 1, maxX + padding);
            maxY = Mathf.Min(source.height - 1, maxY + padding);

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);

            var croppedPixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    croppedPixels[y * width + x] = pixels[(minY + y) * source.width + minX + x];
            }

            result.SetPixels32(croppedPixels);
            result.Apply(false, false);
            return result;
        }

        private static void ImportSprite(string assetPath, bool sliced, bool alpha)
        {
            ImportSprite(assetPath, sliced, alpha, sliced ? ButtonBorder : Vector4.zero);
        }

        private static void ImportSprite(string assetPath, bool sliced, bool alpha, Vector4 spriteBorder)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = alpha;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.spriteBorder = sliced ? spriteBorder : Vector4.zero;
                importer.SaveAndReimport();
            }
        }

        private static Canvas FindOrCreateCanvas()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
                return canvas;

            var canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<GraphicRaycaster>();

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static RectTransform FindOrCreatePanel(Transform canvasTransform)
        {
            var existing = canvasTransform.Find(PanelName);
            if (existing != null)
                return (RectTransform)existing;

            var panelObj = new GameObject(PanelName);
            panelObj.transform.SetParent(canvasTransform, false);
            var panel = panelObj.AddComponent<RectTransform>();
            Stretch(panel);
            panelObj.SetActive(true);
            return panel;
        }

        private static void RebuildPanel(RectTransform panel, TitleUI titleUI)
        {
            for (int i = panel.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(panel.GetChild(i).gameObject);

            Stretch(panel);
            panel.SetAsLastSibling();

            var contentRoot = CreateContentRoot(panel);
            CreateStretchImage(contentRoot, "BackgroundImage", BackgroundSpritePath);
            CreateImage(contentRoot, "BottomNavigationBase", BottomNavigationBaseSpritePath, new Vector2(0f, -430f), new Vector2(1920f, 220f), false, false);
            CreateImage(contentRoot, "LogoImage", LogoSpritePath, new Vector2(0f, 300f), new Vector2(1040f, 310f), false, true);

            CreateTitleButton(contentRoot, "SettingsButton", "SETTINGS", new Vector2(640f, 345f), new Vector2(350f, 92f), 34f, titleUI.ShowOptionsPanel);
            CreateTitleButton(contentRoot, "ExitButton", "EXIT GAME", new Vector2(640f, 225f), new Vector2(350f, 92f), 32f, titleUI.QuitGame);
            CreateBottomMenuButton(contentRoot, "UpgradesButton", "UPGRADES", "ENHANCE YOUR DRILL", UpgradesInnerPanelSpritePath, UpgradesIconSpritePath, new Color(0.337f, 0.949f, 0.698f, 0.65f), new Vector2(-510f, -421f), titleUI.ShowUpgradeHubPanel);
            CreateBottomMenuButton(contentRoot, "CharacterButton", "CHARACTER", "SELECT YOUR SPECIALIST", CharacterInnerPanelSpritePath, CharacterIconSpritePath, new Color(0.325f, 0.867f, 0.961f, 0.65f), new Vector2(0f, -421f), titleUI.ShowCharacterHubPanel);
            CreateBottomMenuButton(contentRoot, "CraftingButton", "CRAFTING", "BUILD POWERFUL WEAPONS", CraftingInnerPanelSpritePath, CraftingIconSpritePath, new Color(0.757f, 0.518f, 0.957f, 0.65f), new Vector2(510f, -421f), titleUI.ShowCraftingHubPanel);
        }

        private static RectTransform CreateContentRoot(RectTransform parent)
        {
            var obj = new GameObject("ContentRoot_16x9");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            Stretch(rect);

            var fitter = obj.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;

            return rect;
        }

        private static Image CreateStretchImage(RectTransform parent, string name, string spritePath)
        {
            var image = CreateImage(parent, name, spritePath, Vector2.zero, Vector2.zero, false, false);
            Stretch(image.rectTransform);
            return image;
        }

        private static Image CreateImage(RectTransform parent, string name, string spritePath, Vector2 position, Vector2 size, bool sliced, bool preserveAspect)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = obj.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateTitleButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size, float fontSize, UnityEngine.Events.UnityAction action)
        {
            var image = CreateImage(parent, name, ButtonNormalSpritePath, position, size, true, false);
            image.raycastTarget = true;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonHoverSpritePath),
                pressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonHoverSpritePath),
                selectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonHoverSpritePath),
                disabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonDisabledSpritePath)
            };

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.9f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            button.colors = colors;

            UnityEventTools.AddPersistentListener(button.onClick, action);

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(image.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(38f, 14f);
            textRect.offsetMax = new Vector2(-38f, -14f);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color32(0xC9, 0xE6, 0xA5, 0xFF);
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(16f, fontSize - 10f);
            text.fontSizeMax = fontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            ApplyD2Coding(text);
        }

        private static void CreateBottomMenuButton(RectTransform parent, string name, string title, string subtitle, string innerPanelSpritePath, string iconSpritePath, Color glowTint, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var rootObj = new GameObject(name);
            rootObj.transform.SetParent(parent, false);

            var rootRect = rootObj.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = BottomButtonSize;

            var rootImage = rootObj.AddComponent<Image>();
            rootImage.color = new Color(1f, 1f, 1f, 0f);
            rootImage.raycastTarget = true;

            var button = rootObj.AddComponent<Button>();
            button.targetGraphic = rootImage;
            button.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(button.onClick, action);

            var visualRoot = CreateRect(rootRect, "VisualRoot", Vector2.zero, BottomButtonSize);
            var visualGroup = visualRoot.gameObject.AddComponent<CanvasGroup>();

            CreateLayerImage(visualRoot, "InnerPanel", innerPanelSpritePath, Vector2.zero, BottomPanelSize, Color.white);

            var hoverGlow = CreateLayerImage(visualRoot, "HoverGlow", BottomHoverGlowSpritePath, Vector2.zero, BottomPanelSize, glowTint);
            hoverGlow.gameObject.SetActive(false);

            CreateLayerImage(visualRoot, "MetalFrame", BottomMetalFrameSpritePath, Vector2.zero, BottomFrameSize, Color.white);

            var icon = CreateIconImage(visualRoot, "Icon", iconSpritePath, new Vector2(-178f, 0f), BottomIconSize);
            icon.raycastTarget = false;

            CreateDivider(visualRoot, "IconDivider", new Vector2(-118f, 0f), new Vector2(2f, 86f), new Color(0.62f, 0.9f, 0.86f, 0.24f));
            CreateBottomButtonText(visualRoot, "TitleText", title, new Vector2(78f, 16f), BottomTitleSize, 31f, new Color32(0xF1, 0xE5, 0xD2, 0xFF), FontStyles.Bold);
            CreateBottomButtonText(visualRoot, "SubtitleText", subtitle, new Vector2(78f, -25f), BottomSubtitleSize, 15f, new Color32(0xB6, 0xC5, 0xB8, 0xDD), FontStyles.Bold);

            var visual = rootObj.AddComponent<BottomMenuButtonVisual>();
            var so = new SerializedObject(visual);
            so.FindProperty("_visualRoot").objectReferenceValue = visualRoot;
            so.FindProperty("_hoverGlow").objectReferenceValue = hoverGlow.gameObject;
            so.FindProperty("_visualGroup").objectReferenceValue = visualGroup;
            so.FindProperty("_pressedScale").floatValue = 0.98f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform CreateRect(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image CreateLayerImage(RectTransform parent, string name, string spritePath, Vector2 position, Vector2 size, Color color)
        {
            var rect = CreateRect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = string.IsNullOrEmpty(spritePath) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateIconImage(RectTransform parent, string name, string spritePath, Vector2 position, Vector2 size)
        {
            var rect = CreateRect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateDivider(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var rect = CreateRect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void CreateBottomButtonText(RectTransform parent, string name, string textValue, Vector2 position, Vector2 size, float fontSize, Color32 color, FontStyles fontStyle)
        {
            var rect = CreateRect(parent, name, position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = textValue;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10f, fontSize - 8f);
            text.fontSizeMax = fontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            ApplyD2Coding(text);
        }

        private static void ApplyD2Coding(TextMeshProUGUI tmp)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset");
            if (font != null)
                tmp.font = font;
        }

        private static void EnsureTMPFontHolder()
        {
            var holder = Object.FindAnyObjectByType<TMPFontHolder>();
            if (holder == null)
            {
                var obj = new GameObject("TMPFontHolder");
                holder = obj.AddComponent<TMPFontHolder>();
            }

            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset");
            var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/D2CodingBold-Ver1.3.asset");

            if (regular == null)
            {
                Debug.LogWarning("[TitleLandingSetup] D2Coding-Ver1.3.asset not found. TMP runtime text will use TMP default font.");
                return;
            }

            var so = new SerializedObject(holder);
            so.FindProperty("_defaultFont").objectReferenceValue = regular;
            so.FindProperty("_boldFont").objectReferenceValue = bold != null ? bold : regular;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConnectTitleUI(TitleUI titleUI, Transform canvasTransform, RectTransform titleLandingPanel)
        {
            var so = new SerializedObject(titleUI);
            so.FindProperty("_titleLandingPanel").objectReferenceValue = titleLandingPanel.gameObject;
            so.FindProperty("_mainPanel").objectReferenceValue = FindDirect(canvasTransform, "MainPanel")?.gameObject;
            so.FindProperty("_upgradePanel").objectReferenceValue = FindDirect(canvasTransform, "UpgradePanel")?.gameObject;
            so.FindProperty("_optionsPanel").objectReferenceValue = FindDirect(canvasTransform, "OptionsPanel")?.gameObject;
            so.FindProperty("_hubPanel").objectReferenceValue = FindDirect(canvasTransform, "HubPanel")?.gameObject;
            so.FindProperty("_useHubForUpgrade").boolValue = true;

            var mainPanel = FindDirect(canvasTransform, "MainPanel");
            if (mainPanel != null)
            {
                so.FindProperty("_startButton").objectReferenceValue = mainPanel.Find("StartButton")?.GetComponent<Button>();
                so.FindProperty("_upgradeButton").objectReferenceValue = mainPanel.Find("UpgradeButton")?.GetComponent<Button>();
                so.FindProperty("_optionsButton").objectReferenceValue = mainPanel.Find("OptionsButton")?.GetComponent<Button>();
                so.FindProperty("_quitButton").objectReferenceValue = mainPanel.Find("QuitButton")?.GetComponent<Button>();
            }

            var currencyText = FindDeep(canvasTransform, "CurrencyText")?.GetComponent<TextMeshProUGUI>();
            if (currencyText != null)
                so.FindProperty("_currencyText").objectReferenceValue = currencyText;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInitialPanelState(Transform canvasTransform, RectTransform titleLandingPanel)
        {
            string[] panels = { "CurrencyText", "MainPanel", "UpgradePanel", "OptionsPanel", "HubPanel", "ResultOverlay" };
            foreach (var panelName in panels)
            {
                var panel = FindDirect(canvasTransform, panelName);
                if (panel != null)
                    panel.gameObject.SetActive(false);
            }

            titleLandingPanel.gameObject.SetActive(true);
            titleLandingPanel.SetAsLastSibling();
        }

        private static void EnsureInputSystemEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var obj = new GameObject("EventSystem");
                eventSystem = obj.AddComponent<EventSystem>();
            }

            var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
                Object.DestroyImmediate(legacyModule);

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        private static Transform FindDirect(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (t.name == name)
                    return t;
            }

            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

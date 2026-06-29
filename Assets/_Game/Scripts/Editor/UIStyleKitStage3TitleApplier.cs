#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// UI 3단계 Title 씬 리스킨 적용기.
    ///
    /// 기존 Title 씬의 버튼/패널/자원 표시 오브젝트를 찾아 1~2단계 공통 UI 키트 스프라이트를 적용한다.
    /// 동작 바인딩은 유지하고 Image/Button/TMP 시각 요소만 변경한다.
    ///
    /// 메뉴:
    /// Legacy applier. 실제 빌드 UI 작업에서는 메뉴에 노출하지 않는다.
    /// </summary>
    public static class UIStyleKitStage3TitleApplier
    {
        private const string CommonSpriteFolder = "Assets/_Game/Sprites/UI/Common";
        private const string IconFolder = "Assets/_Game/Sprites/UI/Icons";
        private const string FontPath = "Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset";

        private static readonly Color TextHi = Hex(0xE8, 0xE0, 0xD0, 0xFF);
        private static readonly Color TextLow = Hex(0xA8, 0xA0, 0x90, 0xFF);
        private static readonly Color Ore = Hex(0xFF, 0xC4, 0x4D, 0xFF);
        private static readonly Color Gem = Hex(0x78, 0xD8, 0xFF, 0xFF);
        private static readonly Color Credit = Hex(0xE7, 0xB9, 0x60, 0xFF);

        public static void ApplyToTitleScene()
        {
            if (!CheckResources()) return;

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Title UI 스타일 적용 실패", "현재 씬에 Canvas가 없습니다. Title 씬을 연 뒤 실행하세요.", "확인");
                return;
            }

            ApplyCanvasSettings(canvas);
            ApplyPanels(canvas.transform);
            ApplyCards(canvas.transform);
            ApplyButtons(canvas.transform);
            ApplyCurrencyDisplays(canvas.transform);
            ApplyTextDefaults(canvas.transform);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[UIStyleKitStage3] Title UI 스타일 적용 완료. 버튼/패널/자원 표시를 확인하세요.");
        }

        private static bool CheckResources()
        {
            string[] required =
            {
                $"{CommonSpriteFolder}/metal_button_normal.png",
                $"{CommonSpriteFolder}/metal_button_hover.png",
                $"{CommonSpriteFolder}/metal_button_pressed.png",
                $"{CommonSpriteFolder}/metal_button_disabled.png",
                $"{CommonSpriteFolder}/metal_panel.png",
                $"{CommonSpriteFolder}/metal_panel_small.png",
                $"{CommonSpriteFolder}/resource_slot.png",
                $"{IconFolder}/icon_ore.png",
                $"{IconFolder}/icon_gem.png",
                $"{IconFolder}/icon_upgrade.png",
                $"{IconFolder}/icon_settings.png",
                $"{IconFolder}/icon_exit.png",
                $"{IconFolder}/icon_crafting.png",
            };

            foreach (var path in required)
            {
                if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null) continue;

                EditorUtility.DisplayDialog(
                    "Title UI 스타일 적용 전 확인",
                    $"필수 UI 리소스를 찾을 수 없습니다.\n{path}\n\n1단계와 2단계 UI 키트 생성 메뉴를 먼저 실행하세요.",
                    "확인");
                return false;
            }

            return true;
        }

        private static void ApplyCanvasSettings(Canvas canvas)
        {
            canvas.vertexColorAlwaysGammaSpace = true;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static void ApplyPanels(Transform root)
        {
            ApplyFlatBackground(root, "HubPanel");
            ApplyFlatBackground(root, "MainPanel");
            ApplyFlatBackground(root, "UpgradePanel");
            ApplyFlatBackground(root, "OptionsPanel");
            ApplyPanel(root, "TopBar", "metal_panel_small", new Color(1f, 1f, 1f, 1f));
            ApplyPanel(root, "CharacterSelectSubPanel", "metal_panel_small", new Color(1f, 1f, 1f, 1f));
            ApplyPanel(root, "ExcavatorUpgradeSubPanel", "metal_panel_small", new Color(1f, 1f, 1f, 1f));
            ApplyPanel(root, "GemUpgradeSubPanel", "metal_panel_small", new Color(1f, 1f, 1f, 1f));
            ApplyPanel(root, "AbilityShopSubPanel", "metal_panel_small", new Color(1f, 1f, 1f, 1f));
            ApplyPanel(root, "WeaponShopSubPanel", "metal_panel_small", new Color(1f, 1f, 1f, 1f));
        }

        private static void ApplyCards(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (!t.name.EndsWith("Card")) continue;
                var image = t.GetComponent<Image>();
                if (image == null) image = t.gameObject.AddComponent<Image>();
                image.sprite = LoadCommon("card_frame");
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
        }

        private static void ApplyFlatBackground(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (t.name != name) continue;
                var image = t.GetComponent<Image>();
                if (image == null) image = t.gameObject.AddComponent<Image>();
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = Hex(0x0D, 0x0E, 0x1A, 0xFF);
                image.raycastTarget = true;
            }
        }

        private static void ApplyPanel(Transform root, string name, string spriteName, Color color)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (t.name != name) continue;
                var image = t.GetComponent<Image>();
                if (image == null) image = t.gameObject.AddComponent<Image>();
                image.sprite = LoadCommon(spriteName);
                image.type = Image.Type.Sliced;
                image.color = color;
                image.raycastTarget = image.raycastTarget && name != "TopBar";
            }
        }

        private static void ApplyButtons(Transform root)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(includeInactive: true))
            {
                if (IsUnder(button.transform, "TitleLandingPanel")) continue;

                StyleButton(button);
                ApplyKnownIcon(button);
            }
        }

        private static void StyleButton(Button button)
        {
            var image = button.GetComponent<Image>();
            if (image == null) image = button.gameObject.AddComponent<Image>();
            image.sprite = LoadCommon("metal_button_normal");
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;

            button.transition = Selectable.Transition.SpriteSwap;
            button.targetGraphic = image;
            button.spriteState = new SpriteState
            {
                highlightedSprite = LoadCommon("metal_button_hover"),
                pressedSprite = LoadCommon("metal_button_pressed"),
                selectedSprite = LoadCommon("metal_button_hover"),
                disabledSprite = LoadCommon("metal_button_disabled"),
            };

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            button.colors = colors;

            var le = button.GetComponent<LayoutElement>();
            if (le != null)
            {
                if (button.name == "OptionsButton" || button.name == "QuitButton") le.preferredWidth = Mathf.Max(le.preferredWidth, 92f);
                if (button.name == "StartButton") le.preferredWidth = Mathf.Max(le.preferredWidth, 160f);
            }

            foreach (var tmp in button.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
            {
                StyleTmp(tmp, TextHi, 0.16f, forceBold: true);
            }
        }

        private static void ApplyKnownIcon(Button button)
        {
            if (!ShouldUseButtonIcon(button))
            {
                RemoveButtonIcon(button.transform);
                return;
            }

            string iconName = button.name switch
            {
                "UpgradeButton" => "icon_upgrade",
                "MachineButton" => "icon_upgrade",
                "OptionsButton" => "icon_settings",
                "QuitButton" => "icon_exit",
                "StartButton" => "icon_crafting",
                "BackButton" => "icon_back",
                "ResetButton" => "icon_exit",
                _ => null
            };

            if (string.IsNullOrEmpty(iconName)) return;
            EnsureButtonIcon(button.transform, iconName);
        }

        private static bool ShouldUseButtonIcon(Button button)
        {
            var rt = button.GetComponent<RectTransform>();
            float width = rt != null ? rt.rect.width : 0f;
            if (width <= 0f) width = rt != null ? rt.sizeDelta.x : 0f;

            return button.name == "StartButton"
                   || button.name == "UpgradeButton"
                   || button.name == "MachineButton"
                   || button.name == "BackButton"
                   || width >= 130f;
        }

        private static void RemoveButtonIcon(Transform button)
        {
            var existing = button.Find("StyleIcon");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            foreach (var tmp in button.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
            {
                var rt = tmp.rectTransform;
                if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one)
                {
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private static void EnsureButtonIcon(Transform button, string iconName)
        {
            var existing = button.Find("StyleIcon");
            var iconRt = existing as RectTransform;
            if (iconRt == null)
            {
                var iconGo = new GameObject("StyleIcon");
                iconGo.transform.SetParent(button, false);
                iconRt = iconGo.AddComponent<RectTransform>();
                iconRt.SetAsFirstSibling();
            }

            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(14f, 0f);
            iconRt.sizeDelta = new Vector2(24f, 24f);

            var image = iconRt.GetComponent<Image>();
            if (image == null) image = iconRt.gameObject.AddComponent<Image>();
            image.sprite = LoadIcon(iconName);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;

            foreach (var tmp in button.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
            {
                if (tmp.transform == iconRt) continue;
                var rt = tmp.rectTransform;
                if (rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one)
                {
                    rt.offsetMin = new Vector2(38f, rt.offsetMin.y);
                    rt.offsetMax = new Vector2(-10f, rt.offsetMax.y);
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private static void ApplyCurrencyDisplays(Transform root)
        {
            StyleCurrencyDisplay(root, "OreDisplay", "icon_ore", Ore);
            StyleCurrencyDisplay(root, "GemDisplay", "icon_gem", Gem);

            var currencyText = FindDeep(root, "CurrencyText")?.GetComponent<TextMeshProUGUI>();
            if (currencyText != null)
            {
                StyleTmp(currencyText, Credit, 0.16f, forceBold: true);
            }
        }

        private static void StyleCurrencyDisplay(Transform root, string name, string iconName, Color valueColor)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (t.name != name) continue;

                var bg = t.GetComponent<Image>();
                if (bg == null) bg = t.gameObject.AddComponent<Image>();
                bg.sprite = LoadCommon("resource_slot");
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
                bg.raycastTarget = false;

                var icon = FindDirect(t, "Icon");
                if (icon == null)
                {
                    var iconGo = new GameObject("Icon");
                    iconGo.transform.SetParent(t, false);
                    icon = iconGo.transform;
                    icon.SetAsFirstSibling();
                }

                var iconRt = icon as RectTransform;
                if (iconRt == null) iconRt = icon.gameObject.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(10f, 0f);
                iconRt.sizeDelta = new Vector2(24f, 24f);

                var iconImage = icon.GetComponent<Image>();
                if (iconImage == null) iconImage = icon.gameObject.AddComponent<Image>();
                iconImage.sprite = LoadIcon(iconName);
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                foreach (var tmp in t.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
                {
                    var color = tmp.name.ToLowerInvariant().Contains("value") ? valueColor : TextLow;
                    StyleTmp(tmp, color, 0.12f, forceBold: true);
                }
            }
        }

        private static void ApplyTextDefaults(Transform root)
        {
            foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
            {
                if (tmp.color.a <= 0f) continue;
                bool largeText = tmp.fontSize >= 20f;
                StyleTmp(tmp, tmp.color, largeText ? 0.12f : 0f, forceBold: largeText);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (t.name == name) return t;
            }
            return null;
        }

        private static Transform FindDirect(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
            }
            return null;
        }

        private static bool IsUnder(Transform transform, string ancestorName)
        {
            var current = transform;
            while (current != null)
            {
                if (current.name == ancestorName) return true;
                current = current.parent;
            }

            return false;
        }

        private static Sprite LoadCommon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{CommonSpriteFolder}/{name}.png");
        }

        private static Sprite LoadIcon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{name}.png");
        }

        private static void ApplyD2Coding(TextMeshProUGUI tmp)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) tmp.font = font;
        }

        private static void StyleTmp(TextMeshProUGUI tmp, Color color, float outlineWidth, bool forceBold)
        {
            if (tmp == null) return;

            if (tmp.GetComponent<CanvasRenderer>() == null)
            {
                tmp.gameObject.AddComponent<CanvasRenderer>();
            }

            ApplyD2Coding(tmp);
            tmp.color = color;
            tmp.raycastTarget = false;
            if (forceBold) tmp.fontStyle |= FontStyles.Bold;

            if (outlineWidth <= 0f) return;

            try
            {
                tmp.outlineWidth = Mathf.Max(tmp.outlineWidth, outlineWidth);
                tmp.outlineColor = Color.black;
            }
            catch (MissingReferenceException)
            {
                Debug.LogWarning($"[UIStyleKitStage3] TMP outline 적용 생략: {GetPath(tmp.transform)}");
            }
            catch (UnassignedReferenceException)
            {
                Debug.LogWarning($"[UIStyleKitStage3] TMP CanvasRenderer 보정 실패로 outline 적용 생략: {GetPath(tmp.transform)}");
            }
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "(null)";
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        private static Color Hex(byte r, byte g, byte b, byte a) => new Color32(r, g, b, a);
    }
}
#endif

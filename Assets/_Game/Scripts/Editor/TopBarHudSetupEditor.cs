using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrillCorp.UI.HUD;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// Drill-Corp/HUD/Build TopBar 메뉴로 Game 씬 Canvas 하위에 상단바를 자동 생성한다.
    /// 스타일은 Title 씬 HubPanel TopBar와 통일.
    ///
    /// 흐름:
    /// 1. Canvas를 찾아 TopBarHud 오브젝트를 생성 (이미 있으면 재사용·덮어쓰기)
    /// 2. 슬롯 5개(체력/채굴/처치/광석/보석) + 나가기 버튼 생성
    /// 3. TopBarHud 컴포넌트 자동 바인딩
    /// 4. 기존 MachineStatusUI / CurrencyHud 발견 시 비활성화 안내 (삭제는 사용자 판단)
    /// </summary>
    public static class TopBarHudSetupEditor
    {
        const string TopBarName = "TopBarHud";

        // HubPanel과 동일한 색상 팔레트
        static readonly Color ColBg       = new Color32(0x1a, 0x1a, 0x30, 0xF5);
        static readonly Color ColBorder   = new Color32(0x2a, 0x2a, 0x45, 0xFF);
        static readonly Color ColDanger   = new Color32(0xff, 0x6b, 0x6b, 0xFF);
        static readonly Color ColAccent   = new Color32(0xf4, 0xa4, 0x23, 0xFF); // 주황 (채굴 강조)
        static readonly Color ColTextHi   = new Color32(0xee, 0xee, 0xee, 0xFF);
        static readonly Color ColTextLow  = new Color32(0xaa, 0xaa, 0xaa, 0xFF);
        static readonly Color ColOre      = new Color32(0xff, 0xd7, 0x00, 0xFF);
        static readonly Color ColGem      = new Color32(0x88, 0xdd, 0xff, 0xFF);

        [MenuItem("Drill-Corp/HUD/Build TopBar")]
        public static void BuildTopBar()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("TopBar 생성 실패", "현재 씬에 Canvas가 없습니다.", "확인");
                return;
            }

            EnsureEventSystem();

            // 기존 제거 후 재생성
            var existing = canvas.transform.Find(TopBarName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // 루트 — 상단 stretch
            var root = new GameObject(TopBarName);
            root.transform.SetParent(canvas.transform, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(0f, 64f);

            AddImage(root, ColBg);

            var hl = root.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(20, 20, 8, 8);
            hl.spacing = 24;
            hl.childAlignment = TextAnchor.MiddleLeft;
            // childControl=true + LayoutElement 로 자식 크기를 명시적으로 제어.
            // false로 두면 flexibleWidth(Spacer)가 무시돼 버튼이 우측으로 안 밀림.
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;

            // 슬롯 5개 — v2 원본: 체력/채굴/처치/광석/보석.
            var oreIcon = LoadUISprite("06_gold");
            var gemIcon = LoadUISprite("01_diamond");

            var healthText = CreateSlot(root.transform, "HealthSlot", "체력 100", ColTextHi, 140);
            var miningText = CreateSlot(root.transform, "MiningSlot", "0 / 100", ColAccent, 140);
            var killsText  = CreateSlot(root.transform, "KillsSlot",  "처치 0",   ColTextHi, 110);
            var oreText    = CreateIconSlot(root.transform, "OreSlot", "광석 0", ColOre, oreIcon, 160);
            var gemText    = CreateIconSlot(root.transform, "GemSlot", "보석 0", ColGem, gemIcon, 160);

            // 우측 spacer (나가기 버튼을 우측으로 밀기)
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(root.transform, false);
            spacer.AddComponent<RectTransform>().sizeDelta = new Vector2(10, 10);
            var spacerLe = spacer.AddComponent<LayoutElement>();
            spacerLe.flexibleWidth = 1;
            spacerLe.preferredWidth = 10;
            spacerLe.preferredHeight = 10;

            // 나가기 버튼
            var exitBtn = CreateButton(root.transform, "ExitButton", "나가기", ColDanger, 100);

            // TopBarHud 컴포넌트 + 자동 바인딩
            var hud = root.AddComponent<TopBarHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("_healthText").objectReferenceValue = healthText;
            so.FindProperty("_miningText").objectReferenceValue = miningText;
            so.FindProperty("_killsText").objectReferenceValue  = killsText;
            so.FindProperty("_oreText").objectReferenceValue    = oreText;
            so.FindProperty("_gemText").objectReferenceValue    = gemText;
            so.FindProperty("_exitButton").objectReferenceValue = exitBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 기존 중복 HUD 감지 → 비활성화
            DeactivateLegacy(canvas.transform, "MachineStatusUI");
            DeactivateLegacy(canvas.transform, "CurrencyHud");

            // 미니맵 y offset 조정 (TopBar 바로 아래로)
            MoveMinimap(canvas.transform);

            // 씬 변경 마킹
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Selection.activeGameObject = root;
            Debug.Log($"[TopBarHud] 생성 완료. 기존 MachineStatusUI / CurrencyHud는 비활성화됨. 필요하면 수동 삭제.");
        }

        // ───────────── 헬퍼 ─────────────
        // 아이콘 포함 슬롯 — 아이콘 + 텍스트 가로 배치. 광석/보석용.
        // 반환 TMP는 값 텍스트 참조 (TopBarHud._oreText/_gemText로 바인딩).
        static TextMeshProUGUI CreateIconSlot(Transform parent, string name, string initial, Color color, Sprite icon, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 40);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 40;
            le.flexibleWidth = 0;

            var hl = go.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 4;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;

            // 텍스트
            var textGo = new GameObject("Value");
            textGo.transform.SetParent(go.transform, false);
            textGo.AddComponent<RectTransform>();
            var textLe = textGo.AddComponent<LayoutElement>();
            textLe.preferredWidth = width - 32;
            textLe.preferredHeight = 40;
            textLe.flexibleWidth = 1;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = initial;
            tmp.fontSize = 20;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            ApplyD2Coding(tmp);

            // 아이콘 (있으면)
            if (icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                var iconRt = iconGo.AddComponent<RectTransform>();
                iconRt.sizeDelta = new Vector2(24, 24);
                var iconLe = iconGo.AddComponent<LayoutElement>();
                iconLe.preferredWidth = 24;
                iconLe.preferredHeight = 24;
                iconLe.flexibleWidth = 0;
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            return tmp;
        }

        static Sprite LoadUISprite(string fileName)
        {
            string path = $"Assets/_Game/Sprites/UI/{fileName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[TopBarHud] {path} 파일을 찾을 수 없습니다.");
                return null;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static TextMeshProUGUI CreateSlot(Transform parent, string name, string initial, Color color, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 40);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 40;
            le.flexibleWidth = 0;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = initial;
            tmp.fontSize = 20;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;  // 버튼 클릭 가로채지 않도록
            ApplyD2Coding(tmp);
            return tmp;
        }

        static Button CreateButton(Transform parent, string name, string label, Color bg, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 40);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 40;
            le.minWidth = width;
            le.flexibleWidth = 0;

            var img = go.AddComponent<Image>();
            img.color = bg;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor      = bg;
            cb.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
            cb.pressedColor     = Color.Lerp(bg, Color.black, 0.15f);
            cb.selectedColor    = bg;
            btn.colors = cb;
            btn.targetGraphic = img;

            // 라벨 — raycastTarget=false 로 부모 버튼이 클릭 받도록
            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 16;
            labelTmp.color = Color.white;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.raycastTarget = false;
            ApplyD2Coding(labelTmp);

            return btn;
        }

        static void AddImage(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;  // 상단바 배경은 클릭 이벤트 차단하지 않도록
        }

        // 씬에 EventSystem이 없으면 UI 버튼 클릭이 먹히지 않음 — 자동 생성.
        // 프로젝트는 New Input System을 쓰므로 InputSystemUIInputModule 사용.
        static void EnsureEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[TopBarHud] EventSystem이 없어 자동 생성했습니다.");
        }

        static void ApplyD2Coding(TextMeshProUGUI tmp)
        {
            const string path = "Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) tmp.font = font;
        }

        // 기존 HUD 비활성화 (경로 탐색 — Canvas/InGamePanel/... 또는 Canvas 직속)
        static void DeactivateLegacy(Transform canvasRoot, string targetName)
        {
            var all = canvasRoot.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var t in all)
            {
                if (t == null) continue;
                if (t.name == targetName && t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(false);
                    Debug.Log($"[TopBarHud] 기존 {targetName} 비활성화 (경로: {GetPath(t)})");
                }
            }
        }

        static string GetPath(Transform t)
        {
            var sb = new System.Text.StringBuilder(t.name);
            while (t.parent != null)
            {
                t = t.parent;
                sb.Insert(0, t.name + "/");
            }
            return sb.ToString();
        }

        // MiniMap y offset을 TopBar 높이 + 여유만큼 내림
        static void MoveMinimap(Transform canvasRoot)
        {
            var all = canvasRoot.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var t in all)
            {
                if (t == null || t.name != "MiniMap") continue;
                var rt = t as RectTransform;
                if (rt == null) continue;

                // 좌상단 anchor만 조정 대상 (다른 배치면 건드리지 않음)
                if (!Mathf.Approximately(rt.anchorMin.x, 0f) || !Mathf.Approximately(rt.anchorMin.y, 1f)) continue;

                var pos = rt.anchoredPosition;
                if (pos.y > -80f)  // 이미 내려져 있으면 skip
                {
                    rt.anchoredPosition = new Vector2(pos.x, -84f);  // TopBar 64 + 여유 20
                    Debug.Log($"[TopBarHud] MiniMap y offset {pos.y} → -84 로 조정");
                }
                break;
            }
        }
    }
}

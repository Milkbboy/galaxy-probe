using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrillCorp.UI;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// Drill-Corp/HUD/Build Result Panel 메뉴로 Game 씬 Canvas 하위에 v2.html 스타일 결과 팝업을 자동 생성.
    ///
    /// 흐름:
    /// 1. 기존 ResultPanel 있으면 제거 후 재생성
    /// 2. 레거시 SuccessPanel / FailedPanel 완전 삭제
    /// 3. 통합 ResultPanel (딤 배경 + 중앙 다이얼로그) 생성
    /// 4. SessionResultUI 컴포넌트 부착 + 자식 참조 자동 바인딩
    /// 5. 결과 아이콘(drillcorp_result_icons/256px) 자동 로드 후 바인딩
    /// </summary>
    public static class ResultPanelSetupEditor
    {
        const string PanelName = "ResultPanel";

        // v2 팔레트
        static readonly Color ColDim       = new Color(0f, 0f, 0f, 0.6f);
        static readonly Color ColBg        = new Color32(0x1a, 0x1a, 0x30, 0xFF);
        static readonly Color ColBorder    = new Color32(0x2a, 0x2a, 0x45, 0xFF);
        static readonly Color ColAccent    = new Color32(0x4a, 0x18, 0x90, 0xFF);
        static readonly Color ColBtnDim    = new Color32(0x1e, 0x1e, 0x35, 0xFF);
        static readonly Color ColTextHi    = new Color32(0xee, 0xee, 0xee, 0xFF);
        static readonly Color ColTextMid   = new Color32(0xaa, 0xaa, 0xaa, 0xFF);
        static readonly Color ColOre       = new Color32(0xff, 0xd7, 0x00, 0xFF);
        static readonly Color ColGem       = new Color32(0x88, 0xdd, 0xff, 0xFF);

        [MenuItem("Drill-Corp/HUD/Build Result Panel")]
        public static void BuildResultPanel()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("ResultPanel 생성 실패", "현재 씬에 Canvas가 없습니다.", "확인");
                return;
            }

            // 레거시 패널 완전 삭제 (사용자 요청)
            DeleteLegacy(canvas.transform, "SuccessPanel");
            DeleteLegacy(canvas.transform, "FailedPanel");

            // 기존 동일 이름 제거
            var existing = canvas.transform.Find(PanelName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // 딤 배경 (전체 화면)
            var root = new GameObject(PanelName);
            root.transform.SetParent(canvas.transform, false);
            var rRt = root.AddComponent<RectTransform>();
            rRt.anchorMin = Vector2.zero;
            rRt.anchorMax = Vector2.one;
            rRt.offsetMin = Vector2.zero;
            rRt.offsetMax = Vector2.zero;
            var dim = root.AddComponent<Image>();
            dim.color = ColDim;
            dim.raycastTarget = true;   // 뒤쪽 클릭 차단

            // 중앙 다이얼로그
            var dialog = new GameObject("Dialog");
            dialog.transform.SetParent(root.transform, false);
            var dRt = dialog.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0.5f);
            dRt.anchorMax = new Vector2(0.5f, 0.5f);
            dRt.pivot     = new Vector2(0.5f, 0.5f);
            dRt.sizeDelta = new Vector2(520, 460);
            var dBg = dialog.AddComponent<Image>();
            dBg.color = ColBg;

            var dVl = dialog.AddComponent<VerticalLayoutGroup>();
            dVl.padding = new RectOffset(32, 32, 28, 28);
            dVl.spacing = 16;
            dVl.childAlignment = TextAnchor.UpperCenter;
            dVl.childControlWidth = true;
            dVl.childControlHeight = false;
            dVl.childForceExpandWidth = true;

            // 아이콘
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(dialog.transform, false);
            iconGo.AddComponent<RectTransform>().sizeDelta = new Vector2(160, 160);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredHeight = 160;
            iconLe.preferredWidth  = 160;
            var icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // 타이틀
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(dialog.transform, false);
            titleGo.AddComponent<RectTransform>();
            titleGo.AddComponent<LayoutElement>().preferredHeight = 48;
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "채굴 완료!";
            titleTmp.fontSize = 32;
            titleTmp.color = ColOre;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.raycastTarget = false;
            ApplyD2Coding(titleTmp);

            // 부제
            var subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(dialog.transform, false);
            subGo.AddComponent<RectTransform>();
            subGo.AddComponent<LayoutElement>().preferredHeight = 26;
            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "목표 채굴량 100을 달성했습니다!";
            subTmp.fontSize = 16;
            subTmp.color = ColTextMid;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.raycastTarget = false;
            ApplyD2Coding(subTmp);

            // 보상 행 (광석 / 보석)
            var rewardRow = new GameObject("RewardRow");
            rewardRow.transform.SetParent(dialog.transform, false);
            rewardRow.AddComponent<RectTransform>();
            rewardRow.AddComponent<LayoutElement>().preferredHeight = 40;
            var rewardHl = rewardRow.AddComponent<HorizontalLayoutGroup>();
            rewardHl.spacing = 24;
            rewardHl.childAlignment = TextAnchor.MiddleCenter;
            rewardHl.childControlWidth = true;
            rewardHl.childControlHeight = true;
            rewardHl.childForceExpandWidth = false;

            // Hub TopBar와 동일한 스타일 — 「수치 + 아이콘」 배치
            var oreIconSprite = LoadCurrencyIcon("06_gold");
            var gemIconSprite = LoadCurrencyIcon("01_diamond");
            var oreText = CreateRewardRow(rewardRow.transform, "OreReward", "+ 0", ColOre, oreIconSprite);
            var gemText = CreateRewardRow(rewardRow.transform, "GemReward", "+ 0", ColGem, gemIconSprite);

            // 버튼 행
            var buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(dialog.transform, false);
            buttonRow.AddComponent<RectTransform>();
            buttonRow.AddComponent<LayoutElement>().preferredHeight = 52;
            var btnHl = buttonRow.AddComponent<HorizontalLayoutGroup>();
            btnHl.spacing = 12;
            btnHl.childAlignment = TextAnchor.MiddleCenter;
            btnHl.childControlWidth = true;
            btnHl.childControlHeight = true;
            btnHl.childForceExpandWidth = false;

            var upgradeBtn = CreateButton(buttonRow.transform, "UpgradeButton", "업그레이드 하기", ColAccent, 200);
            var retryBtn   = CreateButton(buttonRow.transform, "RetryButton",   "다시 도전",       ColBtnDim, 160);

            // 결과 아이콘 2종 로드
            var successIcon = LoadResultIcon("01_mining_success");
            var failureIcon = LoadResultIcon("02_mining_failure");

            // 결과 아이콘 참조 변수로 저장
            var titleIcon = icon;

            // 컴포넌트 부착 — 루트는 비활성이 되므로 Canvas에 컴포넌트를 두고 root를 _panel로 참조.
            // 기존 Canvas의 컨트롤러는 제거 후 재생성.
            var oldUi = canvas.GetComponent<SessionResultUI>();
            if (oldUi != null) Object.DestroyImmediate(oldUi);

            var ui = canvas.gameObject.AddComponent<SessionResultUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("_panel").objectReferenceValue         = root;
            so.FindProperty("_titleIcon").objectReferenceValue     = titleIcon;
            so.FindProperty("_titleText").objectReferenceValue     = titleTmp;
            so.FindProperty("_subtitleText").objectReferenceValue  = subTmp;
            so.FindProperty("_oreText").objectReferenceValue       = oreText;
            so.FindProperty("_gemText").objectReferenceValue       = gemText;
            so.FindProperty("_upgradeButton").objectReferenceValue = upgradeBtn;
            so.FindProperty("_retryButton").objectReferenceValue   = retryBtn;
            so.FindProperty("_successIcon").objectReferenceValue   = successIcon;
            so.FindProperty("_failureIcon").objectReferenceValue   = failureIcon;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 팝업은 초기에 비활성 (세션 종료 시 컨트롤러가 활성화).
            // 컨트롤러는 Canvas에 있으므로 활성 상태 유지 → 이벤트 수신 가능.
            root.SetActive(false);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("[ResultPanel] 생성 완료. SessionResultUI는 Canvas에 부착. 레거시 SuccessPanel/FailedPanel 삭제됨.");
        }

        // ───────────── 헬퍼 ─────────────
        // Hub TopBar의 CurrencyBadge와 동일 패턴 — 「수치 텍스트 + 아이콘」 가로 그룹.
        // 반환 TMP는 수치 텍스트 참조 (SessionResultUI._oreText/_gemText로 바인딩).
        static TextMeshProUGUI CreateRewardRow(Transform parent, string name, string initial, Color color, Sprite icon)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(180, 40);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredWidth = 180;
            rowLe.preferredHeight = 40;
            rowLe.minWidth = 140;

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8;
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;

            // 수치 텍스트
            var textGo = new GameObject("Value");
            textGo.transform.SetParent(row.transform, false);
            textGo.AddComponent<RectTransform>();
            var textLe = textGo.AddComponent<LayoutElement>();
            textLe.preferredWidth = 100;
            textLe.preferredHeight = 40;
            textLe.flexibleWidth = 0;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = initial;
            tmp.fontSize = 22;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineRight;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            ApplyD2Coding(tmp);

            // 아이콘 (Hub TopBar와 동일 크기 24x24, 보상 영역이니 조금 더 크게 32x32)
            if (icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(row.transform, false);
                iconGo.AddComponent<RectTransform>().sizeDelta = new Vector2(32, 32);
                var iconLe = iconGo.AddComponent<LayoutElement>();
                iconLe.preferredWidth = 32;
                iconLe.preferredHeight = 32;
                iconLe.flexibleWidth = 0;
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            return tmp;
        }

        static Sprite LoadCurrencyIcon(string fileName)
        {
            string path = $"Assets/_Game/Sprites/UI/{fileName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[ResultPanel] {path} 파일을 찾을 수 없습니다.");
                return null;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Button CreateButton(Transform parent, string name, string label, Color bg, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(width, 48);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 48;
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

        static void DeleteLegacy(Transform canvasRoot, string targetName)
        {
            var all = canvasRoot.GetComponentsInChildren<Transform>(includeInactive: true);
            int removed = 0;
            foreach (var t in all)
            {
                if (t == null) continue;
                if (t.name == targetName)
                {
                    Object.DestroyImmediate(t.gameObject);
                    removed++;
                }
            }
            if (removed > 0) Debug.Log($"[ResultPanel] 레거시 {targetName} {removed}개 삭제.");
        }

        static Sprite LoadResultIcon(string fileName)
        {
            string path = $"Assets/_Game/Sprites/UI/drillcorp_result_icons/256px/{fileName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[ResultPanel] {path} 파일을 찾을 수 없습니다.");
                return null;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void ApplyD2Coding(TextMeshProUGUI tmp)
        {
            const string path = "Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) tmp.font = font;
        }
    }
}

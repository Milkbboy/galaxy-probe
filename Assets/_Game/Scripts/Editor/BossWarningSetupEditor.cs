using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using DrillCorp.UI.HUD;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// Drill-Corp/HUD/Build Boss Warning 메뉴로 Game 씬 Canvas 하위에
    /// 보스 등장 경고 패널을 자동 생성·바인딩한다.
    ///
    /// 구조:
    ///   BossWarning (RectTransform stretch + CanvasGroup, 시작 alpha=0)
    ///     ├─ Background    (반투명 검정 + 보라 테두리, 가독성용)
    ///     ├─ TitleText     "보스 등장!"
    ///     └─ SubtitleText  "거미 보스 — 착지 시 새끼 소환"
    ///
    /// GameEvents.OnBossSpawned 구독 → 페이드 인/아웃 자동 처리.
    /// 이미 있으면 제거 후 재생성 (idempotent).
    /// </summary>
    public static class BossWarningSetupEditor
    {
        const string ROOT_NAME = "BossWarning";

        // CLAUDE.md 규약 — 코드 생성 TMP 는 D2Coding 폰트 적용.
        // 에디터 시점에는 TMPFontHolder 의 캐시가 비어있으므로 AssetDatabase 로 직접 로드.
        // ※ D2CodingBold 는 한글 글리프가 빠져있어(빌드 누락) 사용 안 함.
        //   Bold 가 필요한 경우 fontStyle = Bold 로 처리, font 는 일반본 그대로.
        const string FONT_PATH = "Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset";

        // v2 원본 보스 컬러 톤
        static readonly Color ColTitle      = new Color32(0xcc, 0x88, 0xff, 0xff); // 보라 #cc88ff
        static readonly Color ColSubtitle   = new Color32(0xff, 0xff, 0xff, 0xff);
        static readonly Color ColShadow     = new Color32(0x00, 0x00, 0x00, 0xc0);
        static readonly Color ColBackground = new Color32(0x10, 0x05, 0x20, 0xd8); // 검보라 알파 ~85%

        [MenuItem("Drill-Corp/HUD/Build Boss Warning")]
        public static void BuildBossWarning()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog(
                    "Boss Warning 생성 실패",
                    "현재 씬에 Canvas가 없습니다. Canvas가 있는 씬(보통 Game 씬)을 열고 다시 실행하세요.",
                    "확인");
                return;
            }

            // 기존 제거 → 재생성 (idempotent)
            var existing = canvas.transform.Find(ROOT_NAME);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // ─── Root ────────────────────────────────────────────
            var root = new GameObject(ROOT_NAME);
            root.transform.SetParent(canvas.transform, false);
            var rt = root.AddComponent<RectTransform>();
            // 화면 중앙 가로 stretch — 가독성 높이고 모든 해상도 대응
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 100f);   // 중앙보다 살짝 위
            rt.sizeDelta = new Vector2(0f, 200f);

            var cg = root.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;   // 게임 입력 가리지 않게

            // ─── Background (반투명 검보라 박스 + 보라 테두리) ──
            CreateBackground(root.transform);

            // ─── Title (큰 빨강/보라) ────────────────────────────
            var title = CreateText(root.transform, "TitleText", "보스 등장!",
                fontSize: 64,
                color: ColTitle,
                bold: true,
                yOffset: 35);

            // ─── Subtitle (작은 흰색) ────────────────────────────
            var subtitle = CreateText(root.transform, "SubtitleText", "거미 보스 — 착지 시 새끼 소환",
                fontSize: 28,
                color: ColSubtitle,
                bold: false,
                yOffset: -45);

            // ─── 컴포넌트 부착 + 바인딩 ──────────────────────────
            var ui = root.AddComponent<BossWarningUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("_container").objectReferenceValue = cg;
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_subtitleText").objectReferenceValue = subtitle;
            so.ApplyModifiedProperties();

            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[BossWarningSetup] Boss Warning UI 생성·바인딩 완료. Ctrl+S 로 씬 저장하세요.");
        }

        // 가독성용 반투명 박스 — 텍스트 뒤에 깔림.
        // 본체 + 보라 테두리(상하)를 분리해 v2.html 보스 페이즈 어두운 박스 톤 매칭.
        private static void CreateBackground(Transform parent)
        {
            // 메인 박스
            var bg = new GameObject("Background");
            bg.transform.SetParent(parent, false);
            var bgRt = bg.AddComponent<RectTransform>();
            // 부모(루트, 가로 stretch 200 높이) 안에서 가운데 박스로 채움.
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot     = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = new Vector2(720f, 180f);
            // 가장 뒤로 — 텍스트가 위에 오게
            bg.transform.SetSiblingIndex(0);

            var bgImg = bg.AddComponent<Image>();
            bgImg.color = ColBackground;
            bgImg.raycastTarget = false;

            // 상단 보라 테두리 라인
            CreateBorderLine(parent, "BorderTop",   yOffset: 90f);
            // 하단 보라 테두리 라인
            CreateBorderLine(parent, "BorderBottom", yOffset: -90f);
        }

        // 얇은 보라색 가로선 — 박스 위·아래에 배치.
        private static void CreateBorderLine(Transform parent, string name, float yOffset)
        {
            var line = new GameObject(name);
            line.transform.SetParent(parent, false);
            var rt = line.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, yOffset);
            rt.sizeDelta = new Vector2(720f, 2f);
            line.transform.SetSiblingIndex(1);

            var img = line.AddComponent<Image>();
            img.color = ColTitle;            // 보라 (#cc88ff)
            img.raycastTarget = false;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, string name, string text,
            int fontSize, Color color, bool bold, float yOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, yOffset);
            rt.sizeDelta = new Vector2(0f, fontSize * 1.6f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            // 검정 외곽선 — 어두운 배경에서도 또렷하게 보임
            tmp.outlineColor = ColShadow;
            tmp.outlineWidth = 0.2f;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            // 에디터 시점은 TMPFontHolder 미초기화 — AssetDatabase 로 D2Coding 직접 로드.
            // D2CodingBold 는 한글 누락이라 Bold 도 일반본 폰트 + fontStyle.Bold 로 처리.
            var d2Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (d2Font != null) tmp.font = d2Font;
            else Debug.LogWarning($"[BossWarningSetup] D2Coding 폰트 로드 실패: {FONT_PATH}");

            return tmp;
        }
    }
}

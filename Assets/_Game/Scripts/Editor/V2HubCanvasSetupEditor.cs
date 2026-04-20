// v2 Hub Canvas 생성 스크립트
// ─────────────────────────────────────────────────────────────
// Title 씬의 기존 Canvas에 v2 프로토타입 기반 HubPanel과 ResultOverlay를
// 한 번에 생성한다. UI 컴포넌트(C# 스크립트) 바인딩은 포함하지 않고,
// GameObject 계층 + RectTransform + 기본 이미지/텍스트 자리표시까지만.
//
// 전제:
// - Title.unity가 열려 있어야 함
// - Canvas GameObject가 이미 씬에 존재해야 함 (없으면 자동 생성)
//
// 참고 문서:
// - docs/V2_IntegrationPlan.md §3 (Title 씬 허브 패널 재구성)
// - docs/v2.html 10~247줄 (HTML 레이아웃 원본)
// ─────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace DrillCorp.Editor
{
    public static class V2HubCanvasSetupEditor
    {
        // ── v2.html 컬러 팔레트 ──
        static readonly Color ColBg         = new Color32(0x1a, 0x1a, 0x30, 0xFF);
        static readonly Color ColSubBg      = new Color32(0x12, 0x12, 0x2a, 0xFF);
        static readonly Color ColBorder     = new Color32(0x2a, 0x2a, 0x45, 0xFF);
        static readonly Color ColAccent     = new Color32(0x4a, 0x18, 0x90, 0xFF); // 보라
        static readonly Color ColVictor     = new Color32(0xf4, 0xa4, 0x23, 0xFF); // 주황
        static readonly Color ColSara       = new Color32(0x4f, 0xc3, 0xf7, 0xFF); // 하늘
        static readonly Color ColJinus      = new Color32(0x51, 0xcf, 0x66, 0xFF); // 초록
        static readonly Color ColOre        = new Color32(0xff, 0xd7, 0x00, 0xFF); // 금
        static readonly Color ColGem        = new Color32(0x88, 0xdd, 0xff, 0xFF); // 청
        static readonly Color ColDanger     = new Color32(0xff, 0x6b, 0x6b, 0xFF);
        static readonly Color ColOk         = new Color32(0x51, 0xcf, 0x66, 0xFF);
        static readonly Color ColTextHi     = new Color32(0xee, 0xee, 0xee, 0xFF);
        static readonly Color ColTextMid    = new Color32(0xaa, 0xaa, 0xaa, 0xFF);
        static readonly Color ColTextLow    = new Color32(0x66, 0x66, 0x77, 0xFF);

        const string HUB_PANEL_NAME      = "HubPanel";
        const string RESULT_OVERLAY_NAME = "ResultOverlay";

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/Title/3. v2 Hub Canvas 추가")]
        public static void BuildHubCanvas()
        {
            // TMP 한글 폰트(D2Coding) 먼저 보장
            EnsureTMPFontHolder();

            var canvas = FindOrCreateCanvas();
            if (canvas == null) return;

            // 이미 있으면 삭제 후 재생성 (안전)
            RemoveExisting(canvas.transform, HUB_PANEL_NAME);
            RemoveExisting(canvas.transform, RESULT_OVERLAY_NAME);

            var hub = CreateHubPanel(canvas.transform);
            var result = CreateResultOverlay(canvas.transform);

            // HubController 부착 + TitleUI에 자동 연결
            var controller = hub.AddComponent<DrillCorp.OutGame.HubController>();
            var titleUI = Object.FindAnyObjectByType<DrillCorp.OutGame.TitleUI>();
            if (titleUI != null)
            {
                var titleSo = new SerializedObject(titleUI);
                titleSo.FindProperty("_hubPanel").objectReferenceValue = hub;
                titleSo.ApplyModifiedPropertiesWithoutUndo();

                var ctrlSo = new SerializedObject(controller);
                ctrlSo.FindProperty("_titleUI").objectReferenceValue = titleUI;
                ctrlSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[V2HubCanvas] TitleUI를 찾을 수 없습니다. '2. Scene UI 설정'을 먼저 돌리세요.");
            }

            // CharacterSelectUI 부착 + 3카드·3SO 자동 연결
            AttachCharacterSelectUI(hub);

            // WeaponUpgradeManager GameObject (씬에 아직 없으면 생성)
            EnsureWeaponUpgradeManager();

            // CharacterRegistry GameObject (Game 씬에서 SelectedCharacterId → DefaultMachine 조회용)
            EnsureCharacterRegistry();

            // WeaponShopUI 부착
            AttachWeaponShopUI(hub);

            // AbilityShopUI 부착 + 9개 어빌리티 자동 연결
            AttachAbilityShopUI(hub);

            // 기존 UpgradeManager에 누락된 SO(특히 v2 신규 GemDrop/GemSpeed/MiningTarget) 동기화
            EnsureUpgradeManagerLinks();

            // ExcavatorUpgradeUI 부착 (UpgradeManager의 굴착기 관련 강화)
            AttachExcavatorUpgradeUI(hub);

            // StatDisplayUI 부착 (현재 스탯 실시간 합산)
            AttachStatDisplayUI(hub);

            // GemUpgradeUI 부착 (보석 채집 강화)
            AttachGemUpgradeUI(hub);

            // 초기 상태: 둘 다 비활성 (MainPanel이 열리도록)
            hub.SetActive(false);
            result.SetActive(false);

            Selection.activeGameObject = hub;
            EditorUtility.SetDirty(canvas);
            EditorSceneMarkDirty();

            Debug.Log("[V2HubCanvas] HubPanel + ResultOverlay + HubController 생성 완료. Ctrl+S로 씬을 저장하세요.");
            Debug.Log("[V2HubCanvas] MainPanel의 UPGRADE 버튼을 누르면 HubPanel이 열립니다.");
        }

        // ═════════════════════════════════════════════════════
        // TMPFontHolder (D2Coding 폰트 런타임 제공)
        // ═════════════════════════════════════════════════════
        static void EnsureTMPFontHolder()
        {
            var existing = Object.FindAnyObjectByType<DrillCorp.UI.TMPFontHolder>();
            if (existing != null)
            {
                LinkD2CodingFonts(existing);
                return;
            }

            var obj = new GameObject("TMPFontHolder");
            var holder = obj.AddComponent<DrillCorp.UI.TMPFontHolder>();
            LinkD2CodingFonts(holder);
            Debug.Log("[V2HubCanvas] TMPFontHolder 생성 + D2Coding 연결 완료.");
        }

        static void LinkD2CodingFonts(DrillCorp.UI.TMPFontHolder holder)
        {
            var regular = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset");
            var bold = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/TextMesh Pro/Fonts/D2CodingBold-Ver1.3.asset");

            if (regular == null)
            {
                Debug.LogWarning("[V2HubCanvas] D2Coding-Ver1.3.asset을 찾을 수 없습니다. Assets/TextMesh Pro/Fonts/ 확인.");
                return;
            }

            var so = new SerializedObject(holder);
            so.FindProperty("_defaultFont").objectReferenceValue = regular;
            if (bold != null) so.FindProperty("_boldFont").objectReferenceValue = bold;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ═════════════════════════════════════════════════════
        // Canvas / 루트 찾기
        // ═════════════════════════════════════════════════════
        static Canvas FindOrCreateCanvas()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) return canvas;

            Debug.LogWarning("[V2HubCanvas] Canvas를 찾을 수 없습니다. 기본 Canvas를 생성합니다.");
            var obj = new GameObject("Canvas");
            canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            obj.AddComponent<GraphicRaycaster>();
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            return canvas;
        }

        static void RemoveExisting(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        static void EditorSceneMarkDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        // ═════════════════════════════════════════════════════
        // HubPanel (허브 메인 화면)
        //   v2.html의 #outgame 영역에 대응
        // ═════════════════════════════════════════════════════
        static GameObject CreateHubPanel(Transform canvasRoot)
        {
            // 루트 — 풀스크린 이미지 배경
            var hub = CreatePanel(canvasRoot, HUB_PANEL_NAME);
            AddImage(hub, new Color32(0x0d, 0x0d, 0x1a, 0xFF)); // v2 body 배경

            // 세로 레이아웃 (TopBar / CharacterRow / BodyScroll)
            // childControlHeight = true + BodyScrollView의 flexibleHeight=1 →
            // TopBar·CharacterSelect는 LayoutElement preferredHeight로 고정,
            // BodyScrollView가 남는 세로 공간 다 차지.
            var hubVL = hub.AddComponent<VerticalLayoutGroup>();
            hubVL.padding = new RectOffset(24, 24, 24, 24);
            hubVL.spacing = 12;
            hubVL.childControlWidth = true;
            hubVL.childControlHeight = true;
            hubVL.childForceExpandWidth = true;
            hubVL.childForceExpandHeight = false;

            CreateTopBar(hub.transform);
            CreateCharacterSelectSubPanel(hub.transform);
            CreateBodyScrollArea(hub.transform);

            return hub;
        }

        // ── TopBar: 타이틀 + 재화 + 치트/리셋/시작 ──
        static void CreateTopBar(Transform parent)
        {
            var bar = CreateRow(parent, "TopBar", 80);
            AddImage(bar, ColBg);
            AddRoundedPadding(bar, 14, 14);

            var hl = bar.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 16;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = false;
            hl.childControlHeight = false;       // 자식 sizeDelta.y 유지 (34·40·70)
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;   // 버튼이 세로로 강제 확장되지 않음

            // 왼쪽: 타이틀 + 서브타이틀
            var titleGroup = CreateVGroup(bar.transform, "TitleGroup", 340, 70);
            CreateText(titleGroup.transform, "TitleText", "DRILL-CORP", 22, ColTextHi);
            var sub = CreateText(titleGroup.transform, "TargetLabel",
                "목표: <color=#f4a423>150</color> 채굴 · 보석 드랍 7% (1.6초 수집)",
                13, ColTextLow);
            sub.richText = true;

            // 가운데 spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(bar.transform, false);
            var spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.sizeDelta = new Vector2(10, 10);
            var sle = spacer.AddComponent<LayoutElement>();
            sle.flexibleWidth = 1;

            // 오른쪽: 재화·치트·리셋·시작
            // 광석/보석 아이콘은 Assets/_Game/Sprites/UI/06_gold·01_diamond에서 자동 로드.
            var oreIcon = LoadUISprite("06_gold");
            var gemIcon = LoadUISprite("01_diamond");
            CreateCurrencyBadge(bar.transform, "OreDisplay", "광석", "0", ColOre, oreIcon);
            CreateCurrencyBadge(bar.transform, "GemDisplay", "보석", "0", ColGem, gemIcon);
            CreateSmallButton(bar.transform, "CheatButton", "치트 +1000", ColOk, 110);
            CreateSmallButton(bar.transform, "ResetButton", "초기화", ColDanger, 90);
            CreateSmallButton(bar.transform, "OptionsButton", "옵션", ColBorder, 70, 14);
            CreateSmallButton(bar.transform, "QuitButton", "종료", ColBorder, 70, 14);
            CreateSmallButton(bar.transform, "StartButton", "채굴 시작", ColAccent, 150, 18);
        }

        static void CreateCurrencyBadge(Transform parent, string name, string label,
            string value, Color valueColor, Sprite icon = null)
        {
            var badge = new GameObject(name);
            badge.transform.SetParent(parent, false);
            var rt = badge.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 40);
            var badgeLE = badge.AddComponent<LayoutElement>();
            badgeLE.preferredWidth = 160;
            badgeLE.minWidth = 160;

            var hl = badge.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 6;
            hl.childAlignment = TextAnchor.MiddleRight;
            hl.childControlWidth = true;    // 자식 RectTransform 폭을 LayoutElement로 제어
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = true;

            // v2 원본: 「라벨 → 값 → 아이콘」 순서 (아이콘이 오른쪽 끝)
            var labelText = CreateText(badge.transform, "Label", label, 13, ColTextMid);
            labelText.alignment = TextAlignmentOptions.MidlineRight;
            var labelLE = labelText.gameObject.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 40;
            labelLE.minWidth = 30;

            var val = CreateText(badge.transform, "Value", value, 18, valueColor);
            val.fontStyle = FontStyles.Bold;
            val.alignment = TextAlignmentOptions.MidlineRight;
            var valLE = val.gameObject.AddComponent<LayoutElement>();
            valLE.preferredWidth = 80;
            valLE.minWidth = 60;

            if (icon != null)
            {
                var iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(badge.transform, false);
                iconObj.AddComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
                var iconLE = iconObj.AddComponent<LayoutElement>();
                iconLE.preferredWidth = 24;
                iconLE.minWidth = 24;
                iconLE.preferredHeight = 24;
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
            }
        }

        // ── 캐릭터 선택 (상단 전체 폭, 내용물 높이에 맞게 자동) ──
        static void CreateCharacterSelectSubPanel(Transform parent)
        {
            // 고정 높이(180)로 생성 — CSF 누적이 첫 프레임에 TopBar를 덮는 버그 방지.
            var sub = CreateSubPanel(parent, "CharacterSelectSubPanel", "캐릭터 선택", 180f);

            // 내부 3열 컨테이너 — 카드 높이에 맞춰 자동
            var content = sub.transform.Find("Content").gameObject;
            var hl = content.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12;
            hl.childAlignment = TextAnchor.UpperCenter;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = false;  // 카드 세로 확장 방지

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateCharacterCard(content.transform, "VictorCard", "빅터", "중장비 전문가",
                "네이팜·화염방사기·지뢰로 화력을 극대화", ColVictor);
            CreateCharacterCard(content.transform, "SaraCard", "사라", "방어 전문가",
                "블랙홀·충격파·메테오로 전장을 제어", ColSara);
            CreateCharacterCard(content.transform, "JinusCard", "지누스", "채굴 전문가",
                "드론 포탑·채굴 드론·거미 드론으로 자원 장악", ColJinus);
        }

        static void CreateCharacterCard(Transform parent, string name, string chrName,
            string title, string desc, Color color)
        {
            var card = new GameObject(name);
            card.transform.SetParent(parent, false);
            card.AddComponent<RectTransform>();
            AddImage(card, ColSubBg);

            var vl = card.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(14, 14, 12, 12);
            vl.spacing = 4;
            vl.childControlWidth = true;
            vl.childControlHeight = false;

            // 카드 높이를 내용물에 맞춤
            var fitter = card.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var nameT = CreateText(card.transform, "NameText", chrName, 20, color);
            nameT.fontStyle = FontStyles.Bold;
            CreateText(card.transform, "TitleText", title, 12, ColTextMid);
            CreateText(card.transform, "DescText", desc, 12, ColTextLow);

            // 하단 뱃지 (선택 상태 표시)
            var badge = CreateText(card.transform, "SelectBadge", "선택하기", 11, color);
            badge.alignment = TextAlignmentOptions.Left;
            badge.fontStyle = FontStyles.Bold;

            // 클릭용 Button
            var btn = card.AddComponent<Button>();
            var img = card.GetComponent<Image>();
            var cb = btn.colors;
            cb.highlightedColor = Color.Lerp(ColSubBg, color, 0.15f);
            cb.pressedColor = Color.Lerp(ColSubBg, color, 0.25f);
            btn.colors = cb;
            btn.targetGraphic = img;
        }

        // ── 본문 스크롤 영역 (Grid 3열 x 2행 서브패널) ──
        static void CreateBodyScrollArea(Transform parent)
        {
            var scrollObj = new GameObject("BodyScrollView");
            scrollObj.transform.SetParent(parent, false);
            var rt = scrollObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 560);
            var le = scrollObj.AddComponent<LayoutElement>();
            le.preferredHeight = 560;
            le.flexibleHeight = 1;

            var scroll = scrollObj.AddComponent<ScrollRect>();
            AddImage(scrollObj, new Color(0, 0, 0, 0));

            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            // Content — 3열 Masonry (HLG + 각 열 VLG)
            // 각 서브패널은 자체 ContentSizeFitter로 내용물 높이에 맞춰 자동.
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.sizeDelta = new Vector2(0, 0);

            var hlCols = content.AddComponent<HorizontalLayoutGroup>();
            hlCols.spacing = 12;
            hlCols.childControlWidth = true;
            hlCols.childControlHeight = true;
            hlCols.childForceExpandWidth = false;  // flexibleWidth 비율 적용
            hlCols.childForceExpandHeight = false; // 컬럼 각자 자기 preferredHeight 유지 (아래 CreateColumn에서 CSF 제거로 충돌 해소)
            hlCols.childAlignment = TextAnchor.UpperLeft;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRt;
            scroll.content = cRt;
            scroll.horizontal = false;
            scroll.vertical = true;

            // 3개 열 생성 — 동일 너비 (1:1:1)
            var col1 = CreateColumn(content.transform, "Column_Left",  flexibleWidth: 1);
            var col2 = CreateColumn(content.transform, "Column_Mid",   flexibleWidth: 1);
            var col3 = CreateColumn(content.transform, "Column_Right", flexibleWidth: 1);

            // 서브패널 분배 (v2 원본 배치 재현)
            CreateExcavatorSubPanel(col1.transform);
            CreateGemUpgradeSubPanel(col1.transform);

            CreateWeaponShopSubPanel(col2.transform);
            CreateStatDisplaySubPanel(col2.transform);

            CreateAbilityShopSubPanel(col3.transform);
        }

        static GameObject CreateColumn(Transform parent, string name, float flexibleWidth = 1f)
        {
            var col = new GameObject(name);
            col.transform.SetParent(parent, false);
            col.AddComponent<RectTransform>();

            var vl = col.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 12;
            vl.childControlWidth = true;
            vl.childControlHeight = true;    // 서브패널 preferredHeight를 LayoutElement 경유 즉시 조회 (CSF 딜레이 차단)
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            vl.childAlignment = TextAnchor.UpperCenter;

            // ContentSizeFitter 의도적으로 없음 — 상위 HLG(childControlHeight=true)가 컬럼 높이를
            // ILayoutElement 경유로 쿼리해 직접 관리. CSF 병행 시 sizeDelta 덮어쓰기 충돌로 컬럼
            // top 정렬이 깨지는 문제(weapon_00 참조) 방지.

            var le = col.AddComponent<LayoutElement>();
            le.flexibleWidth = flexibleWidth;
            le.minWidth = 200;
            // 자식 VLG/GridLayout의 preferredWidth가 상위 HLG로 누수되어 컬럼 균등 분배를
            // 깨뜨리는 문제 방지 — 0으로 명시해 flexibleWidth 비율이 그대로 반영되게 한다.
            le.preferredWidth = 0;

            return col;
        }

        // ── 5개 서브패널 (내용은 빈 Content 컨테이너만) ──
        static void CreateExcavatorSubPanel(Transform parent)
        {
            var sub = CreateSubPanel(parent, "ExcavatorUpgradeSubPanel", "굴착기 강화");
            AddVerticalItemContainer(sub.transform.Find("Content").gameObject);
        }

        static void CreateWeaponShopSubPanel(Transform parent)
        {
            var sub = CreateSubPanel(parent, "WeaponShopSubPanel", "무기 & 강화");
            var content = sub.transform.Find("Content").gameObject;

            // 행 단위 VLG. WeaponShopUI가 ceil(N/2) 행을 직접 만들어
            // 행마다 카드 2장(HLG, force expand width)을 배치 — v2 grid 1fr 1fr 재현.
            // childControlHeight=true: 자식 Row의 preferredHeight를 LayoutElement 경유로 즉시 조회
            //   (sizeDelta 캐스케이드 딜레이 차단). childForceExpandHeight=false라 강제 늘림은 없음.
            var vl = content.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 6;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            vl.childAlignment = TextAnchor.UpperLeft;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void CreateAbilityShopSubPanel(Transform parent)
        {
            var sub = CreateSubPanel(parent, "AbilityShopSubPanel", "고유 장비");
            AddVerticalItemContainer(sub.transform.Find("Content").gameObject);
        }

        static void CreateGemUpgradeSubPanel(Transform parent)
        {
            var sub = CreateSubPanel(parent, "GemUpgradeSubPanel", "보석 채집");
            AddVerticalItemContainer(sub.transform.Find("Content").gameObject);
        }

        static void CreateStatDisplaySubPanel(Transform parent)
        {
            var sub = CreateSubPanel(parent, "StatDisplaySubPanel", "현재 스탯");
            var content = sub.transform.Find("Content").gameObject;
            var vl = content.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 4;
            vl.childControlWidth = true;
            vl.childControlHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 샘플 스탯 행 — 실제는 런타임에 동적 생성
            string[] sampleStats = {
                "광석 / 0개",
                "보석 / 0개",
                "최대 체력 / 100 HP",
                "채굴 속도 / 초당 5",
                "해금 무기 / 저격총",
                "고유 장비 / 없음"
            };
            foreach (var s in sampleStats)
            {
                var parts = s.Split('/');
                CreateStatRow(content.transform, parts[0].Trim(), parts[1].Trim());
            }
        }

        static void CreateStatRow(Transform parent, string label, string value)
        {
            var row = new GameObject("Row_" + label);
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 22);
            row.AddComponent<LayoutElement>().preferredHeight = 22;
            AddImage(row, ColSubBg);

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(8, 8, 2, 2);
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;

            var lText = CreateText(row.transform, "Label", label, 12, ColTextMid);
            lText.alignment = TextAlignmentOptions.Left;
            var vText = CreateText(row.transform, "Value", value, 12, ColTextHi);
            vText.alignment = TextAlignmentOptions.Right;
        }

        // ═════════════════════════════════════════════════════
        // 결과 오버레이 (세션 종료 후 표시)
        //   v2.html의 #resultPanel에 대응
        // ═════════════════════════════════════════════════════
        static GameObject CreateResultOverlay(Transform canvasRoot)
        {
            var overlay = CreatePanel(canvasRoot, RESULT_OVERLAY_NAME);
            AddImage(overlay, new Color(0, 0, 0, 0.85f));

            // 중앙 카드
            var card = new GameObject("ResultCard");
            card.transform.SetParent(overlay.transform, false);
            var rt = card.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560, 420);
            AddImage(card, ColBg);

            var vl = card.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(24, 24, 24, 24);
            vl.spacing = 12;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.childAlignment = TextAnchor.UpperCenter;

            var title = CreateText(card.transform, "TitleText", "채굴 완료!", 32, ColOre);
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;

            var sub = CreateText(card.transform, "SubText", "목표 채굴량을 달성했습니다!", 16, ColTextMid);
            sub.alignment = TextAlignmentOptions.Center;

            // 보상 row
            var rewardRow = new GameObject("RewardRow");
            rewardRow.transform.SetParent(card.transform, false);
            rewardRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 80);
            rewardRow.AddComponent<LayoutElement>().preferredHeight = 80;
            var rl = rewardRow.AddComponent<HorizontalLayoutGroup>();
            rl.spacing = 40;
            rl.childAlignment = TextAnchor.MiddleCenter;
            rl.childControlWidth = false;
            rl.childControlHeight = true;

            var oreGain = CreateText(rewardRow.transform, "OreGainText", "광석 +0", 28, ColOre);
            oreGain.fontStyle = FontStyles.Bold;
            var gemGain = CreateText(rewardRow.transform, "GemGainText", "보석 +0", 28, ColGem);
            gemGain.fontStyle = FontStyles.Bold;

            // 버튼 row
            var btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(card.transform, false);
            btnRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 60);
            btnRow.AddComponent<LayoutElement>().preferredHeight = 60;
            var bl = btnRow.AddComponent<HorizontalLayoutGroup>();
            bl.spacing = 12;
            bl.childAlignment = TextAnchor.MiddleCenter;
            bl.childControlWidth = false;
            bl.childControlHeight = true;

            CreateBigButton(btnRow.transform, "UpgradeHubButton", "업그레이드 하기", ColAccent);
            CreateBigButton(btnRow.transform, "RetryButton", "다시 도전", ColBorder);

            return overlay;
        }

        // ═════════════════════════════════════════════════════
        // CharacterSelectUI 자동 연결
        // ═════════════════════════════════════════════════════
        static void AttachCharacterSelectUI(GameObject hub)
        {
            var subPanel = hub.transform.Find("CharacterSelectSubPanel");
            if (subPanel == null)
            {
                Debug.LogWarning("[V2HubCanvas] CharacterSelectSubPanel을 찾을 수 없습니다.");
                return;
            }

            var ui = subPanel.gameObject.AddComponent<DrillCorp.OutGame.CharacterSelectUI>();
            var so = new SerializedObject(ui);

            // 3카드 GameObject 연결
            var content = subPanel.Find("Content");
            var cardNames = new[] { "VictorCard", "SaraCard", "JinusCard" };
            var cardsProp = so.FindProperty("_cards");
            cardsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var card = content != null ? content.Find(cardNames[i]) : null;
                cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = card != null ? card.gameObject : null;
            }

            // 3 CharacterData SO 연결
            var dataProp = so.FindProperty("_characters");
            dataProp.arraySize = 3;
            dataProp.GetArrayElementAtIndex(0).objectReferenceValue = LoadCharacter("Character_Victor");
            dataProp.GetArrayElementAtIndex(1).objectReferenceValue = LoadCharacter("Character_Sara");
            dataProp.GetArrayElementAtIndex(2).objectReferenceValue = LoadCharacter("Character_Jinus");

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static DrillCorp.Data.CharacterData LoadCharacter(string fileName)
        {
            const string dir = "Assets/_Game/Data/Characters";
            var asset = AssetDatabase.LoadAssetAtPath<DrillCorp.Data.CharacterData>($"{dir}/{fileName}.asset");
            if (asset == null)
                Debug.LogWarning($"[V2HubCanvas] {fileName}.asset을 찾을 수 없습니다. '4. v2 Data Assets 생성'을 먼저 돌리세요.");
            return asset;
        }

        // ═════════════════════════════════════════════════════
        // CharacterRegistry GameObject + 3 캐릭터 SO 자동 연결
        // ═════════════════════════════════════════════════════
        static void EnsureCharacterRegistry()
        {
            var existing = Object.FindAnyObjectByType<DrillCorp.OutGame.CharacterRegistry>();
            if (existing == null)
            {
                var obj = new GameObject("CharacterRegistry");
                existing = obj.AddComponent<DrillCorp.OutGame.CharacterRegistry>();
            }

            var so = new SerializedObject(existing);
            var listProp = so.FindProperty("_characters");
            listProp.arraySize = 3;
            listProp.GetArrayElementAtIndex(0).objectReferenceValue = LoadCharacter("Character_Victor");
            listProp.GetArrayElementAtIndex(1).objectReferenceValue = LoadCharacter("Character_Sara");
            listProp.GetArrayElementAtIndex(2).objectReferenceValue = LoadCharacter("Character_Jinus");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ═════════════════════════════════════════════════════
        // WeaponUpgradeManager GameObject + 15개 SO 자동 연결
        // ═════════════════════════════════════════════════════
        static void EnsureWeaponUpgradeManager()
        {
            var existing = Object.FindAnyObjectByType<DrillCorp.OutGame.WeaponUpgradeManager>();
            if (existing != null)
            {
                // 이미 있으면 SO 리스트만 갱신
                LinkWeaponUpgrades(existing);
                return;
            }

            var obj = new GameObject("WeaponUpgradeManager");
            var mgr = obj.AddComponent<DrillCorp.OutGame.WeaponUpgradeManager>();
            LinkWeaponUpgrades(mgr);
        }

        static void LinkWeaponUpgrades(DrillCorp.OutGame.WeaponUpgradeManager mgr)
        {
            const string dir = "Assets/_Game/Data/WeaponUpgrades";
            string[] guids = AssetDatabase.FindAssets("t:WeaponUpgradeData", new[] { dir });

            var so = new SerializedObject(mgr);
            var listProp = so.FindProperty("_allUpgrades");
            listProp.arraySize = guids.Length;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<DrillCorp.Data.WeaponUpgradeData>(path);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = asset;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            if (guids.Length == 0)
                Debug.LogWarning("[V2HubCanvas] WeaponUpgrades 폴더가 비어있습니다. '4. v2 Data Assets 생성'을 먼저 돌리세요.");
        }

        // ═════════════════════════════════════════════════════
        // WeaponShopUI 부착 + 무기 아이콘 + 비용 아이콘 자동 바인딩
        // ═════════════════════════════════════════════════════
        static void AttachWeaponShopUI(GameObject hub)
        {
            var subPanel = FindDeep(hub.transform, "WeaponShopSubPanel");
            if (subPanel == null)
            {
                Debug.LogWarning("[V2HubCanvas] WeaponShopSubPanel을 찾을 수 없습니다.");
                return;
            }
            var ui = subPanel.gameObject.AddComponent<DrillCorp.OutGame.WeaponShopUI>();

            // _slots[*].Icon에 sprite 주입 — sniper/bomb/gun/laser는 wIcon0~3, saw는 없음(텍스트만).
            var slotIcons = new System.Collections.Generic.Dictionary<string, string>
            {
                { "sniper", "wIcon0" },
                { "bomb",   "wIcon1" },
                { "gun",    "wIcon2" },
                { "laser",  "wIcon3" },
            };

            var so = new SerializedObject(ui);
            var slotsProp = so.FindProperty("_slots");
            for (int i = 0; i < slotsProp.arraySize; i++)
            {
                var slot = slotsProp.GetArrayElementAtIndex(i);
                var idProp = slot.FindPropertyRelative("WeaponId");
                var iconProp = slot.FindPropertyRelative("Icon");
                if (idProp != null && iconProp != null
                    && slotIcons.TryGetValue(idProp.stringValue, out var iconName))
                {
                    iconProp.objectReferenceValue = LoadUISprite(iconName);
                }
            }

            // 비용 아이콘 (광석/보석)
            so.FindProperty("_oreIcon").objectReferenceValue = LoadUISprite("06_gold");
            so.FindProperty("_gemIcon").objectReferenceValue = LoadUISprite("01_diamond");

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 비용 아이콘만 주입하는 공통 헬퍼 (Excavator/Gem/Ability)
        static void InjectCostIcons(MonoBehaviour ui, bool ore = true, bool gem = true)
        {
            if (ui == null) return;
            var so = new SerializedObject(ui);
            if (ore)
            {
                var p = so.FindProperty("_oreIcon");
                if (p != null) p.objectReferenceValue = LoadUISprite("06_gold");
            }
            if (gem)
            {
                var p = so.FindProperty("_gemIcon");
                if (p != null) p.objectReferenceValue = LoadUISprite("01_diamond");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Assets/_Game/Sprites/UI/{name}.png Sprite 로드.
        // Texture Type이 Default로 임포트되어 있으면 Sprite로 전환 + 재임포트 후 재로드.
        static Sprite LoadUISprite(string fileName)
        {
            string path = $"Assets/_Game/Sprites/UI/{fileName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[V2HubCanvas] {path} 파일을 찾을 수 없습니다.");
                return null;
            }

            // Default → Sprite 자동 전환
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            Debug.Log($"[V2HubCanvas] {path} Texture Type을 Sprite로 자동 전환했습니다.");

            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[V2HubCanvas] {path} 재임포트 후에도 Sprite 로드 실패.");
            return sprite;
        }

        // ═════════════════════════════════════════════════════
        // AbilityShopUI 부착 + 9개 SO 자동 연결
        // ═════════════════════════════════════════════════════
        static void AttachAbilityShopUI(GameObject hub)
        {
            var subPanel = FindDeep(hub.transform, "AbilityShopSubPanel");
            if (subPanel == null)
            {
                Debug.LogWarning("[V2HubCanvas] AbilityShopSubPanel을 찾을 수 없습니다.");
                return;
            }
            var ui = subPanel.gameObject.AddComponent<DrillCorp.OutGame.AbilityShopUI>();

            const string dir = "Assets/_Game/Data/Abilities";
            string[] guids = AssetDatabase.FindAssets("t:AbilityData", new[] { dir });

            var so = new SerializedObject(ui);
            var listProp = so.FindProperty("_allAbilities");
            listProp.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<DrillCorp.Data.AbilityData>(path);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = asset;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            InjectCostIcons(ui, ore: false, gem: true);

            if (guids.Length == 0)
                Debug.LogWarning("[V2HubCanvas] Abilities 폴더가 비어있습니다. '4. v2 Data Assets 생성'을 먼저 돌리세요.");
        }

        // ═════════════════════════════════════════════════════
        // ExcavatorUpgradeUI 부착
        // ═════════════════════════════════════════════════════
        static void AttachExcavatorUpgradeUI(GameObject hub)
        {
            var subPanel = FindDeep(hub.transform, "ExcavatorUpgradeSubPanel");
            if (subPanel == null)
            {
                Debug.LogWarning("[V2HubCanvas] ExcavatorUpgradeSubPanel을 찾을 수 없습니다.");
                return;
            }
            var ui = subPanel.gameObject.AddComponent<DrillCorp.OutGame.ExcavatorUpgradeUI>();
            InjectCostIcons(ui, ore: true, gem: true);
        }

        // ═════════════════════════════════════════════════════
        // UpgradeManager 동기화 — 폴더 안 모든 SO를 _availableUpgrades에 연결
        // (씬에 이미 있는 인스턴스 사용, 없으면 새로 생성)
        // ═════════════════════════════════════════════════════
        static void EnsureUpgradeManagerLinks()
        {
            var existing = Object.FindAnyObjectByType<DrillCorp.OutGame.UpgradeManager>();
            if (existing == null)
            {
                var obj = new GameObject("UpgradeManager");
                existing = obj.AddComponent<DrillCorp.OutGame.UpgradeManager>();
            }

            const string dir = "Assets/_Game/Data/Upgrades";
            string[] guids = AssetDatabase.FindAssets("t:UpgradeData", new[] { dir });

            var so = new SerializedObject(existing);
            var listProp = so.FindProperty("_availableUpgrades");
            listProp.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<DrillCorp.Data.UpgradeData>(path);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = asset;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            if (guids.Length == 0)
                Debug.LogWarning("[V2HubCanvas] Upgrades 폴더가 비어있습니다.");
            else
                Debug.Log($"[V2HubCanvas] UpgradeManager에 {guids.Length}개 SO 연결됨.");
        }

        // ═════════════════════════════════════════════════════
        // GemUpgradeUI 부착
        // ═════════════════════════════════════════════════════
        static void AttachGemUpgradeUI(GameObject hub)
        {
            var subPanel = FindDeep(hub.transform, "GemUpgradeSubPanel");
            if (subPanel == null)
            {
                Debug.LogWarning("[V2HubCanvas] GemUpgradeSubPanel을 찾을 수 없습니다.");
                return;
            }
            var ui = subPanel.gameObject.AddComponent<DrillCorp.OutGame.GemUpgradeUI>();
            InjectCostIcons(ui, ore: true, gem: true);
        }

        // ═════════════════════════════════════════════════════
        // StatDisplayUI 부착 + 3개 캐릭터 SO 자동 연결
        // ═════════════════════════════════════════════════════
        static void AttachStatDisplayUI(GameObject hub)
        {
            var subPanel = FindDeep(hub.transform, "StatDisplaySubPanel");
            if (subPanel == null)
            {
                Debug.LogWarning("[V2HubCanvas] StatDisplaySubPanel을 찾을 수 없습니다.");
                return;
            }
            var ui = subPanel.gameObject.AddComponent<DrillCorp.OutGame.StatDisplayUI>();

            var so = new SerializedObject(ui);
            var arr = so.FindProperty("_characters");
            arr.arraySize = 3;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = LoadCharacter("Character_Victor");
            arr.GetArrayElementAtIndex(1).objectReferenceValue = LoadCharacter("Character_Sara");
            arr.GetArrayElementAtIndex(2).objectReferenceValue = LoadCharacter("Character_Jinus");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>자손 전체에서 이름으로 Transform 찾기 (비활성 포함).</summary>
        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        // ═════════════════════════════════════════════════════
        // 공통 유틸
        // ═════════════════════════════════════════════════════
        static GameObject CreatePanel(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return obj;
        }

        // 제목 헤더 + 내부 Content 컨테이너를 가진 서브패널
        // forcedHeight != null: 고정 높이 (캐릭터 선택 같은 상단 패널)
        // forcedHeight == null: 내용물 높이에 맞춰 자동 (ContentSizeFitter)
        static GameObject CreateSubPanel(Transform parent, string name, string headerLabel, float? forcedHeight = null)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            if (forcedHeight.HasValue)
            {
                rt.sizeDelta = new Vector2(0, forcedHeight.Value);
                panel.AddComponent<LayoutElement>().preferredHeight = forcedHeight.Value;
            }
            AddImage(panel, ColBg);

            var vl = panel.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(14, 14, 12, 14);
            vl.spacing = 8;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = true;
            // 명시적으로 false — Unity 기본값 true면 자식(Header/Content) flex를 1로 강제하고
            // 서브패널 자체가 flexibleHeight>0을 상위 Column VLG에 리포트해 surplus가
            // flex 분배로 계산됨 → CSF가 sizeDelta를 되돌리며 위치 간격이 벌어지는 현상 발생.
            vl.childForceExpandHeight = false;

            // forcedHeight 없으면 내용물에 맞춰 자동 크기
            if (!forcedHeight.HasValue)
            {
                var panelFitter = panel.AddComponent<ContentSizeFitter>();
                panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Header
            var header = CreateText(panel.transform, "Header", headerLabel.ToUpper(), 13, ColTextLow);
            header.characterSpacing = 1.5f;
            header.fontStyle = FontStyles.Bold;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;

            // Content 컨테이너 — 각 서브패널이 VLG/Grid + CSF 부착
            var content = new GameObject("Content");
            content.transform.SetParent(panel.transform, false);
            content.AddComponent<RectTransform>();

            return panel;
        }

        // 한 행 (가로 배치 컨테이너)
        static GameObject CreateRow(Transform parent, string name, float height)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            row.AddComponent<LayoutElement>().preferredHeight = height;
            row.AddComponent<HorizontalLayoutGroup>();
            return row;
        }

        static GameObject CreateVGroup(Transform parent, string name, float width, float height)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.AddComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            var le = g.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            var vl = g.AddComponent<VerticalLayoutGroup>();
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.spacing = 2;
            return g;
        }

        static Image AddImage(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static void AddRoundedPadding(GameObject go, int horizontal, int vertical)
        {
            var hl = go.GetComponent<HorizontalLayoutGroup>();
            if (hl != null) hl.padding = new RectOffset(horizontal, horizontal, vertical, vertical);
            var vl = go.GetComponent<VerticalLayoutGroup>();
            if (vl != null) vl.padding = new RectOffset(horizontal, horizontal, vertical, vertical);
        }

        static void AddVerticalItemContainer(GameObject content)
        {
            var vl = content.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 6;
            vl.childControlWidth = true;
            vl.childControlHeight = true;       // 자식 CSF preferredHeight 1프레임 내 반영
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;  // 자식이 자체 preferredHeight 유지

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // 텍스트 생성 — D2Coding 자동 적용
        static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            ApplyD2Coding(tmp);
            return tmp;
        }

        static void ApplyD2Coding(TextMeshProUGUI tmp)
        {
            const string path = "Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) tmp.font = font;
        }

        // 작은 버튼 (TopBar 우측 행)
        static GameObject CreateSmallButton(Transform parent, string name, string label,
            Color bgColor, float width, float fontSize = 13)
        {
            var btn = new GameObject(name);
            btn.transform.SetParent(parent, false);
            var rt = btn.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 34);
            btn.AddComponent<LayoutElement>().preferredWidth = width;

            var img = btn.AddComponent<Image>();
            img.color = bgColor;
            var button = btn.AddComponent<Button>();
            var cb = button.colors;
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.15f);
            cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.15f);
            button.colors = cb;
            button.targetGraphic = img;

            var t = CreateText(btn.transform, "Text", label, fontSize, Color.white);
            t.alignment = TextAlignmentOptions.Center;
            var tRt = t.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;

            return btn;
        }

        // 큰 버튼 (결과 오버레이 하단)
        static GameObject CreateBigButton(Transform parent, string name, string label, Color bgColor)
        {
            var btn = new GameObject(name);
            btn.transform.SetParent(parent, false);
            var rt = btn.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220, 52);
            btn.AddComponent<LayoutElement>().preferredWidth = 220;

            var img = btn.AddComponent<Image>();
            img.color = bgColor;
            var button = btn.AddComponent<Button>();
            button.targetGraphic = img;
            var cb = button.colors;
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.12f);
            cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.15f);
            button.colors = cb;

            var t = CreateText(btn.transform, "Text", label, 16, Color.white);
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            var tRt = t.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;

            return btn;
        }
    }
}

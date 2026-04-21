#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrillCorp.UI.HUD;

namespace DrillCorp.EditorTools
{
    /// <summary>
    /// Game 씬 우상단에 어빌리티 HUD(슬롯 3개)를 자동 생성한다.
    ///
    /// 레이아웃:
    ///   - Canvas 자식 "AbilityHud"  (우상단 anchor, top y = -84 = TopBar 64 + 여유 20)
    ///   - VerticalLayoutGroup 으로 슬롯 3개 세로 스택, 슬롯 간격 8
    ///   - 각 슬롯 = 박스 180×52
    ///       · Background (rgba 10,10,30,0.75)
    ///       · Border     (itemColor @ 0.53 — 런타임에서 색만 변경)
    ///       · Icon       (좌측, 40×40)
    ///       · NameLabel  ("[1] 네이팜")  — 우측 상단
    ///       · CooldownBar (BG + Fill, 우측 가운데)
    ///       · StatusLabel ("사용가능"/"3s") — 우측 하단
    ///
    /// 메뉴: Drill-Corp/HUD/Build Ability HUD
    ///
    /// 캐릭터 중립 — 어떤 캐릭터(빅터/Sara/Jinus)를 선택했든 슬롯 3개 박스만 만든다.
    /// 실제 표시 내용(이름/아이콘/색)은 런타임에 AbilityHud 가 ResolvedCharacter 에서 가져와 채움.
    /// </summary>
    public static class AbilityHudSetupEditor
    {
        const string HudName = "AbilityHud";

        // TopBarHud 와 동일 팔레트
        static readonly Color ColBg       = new Color32(0x0a, 0x0a, 0x1e, 0xBF); // rgba(10,10,30,0.75)
        static readonly Color ColBarBg    = new Color(1f, 1f, 1f, 0.10f);
        static readonly Color ColTextHi   = new Color32(0xee, 0xee, 0xee, 0xFF);
        static readonly Color ColStatus   = new Color(1f, 1f, 1f, 0.55f);

        const float SlotWidth   = 180f;
        const float SlotHeight  = 52f;
        const float TopOffset   = 84f;   // TopBar 64 + 여유 20
        const float SideMargin  = 12f;
        const float SlotSpacing = 8f;

        [MenuItem("Drill-Corp/HUD/Build Ability HUD")]
        public static void BuildAbilityHud()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("AbilityHud 생성 실패", "현재 씬에 Canvas가 없습니다.", "확인");
                return;
            }

            // 기존 제거 후 재생성
            var existing = canvas.transform.Find(HudName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // 루트 — 우상단 anchor, 세로 스택 컨테이너
            var root = new GameObject(HudName, typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-SideMargin, -TopOffset);
            rt.sizeDelta = new Vector2(SlotWidth, (SlotHeight + SlotSpacing) * 3 - SlotSpacing);

            var vl = root.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(0, 0, 0, 0);
            vl.spacing = SlotSpacing;
            vl.childAlignment = TextAnchor.UpperRight;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = false;
            vl.childForceExpandHeight = false;

            var slots = new AbilitySlotUI[3];
            for (int i = 0; i < 3; i++)
                slots[i] = CreateSlot(root.transform, $"Slot_{i + 1}");

            // AbilityHud 컴포넌트 + 슬롯 배열 바인딩
            var hud = root.AddComponent<AbilityHud>();
            var so = new SerializedObject(hud);
            var slotsProp = so.FindProperty("_slots");
            slotsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("[AbilityHudSetup] 우상단 AbilityHud 생성 완료. 캐릭터 이름은 TopBarHud 좌측에서 표시.");
        }

        // ───────────── 헬퍼 ─────────────

        static AbilitySlotUI CreateSlot(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(SlotWidth, SlotHeight);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = SlotWidth;
            le.preferredHeight = SlotHeight;
            le.flexibleWidth   = 0;
            le.flexibleHeight  = 0;

            // Background (꽉 채움)
            var bg = AddChildImage(go.transform, "Background", ColBg, stretch: true);

            // Border (배경 위에 같은 영역, 테두리 sprite 가 따로 없으므로 컬러만 — 시각 구분용 outline 대용)
            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(go.transform, false);
            var borderRt = (RectTransform)borderGo.transform;
            StretchFull(borderRt);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(1f, 0.42f, 0.21f, 0.53f); // 런타임에서 _themeColor 로 갱신
            borderImg.raycastTarget = false;
            // 단색 fill 대신 외곽선 느낌 — Outline 컴포넌트를 흉내내려면 Image+Sliced 가 필요하지만,
            // 빌트인 sprite 가 없는 상태이므로 일단 단색 半투명. 추후 9-slice sprite 추가 시 교체.
            // 단색이라 배경과 합쳐져 강조색이 살짝 입혀진다.
            borderImg.color = new Color(borderImg.color.r, borderImg.color.g, borderImg.color.b, 0.20f);

            // Icon — 좌측
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot     = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(6f, 0f);
            iconRt.sizeDelta = new Vector2(40f, 40f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;

            // NameLabel — 우측 상단
            var nameGo = new GameObject("NameLabel", typeof(RectTransform));
            nameGo.transform.SetParent(go.transform, false);
            var nameRt = (RectTransform)nameGo.transform;
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot     = new Vector2(0f, 1f);
            nameRt.anchoredPosition = new Vector2(52f, -4f);
            nameRt.sizeDelta = new Vector2(-58f, 18f);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "[1] —";
            nameTmp.fontSize = 14;
            nameTmp.color = ColTextHi;
            nameTmp.alignment = TextAlignmentOptions.TopLeft;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.raycastTarget = false;
            ApplyD2Coding(nameTmp);

            // CooldownBar BG — 가운데
            var barBgGo = new GameObject("CooldownBarBg", typeof(RectTransform));
            barBgGo.transform.SetParent(go.transform, false);
            var barBgRt = (RectTransform)barBgGo.transform;
            barBgRt.anchorMin = new Vector2(0f, 0.5f);
            barBgRt.anchorMax = new Vector2(1f, 0.5f);
            barBgRt.pivot     = new Vector2(0f, 0.5f);
            barBgRt.anchoredPosition = new Vector2(52f, -7f);
            barBgRt.sizeDelta = new Vector2(-58f, 6f);
            // 무기 슬롯과 동일한 쿨바 sprite 사용 (Square_White).
            // Image.Type=Filled 는 sprite 가 비어있으면 fillAmount 가 적용되지 않음.
            var barSprite = LoadSquareWhiteSprite();

            var barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.sprite = barSprite;
            barBgImg.color  = new Color(0f, 0f, 0f, 0.8f); // 무기 슬롯과 동일 (검정 80%)
            barBgImg.raycastTarget = false;

            // CooldownBar Fill — BG 위 같은 영역, Image.Type=Filled
            var barFillGo = new GameObject("CooldownBarFill", typeof(RectTransform));
            barFillGo.transform.SetParent(barBgGo.transform, false);
            var barFillRt = (RectTransform)barFillGo.transform;
            StretchFull(barFillRt);
            var barFillImg = barFillGo.AddComponent<Image>();
            barFillImg.sprite = barSprite;
            barFillImg.color = new Color(0x51 / 255f, 0xcf / 255f, 0x66 / 255f, 1f); // ReadyGreen 초기값
            barFillImg.type = Image.Type.Filled;
            barFillImg.fillMethod = Image.FillMethod.Horizontal;
            barFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFillImg.fillAmount = 1f;
            barFillImg.raycastTarget = false;

            // StatusLabel — 우측 하단
            var statusGo = new GameObject("StatusLabel", typeof(RectTransform));
            statusGo.transform.SetParent(go.transform, false);
            var statusRt = (RectTransform)statusGo.transform;
            statusRt.anchorMin = new Vector2(0f, 0f);
            statusRt.anchorMax = new Vector2(1f, 0f);
            statusRt.pivot     = new Vector2(1f, 0f);
            statusRt.anchoredPosition = new Vector2(-6f, 2f);
            statusRt.sizeDelta = new Vector2(-58f, 14f);
            var statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
            statusTmp.text = "사용가능";
            statusTmp.fontSize = 11;
            statusTmp.color = ColStatus;
            statusTmp.alignment = TextAlignmentOptions.BottomRight;
            statusTmp.raycastTarget = false;
            ApplyD2Coding(statusTmp);

            // AbilitySlotUI 컴포넌트 + 자식 자동 바인딩
            var slot = go.AddComponent<AbilitySlotUI>();
            var so = new SerializedObject(slot);
            so.FindProperty("_background").objectReferenceValue      = bg;
            so.FindProperty("_border").objectReferenceValue          = borderImg;
            so.FindProperty("_iconImage").objectReferenceValue       = iconImg;
            so.FindProperty("_cooldownBarBg").objectReferenceValue   = barBgImg;
            so.FindProperty("_cooldownBarFill").objectReferenceValue = barFillImg;
            so.FindProperty("_nameLabel").objectReferenceValue       = nameTmp;
            so.FindProperty("_statusLabel").objectReferenceValue     = statusTmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            return slot;
        }

        static Image AddChildImage(Transform parent, string name, Color color, bool stretch)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            if (stretch) StretchFull(rt);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // 무기 슬롯(WeaponSlotUI)과 동일한 흰색 사각형 sprite — 쿨바 시각 통일.
        // Filled 타입 Image 는 sprite 가 비어있으면 fillAmount 가 적용 안 됨.
        static Sprite LoadSquareWhiteSprite()
        {
            const string path = "Assets/_Game/Sprites/UI/Square_White.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[AbilityHudSetup] {path} 를 찾을 수 없습니다. 쿨바 fillAmount 가 보이지 않을 수 있습니다.");
            return sprite;
        }

        static void ApplyD2Coding(TextMeshProUGUI tmp)
        {
            const string path = "Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) tmp.font = font;
        }
    }
}
#endif

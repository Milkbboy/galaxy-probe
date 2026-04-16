using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Weapon;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DrillCorp.UI.Weapon
{
    /// <summary>
    /// 좌측 무기 패널의 슬롯 1개 (저격총/폭탄/기관총/레이저).
    /// 바인딩된 WeaponBase 상태(쿨다운·타겟·해금)를 매 프레임 UI에 반영한다.
    ///
    /// 상태 텍스트 규칙 (_.html 프로토타입 기준):
    ///   - 잠김             → "-"
    ///   - 쿨 중            → "{남은초}s"
    ///   - 준비 + 타겟      → "발사!"
    ///   - 준비 + 타겟 없음 → "대기"
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class WeaponSlotUI : MonoBehaviour
    {
        [Header("Binding")]
        [Tooltip("이 슬롯이 표시할 무기. WeaponPanelUI가 런타임에 주입하거나 인스펙터에서 지정")]
        [SerializeField] private WeaponBase _weapon;

        [Header("UI References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _stateText;

        [Tooltip("쿨다운 진행 바 (Image Type: Filled, Horizontal)")]
        [SerializeField] private Image _coolBarFill;

        [Tooltip("슬롯 외곽 테두리 (준비+타겟 시 테마색 강조용, 선택)")]
        [SerializeField] private Image _border;

        [Tooltip("잠김 상태에서 슬롯을 덮는 오버레이 (선택)")]
        [SerializeField] private GameObject _lockedOverlay;

        [Tooltip("쿨다운 오버레이 (검은 반투명 + 큰 초 표시, 선택). _weapon.ShowOverlay=true일 때만 활성")]
        [SerializeField] private GameObject _coolOverlay;

        [Tooltip("쿨다운 오버레이 중앙 큰 텍스트 (남은 쿨타임)")]
        [SerializeField] private TMP_Text _overlayText;

        [Header("Style")]
        [Tooltip("잠김 슬롯 아이콘·바·테두리 틴트")]
        [SerializeField] private Color _lockedTint = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Text")]
        [SerializeField] private string _level = "Lv.1";

        private bool _isLocked;

        private void Start()
        {
            RefreshStatic();
        }

        /// <summary>
        /// 이 슬롯이 표시할 무기를 교체한다 (WeaponPanelUI가 Start에서 호출).
        /// </summary>
        public void SetWeapon(WeaponBase weapon)
        {
            _weapon = weapon;
            RefreshStatic();
        }

        /// <summary>아이콘/이름/레벨 같은 변하지 않는 필드 갱신.</summary>
        private void RefreshStatic()
        {
            _isLocked = _weapon == null;

            if (_lockedOverlay != null) _lockedOverlay.SetActive(_isLocked);

            if (_isLocked)
            {
                if (_iconImage != null) _iconImage.color = _lockedTint;
                if (_nameText != null) _nameText.text = "-";
                if (_levelText != null) _levelText.text = "잠김";
                if (_stateText != null) _stateText.text = "-";
                if (_coolBarFill != null)
                {
                    _coolBarFill.fillAmount = 1f;
                    _coolBarFill.color = _lockedTint;
                }
                if (_border != null) _border.color = WeaponBase.IdleBorderColor;
                if (_coolOverlay != null) _coolOverlay.SetActive(false);
                return;
            }

            if (_iconImage != null)
            {
                if (_weapon.Icon != null) _iconImage.sprite = _weapon.Icon;
                _iconImage.color = Color.white;
            }
            if (_nameText != null) _nameText.text = _weapon.DisplayName;
            if (_levelText != null) _levelText.text = _level;
        }

        private void Update()
        {
            if (_isLocked || _weapon == null) return;

            // WeaponBase의 슬롯 표현 프로퍼티만 읽는다 (§4.6.3).
            // 무기별 분기는 WeaponBase 파생이 담당 → 슬롯은 단순 렌더러.
            if (_stateText != null) _stateText.text = _weapon.StateText;
            if (_coolBarFill != null)
            {
                _coolBarFill.fillAmount = _weapon.BarFillAmount;
                _coolBarFill.color = _weapon.BarColor;
            }
            if (_border != null) _border.color = _weapon.BorderColor;

            // 쿨 오버레이 (Phase 2 폭탄 / Phase 3 리로딩 등)
            if (_coolOverlay != null)
            {
                bool show = _weapon.ShowOverlay;
                if (_coolOverlay.activeSelf != show) _coolOverlay.SetActive(show);
                if (show && _overlayText != null) _overlayText.text = _weapon.OverlayText;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 슬롯 내부 자식 (Border/Background/Icon/Name/Level/State/CoolBar)을 표준 레이아웃으로 재생성.
        /// 컴포넌트 헤더 ⋮ 메뉴 → "Build Default Hierarchy" 로 호출.
        /// 기존 자식은 전부 삭제되고 WeaponSlotUI 필드도 재연결된다.
        /// </summary>
        [ContextMenu("Build Default Hierarchy")]
        private void BuildDefaultHierarchy()
        {
            var rt = transform as RectTransform;
            if (rt == null)
            {
                Debug.LogError("[WeaponSlotUI] RectTransform이 필요합니다");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(gameObject, "Build WeaponSlot Hierarchy");

            // 기존 자식 제거
            for (int i = rt.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(rt.GetChild(i).gameObject);
            }

            // 슬롯 높이 (VerticalLayoutGroup이 Width 제어하므로 x는 건드리지 않음)
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 90f);

            var font = LoadD2CodingFont();

            // 1) Border (뒤, 2px 프레임)
            var border = CreateUIImage(rt, "Border");
            SetStretch(border.rectTransform, Vector2.zero, Vector2.zero);
            border.color = WeaponBase.IdleBorderColor;
            border.raycastTarget = false;
            _border = border;

            // 2) Background (Border 안쪽 2px 인셋 → Border가 테두리처럼 보임)
            var bg = CreateUIImage(rt, "Background");
            SetStretch(bg.rectTransform, new Vector2(2, 2), new Vector2(-2, -2));
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            bg.raycastTarget = false;

            // 3) Icon (상단 중앙)
            var icon = CreateUIImage(rt, "Icon");
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -6f);
            iconRt.sizeDelta = new Vector2(32f, 32f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (_weapon != null && _weapon.Icon != null) icon.sprite = _weapon.Icon;
            _iconImage = icon;

            // 4) Name / Level / State
            _nameText = CreateTMP(rt, "Name",
                _weapon != null ? _weapon.DisplayName : "이름", 11f,
                new Vector2(0, -42f), new Vector2(-4f, 14f), Color.white, font);
            _levelText = CreateTMP(rt, "Level", _level, 9f,
                new Vector2(0, -56f), new Vector2(-4f, 12f), new Color(0.65f, 0.65f, 0.65f), font);
            _stateText = CreateTMP(rt, "State", "대기", 10f,
                new Vector2(0, -70f), new Vector2(-4f, 12f), Color.white, font);

            // 5) CoolBar (하단 중앙)
            // Filled 모드 Image는 Sprite 없으면 fillAmount가 적용 안됨 → Square_White 사용
            var whiteSprite = LoadSquareWhiteSprite();

            var coolBg = CreateUIImage(rt, "CoolBarBg");
            var coolBgRt = coolBg.rectTransform;
            coolBgRt.anchorMin = new Vector2(0.5f, 0f);
            coolBgRt.anchorMax = new Vector2(0.5f, 0f);
            coolBgRt.pivot = new Vector2(0.5f, 0f);
            coolBgRt.anchoredPosition = new Vector2(0f, 4f);
            coolBgRt.sizeDelta = new Vector2(80f, 4f);
            coolBg.color = new Color(0f, 0f, 0f, 0.8f);
            coolBg.sprite = whiteSprite;
            coolBg.raycastTarget = false;

            var coolFill = CreateUIImage(coolBg.transform, "CoolBarFill");
            SetStretch(coolFill.rectTransform, Vector2.zero, Vector2.zero);
            coolFill.sprite = whiteSprite;
            coolFill.color = WeaponBase.ReadyBarColor;
            coolFill.type = Image.Type.Filled;
            coolFill.fillMethod = Image.FillMethod.Horizontal;
            coolFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            coolFill.fillAmount = 1f;
            coolFill.raycastTarget = false;
            _coolBarFill = coolFill;

            // 6) CoolOverlay (검은 반투명 + 큰 초 텍스트) — 폭탄/리로딩 등 ShowOverlay=true일 때만 활성
            var overlay = CreateUIImage(rt, "CoolOverlay");
            SetStretch(overlay.rectTransform, new Vector2(2, 2), new Vector2(-2, -2));
            overlay.color = new Color(0f, 0f, 0f, 0.65f);
            overlay.sprite = whiteSprite;
            overlay.raycastTarget = false;
            overlay.gameObject.SetActive(false);
            _coolOverlay = overlay.gameObject;

            // 오버레이 중앙 큰 텍스트 (Stretch + 중앙 정렬)
            var overlayTextGo = new GameObject("OverlayText", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(overlayTextGo, "Create OverlayText");
            overlayTextGo.transform.SetParent(overlay.transform, false);
            var overlayRt = overlayTextGo.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.pivot = new Vector2(0.5f, 0.5f);
            overlayRt.anchoredPosition = Vector2.zero;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            var overlayTmp = overlayTextGo.AddComponent<TextMeshProUGUI>();
            overlayTmp.text = "5.0s";
            overlayTmp.fontSize = 18f;
            overlayTmp.alignment = TextAlignmentOptions.Center;
            overlayTmp.color = Color.white;
            overlayTmp.fontStyle = FontStyles.Bold;
            overlayTmp.raycastTarget = false;
            overlayTmp.textWrappingMode = TextWrappingModes.NoWrap;
            overlayTmp.overflowMode = TextOverflowModes.Overflow;
            if (font != null) overlayTmp.font = font;
            _overlayText = overlayTmp;

            EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);

            Debug.Log($"[WeaponSlotUI] '{name}' 기본 계층 생성 완료");
        }

        private static Image CreateUIImage(Transform parent, string goName)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Create " + goName);
            go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }

        private static void SetStretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        /// <summary>좌우 Stretch + 고정 높이 TMP 라벨 생성 (헤더 기준 top-anchor).</summary>
        private static TMP_Text CreateTMP(Transform parent, string goName, string text, float fontSize,
            Vector2 anchoredPos, Vector2 sizeDelta, Color color, TMP_FontAsset font)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + goName);
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            if (font != null) tmp.font = font;
            return tmp;
        }

        /// <summary>D2Coding 폰트 로드. 실패 시 TMP 기본 폰트.</summary>
        private static TMP_FontAsset LoadD2CodingFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset");
            return font != null ? font : TMP_Settings.defaultFontAsset;
        }

        /// <summary>Square_White 스프라이트 로드. Filled Image의 fillAmount 적용에 필요.</summary>
        private static Sprite LoadSquareWhiteSprite()
        {
            var path = "Assets/_Game/Sprites/UI/Square_White.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[WeaponSlotUI] {path} 를 찾을 수 없습니다. CoolBar가 제대로 표시되지 않을 수 있습니다.");
            return sprite;
        }
#endif
    }
}

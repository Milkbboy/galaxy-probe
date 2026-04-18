using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.UI;

namespace DrillCorp.OutGame
{
    /// <summary>
    /// HubPanel의 WeaponShopSubPanel에 부착.
    /// 5종 무기 카드 — 첫 OnEnable에 1회 빌드, 이후 이벤트엔 텍스트·색상만 패치.
    /// 잠김↔해금 전환이 일어난 카드만 본문(UnlockButton↔UpgradeRows)을 재구성.
    /// v2 가이드: docs/WeaponUnlockUpgradeSystem.md §3·§4·§5
    /// </summary>
    public class WeaponShopUI : MonoBehaviour
    {
        // 무기 메타 (v2 해금 체인)
        [System.Serializable]
        public struct WeaponSlot
        {
            public string WeaponId;
            public string DisplayName;
            public bool UnlockedByDefault;
            public int UnlockGemCost;
            public string RequiredWeaponId;
            public Sprite Icon; // V2HubCanvasSetupEditor가 자동 바인딩 (없으면 텍스트만)
        }

        [SerializeField] private WeaponSlot[] _slots = new WeaponSlot[]
        {
            new WeaponSlot { WeaponId = "sniper", DisplayName = "저격총",   UnlockedByDefault = true },
            new WeaponSlot { WeaponId = "bomb",   DisplayName = "폭탄",     UnlockGemCost = 30 },
            new WeaponSlot { WeaponId = "gun",    DisplayName = "기관총",   UnlockGemCost = 20, RequiredWeaponId = "bomb" },
            new WeaponSlot { WeaponId = "laser",  DisplayName = "레이저",   UnlockGemCost = 40, RequiredWeaponId = "gun" },
            new WeaponSlot { WeaponId = "saw",    DisplayName = "회전톱날", UnlockGemCost = 40, RequiredWeaponId = "laser" },
        };

        [Tooltip("비용 아이콘 — V2HubCanvasSetupEditor가 자동 주입")]
        [SerializeField] private Sprite _oreIcon;
        [SerializeField] private Sprite _gemIcon;

        // ── 카드별 뷰 ────────────────────────────────
        private class UpgradeRowView
        {
            public WeaponUpgradeData Data;
            public Image Bg;
            public Button Button;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI LvText;
            public CostDisplayView Cost;
        }

        private class CardView
        {
            public WeaponSlot Slot;
            public GameObject Card;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI StatusText;
            public Transform Body;            // UnlockButton 또는 UpgradeRows를 담는 컨테이너
            public bool BodyIsUnlocked;       // 현재 Body가 unlocked 형태인가

            // unlocked 본문
            public List<UpgradeRowView> UpgradeRows = new List<UpgradeRowView>();

            // locked 본문 — UnlockButton 안에 [라벨][숫자][보석아이콘] HLG
            public Button UnlockButton;
            public Image UnlockButtonImage;
            public TextMeshProUGUI UnlockLabelText;
            public TextMeshProUGUI UnlockGemNumText;
            public Image UnlockGemIcon;
        }

        private Transform _content;
        private readonly List<CardView> _views = new List<CardView>();
        private bool _builtOnce;
        private const int CardsPerRow = 2;

        // v2 팔레트
        private static readonly Color ColBg         = new Color32(0x12, 0x12, 0x2a, 0xFF);
        private static readonly Color ColAccent     = new Color32(0x4a, 0x18, 0x90, 0xFF);
        private static readonly Color ColGemAccent  = new Color32(0xb0, 0x60, 0xff, 0xFF);
        private static readonly Color ColOre        = new Color32(0xff, 0xd7, 0x00, 0xFF);
        private static readonly Color ColTextHi     = new Color32(0xcc, 0xcc, 0xcc, 0xFF);
        private static readonly Color ColTextMid    = new Color32(0x88, 0x88, 0x88, 0xFF);
        private static readonly Color ColTextLow    = new Color32(0x55, 0x55, 0x66, 0xFF);
        private static readonly Color ColOk         = new Color32(0x51, 0xcf, 0x66, 0xFF);
        private static readonly Color ColDisabled   = new Color32(0x33, 0x33, 0x44, 0xFF);

        private void Awake()
        {
            _content = transform.Find("Content");
            if (_content == null)
                Debug.LogError("[WeaponShopUI] Content 자식이 없습니다. V2HubCanvasSetupEditor로 생성됐는지 확인하세요.");
        }

        private void OnEnable()
        {
            BuildOnce();
            UpdateAll();
            StartCoroutine(ForceRebuildNextFrame());
            GameEvents.OnOreChanged       += OnCurrencyAny;
            GameEvents.OnGemsChanged      += OnCurrencyAny;
            GameEvents.OnWeaponUnlocked   += OnWeaponChangedAny;
            GameEvents.OnWeaponUpgraded   += OnWeaponChangedAny;
        }

        // 다단 ContentSizeFitter 첫-프레임 settle 강제 (CharacterSelectUI 패턴 차용).
        private IEnumerator ForceRebuildNextFrame()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            var rt = transform as RectTransform;
            if (rt == null) yield break;
            var rts = rt.GetComponentsInChildren<RectTransform>(true);
            for (int i = rts.Length - 1; i >= 0; i--) // leaf-first
                LayoutRebuilder.ForceRebuildLayoutImmediate(rts[i]);
        }

        private void OnDisable()
        {
            GameEvents.OnOreChanged       -= OnCurrencyAny;
            GameEvents.OnGemsChanged      -= OnCurrencyAny;
            GameEvents.OnWeaponUnlocked   -= OnWeaponChangedAny;
            GameEvents.OnWeaponUpgraded   -= OnWeaponChangedAny;
        }

        private void OnCurrencyAny(int _)        => UpdateAll();
        private void OnWeaponChangedAny(string _) => UpdateAll();

        // ═══════════════════════════════════════════════════
        // 1. 최초 1회 빌드
        // ═══════════════════════════════════════════════════
        private void BuildOnce()
        {
            if (_builtOnce || _content == null) return;

            // ceil(N / 2) 행 생성. 각 행은 HLG로 카드 2장 균등 분배.
            // 마지막 행 카드 수가 1개면 빈 spacer로 남은 절반 폭 채움 (v2 grid 1fr 1fr 재현).
            int rowCount = (_slots.Length + CardsPerRow - 1) / CardsPerRow;
            for (int r = 0; r < rowCount; r++)
            {
                var row = MakeCardRow(_content, r);
                int firstIdx = r * CardsPerRow;
                int lastIdx  = Mathf.Min(firstIdx + CardsPerRow, _slots.Length);
                for (int i = firstIdx; i < lastIdx; i++)
                    _views.Add(BuildCard(_slots[i], row));

                // 카드가 한 장만 있으면 빈 spacer 1개 추가
                int placed = lastIdx - firstIdx;
                for (int s = placed; s < CardsPerRow; s++)
                    AddRowSpacer(row);
            }
            _builtOnce = true;
        }

        // 카드 2장이 들어가는 행 컨테이너. 두 자식이 폭 균등 분배 + 같은 시작 위치(상단 정렬).
        private Transform MakeCardRow(Transform parent, int index)
        {
            var rowObj = new GameObject($"Row_{index}");
            rowObj.transform.SetParent(parent, false);
            rowObj.AddComponent<RectTransform>();

            var hl = rowObj.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8;
            hl.childControlWidth = true;
            hl.childControlHeight = true;       // 자식 card의 preferredHeight를 LayoutElement 경유 즉시 조회
            hl.childForceExpandWidth = false;   // 자식 LE.flexibleWidth 비율로 분배 (force는 minWidth 차이로 spacer 짓눌림)
            hl.childForceExpandHeight = false;  // 강제 늘림 없음 → 작은 카드는 자기 preferredHeight 유지
            hl.childAlignment = TextAnchor.UpperLeft;  // 작은 카드는 위 정렬, 큰 카드가 row 높이 결정

            var fitter = rowObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rowObj.transform;
        }

        private void AddRowSpacer(Transform row)
        {
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row, false);
            spacer.AddComponent<RectTransform>();
            var le = spacer.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;     // 카드와 동일 비율 → 정확한 50/50
            le.preferredWidth = 0;
            le.minWidth = 80;         // 카드 minWidth와 동일 — 균등 분배 보장
            le.preferredHeight = 0;
        }

        private CardView BuildCard(WeaponSlot slot, Transform parent)
        {
            var view = new CardView { Slot = slot };

            var card = new GameObject($"Card_{slot.WeaponId}");
            card.transform.SetParent(parent, false);
            view.Card = card;

            var cardRt = card.AddComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(0, 0);

            // row HLG의 childForceExpandWidth가 균등 분배를 처리하지만, 명시적 LE로
            // 자식 누수(다른 컬럼/카드 폭 비대칭)를 차단.
            var cardLE = card.AddComponent<LayoutElement>();
            cardLE.flexibleWidth = 1;
            cardLE.preferredWidth = 0;
            cardLE.minWidth = 80;

            var cardImg = card.AddComponent<Image>();
            cardImg.color = ColBg;

            var cardVL = card.AddComponent<VerticalLayoutGroup>();
            cardVL.padding = new RectOffset(8, 8, 6, 6);
            cardVL.spacing = 4;
            cardVL.childControlWidth = true;
            cardVL.childControlHeight = true;
            cardVL.childForceExpandWidth = true;
            cardVL.childForceExpandHeight = false;

            var cardFitter = card.AddComponent<ContentSizeFitter>();
            cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 헤더: (아이콘 24×24) + 이름 + 상태 — v2 wc-icon 28×28 기반, 카드 컴팩트화로 24
            var header = MakeRow(card.transform, "Header", 24);
            if (slot.Icon != null)
            {
                var iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(header.transform, false);
                iconObj.AddComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
                var iconLE = iconObj.AddComponent<LayoutElement>();
                iconLE.preferredWidth = 24;
                iconLE.minWidth = 24;
                iconLE.preferredHeight = 24;
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = slot.Icon;
                iconImg.preserveAspect = true;
            }
            view.NameText = MakeText(header.transform, "Name", slot.DisplayName, 13, ColTextHi);
            view.NameText.fontStyle = FontStyles.Bold;
            AddFlexible(view.NameText.gameObject);
            view.StatusText = MakeText(header.transform, "Status", "", 10, ColTextLow);

            // Body 컨테이너 — UnlockButton 또는 UpgradeRows를 담음
            var body = new GameObject("Body");
            body.transform.SetParent(card.transform, false);
            body.AddComponent<RectTransform>();
            var bodyVL = body.AddComponent<VerticalLayoutGroup>();
            bodyVL.spacing = 4;
            bodyVL.childControlWidth = true;
            bodyVL.childControlHeight = true;
            bodyVL.childForceExpandWidth = true;
            bodyVL.childForceExpandHeight = false;
            var bodyFitter = body.AddComponent<ContentSizeFitter>();
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            view.Body = body.transform;

            // 초기 본문 — DataManager 상태에 따라
            var dm = DataManager.Instance;
            bool unlocked = slot.UnlockedByDefault || (dm != null && dm.Data.HasWeapon(slot.WeaponId));
            if (unlocked) BuildUnlockedBody(view);
            else          BuildLockedBody(view);

            return view;
        }

        // ═══════════════════════════════════════════════════
        // 2. 본문 — Locked (UnlockButton)
        // ═══════════════════════════════════════════════════
        private void BuildLockedBody(CardView view)
        {
            ClearBody(view);
            view.BodyIsUnlocked = false;

            var btn = new GameObject("UnlockButton");
            btn.transform.SetParent(view.Body, false);
            var rt = btn.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 40);
            var le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 40;

            var img = btn.AddComponent<Image>();
            view.UnlockButtonImage = img;

            var button = btn.AddComponent<Button>();
            button.targetGraphic = img;
            view.UnlockButton = button;

            // 버튼 안 HLG: [라벨][숫자][보석아이콘] 가운데 정렬
            var hl = btn.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(8, 8, 0, 0);
            hl.spacing = 4;
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = true;

            view.UnlockLabelText = MakeText(btn.transform, "Label", "", 11, Color.white);
            view.UnlockLabelText.alignment = TextAlignmentOptions.Center;
            view.UnlockLabelText.raycastTarget = false;

            view.UnlockGemNumText = MakeText(btn.transform, "GemNum", "", 11, Color.white);
            view.UnlockGemNumText.alignment = TextAlignmentOptions.Center;
            view.UnlockGemNumText.raycastTarget = false;
            view.UnlockGemNumText.gameObject.SetActive(false);

            var iconObj = new GameObject("GemIcon");
            iconObj.transform.SetParent(btn.transform, false);
            iconObj.AddComponent<RectTransform>().sizeDelta = new Vector2(14, 14);
            var iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 14;
            iconLE.minWidth = 14;
            iconLE.preferredHeight = 14;
            view.UnlockGemIcon = iconObj.AddComponent<Image>();
            view.UnlockGemIcon.sprite = _gemIcon;
            view.UnlockGemIcon.preserveAspect = true;
            view.UnlockGemIcon.raycastTarget = false;
            iconObj.SetActive(false);

            var captured = view.Slot;
            button.onClick.AddListener(() =>
                DataManager.Instance?.TryUnlockWeapon(captured.WeaponId, captured.UnlockGemCost));
        }

        // ═══════════════════════════════════════════════════
        // 3. 본문 — Unlocked (UpgradeRows)
        // ═══════════════════════════════════════════════════
        private void BuildUnlockedBody(CardView view)
        {
            ClearBody(view);
            view.BodyIsUnlocked = true;

            var mgr = WeaponUpgradeManager.Instance;
            if (mgr == null)
            {
                MakeText(view.Body, "NoMgr", "(WeaponUpgradeManager 없음)", 10, ColTextLow);
                return;
            }

            foreach (var u in mgr.GetUpgradesFor(view.Slot.WeaponId))
                view.UpgradeRows.Add(BuildUpgradeRow(view.Body, u));
        }

        private UpgradeRowView BuildUpgradeRow(Transform parent, WeaponUpgradeData u)
        {
            var rv = new UpgradeRowView { Data = u };

            var row = new GameObject($"Upg_{u.UpgradeId}");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
            row.AddComponent<LayoutElement>().preferredHeight = 20;

            var img = row.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.25f);
            rv.Bg = img;

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(6, 6, 2, 2);
            hl.spacing = 6;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;

            rv.NameText = MakeText(row.transform, "Name", u.DisplayName, 10, ColTextHi);
            AddFlexible(rv.NameText.gameObject);

            rv.LvText = MakeText(row.transform, "Lv", "", 10, ColTextMid);
            AddPreferredWidth(rv.LvText.gameObject, 30);

            rv.Cost = CostDisplay.Build(row.transform, _oreIcon, _gemIcon, 10, 12, 70);

            var btn = row.AddComponent<Button>();
            btn.targetGraphic = img;
            rv.Button = btn;
            var captured = u;
            btn.onClick.AddListener(() => WeaponUpgradeManager.Instance?.TryBuy(captured));

            return rv;
        }

        private void ClearBody(CardView view)
        {
            // SetActive(false) 먼저 — Destroy는 프레임 끝에 처리되어 한 프레임 동안 옛/새 자식이
            // 동시 존재하며 Body preferredHeight 스파이크 발생. 비활성으로 즉시 layout 제외.
            for (int i = view.Body.childCount - 1; i >= 0; i--)
            {
                var child = view.Body.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            view.UpgradeRows.Clear();
            view.UnlockButton = null;
            view.UnlockButtonImage = null;
            view.UnlockLabelText = null;
            view.UnlockGemNumText = null;
            view.UnlockGemIcon = null;
        }

        // ═══════════════════════════════════════════════════
        // 4. 패치 — 데이터만 갱신, 노드는 유지
        // ═══════════════════════════════════════════════════
        private void UpdateAll()
        {
            foreach (var v in _views) UpdateCard(v);
        }

        private void UpdateCard(CardView view)
        {
            var dm = DataManager.Instance;
            bool unlocked = view.Slot.UnlockedByDefault
                            || (dm != null && dm.Data.HasWeapon(view.Slot.WeaponId));
            bool reqMet = string.IsNullOrEmpty(view.Slot.RequiredWeaponId)
                          || (dm != null && dm.Data.HasWeapon(view.Slot.RequiredWeaponId));

            // 헤더 갱신
            view.NameText.color = unlocked ? ColTextHi : ColTextLow;
            view.StatusText.text = unlocked ? "활성화" : (reqMet ? "미해금" : "잠김");
            view.StatusText.color = unlocked ? ColOk : ColTextLow;

            // Body 형태 전환 필요 시에만 재구성
            if (unlocked != view.BodyIsUnlocked)
            {
                if (unlocked) BuildUnlockedBody(view);
                else          BuildLockedBody(view);
            }

            // Body 데이터 패치
            if (view.BodyIsUnlocked) PatchUnlockedBody(view);
            else                     PatchLockedBody(view, reqMet);
        }

        private void PatchLockedBody(CardView view, bool reqMet)
        {
            if (view.UnlockButton == null) return;
            var dm = DataManager.Instance;
            bool canAfford = reqMet && dm != null && dm.Gems >= view.Slot.UnlockGemCost;

            view.UnlockButtonImage.color = canAfford ? ColAccent : ColDisabled;
            view.UnlockButton.interactable = canAfford;
            var cb = view.UnlockButton.colors;
            cb.highlightedColor = Color.Lerp(view.UnlockButtonImage.color, Color.white, 0.15f);
            cb.disabledColor = ColDisabled;
            view.UnlockButton.colors = cb;

            Color labelColor = canAfford ? Color.white
                : (reqMet ? ColGemAccent : ColTextLow);

            if (reqMet)
            {
                view.UnlockLabelText.text = $"{view.Slot.DisplayName} 해금 —";
                view.UnlockGemNumText.text = view.Slot.UnlockGemCost.ToString();
                view.UnlockGemNumText.gameObject.SetActive(true);
                if (view.UnlockGemIcon != null)
                    view.UnlockGemIcon.gameObject.SetActive(view.UnlockGemIcon.sprite != null);
            }
            else
            {
                view.UnlockLabelText.text = $"{FindSlotName(view.Slot.RequiredWeaponId)} 먼저 해금";
                view.UnlockGemNumText.gameObject.SetActive(false);
                if (view.UnlockGemIcon != null) view.UnlockGemIcon.gameObject.SetActive(false);
            }
            view.UnlockLabelText.color = labelColor;
            view.UnlockGemNumText.color = labelColor;
        }

        private void PatchUnlockedBody(CardView view)
        {
            var mgr = WeaponUpgradeManager.Instance;
            if (mgr == null) return;

            foreach (var rv in view.UpgradeRows)
            {
                int lv = mgr.GetLevel(rv.Data.UpgradeId);
                bool maxed = mgr.IsMaxed(rv.Data);
                var (ore, gem) = mgr.GetNextCost(rv.Data);
                bool canBuy = !maxed && mgr.CanAfford(rv.Data);

                rv.NameText.color = maxed ? ColTextLow : ColTextHi;
                rv.LvText.text    = $"{lv}/{rv.Data.MaxLevel}";

                if (maxed)
                    CostDisplay.PatchSpecial(rv.Cost, "완료", ColOre);
                else
                    CostDisplay.PatchPaid(rv.Cost, ore, gem, canBuy ? ColOk : ColTextLow);

                rv.Button.interactable = canBuy;
            }
        }

        // ═══════════════════════════════════════════════════
        // 5. 소품
        // ═══════════════════════════════════════════════════
        private string FindSlotName(string weaponId)
        {
            foreach (var s in _slots)
                if (s.WeaponId == weaponId) return s.DisplayName;
            return weaponId;
        }

        private GameObject MakeRow(Transform parent, string name, float height)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            row.AddComponent<LayoutElement>().preferredHeight = height;
            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 4;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            return row;
        }

        private TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            TMPFontHelper.ApplyDefaultFont(tmp);
            return tmp;
        }

        private void AddFlexible(GameObject go)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
        }

        private void AddPreferredWidth(GameObject go, float w)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = w;
        }
    }
}

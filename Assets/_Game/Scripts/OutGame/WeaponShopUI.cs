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
    /// UPGRADES 화면 좌측 무기 목록.
    /// 5종 무기를 세로 아코디언으로 한 번만 생성하고 각 무기의 강화 3종을 항상 보유한다.
    /// 모든 패널은 처음에 펼쳐지며 헤더 버튼으로 개별 접기/펼치기가 가능하다.
    /// </summary>
    public class WeaponShopUI : MonoBehaviour
    {
        [System.Serializable]
        public struct WeaponSlot
        {
            public string WeaponId;
            public string DisplayName;
            public bool UnlockedByDefault;
            public int UnlockGemCost;
            public string RequiredWeaponId;
            public Sprite Icon;
        }

        [SerializeField] private WeaponSlot[] _slots =
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
            public GameObject Body;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI StatusText;
            public TextMeshProUGUI FoldText;
            public Button UnlockButton;
            public Image UnlockButtonImage;
            public TextMeshProUGUI UnlockLabelText;
            public TextMeshProUGUI UnlockGemNumText;
            public Image UnlockGemIcon;
            public readonly List<UpgradeRowView> UpgradeRows = new List<UpgradeRowView>();
            public bool Expanded = true;
            public bool LastUnlocked;
        }

        private Transform _content;
        private readonly List<CardView> _views = new List<CardView>();
        private bool _builtOnce;

        private static readonly Color ColBg        = new Color32(0x12, 0x12, 0x2a, 0xFF);
        private static readonly Color ColHeader    = new Color32(0x0d, 0x2a, 0x3a, 0xFF);
        private static readonly Color ColAccent    = new Color32(0x08, 0x54, 0x70, 0xFF);
        private static readonly Color ColGemAccent = new Color32(0x88, 0xdd, 0xff, 0xFF);
        private static readonly Color ColOre       = new Color32(0xff, 0xd7, 0x00, 0xFF);
        private static readonly Color ColTextHi    = new Color32(0xee, 0xee, 0xee, 0xFF);
        private static readonly Color ColTextMid   = new Color32(0xaa, 0xaa, 0xaa, 0xFF);
        private static readonly Color ColTextLow   = new Color32(0x55, 0x55, 0x66, 0xFF);
        private static readonly Color ColOk        = new Color32(0x51, 0xcf, 0x66, 0xFF);
        private static readonly Color ColDisabled  = new Color32(0x25, 0x25, 0x34, 0xFF);

        private void Awake()
        {
            _content = transform.Find("ScrollArea/Viewport/Content");
            if (_content == null)
                _content = transform.Find("Content");

            if (_content == null)
                Debug.LogError("[WeaponShopUI] Viewport/Content 자식이 없습니다. V2HubCanvasSetupEditor로 재생성하세요.");
        }

        private void OnEnable()
        {
            BuildOnce();
            UpdateAll();
            StartCoroutine(ForceRebuildNextFrame());
            GameEvents.OnOreChanged += OnCurrencyAny;
            GameEvents.OnGemsChanged += OnCurrencyAny;
            GameEvents.OnWeaponUnlocked += OnWeaponChangedAny;
            GameEvents.OnWeaponUpgraded += OnWeaponChangedAny;
        }

        private void OnDisable()
        {
            GameEvents.OnOreChanged -= OnCurrencyAny;
            GameEvents.OnGemsChanged -= OnCurrencyAny;
            GameEvents.OnWeaponUnlocked -= OnWeaponChangedAny;
            GameEvents.OnWeaponUpgraded -= OnWeaponChangedAny;
        }

        private void OnCurrencyAny(int _) => UpdateAll();
        private void OnWeaponChangedAny(string _) => UpdateAll();

        private IEnumerator ForceRebuildNextFrame()
        {
            yield return null;
            RebuildLayout();
        }

        private void BuildOnce()
        {
            if (_builtOnce || _content == null) return;

            foreach (var slot in _slots)
                _views.Add(BuildCard(slot));

            _builtOnce = true;
        }

        private CardView BuildCard(WeaponSlot slot)
        {
            var view = new CardView { Slot = slot };

            var card = new GameObject($"Weapon_{slot.WeaponId}");
            card.transform.SetParent(_content, false);
            card.AddComponent<RectTransform>();
            card.AddComponent<Image>().color = ColBg;
            view.Card = card;

            var cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(8, 8, 8, 8);
            cardLayout.spacing = 6;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
            card.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildHeader(view);
            BuildBody(view);
            return view;
        }

        private void BuildHeader(CardView view)
        {
            var header = MakeRow(view.Card.transform, "Header", 48);
            var headerImage = header.AddComponent<Image>();
            headerImage.color = ColHeader;

            var layout = header.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8;

            if (view.Slot.Icon != null)
            {
                var iconObject = new GameObject("Icon");
                iconObject.transform.SetParent(header.transform, false);
                iconObject.AddComponent<RectTransform>().sizeDelta = new Vector2(36, 36);
                var iconLayout = iconObject.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = 36;
                iconLayout.minWidth = 36;
                iconLayout.preferredHeight = 36;
                var icon = iconObject.AddComponent<Image>();
                icon.sprite = view.Slot.Icon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            view.NameText = MakeText(header.transform, "Name", view.Slot.DisplayName, 17, ColTextHi);
            view.NameText.fontStyle = FontStyles.Bold;
            view.NameText.raycastTarget = false;
            AddFlexible(view.NameText.gameObject);

            view.StatusText = MakeText(header.transform, "Status", string.Empty, 12, ColTextLow);
            view.StatusText.raycastTarget = false;
            AddPreferredWidth(view.StatusText.gameObject, 58);

            view.FoldText = MakeText(header.transform, "FoldArrow", "▲", 18, ColGemAccent);
            view.FoldText.alignment = TextAlignmentOptions.Center;
            view.FoldText.raycastTarget = false;
            AddPreferredWidth(view.FoldText.gameObject, 24);

            var button = header.AddComponent<Button>();
            button.targetGraphic = headerImage;
            var captured = view;
            button.onClick.AddListener(() => Toggle(captured));
        }

        private void BuildBody(CardView view)
        {
            var body = new GameObject("Body");
            body.transform.SetParent(view.Card.transform, false);
            body.AddComponent<RectTransform>();
            view.Body = body;

            var layout = body.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            body.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildUnlockRow(view);

            var manager = WeaponUpgradeManager.Instance;
            if (manager == null)
            {
                MakeText(body.transform, "NoManager", "강화 데이터를 불러올 수 없습니다.", 11, ColTextLow);
                return;
            }

            foreach (var upgrade in manager.GetUpgradesFor(view.Slot.WeaponId))
                view.UpgradeRows.Add(BuildUpgradeRow(body.transform, upgrade));
        }

        private void BuildUnlockRow(CardView view)
        {
            var row = MakeRow(view.Body.transform, "UnlockButton", 38);
            var image = row.AddComponent<Image>();
            view.UnlockButtonImage = image;

            var button = row.AddComponent<Button>();
            button.targetGraphic = image;
            view.UnlockButton = button;

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 0, 0);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.MiddleCenter;

            view.UnlockLabelText = MakeText(row.transform, "Label", string.Empty, 12, Color.white);
            view.UnlockLabelText.alignment = TextAlignmentOptions.Center;
            view.UnlockLabelText.raycastTarget = false;

            view.UnlockGemNumText = MakeText(row.transform, "GemNum", string.Empty, 12, Color.white);
            view.UnlockGemNumText.alignment = TextAlignmentOptions.Center;
            view.UnlockGemNumText.raycastTarget = false;
            view.UnlockGemNumText.gameObject.SetActive(false);

            var iconObject = new GameObject("GemIcon");
            iconObject.transform.SetParent(row.transform, false);
            iconObject.AddComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
            var iconLayout = iconObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 16;
            iconLayout.minWidth = 16;
            iconLayout.preferredHeight = 16;
            view.UnlockGemIcon = iconObject.AddComponent<Image>();
            view.UnlockGemIcon.sprite = _gemIcon;
            view.UnlockGemIcon.preserveAspect = true;
            view.UnlockGemIcon.raycastTarget = false;
            iconObject.SetActive(false);

            var captured = view.Slot;
            button.onClick.AddListener(() =>
                DataManager.Instance?.TryUnlockWeapon(captured.WeaponId, captured.UnlockGemCost));
        }

        private UpgradeRowView BuildUpgradeRow(Transform parent, WeaponUpgradeData upgrade)
        {
            var view = new UpgradeRowView { Data = upgrade };
            var row = MakeRow(parent, $"Upg_{upgrade.UpgradeId}", 34);

            var image = row.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.3f);
            view.Bg = image;

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 3, 3);
            layout.spacing = 6;

            view.NameText = MakeText(row.transform, "Name", upgrade.DisplayName, 11, ColTextHi);
            AddFlexible(view.NameText.gameObject);

            view.LvText = MakeText(row.transform, "Lv", string.Empty, 11, ColTextMid);
            view.LvText.alignment = TextAlignmentOptions.MidlineRight;
            AddPreferredWidth(view.LvText.gameObject, 42);

            view.Cost = CostDisplay.Build(row.transform, _oreIcon, _gemIcon, 11, 14, 92);

            var button = row.AddComponent<Button>();
            button.targetGraphic = image;
            view.Button = button;
            var captured = upgrade;
            button.onClick.AddListener(() => WeaponUpgradeManager.Instance?.TryBuy(captured));
            return view;
        }

        private void Toggle(CardView view)
        {
            view.Expanded = !view.Expanded;
            view.Body.SetActive(view.Expanded);
            view.FoldText.text = view.Expanded ? "▲" : "▼";
            RebuildLayout();
        }

        private void UpdateAll()
        {
            foreach (var view in _views)
                UpdateCard(view);
        }

        private void UpdateCard(CardView view)
        {
            var dataManager = DataManager.Instance;
            bool unlocked = view.Slot.UnlockedByDefault
                            || (dataManager != null && dataManager.Data.HasWeapon(view.Slot.WeaponId));
            bool requirementMet = string.IsNullOrEmpty(view.Slot.RequiredWeaponId)
                                  || (dataManager != null && dataManager.Data.HasWeapon(view.Slot.RequiredWeaponId));

            view.NameText.color = unlocked ? ColTextHi : ColTextLow;
            view.StatusText.text = unlocked ? "활성화" : (requirementMet ? "미해금" : "잠김");
            view.StatusText.color = unlocked ? ColOk : ColTextLow;
            view.UnlockButton.gameObject.SetActive(!unlocked);

            if (!unlocked)
                PatchUnlockRow(view, requirementMet);

            PatchUpgradeRows(view, unlocked);

            if (view.LastUnlocked != unlocked)
            {
                view.LastUnlocked = unlocked;
                RebuildLayout();
            }
        }

        private void PatchUnlockRow(CardView view, bool requirementMet)
        {
            var dataManager = DataManager.Instance;
            bool canAfford = requirementMet && dataManager != null
                             && dataManager.Gems >= view.Slot.UnlockGemCost;

            view.UnlockButtonImage.color = canAfford ? ColAccent : ColDisabled;
            view.UnlockButton.interactable = canAfford;

            if (requirementMet)
            {
                view.UnlockLabelText.text = $"{view.Slot.DisplayName} 해금";
                view.UnlockGemNumText.text = view.Slot.UnlockGemCost.ToString();
                view.UnlockGemNumText.gameObject.SetActive(true);
                view.UnlockGemIcon.gameObject.SetActive(view.UnlockGemIcon.sprite != null);
            }
            else
            {
                view.UnlockLabelText.text = $"{FindSlotName(view.Slot.RequiredWeaponId)} 먼저 해금";
                view.UnlockGemNumText.gameObject.SetActive(false);
                view.UnlockGemIcon.gameObject.SetActive(false);
            }

            Color color = canAfford ? Color.white : (requirementMet ? ColGemAccent : ColTextLow);
            view.UnlockLabelText.color = color;
            view.UnlockGemNumText.color = color;
        }

        private void PatchUpgradeRows(CardView card, bool weaponUnlocked)
        {
            var manager = WeaponUpgradeManager.Instance;
            if (manager == null) return;

            foreach (var row in card.UpgradeRows)
            {
                int level = manager.GetLevel(row.Data.UpgradeId);
                bool maxed = manager.IsMaxed(row.Data);
                var (ore, gem) = manager.GetNextCost(row.Data);
                bool canBuy = weaponUnlocked && !maxed && manager.CanAfford(row.Data);

                row.LvText.text = $"{level}/{row.Data.MaxLevel}";
                row.NameText.color = weaponUnlocked && !maxed ? ColTextHi : ColTextLow;
                row.LvText.color = weaponUnlocked ? ColTextMid : ColTextLow;
                row.Bg.color = weaponUnlocked
                    ? new Color(0, 0, 0, 0.3f)
                    : new Color(0, 0, 0, 0.55f);

                if (maxed)
                    CostDisplay.PatchSpecial(row.Cost, "완료", ColOre);
                else
                    CostDisplay.PatchPaid(row.Cost, ore, gem, canBuy ? ColOk : ColTextLow);

                row.Button.interactable = canBuy;
            }
        }

        private void RebuildLayout()
        {
            if (_content is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            if (transform is RectTransform rootRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        private string FindSlotName(string weaponId)
        {
            foreach (var slot in _slots)
                if (slot.WeaponId == weaponId) return slot.DisplayName;
            return weaponId;
        }

        private static GameObject MakeRow(Transform parent, string name, float height)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            row.AddComponent<LayoutElement>().preferredHeight = height;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        private static TextMeshProUGUI MakeText(
            Transform parent, string name, string text, float size, Color color)
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

        private static void AddFlexible(GameObject target)
        {
            var layout = target.GetComponent<LayoutElement>();
            if (layout == null) layout = target.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1;
        }

        private static void AddPreferredWidth(GameObject target, float width)
        {
            var layout = target.GetComponent<LayoutElement>();
            if (layout == null) layout = target.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
        }
    }
}

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
    /// HubPanel의 AbilityShopSubPanel에 부착.
    /// 선택된 캐릭터의 3개 어빌리티 카드 표시.
    /// 캐릭터 전환 시: Body 통째로 재구성. 통화/해금 이벤트: 노드 유지하고 패치.
    /// v2 가이드: docs/CharacterAbilitySystem.md §6
    /// </summary>
    public class AbilityShopUI : MonoBehaviour
    {
        [Tooltip("9개 AbilityData SO — V2HubCanvasSetupEditor가 자동 연결")]
        [SerializeField] private AbilityData[] _allAbilities;

        // ── 카드별 뷰 ────────────────────────────────
        private class CardView
        {
            public AbilityData Data;
            public GameObject Card;
            public TextMeshProUGUI HeaderText;     // [1] 네이팜
            public TextMeshProUGUI StatusText;     // 활성화 / 잠김
            public TextMeshProUGUI DescText;
            public Button UnlockButton;
            public Image UnlockButtonImage;
            public TextMeshProUGUI UnlockButtonText;
            public bool BodyIsUnlocked;
        }

        private Transform _content;
        private string _currentCharacterId;        // 현재 빌드된 캐릭터 — 다르면 재빌드
        private readonly List<CardView> _views = new List<CardView>();

        // 팔레트
        private static readonly Color ColBg         = new Color32(0x12, 0x12, 0x2a, 0xFF);
        private static readonly Color ColAccent     = new Color32(0x4a, 0x18, 0x90, 0xFF);
        private static readonly Color ColGemAccent  = new Color32(0xb0, 0x60, 0xff, 0xFF);
        private static readonly Color ColTextHi     = new Color32(0xcc, 0xcc, 0xcc, 0xFF);
        private static readonly Color ColTextMid    = new Color32(0x88, 0x88, 0x88, 0xFF);
        private static readonly Color ColTextLow    = new Color32(0x55, 0x55, 0x66, 0xFF);
        private static readonly Color ColOk         = new Color32(0x51, 0xcf, 0x66, 0xFF);
        private static readonly Color ColDisabled   = new Color32(0x33, 0x33, 0x44, 0xFF);

        private void Awake()
        {
            _content = transform.Find("Content");
            if (_content == null)
                Debug.LogError("[AbilityShopUI] Content 자식이 없습니다. V2HubCanvasSetupEditor로 생성됐는지 확인.");
        }

        private void OnEnable()
        {
            RebuildIfCharacterChanged();
            UpdateAll();
            GameEvents.OnCharacterSelected += OnCharacterSelected;
            GameEvents.OnGemsChanged       += OnGemsAny;
            GameEvents.OnAbilityUnlocked   += OnAbilityUnlocked;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterSelected -= OnCharacterSelected;
            GameEvents.OnGemsChanged       -= OnGemsAny;
            GameEvents.OnAbilityUnlocked   -= OnAbilityUnlocked;
        }

        private void OnCharacterSelected(string _)  { RebuildIfCharacterChanged(); UpdateAll(); }
        private void OnGemsAny(int _)               => UpdateAll();
        private void OnAbilityUnlocked(string _)    => UpdateAll();

        // ═══════════════════════════════════════════════════
        // 1. 캐릭터 변경 시에만 카드 전체 재빌드
        // ═══════════════════════════════════════════════════
        private void RebuildIfCharacterChanged()
        {
            if (_content == null) return;

            string charId = DataManager.Instance?.Data?.SelectedCharacterId;
            if (charId == _currentCharacterId && _views.Count > 0) return;

            // 기존 카드 제거 — SetActive(false)로 즉시 레이아웃 제외 후 비동기 Destroy.
            // 같은 프레임에 새 카드가 추가되어도 옛 카드와 섞여 폭주하지 않게 함.
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                var child = _content.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            _views.Clear();
            _currentCharacterId = charId;

            if (string.IsNullOrEmpty(charId) || _allAbilities == null) return;

            // 슬롯 1·2·3 순으로 정렬해서 빌드
            var picks = new List<AbilityData>();
            foreach (var a in _allAbilities)
                if (a != null && a.CharacterId == charId) picks.Add(a);
            picks.Sort((x, y) => x.SlotKey.CompareTo(y.SlotKey));

            foreach (var a in picks)
                _views.Add(BuildCard(a));
        }

        private CardView BuildCard(AbilityData a)
        {
            var view = new CardView { Data = a };

            var card = new GameObject($"Card_{a.AbilityId}");
            card.transform.SetParent(_content, false);
            view.Card = card;

            card.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
            card.AddComponent<Image>().color = ColBg;

            var vl = card.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(8, 8, 6, 6);
            vl.spacing = 4;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            var fitter = card.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 헤더: [슬롯] 이름 ── 상태
            var header = MakeRow(card.transform, "Header", 20);
            view.HeaderText = MakeText(header.transform, "Title",
                $"[{a.SlotKey}] {a.DisplayName}", 12, ColTextHi);
            view.HeaderText.fontStyle = FontStyles.Bold;
            AddFlexible(view.HeaderText.gameObject);
            view.StatusText = MakeText(header.transform, "Status", "", 10, ColTextLow);

            // 설명
            view.DescText = MakeText(card.transform, "Desc",
                string.IsNullOrEmpty(a.Description) ? "—" : a.Description,
                10, ColTextMid);
            view.DescText.textWrappingMode = TextWrappingModes.Normal;
            view.DescText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // 해금 버튼 (잠김일 때만 활성)
            var btn = new GameObject("UnlockButton");
            btn.transform.SetParent(card.transform, false);
            btn.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 32);
            btn.AddComponent<LayoutElement>().preferredHeight = 32;

            var img = btn.AddComponent<Image>();
            view.UnlockButtonImage = img;

            var button = btn.AddComponent<Button>();
            button.targetGraphic = img;
            view.UnlockButton = button;

            var btnText = MakeText(btn.transform, "Text", "", 11, Color.white);
            btnText.alignment = TextAlignmentOptions.Center;
            var btnTextRt = btnText.GetComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero; btnTextRt.anchorMax = Vector2.one;
            btnTextRt.offsetMin = Vector2.zero; btnTextRt.offsetMax = Vector2.zero;
            view.UnlockButtonText = btnText;

            var captured = a;
            button.onClick.AddListener(() =>
                DataManager.Instance?.TryUnlockAbility(captured.AbilityId, captured.UnlockGemCost));

            return view;
        }

        // ═══════════════════════════════════════════════════
        // 2. 패치 — 텍스트·색상·interactable만
        // ═══════════════════════════════════════════════════
        private void UpdateAll()
        {
            foreach (var v in _views) UpdateCard(v);
        }

        private void UpdateCard(CardView view)
        {
            var dm = DataManager.Instance;
            bool unlocked = dm != null && dm.Data.HasAbility(view.Data.AbilityId);
            bool reqMet = view.Data.RequiredAbility == null
                          || (dm != null && dm.Data.HasAbility(view.Data.RequiredAbility.AbilityId));

            view.HeaderText.color = unlocked ? ColTextHi : ColTextLow;
            view.StatusText.text  = unlocked ? "활성화" : (reqMet ? "미해금" : "잠김");
            view.StatusText.color = unlocked ? ColOk : ColTextLow;
            view.DescText.color   = unlocked ? ColTextMid : ColTextLow;

            if (unlocked)
            {
                // 해금 완료 — 버튼 비활성, 라벨 변경
                view.UnlockButton.interactable = false;
                view.UnlockButtonImage.color = ColDisabled;
                view.UnlockButtonText.text = "활성화됨";
                view.UnlockButtonText.color = ColTextLow;
            }
            else
            {
                bool canAfford = reqMet && dm != null && dm.Gems >= view.Data.UnlockGemCost;
                view.UnlockButton.interactable = canAfford;
                view.UnlockButtonImage.color = canAfford ? ColAccent : ColDisabled;

                view.UnlockButtonText.text = reqMet
                    ? $"해금 — {view.Data.UnlockGemCost} 보석"
                    : $"{view.Data.RequiredAbility.DisplayName} 먼저 해금";
                view.UnlockButtonText.color = canAfford ? Color.white
                    : (reqMet ? ColGemAccent : ColTextLow);
            }
        }

        // ═══════════════════════════════════════════════════
        // 3. 소품
        // ═══════════════════════════════════════════════════
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
    }
}

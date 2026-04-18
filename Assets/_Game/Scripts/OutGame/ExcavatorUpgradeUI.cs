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
    /// HubPanel의 ExcavatorUpgradeSubPanel에 부착.
    /// 기존 UpgradeManager에서 굴착기 관련 업그레이드(체력·방어·연료·채굴 속도)만 필터해 표시.
    /// 첫 OnEnable 1회 빌드, 이후 이벤트엔 텍스트·색상·interactable만 패치.
    /// </summary>
    public class ExcavatorUpgradeUI : MonoBehaviour
    {
        [Tooltip("이 패널에 표시할 업그레이드 타입 (v2 굴착기 강화 4종)")]
        [SerializeField] private UpgradeType[] _includedTypes = new[]
        {
            UpgradeType.MaxHealth,      // excavator_hp
            UpgradeType.Armor,          // excavator_armor
            UpgradeType.MiningRate,     // mine_speed
            UpgradeType.MiningTarget,   // mine_target (v2는 굴착기 섹션에 배치)
        };

        private class RowView
        {
            public UpgradeData Data;
            public Image Bg;
            public Button Button;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI EffectText;
            public TextMeshProUGUI LvText;
            public TextMeshProUGUI CostText;
        }

        private Transform _content;
        private readonly List<RowView> _views = new List<RowView>();
        private bool _builtOnce;

        // 팔레트
        private static readonly Color ColRowBg     = new Color32(0x12, 0x12, 0x2a, 0xFF);
        private static readonly Color ColRowBgDim  = new Color32(0x0a, 0x0a, 0x18, 0xFF);
        private static readonly Color ColTextHi    = new Color32(0xcc, 0xcc, 0xcc, 0xFF);
        private static readonly Color ColTextMid   = new Color32(0x88, 0x88, 0x88, 0xFF);
        private static readonly Color ColTextLow   = new Color32(0x55, 0x55, 0x66, 0xFF);
        private static readonly Color ColOk        = new Color32(0x51, 0xcf, 0x66, 0xFF);
        private static readonly Color ColOre       = new Color32(0xff, 0xd7, 0x00, 0xFF);

        private void Awake()
        {
            _content = transform.Find("Content");
            if (_content == null)
                Debug.LogError("[ExcavatorUpgradeUI] Content 자식이 없습니다.");
        }

        private void OnEnable()
        {
            BuildOnce();
            UpdateAll();
            GameEvents.OnOreChanged       += OnOreAny;
            GameEvents.OnUpgradePurchased += OnUpgradePurchased;
        }

        private void OnDisable()
        {
            GameEvents.OnOreChanged       -= OnOreAny;
            GameEvents.OnUpgradePurchased -= OnUpgradePurchased;
        }

        private void OnOreAny(int _) => UpdateAll();
        private void OnUpgradePurchased(string id, int lv) => UpdateAll();

        // ═══════════════════════════════════════════════════
        private void BuildOnce()
        {
            if (_builtOnce || _content == null) return;
            var mgr = UpgradeManager.Instance;
            if (mgr == null)
            {
                MakeText(_content, "NoMgr", "(UpgradeManager 미설치)", 11, ColTextLow);
                return;
            }

            var typeSet = new HashSet<UpgradeType>(_includedTypes);
            foreach (var u in mgr.GetAllUpgrades())
            {
                if (u == null || !typeSet.Contains(u.Type)) continue;
                _views.Add(BuildRow(u));
            }
            _builtOnce = true;
        }

        private RowView BuildRow(UpgradeData u)
        {
            var rv = new RowView { Data = u };

            var row = new GameObject($"Upg_{u.UpgradeId}");
            row.transform.SetParent(_content, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
            row.AddComponent<LayoutElement>().preferredHeight = 28;

            var img = row.AddComponent<Image>();
            img.color = ColRowBg;
            rv.Bg = img;

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(8, 8, 4, 4);
            hl.spacing = 6;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;

            rv.NameText = MakeText(row.transform, "Name", LocalizeName(u), 11, ColTextHi);
            AddPreferredWidth(rv.NameText.gameObject, 100);

            rv.EffectText = MakeText(row.transform, "Effect", u.Description, 10, ColTextMid);
            AddFlexible(rv.EffectText.gameObject);

            rv.LvText = MakeText(row.transform, "Lv", "", 10, ColTextMid);
            AddPreferredWidth(rv.LvText.gameObject, 36);
            rv.LvText.alignment = TextAlignmentOptions.MidlineRight;

            rv.CostText = MakeText(row.transform, "Cost", "", 10, ColTextLow);
            AddPreferredWidth(rv.CostText.gameObject, 80);
            rv.CostText.alignment = TextAlignmentOptions.MidlineRight;

            var btn = row.AddComponent<Button>();
            btn.targetGraphic = img;
            rv.Button = btn;
            var captured = u;
            btn.onClick.AddListener(() => UpgradeManager.Instance?.TryUpgrade(captured.UpgradeId));

            return rv;
        }

        private void UpdateAll()
        {
            foreach (var v in _views) UpdateRow(v);
        }

        private void UpdateRow(RowView v)
        {
            var mgr = UpgradeManager.Instance;
            if (mgr == null) return;

            int lv = mgr.GetUpgradeLevel(v.Data.UpgradeId);
            int max = v.Data.MaxLevel;
            bool maxed = lv >= max;
            int cost = maxed ? -1 : v.Data.GetCostForLevel(lv);
            bool canBuy = !maxed && mgr.CanUpgrade(v.Data.UpgradeId);

            v.LvText.text = $"{lv}/{max}";

            if (maxed)
            {
                v.CostText.text   = "완료";
                v.CostText.color  = ColOre;
                v.NameText.color  = ColTextLow;
                v.EffectText.color = ColTextLow;
                v.Bg.color        = ColRowBgDim;
            }
            else
            {
                v.CostText.text   = $"{cost}광석";
                v.CostText.color  = canBuy ? ColOk : ColTextLow;
                v.NameText.color  = ColTextHi;
                v.EffectText.color = ColTextMid;
                v.Bg.color        = ColRowBg;
            }
            v.Button.interactable = canBuy;
        }

        // ═══════════════════════════════════════════════════
        // UpgradeId → 한글 이름 매핑
        // ═══════════════════════════════════════════════════
        private static string LocalizeName(UpgradeData u)
        {
            switch (u.UpgradeId)
            {
                case "excavator_hp":     return "굴착기 내구도";
                case "excavator_armor":  return "장갑 강화";
                case "mine_speed":       return "채굴 속도 향상";
                case "mine_target":      return "목표량 확장";
                default:                 return string.IsNullOrEmpty(u.DisplayName) ? u.UpgradeId : u.DisplayName;
            }
        }

        // ═══════════════════════════════════════════════════
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

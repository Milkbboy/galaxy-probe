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
    /// HubPanel의 StatDisplaySubPanel에 부착.
    /// 광석/보석/캐릭터/굴착기 강화/해금 수를 실시간 합산해 표시.
    /// 첫 OnEnable에 행 1회 빌드, 이후 모든 이벤트는 Value 텍스트만 패치.
    /// </summary>
    public class StatDisplayUI : MonoBehaviour
    {
        [Tooltip("3개 캐릭터 SO — 이름·테마색 표시용. V2HubCanvasSetupEditor가 자동 연결")]
        [SerializeField] private CharacterData[] _characters;

        [Tooltip("최대 체력 베이스 (v2: 100)")]
        [SerializeField] private float _baseMaxHealth = 100f;

        [Tooltip("채굴 속도 베이스 (v2: 초당 5 광석)")]
        [SerializeField] private float _baseMiningRate = 5f;

        [Tooltip("목표 채굴량 베이스 (v2: 100)")]
        [SerializeField] private float _baseMiningTarget = 100f;

        [Tooltip("보석 출현 확률 베이스 (v2: 0.05 = 5%)")]
        [Range(0f, 1f)]
        [SerializeField] private float _baseGemDropRate = 0.05f;

        // 행 캐시 — v2 표시 항목
        private Transform _content;
        private TextMeshProUGUI _valOre;
        private TextMeshProUGUI _valGem;
        private TextMeshProUGUI _valCharacter;
        private TextMeshProUGUI _valMaxHp;       // 최대 체력 (HP)
        private TextMeshProUGUI _valDmgReduce;   // 받는 피해 감소율 (armor)
        private TextMeshProUGUI _valMineRate;    // 채굴 속도 (초당 N 광석)
        private TextMeshProUGUI _valMineTarget;  // 목표 채굴량
        private TextMeshProUGUI _valGemDrop;     // 보석 출현 확률 (5%+bonus)
        private TextMeshProUGUI _valGemSpeed;    // 채집 속도 (100%×mult)
        private bool _builtOnce;

        // 팔레트
        private static readonly Color ColRowBg   = new Color32(0x12, 0x12, 0x2a, 0xFF);
        private static readonly Color ColTextHi  = new Color32(0xee, 0xee, 0xee, 0xFF);
        private static readonly Color ColTextMid = new Color32(0xaa, 0xaa, 0xaa, 0xFF);
        private static readonly Color ColOre     = new Color32(0xff, 0xd7, 0x00, 0xFF);
        private static readonly Color ColGem     = new Color32(0x88, 0xdd, 0xff, 0xFF);

        private void Awake()
        {
            _content = transform.Find("Content");
            if (_content == null)
                Debug.LogError("[StatDisplayUI] Content 자식이 없습니다.");
        }

        private void OnEnable()
        {
            BuildOnce();
            UpdateAll();
            GameEvents.OnOreChanged       += OnIntAny;
            GameEvents.OnGemsChanged      += OnIntAny;
            GameEvents.OnUpgradePurchased += OnUpgrade;
            GameEvents.OnWeaponUpgraded   += OnStringAny;
            GameEvents.OnWeaponUnlocked   += OnStringAny;
            GameEvents.OnAbilityUnlocked  += OnStringAny;
            GameEvents.OnCharacterSelected += OnStringAny;
        }

        private void OnDisable()
        {
            GameEvents.OnOreChanged       -= OnIntAny;
            GameEvents.OnGemsChanged      -= OnIntAny;
            GameEvents.OnUpgradePurchased -= OnUpgrade;
            GameEvents.OnWeaponUpgraded   -= OnStringAny;
            GameEvents.OnWeaponUnlocked   -= OnStringAny;
            GameEvents.OnAbilityUnlocked  -= OnStringAny;
            GameEvents.OnCharacterSelected -= OnStringAny;
        }

        private void OnIntAny(int _) => UpdateAll();
        private void OnStringAny(string _) => UpdateAll();
        private void OnUpgrade(string _, int __) => UpdateAll();

        // ═══════════════════════════════════════════════════
        private void BuildOnce()
        {
            if (_builtOnce || _content == null) return;

            // 에디터가 미리 만든 샘플 행 제거
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                var c = _content.GetChild(i).gameObject;
                c.SetActive(false);
                Destroy(c);
            }

            // v2 표시 형식 — docs/v2.html L514-523
            _valOre         = MakeRow("광석",          ColOre);
            _valGem         = MakeRow("보석",          ColGem);
            _valCharacter   = MakeRow("선택 캐릭터",   ColTextHi);
            _valMaxHp       = MakeRow("최대 체력",     ColTextHi);
            _valDmgReduce   = MakeRow("받는 피해",     ColTextHi);
            _valMineRate    = MakeRow("채굴 속도",     ColTextHi);
            _valMineTarget  = MakeRow("목표 채굴량",   ColTextHi);
            _valGemDrop     = MakeRow("보석 출현 확률", ColTextHi);
            _valGemSpeed    = MakeRow("채집 속도",     ColTextHi);

            _builtOnce = true;
        }

        private TextMeshProUGUI MakeRow(string label, Color valueColor)
        {
            var row = new GameObject("Row_" + label);
            row.transform.SetParent(_content, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 22);
            row.AddComponent<LayoutElement>().preferredHeight = 22;

            var img = row.AddComponent<Image>();
            img.color = ColRowBg;

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(8, 8, 2, 2);
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = false;

            var labelText = MakeText(row.transform, "Label", label, 11, ColTextMid);
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            var valText = MakeText(row.transform, "Value", "", 11, valueColor);
            valText.alignment = TextAlignmentOptions.MidlineRight;

            return valText;
        }

        // ═══════════════════════════════════════════════════
        private void UpdateAll()
        {
            if (!_builtOnce) return;
            var dm = DataManager.Instance;
            var um = UpgradeManager.Instance;

            _valOre.text = dm != null ? $"{dm.Ore:N0} 개" : "—";
            _valGem.text = dm != null ? $"{dm.Gems:N0} 개" : "—";

            // 캐릭터
            string charId = dm?.Data?.SelectedCharacterId;
            var charData = FindCharacter(charId);
            if (charData != null)
            {
                _valCharacter.text  = charData.DisplayName;
                _valCharacter.color = charData.ThemeColor;
            }
            else
            {
                _valCharacter.text  = string.IsNullOrEmpty(charId) ? "—" : charId;
                _valCharacter.color = ColTextHi;
            }

            // v2 공식 (docs/v2.html L421-454, L514-523)
            //   maxHp        = 100 + excavator_hp×30
            //   damageReduce = excavator_armor×0.15  (받는 피해 = 100% - reduce)
            //   mineRate     = 5 + mine_speed×2
            //   mineTarget   = 100 + mine_target×50
            //   gemDropPct   = 5% + gem_drop×2%
            //   gemSpeedMult = 100% + gem_speed×20%
            float hpBonus      = um != null ? um.GetTotalBonus(UpgradeType.MaxHealth)       : 0f;
            float dmgReduce    = um != null ? um.GetTotalBonus(UpgradeType.Armor)           : 0f;
            float mineRateAdd  = um != null ? um.GetTotalBonus(UpgradeType.MiningRate)      : 0f;
            float mineTgtAdd   = um != null ? um.GetTotalBonus(UpgradeType.MiningTarget)    : 0f;
            float gemDropAdd   = um != null ? um.GetTotalBonus(UpgradeType.GemDropRate)     : 0f;
            float gemSpeedAdd  = um != null ? um.GetTotalBonus(UpgradeType.GemCollectSpeed) : 0f;

            _valMaxHp.text      = $"{_baseMaxHealth + hpBonus:F0} HP";
            _valDmgReduce.text  = $"-{dmgReduce * 100f:F0}%";
            _valMineRate.text   = $"초당 {_baseMiningRate + mineRateAdd:F0} 광석";
            _valMineTarget.text = $"{_baseMiningTarget + mineTgtAdd:F0} 광석";
            _valGemDrop.text    = $"{(_baseGemDropRate + gemDropAdd) * 100f:F0}%";
            _valGemSpeed.text   = $"{(1f + gemSpeedAdd) * 100f:F0}%";
        }

        private CharacterData FindCharacter(string id)
        {
            if (string.IsNullOrEmpty(id) || _characters == null) return null;
            foreach (var c in _characters)
                if (c != null && c.CharacterId == id) return c;
            return null;
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
    }
}

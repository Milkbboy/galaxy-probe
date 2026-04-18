using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Core;
using DrillCorp.Data;

namespace DrillCorp.OutGame
{
    /// <summary>
    /// HubPanel의 CharacterSelectSubPanel에 부착.
    /// Victor/Sara/Jinus 3카드 바인딩 + 클릭 선택 + 선택 강조.
    /// V2HubCanvasSetupEditor가 _characters·_cards를 자동 연결.
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        [Tooltip("3개 캐릭터 SO — 순서: Victor, Sara, Jinus")]
        [SerializeField] private CharacterData[] _characters = new CharacterData[3];

        [Tooltip("3개 카드 GameObject — _characters와 같은 순서")]
        [SerializeField] private GameObject[] _cards = new GameObject[3];

        // 카드별 캐시
        private Image[] _cardBgs;
        private Button[] _cardBtns;
        private TextMeshProUGUI[] _nameTexts;
        private TextMeshProUGUI[] _titleTexts;
        private TextMeshProUGUI[] _descTexts;
        private TextMeshProUGUI[] _badgeTexts;

        // 비선택 기본 배경색
        private static readonly Color DefaultBg = new Color32(0x12, 0x12, 0x2a, 0xFF);

        private void Awake()
        {
            Cache();
            Wire();
        }

        private void OnEnable()
        {
            GameEvents.OnCharacterSelected += OnCharacterSelected;
            Refresh();
            // TMP 텍스트 측정 전에 CSF가 계산되는 초기 1프레임 문제 방지:
            // 다음 프레임에 상위 서브패널까지 강제 재빌드.
            StartCoroutine(ForceRebuildNextFrame());
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterSelected -= OnCharacterSelected;
        }

        // ═══════════════════════════════════════════════════
        private void Cache()
        {
            int n = _cards.Length;
            _cardBgs    = new Image[n];
            _cardBtns   = new Button[n];
            _nameTexts  = new TextMeshProUGUI[n];
            _titleTexts = new TextMeshProUGUI[n];
            _descTexts  = new TextMeshProUGUI[n];
            _badgeTexts = new TextMeshProUGUI[n];

            for (int i = 0; i < n; i++)
            {
                var card = _cards[i];
                if (card == null) continue;

                _cardBgs[i]    = card.GetComponent<Image>();
                _cardBtns[i]   = card.GetComponent<Button>();
                _nameTexts[i]  = card.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                _titleTexts[i] = card.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                _descTexts[i]  = card.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
                _badgeTexts[i] = card.transform.Find("SelectBadge")?.GetComponent<TextMeshProUGUI>();
            }
        }

        private void Wire()
        {
            for (int i = 0; i < _cardBtns.Length; i++)
            {
                if (_cardBtns[i] == null) continue;
                int idx = i; // 클로저 캡처
                _cardBtns[i].onClick.AddListener(() => OnCardClicked(idx));
            }
        }

        // ═══════════════════════════════════════════════════
        private void OnCardClicked(int idx)
        {
            if (idx < 0 || idx >= _characters.Length) return;
            var c = _characters[idx];
            if (c == null) return;

            DataManager.Instance?.SelectCharacter(c.CharacterId);
            // OnCharacterSelected 이벤트 → Refresh() 자동 호출
        }

        private void OnCharacterSelected(string _) => Refresh();

        private IEnumerator ForceRebuildNextFrame()
        {
            yield return null;               // TMP 메시 생성 대기
            yield return new WaitForEndOfFrame();
            var rt = transform as RectTransform;
            if (rt == null) yield break;
            // 중첩 ContentSizeFitter는 한 프레임에 한 단계만 해결 —
            // 리프(카드) → 상위(Content) → 최상위(SubPanel) 순으로 수동 재빌드.
            var rts = rt.GetComponentsInChildren<RectTransform>(true);
            for (int i = rts.Length - 1; i >= 0; i--)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rts[i]);
        }

        private void Refresh()
        {
            string currentId = DataManager.Instance?.Data?.SelectedCharacterId;

            for (int i = 0; i < _cards.Length; i++)
            {
                var c = _characters[i];
                if (c == null || _cards[i] == null) continue;

                // 데이터 바인딩
                if (_nameTexts[i] != null)
                {
                    _nameTexts[i].text = c.DisplayName;
                    _nameTexts[i].color = c.ThemeColor;
                }
                if (_titleTexts[i] != null) _titleTexts[i].text = c.Title;
                if (_descTexts[i]  != null) _descTexts[i].text  = c.Description;

                // 선택 상태
                bool selected = c.CharacterId == currentId;
                if (_badgeTexts[i] != null)
                {
                    _badgeTexts[i].text  = selected ? "[선택됨]" : "선택하기";
                    _badgeTexts[i].color = c.ThemeColor;
                }

                // 배경 강조
                if (_cardBgs[i] != null)
                {
                    _cardBgs[i].color = selected
                        ? Color.Lerp(DefaultBg, c.ThemeColor, 0.25f)
                        : DefaultBg;
                }
            }
        }
    }
}

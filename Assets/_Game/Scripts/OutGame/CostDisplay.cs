using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.UI;

namespace DrillCorp.OutGame
{
    /// <summary>
    /// 강화/해금 카드의 비용 표시 — 「숫자 + 아이콘」 멀티 엘리먼트.
    /// v2.html: `30🪨 15💎` 형태를 D2Coding 폰트로 재현하기 위해
    /// emoji 대신 sprite Image를 사용. patch-pattern 호환.
    /// </summary>
    public class CostDisplayView
    {
        public GameObject Container;
        public TextMeshProUGUI OreText;
        public Image OreIcon;
        public TextMeshProUGUI GemText;
        public Image GemIcon;
        public TextMeshProUGUI SpecialText; // "무료" / "완료" / 잠금 사유 등
    }

    public static class CostDisplay
    {
        /// <summary>
        /// 우측 정렬 비용 컨테이너 생성. 5개 자식(OreText, OreIcon, GemText, GemIcon, SpecialText)
        /// 모두 SetActive(false)로 시작 — Patch 시 필요한 것만 켬.
        /// </summary>
        public static CostDisplayView Build(Transform parent, Sprite oreIcon, Sprite gemIcon,
            float fontSize = 10, float iconSize = 12, float preferredContainerWidth = 90)
        {
            var view = new CostDisplayView();

            var container = new GameObject("Cost");
            container.transform.SetParent(parent, false);
            container.AddComponent<RectTransform>();
            var le = container.AddComponent<LayoutElement>();
            le.preferredWidth = preferredContainerWidth;

            var hl = container.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 2;
            hl.childAlignment = TextAnchor.MiddleRight;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;
            view.Container = container;

            view.OreText = MakeText(container.transform, "OreNum", fontSize);
            view.OreText.alignment = TextAlignmentOptions.MidlineRight;
            view.OreIcon = MakeIcon(container.transform, "OreIcon", oreIcon, iconSize);

            view.GemText = MakeText(container.transform, "GemNum", fontSize);
            view.GemText.alignment = TextAlignmentOptions.MidlineRight;
            view.GemIcon = MakeIcon(container.transform, "GemIcon", gemIcon, iconSize);

            view.SpecialText = MakeText(container.transform, "Special", fontSize);
            view.SpecialText.alignment = TextAlignmentOptions.MidlineRight;

            // 모두 비활성화로 시작
            view.OreText.gameObject.SetActive(false);
            view.OreIcon.gameObject.SetActive(false);
            view.GemText.gameObject.SetActive(false);
            view.GemIcon.gameObject.SetActive(false);
            view.SpecialText.gameObject.SetActive(false);

            return view;
        }

        /// <summary>광석/보석 비용 표시. 0이면 해당 토큰 숨김. 둘 다 0이면 "무료" 처리.</summary>
        public static void PatchPaid(CostDisplayView v, int ore, int gem, Color color)
        {
            if (v == null) return;

            if (ore <= 0 && gem <= 0)
            {
                PatchSpecial(v, "무료", color);
                return;
            }

            v.SpecialText.gameObject.SetActive(false);

            bool showOre = ore > 0;
            v.OreText.gameObject.SetActive(showOre);
            v.OreIcon.gameObject.SetActive(showOre && v.OreIcon.sprite != null);
            if (showOre)
            {
                v.OreText.text = ore.ToString();
                v.OreText.color = color;
            }

            bool showGem = gem > 0;
            v.GemText.gameObject.SetActive(showGem);
            v.GemIcon.gameObject.SetActive(showGem && v.GemIcon.sprite != null);
            if (showGem)
            {
                v.GemText.text = gem.ToString();
                v.GemText.color = color;
            }
        }

        /// <summary>"완료" / "무료" / "X 먼저 해금" 같은 단일 라벨. 숫자·아이콘은 모두 숨김.</summary>
        public static void PatchSpecial(CostDisplayView v, string label, Color color)
        {
            if (v == null) return;
            v.OreText.gameObject.SetActive(false);
            v.OreIcon.gameObject.SetActive(false);
            v.GemText.gameObject.SetActive(false);
            v.GemIcon.gameObject.SetActive(false);
            v.SpecialText.gameObject.SetActive(true);
            v.SpecialText.text = label;
            v.SpecialText.color = color;
        }

        // ───────────────────────────────────────────────
        private static TextMeshProUGUI MakeText(Transform parent, string name, float size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = "0";
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineRight;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            TMPFontHelper.ApplyDefaultFont(tmp);
            return tmp;
        }

        private static Image MakeIcon(Transform parent, string name, Sprite sprite, float size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(size, size);
            var le = obj.AddComponent<LayoutElement>();
            le.preferredWidth = size;
            le.minWidth = size;
            le.preferredHeight = size;
            var img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            return img;
        }
    }
}

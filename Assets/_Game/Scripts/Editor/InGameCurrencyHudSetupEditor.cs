using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using DrillCorp.UI;
using DrillCorp.Machine;
using DrillCorp.Pickup;

namespace DrillCorp.Editor
{
    /// <summary>
    /// Game 씬의 좌상단에 "광석·보석 세션 카운터" HUD를 자동 생성.
    /// - 광석(아이콘 + 세션 채굴량) : MiningUI 컴포넌트
    /// - 보석(아이콘 + 세션 채집량) : GemCounterUI 컴포넌트
    /// 재실행 시 기존 "CurrencyHud"를 제거 후 재생성 (idempotent).
    /// </summary>
    public static class InGameCurrencyHudSetupEditor
    {
        private const string HudName = "CurrencyHud";
        // v2 로우폴리+글로시 팩 — 철광석(범용 광맥 룩) + 다이아몬드(시안 글로우)
        private const string OreSpritePath = "Assets/_Game/Sprites/UI/05_iron.png";
        private const string GemSpritePath = "Assets/_Game/Sprites/UI/01_diamond.png";
        private const string D2CodingPath  = "Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset";

        private static readonly Color BgColor   = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color OreColor  = new Color(1f, 0.65f, 0.35f, 1f);     // 철광석 광맥 주황
        private static readonly Color GemColor  = new Color(0.53f, 0.87f, 1f, 1f);     // #88ddff

        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/3. 광석·보석 HUD 추가")]
        public static void SetupCurrencyHud()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("CurrencyHud", "Canvas가 없습니다. 먼저 'InGame UI 설정'을 실행하세요.", "확인");
                return;
            }

            // 기존 HUD 제거 — 재실행 idempotent
            var existing = canvas.transform.Find(HudName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // 기존 MiningUI / GemCounterUI 중복 제거 (우상단 레거시 MiningUI 등)
            RemoveExistingCounters<MiningUI>();
            RemoveExistingCounters<GemCounterUI>();

            var hud = BuildHud(canvas.transform);

            // GemDropSpawner GameObject 자동 생성 + 스프라이트 할당
            EnsureGemDropSpawner();

            Debug.Log($"[CurrencyHud] 생성 완료: {hud.name} (좌상단) + GemDropSpawner");

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorUtility.SetDirty(canvas);
            Selection.activeGameObject = hud;
        }

        private static void EnsureGemDropSpawner()
        {
            var spawner = Object.FindAnyObjectByType<GemDropSpawner>();
            if (spawner == null)
            {
                var go = new GameObject("GemDropSpawner");
                spawner = go.AddComponent<GemDropSpawner>();
            }

            var so = new SerializedObject(spawner);
            var spriteProp = so.FindProperty("_gemSprite");
            if (spriteProp != null && spriteProp.objectReferenceValue == null)
                spriteProp.objectReferenceValue = LoadSprite(GemSpritePath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildHud(Transform canvasRoot)
        {
            // 컨테이너 — 우상단 anchor, 20,20 여백 (좌상단은 미니맵이 점유)
            var hud = new GameObject(HudName);
            hud.transform.SetParent(canvasRoot, false);

            var rt = hud.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
            rt.sizeDelta = new Vector2(220f, 80f);

            var bg = hud.AddComponent<Image>();
            bg.color = BgColor;
            bg.raycastTarget = false;

            var vl = hud.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(10, 10, 8, 8);
            vl.spacing = 6f;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            // 둥근 모서리는 기본 Image로는 어려우니 생략 — 필요 시 스프라이트 교체

            var machine = Object.FindAnyObjectByType<MachineController>();

            // 광석 행 — MiningUI (세션 채굴량)
            var oreRow = BuildRow(hud.transform, "OreRow", LoadSprite(OreSpritePath), OreColor, out var oreText);
            var miningUI = oreRow.AddComponent<MiningUI>();
            var soOre = new SerializedObject(miningUI);
            soOre.FindProperty("_miningText").objectReferenceValue = oreText;
            soOre.FindProperty("_prefix").stringValue = "";   // 아이콘이 라벨 역할
            if (machine != null)
                soOre.FindProperty("_machine").objectReferenceValue = machine;
            soOre.ApplyModifiedPropertiesWithoutUndo();

            // 보석 행 — GemCounterUI (세션 채집량)
            var gemRow = BuildRow(hud.transform, "GemRow", LoadSprite(GemSpritePath), GemColor, out var gemText);
            var gemUI = gemRow.AddComponent<GemCounterUI>();
            var soGem = new SerializedObject(gemUI);
            soGem.FindProperty("_gemText").objectReferenceValue = gemText;
            soGem.FindProperty("_prefix").stringValue = "";
            soGem.ApplyModifiedPropertiesWithoutUndo();

            return hud;
        }

        /// <summary>
        /// 한 행: [아이콘 32x32] [수치 텍스트(flex)]
        /// </summary>
        private static GameObject BuildRow(Transform parent, string name, Sprite icon, Color textColor, out TextMeshProUGUI valueText)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);

            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 28f);

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 28f;

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8f;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;

            // 아이콘
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(row.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(28f, 28f);
            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 28f;
            iconLe.preferredHeight = 28f;
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;
            if (icon != null)
                iconImg.sprite = icon;
            else
                iconImg.color = textColor; // sprite 못 찾으면 색 단색 박스로 대체

            // 수치 텍스트
            var textGo = new GameObject("Value");
            textGo.transform.SetParent(row.transform, false);
            textGo.AddComponent<RectTransform>();
            var textLe = textGo.AddComponent<LayoutElement>();
            textLe.flexibleWidth = 1f;
            valueText = textGo.AddComponent<TextMeshProUGUI>();
            valueText.text = "0";
            valueText.fontSize = 22f;
            valueText.color = textColor;
            valueText.alignment = TextAlignmentOptions.MidlineLeft;
            valueText.fontStyle = FontStyles.Bold;
            ApplyD2Coding(valueText);

            return row;
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            // 임포트 설정 자동 보정 — textureType=Sprite + spriteImportMode=Single
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    // Multiple 모드인데 슬라이스가 없으면 LoadAssetAtPath<Sprite>가 null 반환
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (dirty)
                {
                    importer.SaveAndReimport();
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }

            if (sprite == null)
                Debug.LogWarning($"[CurrencyHud] Sprite 로드 실패: {path} — 단색 박스로 대체됩니다.");
            return sprite;
        }

        private static void RemoveExistingCounters<T>() where T : MonoBehaviour
        {
            var found = Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            foreach (var c in found)
            {
                if (c == null) continue;
                Object.DestroyImmediate(c.gameObject);
            }
        }

        private static void ApplyD2Coding(TextMeshProUGUI tmp)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(D2CodingPath);
            if (font != null) tmp.font = font;
        }
    }
}

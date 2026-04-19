using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using DrillCorp.UI;

namespace DrillCorp.Editor
{
    public class UISetupEditor : UnityEditor.Editor
    {
        [MenuItem("Tools/Drill-Corp/3. 게임 초기 설정/2. InGame UI 설정")]
        public static void SetupInGameUI()
        {
            // 기존 Canvas 확인
            Canvas existingCanvas = FindAnyObjectByType<Canvas>();
            if (existingCanvas != null)
            {
                if (!EditorUtility.DisplayDialog("UI Setup",
                    "기존 Canvas가 있습니다. 새로 생성하시겠습니까?",
                    "새로 생성", "취소"))
                {
                    return;
                }
            }

            // Canvas 생성
            GameObject canvasObj = CreateCanvas();

            // InGame Panel
            GameObject inGamePanel = CreateInGamePanel(canvasObj.transform);

            // v2 포팅 이후 AimChargeUI는 제거 (단일 무기 쿨다운 표시는 각 무기의 AimRing·게이지로 대체)

            // Result Panels
            GameObject successPanel = CreateSuccessPanel(canvasObj.transform);
            GameObject failedPanel = CreateFailedPanel(canvasObj.transform);

            // SessionResultUI를 Canvas에 추가 (항상 활성화 상태 유지)
            SessionResultUI sessionResultUI = canvasObj.AddComponent<SessionResultUI>();
            ConnectSessionResultUI(sessionResultUI, successPanel, failedPanel);

            // UIManager 추가 및 연결
            UIManager uiManager = canvasObj.AddComponent<UIManager>();
            SerializedObject so = new SerializedObject(uiManager);
            so.FindProperty("_inGameUI").objectReferenceValue = inGamePanel;
            so.FindProperty("_sessionSuccessUI").objectReferenceValue = successPanel;
            so.FindProperty("_sessionFailedUI").objectReferenceValue = failedPanel;
            so.ApplyModifiedProperties();

            Debug.Log("[UISetupEditor] InGame UI 생성 완료!");
        }

        private static GameObject CreateCanvas()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem 확인
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            return canvasObj;
        }

        private static GameObject CreateInGamePanel(Transform parent)
        {
            GameObject panel = CreatePanel("InGamePanel", parent);

            // Machine Status UI
            GameObject statusUI = new GameObject("MachineStatusUI");
            statusUI.transform.SetParent(panel.transform, false);
            RectTransform statusRect = statusUI.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 1f);
            statusRect.anchorMax = new Vector2(0.5f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0, -20);
            statusRect.sizeDelta = new Vector2(400, 60);

            // HP Bar (배경 + Fill)
            GameObject hpBarGroup = CreateBarWithBackground("HPBar", statusUI.transform,
                new Vector2(0, -5), new Vector2(380, 25),
                new Color(0.2f, 0.2f, 0.2f, 0.8f), new Color(0.2f, 0.8f, 0.2f));
            Image hpFillImage = hpBarGroup.transform.Find("Fill").GetComponent<Image>();
            GameObject hpText = CreateText("HPText", hpBarGroup.transform, "100 / 100", 14, Color.black);

            // Fuel Bar (배경 + Fill)
            GameObject fuelBarGroup = CreateBarWithBackground("FuelBar", statusUI.transform,
                new Vector2(0, -35), new Vector2(380, 25),
                new Color(0.2f, 0.2f, 0.2f, 0.8f), new Color(0.9f, 0.7f, 0.1f));
            Image fuelFillImage = fuelBarGroup.transform.Find("Fill").GetComponent<Image>();
            GameObject fuelText = CreateText("FuelText", fuelBarGroup.transform, "60s", 14, Color.black);

            // MachineStatusUI 컴포넌트 추가
            MachineStatusUI machineStatusUI = statusUI.AddComponent<MachineStatusUI>();
            SerializedObject so = new SerializedObject(machineStatusUI);
            so.FindProperty("_hpFillImage").objectReferenceValue = hpFillImage;
            so.FindProperty("_hpText").objectReferenceValue = hpText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_fuelFillImage").objectReferenceValue = fuelFillImage;
            so.FindProperty("_fuelText").objectReferenceValue = fuelText.GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedProperties();

            // Mining UI
            GameObject miningUI = new GameObject("MiningUI");
            miningUI.transform.SetParent(panel.transform, false);
            RectTransform miningRect = miningUI.AddComponent<RectTransform>();
            miningRect.anchorMin = new Vector2(1f, 1f);
            miningRect.anchorMax = new Vector2(1f, 1f);
            miningRect.pivot = new Vector2(1f, 1f);
            miningRect.anchoredPosition = new Vector2(-20, -20);
            miningRect.sizeDelta = new Vector2(200, 40);

            GameObject miningText = CreateText("MiningText", miningUI.transform, "채굴: 0", 24);
            RectTransform miningTextRect = miningText.GetComponent<RectTransform>();
            miningTextRect.anchorMin = Vector2.zero;
            miningTextRect.anchorMax = Vector2.one;
            miningTextRect.sizeDelta = Vector2.zero;
            miningText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;

            // MiningUI 컴포넌트 추가
            MiningUI miningUIComp = miningUI.AddComponent<MiningUI>();
            SerializedObject miningso = new SerializedObject(miningUIComp);
            miningso.FindProperty("_miningText").objectReferenceValue = miningText.GetComponent<TextMeshProUGUI>();
            miningso.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject CreateSuccessPanel(Transform parent)
        {
            GameObject panel = CreateResultPanel("SuccessPanel", parent, "세션 성공!", new Color(0.1f, 0.5f, 0.1f, 0.9f));

            // Mining Text
            GameObject miningText = CreateText("MiningText", panel.transform, "채굴량: 0", 28);
            RectTransform miningRect = miningText.GetComponent<RectTransform>();
            miningRect.anchoredPosition = new Vector2(0, 20);

            // Currency Text
            GameObject currencyText = CreateText("CurrencyText", panel.transform, "보유 재화: 0", 24);
            RectTransform currencyRect = currencyText.GetComponent<RectTransform>();
            currencyRect.anchoredPosition = new Vector2(0, -20);

            // Continue Button
            CreateButton("ContinueButton", panel.transform, "계속하기", new Vector2(0, -80));

            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateFailedPanel(Transform parent)
        {
            GameObject panel = CreateResultPanel("FailedPanel", parent, "세션 실패...", new Color(0.5f, 0.1f, 0.1f, 0.9f));

            // Mining Text
            GameObject miningText = CreateText("MiningText", panel.transform, "채굴량: 0 (획득 불가)", 24);
            RectTransform miningRect = miningText.GetComponent<RectTransform>();
            miningRect.anchoredPosition = new Vector2(0, 10);

            // Retry Button
            CreateButton("RetryButton", panel.transform, "다시하기", new Vector2(-100, -70));

            // Quit Button
            CreateButton("QuitButton", panel.transform, "나가기", new Vector2(100, -70));

            panel.SetActive(false);
            return panel;
        }

        private static void ConnectSessionResultUI(SessionResultUI resultUI, GameObject successPanel, GameObject failedPanel)
        {
            SerializedObject so = new SerializedObject(resultUI);

            // Success Panel
            so.FindProperty("_successPanel").objectReferenceValue = successPanel;
            so.FindProperty("_successMiningText").objectReferenceValue = successPanel.transform.Find("MiningText")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_successCurrencyText").objectReferenceValue = successPanel.transform.Find("CurrencyText")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_successContinueButton").objectReferenceValue = successPanel.transform.Find("ContinueButton")?.GetComponent<Button>();

            // Failed Panel
            so.FindProperty("_failedPanel").objectReferenceValue = failedPanel;
            so.FindProperty("_failedMiningText").objectReferenceValue = failedPanel.transform.Find("MiningText")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_failedRetryButton").objectReferenceValue = failedPanel.transform.Find("RetryButton")?.GetComponent<Button>();
            so.FindProperty("_failedQuitButton").objectReferenceValue = failedPanel.transform.Find("QuitButton")?.GetComponent<Button>();

            so.ApplyModifiedProperties();
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return panel;
        }

        private static GameObject CreateResultPanel(string name, Transform parent, string title, Color bgColor)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 300);

            Image bg = panel.AddComponent<Image>();
            bg.color = bgColor;

            // Title
            GameObject titleText = CreateText("TitleText", panel.transform, title, 36);
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 100);

            return panel;
        }

        private static GameObject CreateBarWithBackground(string name, Transform parent, Vector2 position, Vector2 size, Color bgColor, Color fillColor)
        {
            // 그룹 컨테이너
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            RectTransform groupRect = group.AddComponent<RectTransform>();
            groupRect.anchorMin = new Vector2(0.5f, 1f);
            groupRect.anchorMax = new Vector2(0.5f, 1f);
            groupRect.pivot = new Vector2(0.5f, 1f);
            groupRect.anchoredPosition = position;
            groupRect.sizeDelta = size;

            // 배경 (단색)
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(group.transform, false);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = bgColor;
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Fill (단색, Filled)
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(group.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = new Vector2(-4, -4);
            fillRect.anchoredPosition = Vector2.zero;

            return group;
        }

        private static GameObject CreateFilledBar(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(parent, false);

            Image image = bar.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillAmount = 1f;

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            return bar;
        }

        private static GameObject CreateText(string name, Transform parent, string text, int fontSize, Color? textColor = null)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor ?? Color.white;

            // D2Coding 폰트 로드
            TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset");
            if (koreanFont != null)
            {
                tmp.font = koreanFont;
            }

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300, 50);

            return textObj;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, Vector2 position)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image image = btnObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button button = btnObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            button.colors = colors;

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(150, 50);

            // Button Text
            GameObject textObj = CreateText("Text", btnObj.transform, text, 20);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return btnObj;
        }
    }
}

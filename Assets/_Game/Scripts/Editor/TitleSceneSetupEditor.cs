using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace DrillCorp.Editor
{
    public class TitleSceneSetupEditor : EditorWindow
    {
        [MenuItem("Tools/Drill-Corp/Title/2. Setup Scene UI")]
        public static void SetupTitleSceneUI()
        {
            // 프리팹이 있는지 확인
            if (!CheckPrefabsExist())
            {
                Debug.LogError("[TitleSceneSetup] Prefabs not found! Run 'Tools > Drill-Corp > Title > 1. Create Prefabs' first.");
                return;
            }

            // EventSystem 확인/생성 (New Input System 사용)
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }

            // Canvas 생성
            var canvasObj = CreateCanvas();
            var canvas = canvasObj.GetComponent<Canvas>();

            // 재화 표시 (우상단)
            var currencyText = CreateCurrencyDisplay(canvasObj.transform);

            // 패널들 생성
            var mainPanel = CreateMainPanel(canvasObj.transform);
            var upgradePanel = CreateUpgradePanel(canvasObj.transform);
            var machinePanel = CreateMachineSelectPanel(canvasObj.transform);
            var optionsPanel = CreateOptionsPanel(canvasObj.transform);

            // TitleUI 컴포넌트 추가 및 연결
            var titleUI = canvasObj.AddComponent<OutGame.TitleUI>();
            ConnectTitleUI(titleUI, mainPanel, upgradePanel, machinePanel, optionsPanel, currencyText);

            // UpgradeUI 연결
            var upgradeUI = upgradePanel.GetComponent<OutGame.UpgradeUI>();
            if (upgradeUI != null)
            {
                ConnectUpgradeUI(upgradeUI, titleUI);
            }

            // MachineSelectUI 연결
            var machineUI = machinePanel.GetComponent<OutGame.MachineSelectUI>();
            if (machineUI != null)
            {
                ConnectMachineSelectUI(machineUI, titleUI);
            }

            // OptionsUI 연결
            var optionsUI = optionsPanel.GetComponent<OutGame.OptionsUI>();
            if (optionsUI != null)
            {
                ConnectOptionsUI(optionsUI, titleUI);
            }

            // 초기 상태: MainPanel만 활성화
            mainPanel.SetActive(true);
            upgradePanel.SetActive(false);
            machinePanel.SetActive(false);
            optionsPanel.SetActive(false);

            // Managers 생성
            CreateManagers();

            // 프리팹 연결
            ConnectPrefabs();

            Debug.Log("[TitleSceneSetup] Title Scene UI created successfully!");
            Debug.Log("[TitleSceneSetup] Don't forget to save the scene!");
        }

        [MenuItem("Tools/Drill-Corp/Title/1. Create Prefabs")]
        public static void CreateTitleUIPrefabs()
        {
            string prefabPath = "Assets/_Game/Prefabs/UI";

            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");

            if (!AssetDatabase.IsValidFolder(prefabPath))
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "UI");

            // UpgradeItem Prefab
            CreateAndSavePrefab(prefabPath, "UpgradeItem", CreateUpgradeItemPrefab);

            // MachineItem Prefab
            CreateAndSavePrefab(prefabPath, "MachineItem", CreateMachineItemPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[TitleSceneSetup] UI Prefabs created at: " + prefabPath);
        }

        private static void CreateAndSavePrefab(string path, string name, System.Func<GameObject> createFunc)
        {
            string fullPath = $"{path}/{name}.prefab";

            if (System.IO.File.Exists(fullPath))
            {
                Debug.Log($"[TitleSceneSetup] {name}.prefab already exists, skipping.");
                return;
            }

            // 임시 오브젝트 생성
            var tempObj = createFunc();
            tempObj.name = name;

            // 프리팹으로 저장
            var prefab = PrefabUtility.SaveAsPrefabAsset(tempObj, fullPath);

            // 씬의 임시 오브젝트 삭제
            Object.DestroyImmediate(tempObj);

            // Inspector 선택을 생성한 프리팹으로 변경 (참조 문제 해결)
            Selection.activeObject = prefab;

            Debug.Log($"[TitleSceneSetup] Created: {name}.prefab");
        }

        private static bool CheckPrefabsExist()
        {
            string prefabPath = "Assets/_Game/Prefabs/UI";
            bool upgradeExists = System.IO.File.Exists($"{prefabPath}/UpgradeItem.prefab");
            bool machineExists = System.IO.File.Exists($"{prefabPath}/MachineItem.prefab");
            return upgradeExists && machineExists;
        }

        private static void ConnectPrefabs()
        {
            string prefabPath = "Assets/_Game/Prefabs/UI";

            // UpgradeUI에 프리팹 연결
            var upgradeUI = Object.FindFirstObjectByType<OutGame.UpgradeUI>();
            if (upgradeUI != null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{prefabPath}/UpgradeItem.prefab");
                var so = new SerializedObject(upgradeUI);
                so.FindProperty("_upgradeItemPrefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // MachineSelectUI에 프리팹 및 데이터 연결
            var machineUI = Object.FindFirstObjectByType<OutGame.MachineSelectUI>();
            if (machineUI != null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{prefabPath}/MachineItem.prefab");
                var so = new SerializedObject(machineUI);
                so.FindProperty("_machineItemPrefab").objectReferenceValue = prefab;

                // Machine Data 연결
                var machinesProp = so.FindProperty("_availableMachines");
                string[] machineNames = { "Machine_Default", "Machine_Heavy", "Machine_Speed" };
                machinesProp.arraySize = machineNames.Length;
                for (int i = 0; i < machineNames.Length; i++)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Data.MachineData>($"Assets/_Game/Data/Machines/{machineNames[i]}.asset");
                    machinesProp.GetArrayElementAtIndex(i).objectReferenceValue = asset;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject CreateCanvas()
        {
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // TMP용 채널 설정
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;

            return canvasObj;
        }

        private static TextMeshProUGUI CreateCurrencyDisplay(Transform parent)
        {
            var currencyObj = new GameObject("CurrencyText");
            currencyObj.transform.SetParent(parent, false);

            var rect = currencyObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-30, -30);
            rect.sizeDelta = new Vector2(300, 50);

            var text = currencyObj.AddComponent<TextMeshProUGUI>();
            text.text = "0";
            text.fontSize = 36;
            text.alignment = TextAlignmentOptions.Right;
            text.color = Color.white;

            return text;
        }

        private static GameObject CreateMainPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MainPanel");

            // 배경
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // 타이틀
            var titleText = CreateText(panel.transform, "TitleText", "DRILL-CORP", 72);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 250);

            // 서브타이틀
            var subText = CreateText(panel.transform, "SubTitleText", "Mining Defense Survival", 24);
            var subRect = subText.GetComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0, 180);
            subText.color = new Color(0.7f, 0.7f, 0.7f);

            // 버튼들
            float buttonY = 50;
            float buttonSpacing = 70;

            CreateButton(panel.transform, "StartButton", "START", new Vector2(0, buttonY));
            CreateButton(panel.transform, "UpgradeButton", "UPGRADE", new Vector2(0, buttonY - buttonSpacing));
            CreateButton(panel.transform, "MachineButton", "MACHINE", new Vector2(0, buttonY - buttonSpacing * 2));
            CreateButton(panel.transform, "OptionsButton", "OPTIONS", new Vector2(0, buttonY - buttonSpacing * 3));
            CreateButton(panel.transform, "QuitButton", "QUIT", new Vector2(0, buttonY - buttonSpacing * 4));

            return panel;
        }

        private static GameObject CreateUpgradePanel(Transform parent)
        {
            var panel = CreatePanel(parent, "UpgradePanel");
            panel.AddComponent<OutGame.UpgradeUI>();

            // 배경
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // 헤더
            var headerText = CreateText(panel.transform, "HeaderText", "UPGRADES", 48);
            var headerRect = headerText.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1);
            headerRect.anchorMax = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -60);

            // 재화 표시
            var currencyText = CreateText(panel.transform, "CurrencyText", "0", 32);
            var currencyRect = currencyText.GetComponent<RectTransform>();
            currencyRect.anchorMin = new Vector2(1, 1);
            currencyRect.anchorMax = new Vector2(1, 1);
            currencyRect.pivot = new Vector2(1, 1);
            currencyRect.anchoredPosition = new Vector2(-30, -50);
            currencyRect.sizeDelta = new Vector2(200, 40);
            currencyText.alignment = TextAlignmentOptions.Right;

            // 스크롤뷰
            var scrollView = CreateScrollView(panel.transform, "UpgradeScrollView");
            var scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.1f, 0.15f);
            scrollRect.anchorMax = new Vector2(0.9f, 0.85f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            // Back 버튼
            var backBtn = CreateButton(panel.transform, "BackButton", "BACK", Vector2.zero);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0);
            backRect.anchorMax = new Vector2(0.5f, 0);
            backRect.anchoredPosition = new Vector2(0, 60);

            return panel;
        }

        private static GameObject CreateMachineSelectPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MachineSelectPanel");
            panel.AddComponent<OutGame.MachineSelectUI>();

            // 배경
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // 헤더
            var headerText = CreateText(panel.transform, "HeaderText", "SELECT MACHINE", 48);
            var headerRect = headerText.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1);
            headerRect.anchorMax = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -60);

            // 머신 리스트 컨테이너 (왼쪽)
            var listContainer = new GameObject("MachineListContainer");
            listContainer.transform.SetParent(panel.transform, false);
            var listRect = listContainer.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.05f, 0.2f);
            listRect.anchorMax = new Vector2(0.45f, 0.85f);
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = Vector2.zero;

            var listLayout = listContainer.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 10;
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            // 선택 정보 패널 (오른쪽)
            var infoPanel = new GameObject("SelectedInfoPanel");
            infoPanel.transform.SetParent(panel.transform, false);
            var infoRect = infoPanel.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.5f, 0.2f);
            infoRect.anchorMax = new Vector2(0.95f, 0.85f);
            infoRect.offsetMin = Vector2.zero;
            infoRect.offsetMax = Vector2.zero;

            var infoBg = infoPanel.AddComponent<Image>();
            infoBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // 선택 정보 텍스트들
            var nameText = CreateText(infoPanel.transform, "SelectedNameText", "Machine Name", 36);
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.anchoredPosition = new Vector2(0, -20);
            nameRect.sizeDelta = new Vector2(0, 50);

            var descText = CreateText(infoPanel.transform, "SelectedDescText", "Description", 20);
            var descRect = descText.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 1);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.pivot = new Vector2(0.5f, 1);
            descRect.anchoredPosition = new Vector2(0, -80);
            descRect.sizeDelta = new Vector2(-40, 40);
            descText.color = new Color(0.7f, 0.7f, 0.7f);

            var statsText = CreateText(infoPanel.transform, "SelectedStatsText", "HP: 100\nDamage: 20", 24);
            var statsRect = statsText.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 0.1f);
            statsRect.anchorMax = new Vector2(1, 0.7f);
            statsRect.offsetMin = new Vector2(20, 0);
            statsRect.offsetMax = new Vector2(-20, 0);
            statsText.alignment = TextAlignmentOptions.TopLeft;

            // Back 버튼
            var backBtn = CreateButton(panel.transform, "BackButton", "BACK", Vector2.zero);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0);
            backRect.anchorMax = new Vector2(0.5f, 0);
            backRect.anchoredPosition = new Vector2(0, 60);

            return panel;
        }

        private static GameObject CreateOptionsPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "OptionsPanel");
            panel.AddComponent<OutGame.OptionsUI>();

            // 배경
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            // 헤더
            var headerText = CreateText(panel.transform, "HeaderText", "OPTIONS", 48);
            var headerRect = headerText.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1);
            headerRect.anchorMax = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -60);

            // 오디오 섹션
            float startY = 200;
            float spacing = 80;

            CreateSliderOption(panel.transform, "MasterVolumeSlider", "Master Volume", new Vector2(0, startY));
            CreateSliderOption(panel.transform, "BGMVolumeSlider", "BGM Volume", new Vector2(0, startY - spacing));
            CreateSliderOption(panel.transform, "SFXVolumeSlider", "SFX Volume", new Vector2(0, startY - spacing * 2));

            // 풀스크린 토글
            CreateToggleOption(panel.transform, "FullscreenToggle", "Fullscreen", new Vector2(0, startY - spacing * 3));

            // 품질 드롭다운
            CreateDropdownOption(panel.transform, "QualityDropdown", "Quality", new Vector2(0, startY - spacing * 4));

            // 언어 드롭다운
            CreateDropdownOption(panel.transform, "LanguageDropdown", "Language", new Vector2(0, startY - spacing * 5));

            // Reset Data 버튼
            var resetBtn = CreateButton(panel.transform, "ResetDataButton", "RESET DATA", new Vector2(-150, -280));
            var resetBtnImg = resetBtn.GetComponent<Image>();
            resetBtnImg.color = new Color(0.8f, 0.3f, 0.3f);

            // Apply 버튼
            CreateButton(panel.transform, "ApplyButton", "APPLY", new Vector2(150, -280));

            // Back 버튼
            var backBtn = CreateButton(panel.transform, "BackButton", "BACK", Vector2.zero);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0);
            backRect.anchorMax = new Vector2(0.5f, 0);
            backRect.anchoredPosition = new Vector2(0, 60);

            return panel;
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return panel;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            var rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 80);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return text;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 position)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 60);

            var image = btnObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.4f, 0.6f);

            var button = btnObj.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.7f);
            colors.pressedColor = new Color(0.15f, 0.3f, 0.5f);
            button.colors = colors;

            // 버튼 텍스트
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return btnObj;
        }

        private static GameObject CreateScrollView(Transform parent, string name)
        {
            var scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);

            var scrollRect = scrollObj.AddComponent<RectTransform>();
            var scroll = scrollObj.AddComponent<ScrollRect>();

            var scrollImage = scrollObj.AddComponent<Image>();
            scrollImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);

            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>();

            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;

            return scrollObj;
        }

        private static void CreateSliderOption(Transform parent, string name, string label, Vector2 position)
        {
            var container = new GameObject(name + "Container");
            container.transform.SetParent(parent, false);

            var rect = container.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(600, 50);

            // Label
            var labelText = CreateText(container.transform, "Label", label, 24);
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0.3f, 0.5f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelText.alignment = TextAlignmentOptions.Left;

            // Slider
            var sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(container.transform, false);

            var sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.35f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.85f, 0.5f);
            sliderRect.sizeDelta = new Vector2(0, 20);

            var sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(0.3f, 0.3f, 0.3f);

            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 1;

            // Fill Area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.6f, 0.9f);

            slider.fillRect = fillRect;

            // Value Text
            var valueText = CreateText(container.transform, name.Replace("Slider", "Text"), "100%", 24);
            var valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.88f, 0.5f);
            valueRect.anchorMax = new Vector2(1f, 0.5f);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            valueText.alignment = TextAlignmentOptions.Right;
        }

        private static void CreateToggleOption(Transform parent, string name, string label, Vector2 position)
        {
            var container = new GameObject(name + "Container");
            container.transform.SetParent(parent, false);

            var rect = container.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(600, 50);

            // Label
            var labelText = CreateText(container.transform, "Label", label, 24);
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0.3f, 0.5f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelText.alignment = TextAlignmentOptions.Left;

            // Toggle
            var toggleObj = new GameObject(name);
            toggleObj.transform.SetParent(container.transform, false);

            var toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.35f, 0.5f);
            toggleRect.anchorMax = new Vector2(0.35f, 0.5f);
            toggleRect.sizeDelta = new Vector2(40, 40);

            var toggleBg = toggleObj.AddComponent<Image>();
            toggleBg.color = new Color(0.3f, 0.3f, 0.3f);

            var toggle = toggleObj.AddComponent<Toggle>();

            // Checkmark
            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleObj.transform, false);
            var checkRect = checkmark.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.8f, 0.3f);

            toggle.graphic = checkImage;
            toggle.isOn = true;
        }

        private static void CreateDropdownOption(Transform parent, string name, string label, Vector2 position)
        {
            var container = new GameObject(name + "Container");
            container.transform.SetParent(parent, false);

            var rect = container.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(600, 50);

            // Label
            var labelText = CreateText(container.transform, "Label", label, 24);
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0.3f, 0.5f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelText.alignment = TextAlignmentOptions.Left;

            // Dropdown
            var dropdownObj = new GameObject(name);
            dropdownObj.transform.SetParent(container.transform, false);

            var dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0.35f, 0.5f);
            dropdownRect.anchorMax = new Vector2(0.85f, 0.5f);
            dropdownRect.sizeDelta = new Vector2(0, 40);

            var dropdownBg = dropdownObj.AddComponent<Image>();
            dropdownBg.color = new Color(0.25f, 0.25f, 0.3f);

            var dropdown = dropdownObj.AddComponent<TMP_Dropdown>();

            // Label
            var ddLabel = new GameObject("Label");
            ddLabel.transform.SetParent(dropdownObj.transform, false);
            var ddLabelRect = ddLabel.AddComponent<RectTransform>();
            ddLabelRect.anchorMin = Vector2.zero;
            ddLabelRect.anchorMax = Vector2.one;
            ddLabelRect.offsetMin = new Vector2(10, 0);
            ddLabelRect.offsetMax = new Vector2(-30, 0);
            var ddLabelText = ddLabel.AddComponent<TextMeshProUGUI>();
            ddLabelText.text = "Option";
            ddLabelText.fontSize = 20;
            ddLabelText.alignment = TextAlignmentOptions.Left;
            ddLabelText.color = Color.white;

            dropdown.captionText = ddLabelText;

            // Template (simplified)
            var template = new GameObject("Template");
            template.transform.SetParent(dropdownObj.transform, false);
            var templateRect = template.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.sizeDelta = new Vector2(0, 150);
            template.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 30);

            var item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);
            var itemRect = item.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 30);
            item.AddComponent<Toggle>();

            var itemLabel = new GameObject("Item Label");
            itemLabel.transform.SetParent(item.transform, false);
            var itemLabelRect = itemLabel.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 0);
            itemLabelRect.offsetMax = new Vector2(-10, 0);
            var itemLabelText = itemLabel.AddComponent<TextMeshProUGUI>();
            itemLabelText.fontSize = 18;
            itemLabelText.color = Color.white;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabelText;

            template.SetActive(false);
        }

        private static void ConnectTitleUI(OutGame.TitleUI titleUI, GameObject mainPanel,
            GameObject upgradePanel, GameObject machinePanel, GameObject optionsPanel, TextMeshProUGUI currencyText)
        {
            var so = new SerializedObject(titleUI);

            so.FindProperty("_mainPanel").objectReferenceValue = mainPanel;
            so.FindProperty("_upgradePanel").objectReferenceValue = upgradePanel;
            so.FindProperty("_machineSelectPanel").objectReferenceValue = machinePanel;
            so.FindProperty("_optionsPanel").objectReferenceValue = optionsPanel;

            so.FindProperty("_startButton").objectReferenceValue = mainPanel.transform.Find("StartButton")?.GetComponent<Button>();
            so.FindProperty("_upgradeButton").objectReferenceValue = mainPanel.transform.Find("UpgradeButton")?.GetComponent<Button>();
            so.FindProperty("_machineButton").objectReferenceValue = mainPanel.transform.Find("MachineButton")?.GetComponent<Button>();
            so.FindProperty("_optionsButton").objectReferenceValue = mainPanel.transform.Find("OptionsButton")?.GetComponent<Button>();
            so.FindProperty("_quitButton").objectReferenceValue = mainPanel.transform.Find("QuitButton")?.GetComponent<Button>();

            so.FindProperty("_currencyText").objectReferenceValue = currencyText;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConnectUpgradeUI(OutGame.UpgradeUI upgradeUI, OutGame.TitleUI titleUI)
        {
            var so = new SerializedObject(upgradeUI);
            var panel = ((Component)upgradeUI).gameObject;

            var scrollView = panel.transform.Find("UpgradeScrollView");
            if (scrollView != null)
            {
                var content = scrollView.Find("Viewport/Content");
                so.FindProperty("_upgradeListContainer").objectReferenceValue = content;
            }

            so.FindProperty("_backButton").objectReferenceValue = panel.transform.Find("BackButton")?.GetComponent<Button>();
            so.FindProperty("_titleUI").objectReferenceValue = titleUI;
            so.FindProperty("_currencyText").objectReferenceValue = panel.transform.Find("CurrencyText")?.GetComponent<TextMeshProUGUI>();

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConnectMachineSelectUI(OutGame.MachineSelectUI machineUI, OutGame.TitleUI titleUI)
        {
            var so = new SerializedObject(machineUI);
            var panel = ((Component)machineUI).gameObject;

            so.FindProperty("_machineListContainer").objectReferenceValue = panel.transform.Find("MachineListContainer");
            so.FindProperty("_backButton").objectReferenceValue = panel.transform.Find("BackButton")?.GetComponent<Button>();
            so.FindProperty("_titleUI").objectReferenceValue = titleUI;

            var infoPanel = panel.transform.Find("SelectedInfoPanel");
            if (infoPanel != null)
            {
                so.FindProperty("_selectedNameText").objectReferenceValue = infoPanel.Find("SelectedNameText")?.GetComponent<TextMeshProUGUI>();
                so.FindProperty("_selectedDescText").objectReferenceValue = infoPanel.Find("SelectedDescText")?.GetComponent<TextMeshProUGUI>();
                so.FindProperty("_selectedStatsText").objectReferenceValue = infoPanel.Find("SelectedStatsText")?.GetComponent<TextMeshProUGUI>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConnectOptionsUI(OutGame.OptionsUI optionsUI, OutGame.TitleUI titleUI)
        {
            var so = new SerializedObject(optionsUI);
            var panel = ((Component)optionsUI).gameObject;

            so.FindProperty("_backButton").objectReferenceValue = panel.transform.Find("BackButton")?.GetComponent<Button>();
            so.FindProperty("_titleUI").objectReferenceValue = titleUI;

            // Sliders
            var masterContainer = panel.transform.Find("MasterVolumeSliderContainer");
            if (masterContainer != null)
            {
                so.FindProperty("_masterVolumeSlider").objectReferenceValue = masterContainer.Find("MasterVolumeSlider")?.GetComponent<Slider>();
                so.FindProperty("_masterVolumeText").objectReferenceValue = masterContainer.Find("MasterVolumeText")?.GetComponent<TextMeshProUGUI>();
            }

            var bgmContainer = panel.transform.Find("BGMVolumeSliderContainer");
            if (bgmContainer != null)
            {
                so.FindProperty("_bgmVolumeSlider").objectReferenceValue = bgmContainer.Find("BGMVolumeSlider")?.GetComponent<Slider>();
                so.FindProperty("_bgmVolumeText").objectReferenceValue = bgmContainer.Find("BGMVolumeText")?.GetComponent<TextMeshProUGUI>();
            }

            var sfxContainer = panel.transform.Find("SFXVolumeSliderContainer");
            if (sfxContainer != null)
            {
                so.FindProperty("_sfxVolumeSlider").objectReferenceValue = sfxContainer.Find("SFXVolumeSlider")?.GetComponent<Slider>();
                so.FindProperty("_sfxVolumeText").objectReferenceValue = sfxContainer.Find("SFXVolumeText")?.GetComponent<TextMeshProUGUI>();
            }

            // Toggle
            var fullscreenContainer = panel.transform.Find("FullscreenToggleContainer");
            if (fullscreenContainer != null)
            {
                so.FindProperty("_fullscreenToggle").objectReferenceValue = fullscreenContainer.Find("FullscreenToggle")?.GetComponent<Toggle>();
            }

            // Dropdowns
            var qualityContainer = panel.transform.Find("QualityDropdownContainer");
            if (qualityContainer != null)
            {
                so.FindProperty("_qualityDropdown").objectReferenceValue = qualityContainer.Find("QualityDropdown")?.GetComponent<TMP_Dropdown>();
            }

            var languageContainer = panel.transform.Find("LanguageDropdownContainer");
            if (languageContainer != null)
            {
                so.FindProperty("_languageDropdown").objectReferenceValue = languageContainer.Find("LanguageDropdown")?.GetComponent<TMP_Dropdown>();
            }

            // Buttons
            so.FindProperty("_resetDataButton").objectReferenceValue = panel.transform.Find("ResetDataButton")?.GetComponent<Button>();
            so.FindProperty("_applyButton").objectReferenceValue = panel.transform.Find("ApplyButton")?.GetComponent<Button>();

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateManagers()
        {
            // DontDestroyOnLoad는 루트 오브젝트에서만 동작하므로 각각 루트에 생성

            // UpgradeManager
            if (Object.FindFirstObjectByType<OutGame.UpgradeManager>() == null)
            {
                var upgradeManagerObj = new GameObject("UpgradeManager");
                var upgradeManager = upgradeManagerObj.AddComponent<OutGame.UpgradeManager>();

                // 업그레이드 에셋 연결
                var so = new SerializedObject(upgradeManager);
                var upgradesProp = so.FindProperty("_availableUpgrades");

                string[] upgradeNames = {
                    "Upgrade_MaxHealth",
                    "Upgrade_Armor",
                    "Upgrade_MiningRate",
                    "Upgrade_AttackDamage",
                    "Upgrade_AttackSpeed",
                    "Upgrade_FuelEfficiency"
                };

                upgradesProp.arraySize = upgradeNames.Length;
                for (int i = 0; i < upgradeNames.Length; i++)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Data.UpgradeData>($"Assets/_Game/Data/Upgrades/{upgradeNames[i]}.asset");
                    upgradesProp.GetArrayElementAtIndex(i).objectReferenceValue = asset;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // GameManager (TitleScene에서 시작할 경우 필요) - 루트에 생성
            if (Object.FindFirstObjectByType<Core.GameManager>() == null)
            {
                var gameManagerObj = new GameObject("GameManager");
                gameManagerObj.AddComponent<Core.GameManager>();
            }

            // DataManager - 루트에 생성
            if (Object.FindFirstObjectByType<Core.DataManager>() == null)
            {
                var dataManagerObj = new GameObject("DataManager");
                dataManagerObj.AddComponent<Core.DataManager>();
            }
        }

        private static GameObject CreateUpgradeItemPrefab()
        {
            var item = new GameObject("UpgradeItem");

            var rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 100);

            var layout = item.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 15;
            layout.padding = new RectOffset(15, 15, 10, 10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f);

            // Icon
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(item.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(80, 80);
            var iconLayout = iconObj.AddComponent<LayoutElement>();
            iconLayout.minWidth = 80;
            iconLayout.preferredWidth = 80;
            var iconImage = iconObj.AddComponent<Image>();
            iconImage.color = new Color(0.4f, 0.4f, 0.5f);

            // Info Container
            var infoContainer = new GameObject("InfoContainer");
            infoContainer.transform.SetParent(item.transform, false);
            var infoRect = infoContainer.AddComponent<RectTransform>();
            var infoLayout = infoContainer.AddComponent<LayoutElement>();
            infoLayout.flexibleWidth = 1;
            var infoVLayout = infoContainer.AddComponent<VerticalLayoutGroup>();
            infoVLayout.childControlHeight = false;
            infoVLayout.childForceExpandHeight = false;

            // Name
            var nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(infoContainer.transform, false);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Upgrade Name";
            nameText.fontSize = 24;
            nameText.color = Color.white;
            var nameLayout = nameObj.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = 30;

            // Description
            var descObj = new GameObject("DescriptionText");
            descObj.transform.SetParent(infoContainer.transform, false);
            var descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = "Description";
            descText.fontSize = 16;
            descText.color = new Color(0.7f, 0.7f, 0.7f);
            var descLayout = descObj.AddComponent<LayoutElement>();
            descLayout.preferredHeight = 20;

            // Level & Value
            var levelObj = new GameObject("LevelText");
            levelObj.transform.SetParent(infoContainer.transform, false);
            var levelText = levelObj.AddComponent<TextMeshProUGUI>();
            levelText.text = "Lv. 0 / 10  |  +0 → +10";
            levelText.fontSize = 18;
            levelText.color = new Color(0.5f, 0.8f, 1f);
            var levelLayout = levelObj.AddComponent<LayoutElement>();
            levelLayout.preferredHeight = 25;

            // Cost & Button Container
            var buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(item.transform, false);
            var btnContainerRect = buttonContainer.AddComponent<RectTransform>();
            var btnContainerLayout = buttonContainer.AddComponent<LayoutElement>();
            btnContainerLayout.minWidth = 120;
            btnContainerLayout.preferredWidth = 120;
            var btnVLayout = buttonContainer.AddComponent<VerticalLayoutGroup>();
            btnVLayout.childAlignment = TextAnchor.MiddleCenter;
            btnVLayout.spacing = 5;

            // Cost
            var costObj = new GameObject("CostText");
            costObj.transform.SetParent(buttonContainer.transform, false);
            var costText = costObj.AddComponent<TextMeshProUGUI>();
            costText.text = "100";
            costText.fontSize = 20;
            costText.alignment = TextAlignmentOptions.Center;
            costText.color = new Color(1f, 0.9f, 0.4f);

            // Upgrade Button
            var btnObj = new GameObject("UpgradeButton");
            btnObj.transform.SetParent(buttonContainer.transform, false);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(100, 40);
            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.6f, 0.3f);
            btnObj.AddComponent<Button>();
            var btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.preferredHeight = 40;

            var btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            var btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "UPGRADE";
            btnText.fontSize = 16;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;

            // UpgradeItemUI 컴포넌트 추가
            var itemUI = item.AddComponent<OutGame.UpgradeItemUI>();
            var so = new SerializedObject(itemUI);
            so.FindProperty("_iconImage").objectReferenceValue = iconImage;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_descriptionText").objectReferenceValue = descText;
            so.FindProperty("_levelText").objectReferenceValue = levelText;
            so.FindProperty("_costText").objectReferenceValue = costText;
            so.FindProperty("_upgradeButton").objectReferenceValue = btnObj.GetComponent<Button>();
            so.FindProperty("_buttonImage").objectReferenceValue = btnImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            return item;
        }

        private static GameObject CreateMachineItemPrefab()
        {
            var item = new GameObject("MachineItem");

            var rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 120);

            var layout = item.AddComponent<LayoutElement>();
            layout.preferredHeight = 120;

            var bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f);

            var hLayout = item.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 15;
            hLayout.padding = new RectOffset(15, 15, 10, 10);
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;

            // Icon
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(item.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(100, 100);
            var iconLayout = iconObj.AddComponent<LayoutElement>();
            iconLayout.minWidth = 100;
            iconLayout.preferredWidth = 100;
            var iconImage = iconObj.AddComponent<Image>();
            iconImage.color = new Color(0.4f, 0.4f, 0.5f);

            // Info Container
            var infoContainer = new GameObject("InfoContainer");
            infoContainer.transform.SetParent(item.transform, false);
            var infoLayout = infoContainer.AddComponent<LayoutElement>();
            infoLayout.flexibleWidth = 1;
            var infoVLayout = infoContainer.AddComponent<VerticalLayoutGroup>();
            infoVLayout.childControlHeight = false;
            infoVLayout.spacing = 5;

            // Name
            var nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(infoContainer.transform, false);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Machine Name";
            nameText.fontSize = 28;
            nameText.color = Color.white;
            var nameLayout = nameObj.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = 35;

            // Brief Stats
            var statsObj = new GameObject("BriefStatsText");
            statsObj.transform.SetParent(infoContainer.transform, false);
            var statsText = statsObj.AddComponent<TextMeshProUGUI>();
            statsText.text = "HP: 100  DMG: 20";
            statsText.fontSize = 20;
            statsText.color = new Color(0.7f, 0.7f, 0.7f);
            var statsLayout = statsObj.AddComponent<LayoutElement>();
            statsLayout.preferredHeight = 25;

            // Selected Indicator
            var indicatorObj = new GameObject("SelectedIndicator");
            indicatorObj.transform.SetParent(item.transform, false);
            var indicatorRect = indicatorObj.AddComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(1, 0.5f);
            indicatorRect.anchorMax = new Vector2(1, 0.5f);
            indicatorRect.sizeDelta = new Vector2(30, 30);
            indicatorRect.anchoredPosition = new Vector2(-25, 0);
            var indicatorImage = indicatorObj.AddComponent<Image>();
            indicatorImage.color = new Color(0.3f, 0.8f, 0.3f);

            // Select Button (전체 아이템)
            var selectBtn = item.AddComponent<Button>();

            // MachineItemUI 컴포넌트 추가
            var itemUI = item.AddComponent<OutGame.MachineItemUI>();
            var so = new SerializedObject(itemUI);
            so.FindProperty("_iconImage").objectReferenceValue = iconImage;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_briefStatsText").objectReferenceValue = statsText;
            so.FindProperty("_selectButton").objectReferenceValue = selectBtn;
            so.FindProperty("_selectedIndicator").objectReferenceValue = indicatorObj;
            so.ApplyModifiedPropertiesWithoutUndo();

            return item;
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrillCorp.Editor
{
    public static class BuildTitleUiMenu
    {
        private const string TitleScenePath = "Assets/_Game/Scenes/Title.unity";
        private const string TitleLandingPanelName = "TitleLandingPanel";

        [MenuItem("Drill-Corp/UI/Title/Apply Title UI", priority = 10)]
        public static void ApplyTitleUi()
        {
            if (!EnsureTitleSceneReady())
                return;

            ApplyProject16x9Settings();
            TitleLandingSetupEditor.ApplyTitleLandingScreen();
            FocusTitleLandingPanel();
        }

        [MenuItem("Drill-Corp/UI/Title/Apply Upgrade UI", priority = 20)]
        public static void ApplyUpgradeUi()
        {
            if (!EnsureTitleSceneReady())
                return;

            ApplyProject16x9Settings();
            V2HubCanvasSetupEditor.BuildHubCanvas();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            FocusPanel("HubPanel");
            Debug.Log("[BuildTitleUI] UPGRADES 화면 구조 적용 및 Title 씬 저장 완료.");
        }

        [MenuItem("Drill-Corp/UI/Title/Open Title Scene", priority = 100)]
        public static void OpenTitleScene()
        {
            if (!EnsureTitleSceneReady())
                return;

            FocusCanvas();
        }

        private static bool EnsureTitleSceneReady()
        {
            if (SceneManager.GetActiveScene().path == TitleScenePath)
                return true;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            EditorSceneManager.OpenScene(TitleScenePath);
            return true;
        }

        private static void ApplyProject16x9Settings()
        {
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        }

        private static void FocusTitleLandingPanel()
        {
            FocusPanel(TitleLandingPanelName);
        }

        private static void FocusPanel(string panelName)
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            var panel = canvas.transform.Find(panelName);
            var target = panel != null ? panel.gameObject : canvas.gameObject;
            Selection.activeGameObject = target;
            EditorGUIUtility.PingObject(target);
        }

        private static void FocusCanvas()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            Selection.activeGameObject = canvas.gameObject;
            EditorGUIUtility.PingObject(canvas.gameObject);
        }
    }
}

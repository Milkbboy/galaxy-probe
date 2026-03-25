using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrillCorp.Core
{
    public enum GameState
    {
        Title,
        Playing,
        Paused,
        SessionSuccess,
        SessionFailed
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Title;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            GameEvents.OnGameStateChanged?.Invoke(newState);
        }

        public void StartSession()
        {
            ChangeState(GameState.Playing);
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;

            ChangeState(GameState.Paused);
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;

            ChangeState(GameState.Playing);
            Time.timeScale = 1f;
        }

        public void SessionSuccess()
        {
            Time.timeScale = 1f;
            ChangeState(GameState.SessionSuccess);
            GameEvents.OnSessionSuccess?.Invoke();
        }

        public void SessionFailed()
        {
            Time.timeScale = 1f;
            ChangeState(GameState.SessionFailed);
            GameEvents.OnSessionFailed?.Invoke();
        }

        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        public void RestartSession()
        {
            LoadScene(SceneManager.GetActiveScene().name);
            ChangeState(GameState.Playing);
        }

        // 씬 전환 헬퍼 메서드들
        public void LoadTitleScene()
        {
            ChangeState(GameState.Title);
            LoadScene("Title");
        }

        public void LoadGameScene()
        {
            ChangeState(GameState.Playing);
            LoadScene("Game");
        }

        public void ReturnToTitle()
        {
            LoadTitleScene();
        }

        // 선택된 머신 ID 저장/로드
        private int _selectedMachineId = 1;
        public int SelectedMachineId
        {
            get => _selectedMachineId;
            set
            {
                _selectedMachineId = value;
                PlayerPrefs.SetInt("SelectedMachine", value);
                PlayerPrefs.Save();
            }
        }

        private void Start()
        {
            _selectedMachineId = PlayerPrefs.GetInt("SelectedMachine", 1);
        }
    }
}

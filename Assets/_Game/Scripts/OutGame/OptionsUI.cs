using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Audio;

namespace DrillCorp.OutGame
{
    public class OptionsUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private TitleUI _titleUI;

        [Header("Audio")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _bgmVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI _masterVolumeText;
        [SerializeField] private TextMeshProUGUI _bgmVolumeText;
        [SerializeField] private TextMeshProUGUI _sfxVolumeText;

        [Header("Graphics")]
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private TMP_Dropdown _qualityDropdown;

        [Header("Language")]
        [SerializeField] private TMP_Dropdown _languageDropdown;

        [Header("Buttons")]
        [SerializeField] private Button _resetDataButton;
        [SerializeField] private Button _applyButton;

        private void Start()
        {
            SetupButtons();
            SetupSliders();
            LoadSettings();
        }

        private void SetupButtons()
        {
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);

            if (_resetDataButton != null)
                _resetDataButton.onClick.AddListener(OnResetDataClicked);

            if (_applyButton != null)
                _applyButton.onClick.AddListener(OnApplyClicked);
        }

        private void SetupSliders()
        {
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (_bgmVolumeSlider != null)
            {
                _bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }
        }

        private void LoadSettings()
        {
            // Audio
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.value = masterVolume;
                UpdateVolumeText(_masterVolumeText, masterVolume);
            }

            if (_bgmVolumeSlider != null)
            {
                _bgmVolumeSlider.value = bgmVolume;
                UpdateVolumeText(_bgmVolumeText, bgmVolume);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.value = sfxVolume;
                UpdateVolumeText(_sfxVolumeText, sfxVolume);
            }

            // Fullscreen
            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.isOn = Screen.fullScreen;
            }

            // Quality
            if (_qualityDropdown != null)
            {
                _qualityDropdown.ClearOptions();
                _qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
                _qualityDropdown.value = QualitySettings.GetQualityLevel();
            }

            // Language
            if (_languageDropdown != null)
            {
                _languageDropdown.ClearOptions();
                _languageDropdown.AddOptions(new System.Collections.Generic.List<string> { "한국어", "English" });
                int langIndex = PlayerPrefs.GetInt("Language", 0);
                _languageDropdown.value = langIndex;
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("MasterVolume", value);
            UpdateVolumeText(_masterVolumeText, value);
            AudioListener.volume = value;
        }

        private void OnBGMVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("BGMVolume", value);
            UpdateVolumeText(_bgmVolumeText, value);
            // BGM AudioSource volume 적용은 AudioManager에서 처리
        }

        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            UpdateVolumeText(_sfxVolumeText, value);
            AudioManager.Instance?.SetSfxVolume(value);
        }

        private void UpdateVolumeText(TextMeshProUGUI text, float value)
        {
            if (text != null)
            {
                text.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        private void OnResetDataClicked()
        {
            // 확인 다이얼로그 표시하는 것이 좋지만 일단 바로 초기화
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // UpgradeManager 초기화
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.ResetAllUpgrades();
            }

            LoadSettings();
            Debug.Log("[OptionsUI] All data has been reset");
        }

        private void OnApplyClicked()
        {
            // Quality 적용
            if (_qualityDropdown != null)
            {
                QualitySettings.SetQualityLevel(_qualityDropdown.value);
            }

            // Language 저장
            if (_languageDropdown != null)
            {
                PlayerPrefs.SetInt("Language", _languageDropdown.value);
            }

            PlayerPrefs.Save();
            Debug.Log("[OptionsUI] Settings applied");
        }

        private void OnBackClicked()
        {
            PlayerPrefs.Save();
            if (_titleUI != null)
                _titleUI.ShowHubPanel();  // v2 — Hub가 메인 화면이므로 Hub로 복귀
        }
    }
}

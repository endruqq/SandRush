using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    [Header("FMOD Music")]
    [SerializeField] private string _menuMusic = "event:/Menu_Music";
    private FMOD.Studio.EventInstance _musicInstance;
    
    [Header("Options UI References")]
    [SerializeField] private GameObject _optionsCanvas;
    [SerializeField] private GameObject _mainMenuCanvas;
    [SerializeField] private GameObject _audioPanel;
    [SerializeField] private GameObject _graphicsPanel;
    [SerializeField] private GameObject _controlsPanel;

    [Header("FMOD Audio Settings")]
    [SerializeField] private Slider _masterVolumeSlider;
    private FMOD.Studio.Bus _masterBus;
    private const string PREF_MASTER_VOL = "MasterVolume";
    private bool _audioInitialized = false;

    [Header("Graphics Settings")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private TMP_Dropdown _qualityDropdown;
    private Resolution[] _resolutions;

    [Header("Scenes")]
    [SerializeField] private string _gameplaySceneName = "SandRush_Level_1";

    private System.Collections.IEnumerator Start()
    {
        // 1. --- PLAY MUSIC ---
        // Check if MusicManager exists (from gameplay) to avoid double music
        if (MusicManager.Instance != null)
        {
            Debug.Log("[MainMenu] Found MusicManager Persistence -> Delegating to Global MusicManager");
            MusicManager.Instance.PlayMenuMusic();
        }
        else
        {
            Debug.Log("[MainMenu] No MusicManager found -> Playing Local Menu Music");
            if (!string.IsNullOrEmpty(_menuMusic))
            {
                _musicInstance = FMODUnity.RuntimeManager.CreateInstance(_menuMusic);
                _musicInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MINIMUM_DISTANCE, 0f);
                _musicInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, 10000f);
                _musicInstance.start();
            }
        }

        // 2. --- FMOD BUS SETUP ---
        _masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");

        // 3. --- RESOLUTION & QUALITY SETUP ---
        _masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");

        // 3. --- RESOLUTION & QUALITY SETUP ---
        // (Must be done before ResetOptions to populate lists)
        if (_resolutionDropdown != null)
        {
            _resolutions = Screen.resolutions;
            _resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                string option = _resolutions[i].width + " x " + _resolutions[i].height;
                options.Add(option);

                if (_resolutions[i].width == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }
            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
            _resolutionDropdown.RefreshShownValue();
        }

        if (_qualityDropdown != null)
        {
            _qualityDropdown.ClearOptions();
            List<string> qualityOptions = new List<string>(QualitySettings.names);
            _qualityDropdown.AddOptions(qualityOptions);
            _qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
            _qualityDropdown.RefreshShownValue();
        }

        // 4. --- WAIT FOR UI TO SETTLE ---
        yield return null;
        
        // 5. --- AUDIO INITIALIZED ---
        _audioInitialized = true;

        // 6. --- SESSION RESET LOGIC (Shared with PauseMenu) ---
        // If this is the FIRST time the App runs (MainMenu or PauseMenu), reset to defaults.
        if (!PauseMenuManager.HasResetOnLaunch)
        {
            Debug.Log("[MainMenu] First Launch Detected: Resetting Options to Defaults.");
            ResetOptions();
            PauseMenuManager.HasResetOnLaunch = true;
            yield break; 
        }

        // 7. --- NORMAL LOAD (From PlayerPrefs) ---
        float savedVol = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 1f);
        if (savedVol <= 0.01f) savedVol = 1f;
        
        _masterBus.setVolume(savedVol);
        
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.SetValueWithoutNotify(savedVol);
        }
    }

    public void NewGame()
    {
        // Clear progress for a fresh start
        PlayerPrefs.DeleteKey("TutorialCompleted");
        PlayerPrefs.DeleteKey("MaskCollected");
        PlayerPrefs.Save();
        LoadScene(_gameplaySceneName);
    }

    public void ContinueGame()
    {
        // Just load the scene, existing prefs will determine state
        LoadScene(_gameplaySceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void LoadScene(string sceneName)
    {
        if (_musicInstance.isValid())
        {
            _musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _musicInstance.release();
        }
        SceneManager.LoadSceneAsync(sceneName);
    }

    private void OnDestroy()
    {
        if (_musicInstance.isValid())
        {
            _musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _musicInstance.release();
        }
    }

    // --- OPTIONS LOGIC (Copied from PauseMenuManager) ---

    public void OpenOptions()
    {
        if (_mainMenuCanvas != null) _mainMenuCanvas.SetActive(false);
        if (_optionsCanvas != null) _optionsCanvas.SetActive(true);
        OpenAudioTab(); // Default tab
    }

    public void CloseOptions()
    {
        if (_optionsCanvas != null) _optionsCanvas.SetActive(false);
        if (_mainMenuCanvas != null) _mainMenuCanvas.SetActive(true);
    }

    public void OpenAudioTab() => SwitchTab(_audioPanel);
    public void OpenGraphicsTab() => SwitchTab(_graphicsPanel);
    public void OpenControlsTab() => SwitchTab(_controlsPanel);

    private void SwitchTab(GameObject tabToOpen)
    {
        if (_audioPanel != null) _audioPanel.SetActive(false);
        if (_graphicsPanel != null) _graphicsPanel.SetActive(false);
        if (_controlsPanel != null) _controlsPanel.SetActive(false);
        if (tabToOpen != null) tabToOpen.SetActive(true);
    }

    public void SetMasterVolume(float volume)
    {
        if (!_audioInitialized) return;

        float sliderValue = (_masterVolumeSlider != null) ? _masterVolumeSlider.value : volume;
        if (sliderValue > 0) _masterBus.setMute(false);
        
        _masterBus.setVolume(sliderValue);
        PlayerPrefs.SetFloat(PREF_MASTER_VOL, sliderValue);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SetResolution(int resolutionIndex)
    {
        if (_resolutions == null || resolutionIndex < 0 || resolutionIndex >= _resolutions.Length) return;
        Resolution resolution = _resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void ResetOptions()
    {
        // 1. Reset Volume
        if (_masterVolumeSlider != null) 
            _masterVolumeSlider.value = 1f; 
        else
            SetMasterVolume(1f);

        // 2. Reset Fullscreen
        SetFullscreen(true);

        // 3. Reset Resolution
        if (_resolutions != null && _resolutions.Length > 0)
        {
            int maxResIndex = _resolutions.Length - 1;
            SetResolution(maxResIndex);
            
            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.value = maxResIndex;
                _resolutionDropdown.RefreshShownValue();
            }
        }

        // 4. Reset Quality
        string[] names = QualitySettings.names;
        if (names != null && names.Length > 0)
        {
            int highQualityIndex = names.Length - 1;
            SetQuality(highQualityIndex);
            if (_qualityDropdown != null)
            {
                _qualityDropdown.value = highQualityIndex;
                _qualityDropdown.RefreshShownValue();
            }
        }
        Debug.Log("[MainMenu] Options Reset to Defaults");
    }
}

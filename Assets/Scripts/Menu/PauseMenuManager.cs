using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using FMOD.Studio;

public class PauseMenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pauseMenuCanvas;
    [Header("Options Panels")]
    [SerializeField] private GameObject optionsMenuCanvas;
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

    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    private bool isPaused = false;
    private Resolution[] _resolutions;

    private System.Collections.IEnumerator Start()
    {
        // Make sure menus are hidden at start
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
        if (optionsMenuCanvas != null) optionsMenuCanvas.SetActive(false);

        // 1. --- FMOD BUS SETUP ---
        _masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");

        // 2. --- RESOLUTION & QUALITY SETUP (Must be done before ResetOptions) ---
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
        else
        {
            Debug.LogError("[PauseMenu] CRITICAL: '_resolutionDropdown' NOT ASSIGNED in Inspector!");
        }

        if (_qualityDropdown != null)
        {
            _qualityDropdown.ClearOptions();
            List<string> qualityOptions = new List<string>(QualitySettings.names);
            _qualityDropdown.AddOptions(qualityOptions);
            _qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
            _qualityDropdown.RefreshShownValue();
        }
        else
        {
            Debug.LogError("[PauseMenu] CRITICAL: '_qualityDropdown' NOT ASSIGNED in Inspector!");
        }

        // 3. --- WAIT FOR UI TO SETTLE (Skip initial events) ---
        yield return null; 
        
        // 4. --- AUDIO INITIALIZED (Allow SetMasterVolume callbacks) ---
        _audioInitialized = true;

        // 5. --- SESSION RESET LOGIC ---
        // On first launch: Force Defaults (Volume 100%, Max Res, High Quality)
        // On reload: Keep current settings
        if (!PlayerPrefs.HasKey("HasSetOptionsBefore"))
        {
            Debug.Log("[PauseMenu] First Launch Detected: Resetting Options to Defaults.");
            ResetOptions();
            PlayerPrefs.SetInt("HasSetOptionsBefore", 1);
            PlayerPrefs.Save();
        }
        else
        {
            // 6. --- NORMAL LOAD (From PlayerPrefs) ---
            // Load saved volume (default = full volume)
            float savedVol = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 1f);
            if (savedVol <= 0.01f) savedVol = 1f; // Fix corrupted save from previous bug
            
            FMOD.RESULT result = _masterBus.setVolume(savedVol);
            Debug.Log($"[PauseMenu] FMOD Master Bus setVolume({savedVol}) result: {result}");

            // Set slider WITHOUT triggering OnValueChanged
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.minValue = 0f;
                _masterVolumeSlider.maxValue = 1f;
                _masterVolumeSlider.SetValueWithoutNotify(savedVol);
                Debug.Log($"[PauseMenu] Slider set to: {savedVol}");
            }
            else
            {
                Debug.LogError("[PauseMenu] CRITICAL: '_masterVolumeSlider' NOT ASSIGNED in Inspector! Slider will start at 0 and overwrite volume!");
            }

            // Load Graphics
            if (PlayerPrefs.HasKey("QualitySetting"))
            {
                int quality = PlayerPrefs.GetInt("QualitySetting");
                QualitySettings.SetQualityLevel(quality);
                if (_qualityDropdown != null)
                {
                    _qualityDropdown.SetValueWithoutNotify(quality);
                    _qualityDropdown.RefreshShownValue();
                }
            }

            if (PlayerPrefs.HasKey("FullscreenSetting"))
            {
                Screen.fullScreen = PlayerPrefs.GetInt("FullscreenSetting") == 1;
            }

            if (PlayerPrefs.HasKey("ResolutionIndex"))
            {
                int resIndex = PlayerPrefs.GetInt("ResolutionIndex");
                if (_resolutions != null && resIndex >= 0 && resIndex < _resolutions.Length)
                {
                    Resolution res = _resolutions[resIndex];
                    Screen.SetResolution(res.width, res.height, Screen.fullScreen);
                    if (_resolutionDropdown != null)
                    {
                        _resolutionDropdown.SetValueWithoutNotify(resIndex);
                        _resolutionDropdown.RefreshShownValue();
                    }
                }
            }
        }
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(true);
        if (optionsMenuCanvas != null) optionsMenuCanvas.SetActive(false);
        
        // Unlock cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
        if (optionsMenuCanvas != null) optionsMenuCanvas.SetActive(false);
        
        // Top-Down Shooter Cursor Logic
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }

    public void OpenOptions()
    {
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
        if (optionsMenuCanvas != null) optionsMenuCanvas.SetActive(true);
        
        // Open Audio tab by default
        OpenAudioTab();
    }
    
    public void CloseOptions()
    {
        if (optionsMenuCanvas != null) optionsMenuCanvas.SetActive(false);
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(true);
    }
    
    // --- TABS ---
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

    // --- FMOD AUDIO ---
    public void SetMasterVolume(float volume)
    {
        // Ignore callbacks BEFORE Start() has initialized the saved value
        if (!_audioInitialized) 
        {
            Debug.Log($"[PauseMenu] Ignored SetMasterVolume({volume}) - Not Initialized");
            return;
        }

        // FORCE READ FROM SLIDER (Fix for bad Event Wiring)
        // If user wired "Static Parameter 0" instead of "Dynamic Float", 'volume' will be 0 always.
        // Reading .value directly bypasses this mistake.
        float sliderValue = (_masterVolumeSlider != null) ? _masterVolumeSlider.value : volume;

        // 1. Force Unmute if volume is > 0 (fixes "stuck at mute" issue)
        if (sliderValue > 0)
        {
            _masterBus.setMute(false);
        }

        _masterBus.setVolume(sliderValue);
        PlayerPrefs.SetFloat(PREF_MASTER_VOL, sliderValue);
        PlayerPrefs.Save();
        Debug.Log($"[PauseMenu] SetMasterVolume: {sliderValue} (Unmuted)");
    }

    // --- GRAPHICS ---
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualitySetting", qualityIndex);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("FullscreenSetting", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetResolution(int resolutionIndex)
    {
        if (_resolutions == null || resolutionIndex < 0 || resolutionIndex >= _resolutions.Length) return;
        
        Resolution resolution = _resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.Save();
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ResetOptions()
    {
        // 1. Reset Volume
        if (_masterVolumeSlider != null) 
        {
            _masterVolumeSlider.value = 1f; // This will trigger OnValueChanged -> SetMasterVolume(1f)
        }
        else
        {
            SetMasterVolume(1f); // Fallback if slider missing
        }

        // 3. Reset Fullscreen (Default true)
        SetFullscreen(true);

        // 2. Reset Resolution (Default to highest available)
        if (_resolutions != null && _resolutions.Length > 0)
        {
            // Simple max index is usually best, but let's be robust
            int maxResIndex = _resolutions.Length - 1;
            Resolution res = _resolutions[maxResIndex];
            
            Debug.Log($"[PauseMenu] ResetOptions: Setting Resolution to Index {maxResIndex} ({res.width}x{res.height})");
            SetResolution(maxResIndex);
            
            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.value = maxResIndex;
                _resolutionDropdown.RefreshShownValue();
            }
        }
        else
        {
            Debug.LogWarning("[PauseMenu] ResetOptions: No resolutions found in _resolutions array!");
        }

        // 3. Reset Quality (Default to High/Highest)
        string[] names = QualitySettings.names;
        if (names != null && names.Length > 0)
        {
            int highQualityIndex = names.Length - 1; // Pick highest
            SetQuality(highQualityIndex);
            
            if (_qualityDropdown != null)
            {
                _qualityDropdown.value = highQualityIndex;
                _qualityDropdown.RefreshShownValue();
            }
        }
        
        Debug.Log("[PauseMenu] Options Reset to Defaults");
    }
    
    public void FullRestart()
    {
        // Clear Persistence
        PlayerPrefs.DeleteKey("TutorialCompleted");
        PlayerPrefs.DeleteKey("MaskCollected");
        PlayerPrefs.DeleteKey("HasCustomSave");
        PlayerPrefs.Save();
        
        // Reset Time
        Time.timeScale = 1f;
        isPaused = false;
        
        // Reload Scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadSceneAsync(mainMenuSceneName);
    }
    
    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}

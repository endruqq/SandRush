using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class PauseMenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pauseMenuCanvas;
    [Header("Options Panels")]
    [SerializeField] private GameObject optionsMenuCanvas;
    [SerializeField] private GameObject _audioPanel;
    [SerializeField] private GameObject _graphicsPanel;
    [SerializeField] private GameObject _controlsPanel;

    [Header("Audio Settings")]
    [SerializeField] private UnityEngine.Audio.AudioMixer _audioMixer;
    [SerializeField] private string _musicVolumeParam = "MusicVol";
    [SerializeField] private string _sfxVolumeParam = "SFXVol";

    [Header("Graphics Settings")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private TMP_Dropdown _qualityDropdown;

    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    private bool isPaused = false;
    private Resolution[] _resolutions;

    private void Start()
    {
        // Make sure menus are hidden at start
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
        if (optionsMenuCanvas != null) optionsMenuCanvas.SetActive(false);

        // --- RESOLUTION SETUP ---
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
            _resolutionDropdown.value = currentResolutionIndex;
            _resolutionDropdown.RefreshShownValue();
        }

        // --- QUALITY SETUP ---
        if (_qualityDropdown != null)
        {
            _qualityDropdown.ClearOptions();
            List<string> qualityOptions = new List<string>(QualitySettings.names);
            _qualityDropdown.AddOptions(qualityOptions);
            _qualityDropdown.value = QualitySettings.GetQualityLevel();
            _qualityDropdown.RefreshShownValue();
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

    // --- AUDIO ---
    public void SetMusicVolume(float volume)
    {
        // Convert 0-1 slider value to decibels (-80 to 0)
        // Formula: Mathf.Log10(volume) * 20
        // Use a small epsilon to avoid Log10(0)
        float volumedB = Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20;

        if (_audioMixer != null)
        {
            _audioMixer.SetFloat(_musicVolumeParam, volumedB);
        }
    }

    public void SetSFXVolume(float volume)
    {
        float volumedB = Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20;

        if (_audioMixer != null)
        {
            _audioMixer.SetFloat(_sfxVolumeParam, volumedB);
        }
    }

    // --- GRAPHICS ---
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

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("FMOD Music")]
    [SerializeField] private string _menuMusic = "event:/Menu_Music";

    private FMOD.Studio.EventInstance _musicInstance;

    private void Start()
    {
        // Play menu music
        if (!string.IsNullOrEmpty(_menuMusic))
        {
            _musicInstance = FMODUnity.RuntimeManager.CreateInstance(_menuMusic);
            _musicInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MINIMUM_DISTANCE, 0f);
            _musicInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, 10000f);
            _musicInstance.start();
        }
    }

    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void LoadScene(string sceneName)
    {
        // Stop menu music before loading new scene
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
}

using UnityEngine;
using FMOD.Studio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Events")]
    [SerializeField] private string _combatMusic = "event:/Main_Combat_Music_Part1";
    [SerializeField] private string _menuMusic = "event:/Menu_Music";
    [SerializeField] private string _tutorialMusic = "event:/Tutorial_Music_Theme";

    [Header("Settings")]
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private string _startEvent = "event:/Main_Combat_Music_Part1";

    private EventInstance _currentMusicInstance;
    private string _currentEventPath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (Instance != this) return; // Zabezpieczenie przed duplikatem który nie zdążył się jeszcze zniszczyć
        if (_playOnStart)
        {
            PlayMusic(_startEvent);
        }
    }

    private void Update()
    {
        if (_currentMusicInstance.isValid())
        {
            _currentMusicInstance.getPlaybackState(out PLAYBACK_STATE state);
            if (state == PLAYBACK_STATE.STOPPED && !string.IsNullOrEmpty(_currentEventPath))
            {
                // The track finished naturally (didn't loop in FMOD). Let's restart it!
                Debug.Log($"[MusicManager] Track finished naturally. Restarting to loop: {_currentEventPath}");
                _currentMusicInstance.start();
            }
        }
    }

    /// <summary>
    /// Play a music event. Stops current music if different track.
    /// </summary>
    public void PlayMusic(string eventPath)
    {
        // Don't restart same track
        if (_currentEventPath == eventPath && _currentMusicInstance.isValid())
        {
            _currentMusicInstance.getPlaybackState(out PLAYBACK_STATE state);
            if (state == PLAYBACK_STATE.PLAYING) return;
        }

        StopMusic();

        _currentMusicInstance = FMODUnity.RuntimeManager.CreateInstance(eventPath);
        _currentMusicInstance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, 1f); // 0f może powodować błędy atenuacji
        _currentMusicInstance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, 10000f);
        _currentMusicInstance.setVolume(0.35f); // Reduced by 50% as requested
        _currentMusicInstance.start();
        _currentEventPath = eventPath;

        Debug.Log($"[MusicManager] Playing: {eventPath}");
    }

    /// <summary>
    /// Stop current music with optional fade out.
    /// </summary>
    public void StopMusic(bool allowFadeOut = true)
    {
        if (_currentMusicInstance.isValid())
        {
            _currentMusicInstance.stop(allowFadeOut ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE);
            _currentMusicInstance.release();
            _currentEventPath = null;
        }
    }

    /// <summary>
    /// Switch to combat music.
    /// </summary>
    public void PlayCombatMusic() => PlayMusic(_combatMusic);

    /// <summary>
    /// Switch to menu music.
    /// </summary>
    public void PlayMenuMusic() => PlayMusic(_menuMusic);

    /// <summary>
    /// Switch to tutorial music.
    /// </summary>
    public void PlayTutorialMusic() => PlayMusic(_tutorialMusic);

    private void OnDestroy()
    {
        StopMusic(false);
    }
}

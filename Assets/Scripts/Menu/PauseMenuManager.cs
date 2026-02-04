using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pauseMenuCanvas;
    
    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    private bool isPaused = false;
    
    private void Start()
    {
        // Make sure pause menu is hidden at start
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
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
        
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(true);
        }
        
        // Unlock cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
        
        // Lock cursor back for gameplay (adjust if your game uses different cursor settings)
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }
    
    public void OpenOptions()
    {
        // TODO: Implement options menu logic
        Debug.Log("Options menu not implemented yet");
    }
    
    public void GoToMainMenu()
    {
        // Reset time scale before loading new scene
        Time.timeScale = 1f;
        isPaused = false;
        
        SceneManager.LoadSceneAsync(mainMenuSceneName);
    }
    
    // Called when the object is destroyed (e.g., scene change)
    private void OnDestroy()
    {
        // Ensure time scale is reset
        Time.timeScale = 1f;
    }
}

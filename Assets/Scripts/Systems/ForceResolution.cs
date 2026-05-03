using UnityEngine;

public static class ForceResolution
{
    // This runs immediately when the game finishes initializing, before the first scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void EnforceResolution()
    {
        Debug.Log("[ForceResolution] FORCING 1920x1080 FULLSCREEN NOW!");
        
        // Force 1920x1080 immediately
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        
        // Ensure VSync is on (optional, but good for stability)
        QualitySettings.vSyncCount = 1;
    }
}

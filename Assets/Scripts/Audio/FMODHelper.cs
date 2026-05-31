using UnityEngine;
using FMOD.Studio;

/// <summary>
/// Static helper to play FMOD events with the max-distance fix
/// to prevent 3D sounds from being virtualized (silent).
/// </summary>
public static class FMODHelper
{
    /// <summary>
    /// Play a one-shot FMOD event at a world position.
    /// Automatically overrides 3D distance to prevent virtualization.
    /// </summary>
    public static void PlayOneShot(string eventPath, Vector3 position, float pitch = 1f)
    {
        var instance = FMODUnity.RuntimeManager.CreateInstance(eventPath);
        instance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, 0f);
        instance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, 10000f);
        if (pitch != 1f) instance.setPitch(pitch);
        instance.setVolume(0.05f); // Extremely reduced (0.05) because FMOD volume scaling is linear, not perceived
        instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
        instance.start();
        instance.release();
    }

    /// <summary>
    /// Play a one-shot FMOD event without 3D positioning (for UI sounds).
    /// </summary>
    public static void PlayOneShot2D(string eventPath)
    {
        var instance = FMODUnity.RuntimeManager.CreateInstance(eventPath);
        instance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, 0f);
        instance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, 10000f);
        instance.setVolume(0.05f); // Extremely reduced (0.05)
        instance.start();
        instance.release();
    }

    /// <summary>
    /// Preloads sample data for a given FMOD event to prevent lag spikes on first playback.
    /// </summary>
    public static void PreloadEvent(string eventPath)
    {
        if (string.IsNullOrEmpty(eventPath)) return;
        try
        {
            var eventDescription = FMODUnity.RuntimeManager.GetEventDescription(eventPath);
            if (eventDescription.isValid())
            {
                eventDescription.loadSampleData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[FMODHelper] Failed to preload event '{eventPath}': {e.Message}");
        }
    }
}

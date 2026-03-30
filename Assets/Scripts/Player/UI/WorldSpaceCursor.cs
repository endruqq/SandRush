using UnityEngine;

public class WorldSpaceCursor : MonoBehaviour
{
    // A small offset to prevent the cursor from flickering (Z-fighting) with the ground.
    private const float VerticalOffset = 0.05f;
    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
    }

    private void Update()
    {
        bool isUIMode = Player.IsUIModeActive || Time.timeScale == 0f;
        
        if (_renderer != null)
        {
            _renderer.enabled = !isUIMode;
        }
    }

    public void UpdatePosition(Vector3 newPosition)
    {
        transform.position = newPosition + new Vector3(0, VerticalOffset, 0);
    }
}

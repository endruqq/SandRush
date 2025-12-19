using UnityEngine;

public class WorldSpaceCursor : MonoBehaviour
{
    // A small offset to prevent the cursor from flickering (Z-fighting) with the ground.
    private const float VerticalOffset = 0.05f;

    public void UpdatePosition(Vector3 newPosition)
    {
        transform.position = newPosition + new Vector3(0, VerticalOffset, 0);
    }
}

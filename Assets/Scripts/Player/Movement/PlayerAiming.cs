using UnityEngine;

public class PlayerAiming
{
    private readonly Camera _cam;
    private readonly Transform _firePoint;
    private readonly LayerMask _groundMask;

    /// <summary>
    /// The point on the ground, directly below the AimPosition. This is where the cursor indicator should be.
    /// </summary>
    public Vector3 GroundPosition { get; private set; }

    /// <summary>
    /// The actual point in space the weapon aims at. It lies on the camera's sight line and the weapon's horizontal plane.
    /// </summary>
    public Vector3 AimPosition { get; private set; }

    public PlayerAiming(Camera cam, Transform firePoint, LayerMask groundMask)
    {
        _cam = cam;
        _firePoint = firePoint;
        _groundMask = groundMask;

        // Initialize with fallback values
        AimPosition = _firePoint.position + _firePoint.forward * 5f;
        GroundPosition = AimPosition - Vector3.up * _firePoint.position.y;
    }

    public void Tick()
    {
        // 1. Create an infinite mathematical plane that is horizontal and passes EXACTLY through the weapon's fire point.
        var shootingPlane = new Plane(Vector3.up, _firePoint.position);

        // 2. Create a ray from the camera through the mouse cursor.
        var ray = _cam.ScreenPointToRay(Input.mousePosition);

        // 3. Find the intersection point of the camera ray and the weapon's shooting plane.
        // This is the ONLY point in 3D space that satisfies both conditions:
        // a) It's on the same level as the weapon.
        // b) It's exactly under the mouse cursor from the camera's perspective.
        if (shootingPlane.Raycast(ray, out var enter))
        {
            AimPosition = ray.GetPoint(enter);
        }

        // 4. Find the corresponding ground position by casting a ray downwards from the aim target.
        if (Physics.Raycast(AimPosition, Vector3.down, out var hitInfo, Mathf.Infinity, _groundMask))
        {
            GroundPosition = hitInfo.point;
        }
        else
        {
            // Fallback if there is no ground below the cursor
            GroundPosition = new Vector3(AimPosition.x, 0, AimPosition.z);
        }

        // 5. Debug lines to visualize the perfect aiming line.
        // The bullet travels along this line. From the camera's view, it will perfectly cover the mouse cursor.
        Debug.DrawLine(_firePoint.position, AimPosition, Color.red);
        // The indicator line from the aim point down to the ground.
        Debug.DrawLine(AimPosition, GroundPosition, Color.white);
    }
}

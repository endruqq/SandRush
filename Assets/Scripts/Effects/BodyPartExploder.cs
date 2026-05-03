using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BodyPartExploder : MonoBehaviour
{
    [Header("Explosion Settings")]
    [Tooltip("Force applied to body parts in direction of shot.")]
    [SerializeField] private float _knockbackForce = 15f;
    [Tooltip("Random torque applied to spin parts.")]
    [SerializeField] private float _torqueAmount = 10f;
    [Tooltip("How long parts stay before disappearing.")]
    [SerializeField] private float _partLifetime = 5f;
    [Tooltip("Optional layers to exclude from explosion logic.")]
    [SerializeField] private LayerMask _excludeLayers;
    [Tooltip("Manual scale adjustment for debris.")]
    [SerializeField] private float _debrisScale = 1.0f;

    private List<Renderer> _renderers = new List<Renderer>();
    private Collider _playerCollider;

    private void Awake()
    {
        // Cache all renderers (parts) beforehand
        var allRenderers = GetComponentsInChildren<Renderer>();
        foreach (var r in allRenderers)
        {
            // Skip particle systems or trails, focus on meshes
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
            {
                _renderers.Add(r);
            }
        }

        // Find player collider to ignore debris collision
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerCollider = player.GetComponent<Collider>();
    }

    public void Explode(Vector3 hitDirection)
    {
        // Must disable animator first to stop it overriding positions?
        // Actually we are spawning new objects, so it doesn't matter.
        
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            
            // Create a debris object
            GameObject debris = new GameObject(r.name + "_Debris");
            debris.transform.position = r.transform.position;
            debris.transform.rotation = r.transform.rotation;
            debris.layer = r.gameObject.layer;
            
            Mesh meshToUse = null;
            Material[] matsToUse = r.sharedMaterials;

            if (r is SkinnedMeshRenderer smr)
            {
                // Snapshot the current pose into a static mesh
                Mesh bakedMesh = new Mesh();
                smr.BakeMesh(bakedMesh);
                meshToUse = bakedMesh;
                
                // Adjust transform because BakeMesh is local to the SMR transform (usually 0,0,0 relative to root)
                // Actually BakeMesh puts vertices in World Space if you map it right? No, usually local.
                // Wait, BakeMesh(mesh) puts it in the space of the SMR's transform?
                // Documentation: "Snapshots the minimal bounding volume of the skinned mesh... into the mesh."
                // "The baked mesh will be in the local space of the SkinnedMeshRenderer's Transform."
                
                debris.transform.position = smr.transform.position;
                debris.transform.rotation = smr.transform.rotation;
                debris.transform.localScale = smr.transform.lossyScale * _debrisScale;
            }
            else if (r is MeshRenderer) // Correct check: Is it a MeshRenderer?
            {
                 // If it's a MeshRenderer, the mesh is in the sibling MeshFilter
                 if (r.TryGetComponent<MeshFilter>(out var filter))
                 {
                     meshToUse = filter.sharedMesh;
                     debris.transform.localScale = r.transform.lossyScale * _debrisScale;
                 }
            }


            if (meshToUse != null)
            {
                // Setup Visuals
                var mf = debris.AddComponent<MeshFilter>();
                mf.mesh = meshToUse;
                
                var mr = debris.AddComponent<MeshRenderer>();
                mr.sharedMaterials = matsToUse;
                
                // Add Physics
                var rb = debris.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                
                // Add Collider
                try
                {
                    var col = debris.AddComponent<MeshCollider>();
                    col.convex = true; 
                    col.sharedMesh = meshToUse;
                }
                catch
                {
                    var box = debris.AddComponent<BoxCollider>();
                    box.size = Vector3.one * 0.5f; 
                }
                
                // Apply Forces
                Vector3 randomDir = Random.insideUnitSphere * 0.5f;
                // Add upward lift to simulate explosion
                Vector3 finalForce = (hitDirection.normalized + Vector3.up * 0.8f + randomDir) * _knockbackForce;
                
                rb.AddForce(finalForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * _torqueAmount, ForceMode.Impulse);
                
                // Ignore collision with player so debris doesn't push/block them
                // but still collides with ground and walls
                Collider debrisCol = debris.GetComponent<Collider>();
                if (debrisCol != null && _playerCollider != null)
                {
                    Physics.IgnoreCollision(debrisCol, _playerCollider);
                }
                
                // Cleanup debris
                Destroy(debris, _partLifetime);
            }
            
            // Hide original part
            r.enabled = false;
        }
    }
}

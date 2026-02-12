using UnityEngine;
using System.Collections;

/// <summary>
/// Creates transparent ghost/afterimage copies of the player during dash.
/// Attach this to the Player GameObject.
/// </summary>
public class DashGhostEffect : MonoBehaviour
{
    [Header("Ghost Settings")]
    [Tooltip("How many ghost copies to spawn during one dash")]
    [SerializeField] private int _ghostCount = 3;
    [Tooltip("Time interval between each ghost spawn")]
    [SerializeField] private float _spawnInterval = 0.03f;
    [Tooltip("How long each ghost takes to fade out")]
    [SerializeField] private float _fadeDuration = 0.4f;
    [Tooltip("Starting opacity of the ghost (0-1)")]
    [SerializeField] private float _startAlpha = 0.5f;
    [Tooltip("Color tint for the ghost")]
    [SerializeField] private Color _ghostColor = new Color(0.6f, 0.8f, 1f, 1f); // Slight blue tint

    /// <summary>
    /// Call this when the player dashes. Spawns ghost trail.
    /// </summary>
    public void SpawnGhostTrail()
    {
        StartCoroutine(SpawnGhosts());
    }

    private IEnumerator SpawnGhosts()
    {
        for (int i = 0; i < _ghostCount; i++)
        {
            CreateGhost();
            yield return new WaitForSeconds(_spawnInterval);
        }
    }

    private void CreateGhost()
    {
        // Find all skinned and regular mesh renderers on the player
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();

        // Create a parent object for all ghost parts
        GameObject ghostParent = new GameObject("DashGhost");
        ghostParent.transform.position = transform.position;
        ghostParent.transform.rotation = transform.rotation;

        // Snapshot skinned meshes (animated models)
        foreach (var smr in skinnedRenderers)
        {
            // Bake the current pose into a static mesh
            Mesh bakedMesh = new Mesh();
            smr.BakeMesh(bakedMesh);

            GameObject ghostPart = new GameObject("GhostPart_Skinned");
            ghostPart.transform.SetParent(ghostParent.transform);
            ghostPart.transform.position = smr.transform.position;
            ghostPart.transform.rotation = smr.transform.rotation;
            ghostPart.transform.localScale = smr.transform.lossyScale;

            MeshFilter mf = ghostPart.AddComponent<MeshFilter>();
            mf.mesh = bakedMesh;

            MeshRenderer mr = ghostPart.AddComponent<MeshRenderer>();
            // Create transparent ghost materials
            Material[] ghostMats = new Material[smr.sharedMaterials.Length];
            for (int i = 0; i < ghostMats.Length; i++)
            {
                ghostMats[i] = CreateGhostMaterial(smr.sharedMaterials[i]);
            }
            mr.materials = ghostMats;
        }

        // Snapshot static meshes (weapons, accessories)
        foreach (var mr in meshRenderers)
        {
            MeshFilter sourceMF = mr.GetComponent<MeshFilter>();
            if (sourceMF == null || sourceMF.sharedMesh == null) continue;

            GameObject ghostPart = new GameObject("GhostPart_Static");
            ghostPart.transform.SetParent(ghostParent.transform);
            ghostPart.transform.position = mr.transform.position;
            ghostPart.transform.rotation = mr.transform.rotation;
            ghostPart.transform.localScale = mr.transform.lossyScale;

            MeshFilter mf = ghostPart.AddComponent<MeshFilter>();
            mf.sharedMesh = sourceMF.sharedMesh;

            MeshRenderer ghostMR = ghostPart.AddComponent<MeshRenderer>();
            Material[] ghostMats = new Material[mr.sharedMaterials.Length];
            for (int i = 0; i < ghostMats.Length; i++)
            {
                ghostMats[i] = CreateGhostMaterial(mr.sharedMaterials[i]);
            }
            ghostMR.materials = ghostMats;
        }

        // Start fade-out and destroy
        StartCoroutine(FadeAndDestroy(ghostParent));
    }

    private Material CreateGhostMaterial(Material sourceMaterial)
    {
        // Create a new transparent unlit material for the ghost
        Material ghostMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        
        // Try to get the base color/texture from source
        if (sourceMaterial.HasProperty("_BaseColor"))
        {
            Color baseColor = sourceMaterial.GetColor("_BaseColor");
            ghostMat.SetColor("_BaseColor", new Color(
                _ghostColor.r * baseColor.r,
                _ghostColor.g * baseColor.g,
                _ghostColor.b * baseColor.b,
                _startAlpha));
        }
        else
        {
            ghostMat.SetColor("_BaseColor", new Color(
                _ghostColor.r, _ghostColor.g, _ghostColor.b, _startAlpha));
        }

        // Set to transparent mode
        ghostMat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
        ghostMat.SetFloat("_Blend", 0); // Alpha blend
        ghostMat.SetFloat("_AlphaClip", 0);
        ghostMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ghostMat.SetFloat("_ZWrite", 0);
        ghostMat.renderQueue = 3000; // Transparent queue
        ghostMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        ghostMat.SetOverrideTag("RenderType", "Transparent");

        return ghostMat;
    }

    private IEnumerator FadeAndDestroy(GameObject ghost)
    {
        Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(_startAlpha, 0f, elapsed / _fadeDuration);

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color c = mat.GetColor("_BaseColor");
                        c.a = alpha;
                        mat.SetColor("_BaseColor", c);
                    }
                }
            }

            yield return null;
        }

        Destroy(ghost);
    }
}

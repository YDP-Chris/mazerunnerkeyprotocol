using UnityEngine;

/// <summary>
/// Client-side proximity feedback for the key. Pulsing glow when player is nearby.
/// Attached to the key prefab.
/// </summary>
public class KeyProximityFeedback : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float minimapRevealRadius = 5f;

    [Header("Glow Settings")]
    [SerializeField] private float minEmission = 0.5f;
    [SerializeField] private float maxEmission = 3f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private Color emissionColor = new Color(1f, 0.85f, 0.2f); // gold

    private MeshRenderer meshRenderer;
    private Material keyMaterial;
    private bool isGlowing;
    private bool isOnMinimap;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
            keyMaterial = meshRenderer.material;
    }

    private void Update()
    {
        var localPlayer = GetLocalPlayer();
        if (localPlayer == null || keyMaterial == null) return;

        float distance = Vector3.Distance(localPlayer.position, transform.position);

        // Glow feedback within detection radius
        if (distance <= detectionRadius)
        {
            if (!isGlowing)
            {
                isGlowing = true;
                keyMaterial.EnableKeyword("_EMISSION");
            }

            // Intensity scales inversely with distance (closer = stronger)
            float normalizedDist = 1f - Mathf.Clamp01(distance / detectionRadius);
            float baseIntensity = Mathf.Lerp(minEmission, maxEmission, normalizedDist);

            // Pulse effect
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float intensity = baseIntensity * (0.7f + 0.3f * pulse);

            keyMaterial.SetColor(EmissionColorId, emissionColor * intensity);
        }
        else if (isGlowing)
        {
            isGlowing = false;
            keyMaterial.SetColor(EmissionColorId, Color.black);
        }

        // Minimap reveal (placeholder - fires event when within range)
        bool shouldShowOnMinimap = distance <= minimapRevealRadius;
        if (shouldShowOnMinimap != isOnMinimap)
        {
            isOnMinimap = shouldShowOnMinimap;
            // TODO: Wire to minimap system when implemented
        }
    }

    private Transform GetLocalPlayer()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null) return null;
        var localClient = Unity.Netcode.NetworkManager.Singleton.LocalClient;
        return localClient?.PlayerObject?.transform;
    }
}

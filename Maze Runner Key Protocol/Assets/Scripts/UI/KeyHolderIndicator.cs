using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Directional arrow on HUD pointing toward the key holder.
/// Only active for non-holder players when key is held.
/// Styled as a glowing gold arrow at screen edge.
/// </summary>
public class KeyHolderIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private Image arrowImage;
    [SerializeField] private float edgeMargin = 60f;

    private Camera mainCam;
    private bool isActive;

    private void Start()
    {
        if (arrowImage != null)
            arrowImage.enabled = false;
    }

    private void LateUpdate()
    {
        var keyMgr = KeyManager.Instance;
        if (keyMgr == null || arrowRect == null || arrowImage == null) return;

        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsConnectedClient) return;

        ulong localClientId = nm.LocalClientId;
        ulong holderClientId = keyMgr.KeyHolderClientId.Value;

        // Only show for non-holders when key is held
        bool shouldShow = keyMgr.CurrentKeyState.Value == KeyManager.KeyState.Held
            && holderClientId != ulong.MaxValue
            && holderClientId != localClientId;

        if (!shouldShow)
        {
            if (isActive)
            {
                isActive = false;
                arrowImage.enabled = false;
            }
            return;
        }

        // Find holder position from spawned objects
        Vector3? holderPos = GetHolderPosition(holderClientId);
        if (!holderPos.HasValue)
        {
            arrowImage.enabled = false;
            return;
        }

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // Check if holder is visible in viewport
        Vector3 vpPos = mainCam.WorldToViewportPoint(holderPos.Value);
        bool isInView = vpPos.z > 0 && vpPos.x > 0.05f && vpPos.x < 0.95f
            && vpPos.y > 0.05f && vpPos.y < 0.95f;

        if (isInView)
        {
            // Holder visible - reduce clutter
            arrowImage.enabled = false;
            isActive = false;
            return;
        }

        // Show arrow pointing toward holder at screen edge
        isActive = true;
        arrowImage.enabled = true;

        Vector3 screenPos = mainCam.WorldToScreenPoint(holderPos.Value);

        // If behind camera, flip
        if (screenPos.z < 0)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        // Compute direction from screen center
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 dir = new Vector2(screenPos.x, screenPos.y) - center;

        // Clamp to screen edge with margin
        float maxX = (Screen.width * 0.5f) - edgeMargin;
        float maxY = (Screen.height * 0.5f) - edgeMargin;

        if (Mathf.Abs(dir.x) > 0.01f || Mathf.Abs(dir.y) > 0.01f)
        {
            float scaleX = dir.x != 0 ? maxX / Mathf.Abs(dir.x) : float.MaxValue;
            float scaleY = dir.y != 0 ? maxY / Mathf.Abs(dir.y) : float.MaxValue;
            float scale = Mathf.Min(scaleX, scaleY, 1f);
            dir *= scale;
        }

        arrowRect.position = center + dir;

        // Rotate arrow to point toward holder
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrowRect.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    private Vector3? GetHolderPosition(ulong holderClientId)
    {
        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            if (kvp.Value.OwnerClientId == holderClientId && kvp.Value.GetComponent<PlayerHealth>() != null)
                return kvp.Value.transform.position;
        }
        return null;
    }
}

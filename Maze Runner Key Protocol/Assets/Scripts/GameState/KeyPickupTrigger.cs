using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Attached to the key prefab. Detects player collision and requests pickup from KeyManager.
/// Auto-pickup on contact (no deliberate action required).
/// </summary>
public class KeyPickupTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Only the local player triggers pickup
        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null || !netObj.IsLocalPlayer) return;

        var keyMgr = KeyManager.Instance;
        if (keyMgr == null) return;

        // Only pick up if uncollected or dropped
        if (keyMgr.CurrentKeyState.Value == KeyManager.KeyState.Held) return;

        keyMgr.RequestPickupServerRpc(netObj.OwnerClientId);
    }
}

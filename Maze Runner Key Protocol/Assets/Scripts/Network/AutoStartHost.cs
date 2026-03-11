using UnityEngine;
using Unity.Netcode;

public class AutoStartHost : MonoBehaviour
{
    private void Start()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.IsClient || nm.IsServer) return;

        // Set up connection approval BEFORE starting host
        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = OnConnectionApproval;

        Debug.Log("[AutoStartHost] Starting host...");
        nm.StartHost();
    }

    private void OnConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;

        var spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        if (spawnPoints.Length > 0)
        {
            int index = (int)(request.ClientNetworkId % (ulong)spawnPoints.Length);
            var spawn = spawnPoints[index];
            response.Position = spawn.transform.position;
            response.Rotation = spawn.transform.rotation;
            Debug.Log($"[AutoStartHost] Spawning client {request.ClientNetworkId} at {spawn.name}: {spawn.transform.position}");
        }
        else
        {
            Debug.LogWarning("[AutoStartHost] No spawn points found!");
            response.Position = new Vector3(10, 1, 10);
        }
    }
}

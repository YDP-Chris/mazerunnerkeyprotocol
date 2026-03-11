using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnHandler : MonoBehaviour
{
    private void Start()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = OnConnectionApproval;
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
            Debug.Log($"Approving client {request.ClientNetworkId} at {spawn.name}: {spawn.transform.position}");
        }
    }
}

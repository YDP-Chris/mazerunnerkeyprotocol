using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Spawns the ExitGateway prefab at the ExitSpawnPoint location when the host starts.
/// Attach to a scene GameObject with NetworkObject.
/// </summary>
public class ExitGatewaySpawner : NetworkBehaviour
{
    [SerializeField] private GameObject exitGatewayPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (exitGatewayPrefab == null)
        {
            Debug.LogWarning("[ExitGatewaySpawner] No exit gateway prefab assigned!");
            return;
        }

        var exitPoint = FindFirstObjectByType<ExitSpawnPoint>();
        if (exitPoint == null)
        {
            Debug.LogWarning("[ExitGatewaySpawner] No ExitSpawnPoint found in scene!");
            return;
        }

        var exitObj = Instantiate(exitGatewayPrefab, exitPoint.transform.position, exitPoint.transform.rotation);
        exitObj.GetComponent<NetworkObject>().Spawn();
        Debug.Log($"[ExitGatewaySpawner] Exit gateway spawned at {exitPoint.transform.position}");
    }
}

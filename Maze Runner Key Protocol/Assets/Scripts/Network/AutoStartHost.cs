using UnityEngine;
using Unity.Netcode;

public class AutoStartHost : MonoBehaviour
{
    private void Start()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.StartHost();
        }
    }
}

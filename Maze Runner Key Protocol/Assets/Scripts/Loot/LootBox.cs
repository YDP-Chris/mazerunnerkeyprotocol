using UnityEngine;
using Unity.Netcode;

public class LootBox : NetworkBehaviour
{
    [SerializeField] private float interactionRange = 2.5f;
    [SerializeField] private Light glowLight;

    public NetworkVariable<bool> isAvailable = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> assignedItemType = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Set by spawner on host
    [HideInInspector] public WeaponData weaponData;
    [HideInInspector] public int ammoAmount = 15;
    [HideInInspector] public float healAmount = 25f;

    private bool playerInRange;
    private PlayerInputActions inputActions;
    private float pulseTimer;

    public override void OnNetworkSpawn()
    {
        isAvailable.OnValueChanged += OnAvailabilityChanged;

        if (IsOwner || IsClient)
        {
            inputActions = new PlayerInputActions();
            inputActions.Player.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        isAvailable.OnValueChanged -= OnAvailabilityChanged;
        inputActions?.Player.Disable();
        inputActions?.Dispose();
    }

    private void Update()
    {
        if (!isAvailable.Value) return;

        // Pulsing glow
        if (glowLight != null)
        {
            pulseTimer += Time.deltaTime;
            glowLight.intensity = 1.5f + Mathf.Sin(pulseTimer * 3f) * 0.5f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponentInParent<NetworkObject>()) return;
        var inventory = other.GetComponentInParent<WeaponInventory>();
        if (inventory != null && inventory.IsOwner)
            playerInRange = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!playerInRange || !isAvailable.Value) return;

        var inventory = other.GetComponentInParent<WeaponInventory>();
        if (inventory == null || !inventory.IsOwner) return;

        if (inputActions != null && inputActions.Player.Interact.WasPressedThisFrame())
        {
            RequestPickupServerRpc();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var inventory = other.GetComponentInParent<WeaponInventory>();
        if (inventory != null && inventory.IsOwner)
            playerInRange = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPickupServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!isAvailable.Value) return;

        ulong clientId = rpcParams.Receive.SenderClientId;
        NetworkObject playerObj = null;

        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (obj.OwnerClientId == clientId && obj.GetComponent<WeaponInventory>() != null)
            {
                playerObj = obj;
                break;
            }
        }

        if (playerObj == null) return;

        // Distance check
        float dist = Vector3.Distance(playerObj.transform.position, transform.position);
        if (dist > interactionRange + 1f) return;

        // Grant item
        var inventory = playerObj.GetComponent<WeaponInventory>();
        var health = playerObj.GetComponent<PlayerHealth>();
        LootItemType itemType = (LootItemType)assignedItemType.Value;

        switch (itemType)
        {
            case LootItemType.Shotgun:
            case LootItemType.SMG:
            case LootItemType.Rifle:
                if (inventory != null && weaponData != null)
                    inventory.AddWeapon(weaponData, itemType);
                break;

            case LootItemType.AmmoPack:
                if (inventory != null)
                    inventory.AddAmmo(ammoAmount);
                break;

            case LootItemType.HealthPack:
                if (health != null)
                    health.Heal(healAmount);
                break;
        }

        isAvailable.Value = false;

        // Despawn after brief delay for visual feedback
        GetComponent<NetworkObject>().Despawn(true);
    }

    private void OnAvailabilityChanged(bool oldVal, bool newVal)
    {
        if (!newVal && glowLight != null)
            glowLight.enabled = false;
    }
}

using UnityEngine;
using Unity.Netcode;
using System;

public class ExitGateway : NetworkBehaviour
{
    [Header("Escape Settings")]
    [SerializeField] private float escapeDuration = 2.5f;

    [Header("Visuals")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;
    [SerializeField] private Light beaconLight;

    public NetworkVariable<bool> IsUnlocked = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> EscapeProgress = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool isEscaping;
    private ulong escapingClientId;

    public event Action OnExitUnlocked;
    public event Action<float> OnEscapeProgressChanged; // 0-1
    public event Action OnEscapeCancelled;
    public event Action<ulong> OnEscapeComplete; // winnerClientId

    public static ExitGateway Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        IsUnlocked.OnValueChanged += OnUnlockStateChanged;
        EscapeProgress.OnValueChanged += OnProgressChanged;

        // Subscribe to key pickup to unlock — may need to wait for maze ready
        if (KeyManager.Instance != null)
        {
            KeyManager.Instance.OnKeyPickedUp += OnKeyPickedUp;
        }
        else if (!MazeGenerator.IsReady)
        {
            MazeGenerator.OnMazeReady += OnMazeReadySubscribeKey;
        }

        UpdateVisuals(IsUnlocked.Value);
    }

    private void OnMazeReadySubscribeKey()
    {
        MazeGenerator.OnMazeReady -= OnMazeReadySubscribeKey;
        if (KeyManager.Instance != null)
            KeyManager.Instance.OnKeyPickedUp += OnKeyPickedUp;
    }

    public override void OnNetworkDespawn()
    {
        IsUnlocked.OnValueChanged -= OnUnlockStateChanged;
        EscapeProgress.OnValueChanged -= OnProgressChanged;
        MazeGenerator.OnMazeReady -= OnMazeReadySubscribeKey;

        if (KeyManager.Instance != null)
            KeyManager.Instance.OnKeyPickedUp -= OnKeyPickedUp;

        if (Instance == this) Instance = null;
    }

    private void OnKeyPickedUp(ulong holderClientId)
    {
        if (IsServer)
        {
            IsUnlocked.Value = true;
            Debug.Log("[ExitGateway] Exit unlocked - key was picked up");
        }
    }

    private void Update()
    {
        if (!IsServer || !isEscaping) return;

        EscapeProgress.Value += Time.deltaTime / escapeDuration;

        if (EscapeProgress.Value >= 1f)
        {
            EscapeProgress.Value = 1f;
            isEscaping = false;
            OnEscapeComplete?.Invoke(escapingClientId);
            EscapeCompleteClientRpc(escapingClientId);
            Debug.Log($"[ExitGateway] Escape complete! Winner: client {escapingClientId}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!IsUnlocked.Value) return;

        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        var keyMgr = KeyManager.Instance;
        if (keyMgr == null || !keyMgr.IsKeyHolder(netObj.OwnerClientId)) return;

        BeginEscape(netObj.OwnerClientId);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer || !isEscaping) return;

        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        if (netObj.OwnerClientId == escapingClientId)
        {
            CancelEscape();
            Debug.Log("[ExitGateway] Escape cancelled - player left zone");
        }
    }

    private void BeginEscape(ulong clientId)
    {
        if (isEscaping) return;

        isEscaping = true;
        escapingClientId = clientId;
        EscapeProgress.Value = 0f;
        BeginEscapeClientRpc(clientId);
        Debug.Log($"[ExitGateway] Escape started by client {clientId}");
    }

    public void CancelEscape()
    {
        if (!IsServer || !isEscaping) return;

        isEscaping = false;
        EscapeProgress.Value = 0f;
        CancelEscapeClientRpc();
    }

    /// <summary>
    /// Called by health system when key holder takes damage during escape.
    /// </summary>
    public void OnKeyHolderDamaged(ulong damagedClientId)
    {
        if (!IsServer || !isEscaping) return;
        if (damagedClientId == escapingClientId)
        {
            CancelEscape();
            Debug.Log("[ExitGateway] Escape cancelled - player took damage");
        }
    }

    [ClientRpc]
    private void BeginEscapeClientRpc(ulong clientId)
    {
        // Lock movement for key holder
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            var movement = NetworkManager.Singleton.LocalClient.PlayerObject?.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;

            var combat = NetworkManager.Singleton.LocalClient.PlayerObject?.GetComponent<PlayerCombat>();
            if (combat != null) combat.enabled = false;
        }
    }

    [ClientRpc]
    private void CancelEscapeClientRpc()
    {
        OnEscapeCancelled?.Invoke();

        // Re-enable movement for local player if they were escaping
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer != null)
        {
            var movement = localPlayer.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = true;

            var combat = localPlayer.GetComponent<PlayerCombat>();
            if (combat != null) combat.enabled = true;
        }
    }

    [ClientRpc]
    private void EscapeCompleteClientRpc(ulong winnerClientId)
    {
        OnEscapeComplete?.Invoke(winnerClientId);
    }

    private void OnUnlockStateChanged(bool oldValue, bool newValue)
    {
        UpdateVisuals(newValue);
        if (newValue) OnExitUnlocked?.Invoke();
    }

    private void OnProgressChanged(float oldValue, float newValue)
    {
        OnEscapeProgressChanged?.Invoke(newValue);
    }

    private void UpdateVisuals(bool unlocked)
    {
        if (lockedVisual != null) lockedVisual.SetActive(!unlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(unlocked);
        if (beaconLight != null) beaconLight.enabled = unlocked;
    }
}

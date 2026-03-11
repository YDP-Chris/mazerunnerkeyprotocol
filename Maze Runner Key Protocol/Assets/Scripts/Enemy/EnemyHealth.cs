using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.AI;

/// <summary>
/// Enemy health with NetworkVariable sync. No respawn during match.
/// </summary>
public class EnemyHealth : NetworkBehaviour
{
    [SerializeField] private EnemyConfig config;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsEliminated { get; private set; }
    public event Action OnDied;
    public event Action<float, float> OnHealthChanged; // oldValue, newValue

    public override void OnNetworkSpawn()
    {
        if (IsServer && config != null)
        {
            CurrentHealth.Value = config.health;
        }

        CurrentHealth.OnValueChanged += HandleHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
    }

    public void TakeDamage(float amount)
    {
        if (!IsServer || IsEliminated || amount <= 0) return;

        CurrentHealth.Value = Mathf.Max(0f, CurrentHealth.Value - amount);

        if (CurrentHealth.Value <= 0f)
        {
            IsEliminated = true;
            OnDied?.Invoke();

            // Stop NavMeshAgent
            var agent = GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.enabled = false;
            }

            // Deactivate after short delay
            StartCoroutine(DeactivateAfterDelay(0.5f));
            Debug.Log($"[EnemyHealth] Enemy {gameObject.name} eliminated");
        }
    }

    private void HandleHealthChanged(float oldValue, float newValue)
    {
        OnHealthChanged?.Invoke(oldValue, newValue);
    }

    private System.Collections.IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    public void SetConfig(EnemyConfig newConfig)
    {
        config = newConfig;
    }
}

using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsEliminated { get; private set; }

    public event Action<int, int> OnDamaged;  // damageAmount, currentHealth
    public event Action<int, int> OnHealed;   // healAmount, currentHealth
    public event Action OnDied;

    public int MaxHealth => maxHealth;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
        }

        IsEliminated = false;
        CurrentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (newValue < oldValue)
            OnDamaged?.Invoke(oldValue - newValue, newValue);
        else if (newValue > oldValue)
            OnHealed?.Invoke(newValue - oldValue, newValue);
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer || IsEliminated || amount <= 0) return;

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);

        if (CurrentHealth.Value <= 0)
        {
            IsEliminated = true;
            OnDied?.Invoke();
            StartCoroutine(DeactivateAfterDelay(0.5f));
        }
    }

    public void Heal(int amount)
    {
        if (!IsServer || IsEliminated || amount <= 0) return;

        CurrentHealth.Value = Mathf.Min(maxHealth, CurrentHealth.Value + amount);
    }

    private IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}

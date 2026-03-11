using UnityEngine;

/// <summary>
/// Pure static helpers for health clamping logic — testable without NetworkBehaviour.
/// </summary>
public static class HealthHelper
{
    public static int ClampDamage(int currentHealth, int damage)
    {
        if (damage <= 0) return currentHealth;
        return Mathf.Max(0, currentHealth - damage);
    }

    public static int ClampHeal(int currentHealth, int maxHealth, int healAmount)
    {
        if (healAmount <= 0) return currentHealth;
        return Mathf.Min(maxHealth, currentHealth + healAmount);
    }

    public static float ClampDamageFloat(float currentHealth, float damage)
    {
        if (damage <= 0f) return currentHealth;
        return Mathf.Max(0f, currentHealth - damage);
    }

    public static bool IsEliminated(int currentHealth)
    {
        return currentHealth <= 0;
    }
}

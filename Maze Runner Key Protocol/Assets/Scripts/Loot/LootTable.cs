using UnityEngine;
using System.Collections.Generic;

public enum LootItemType { Shotgun, SMG, Rifle, AmmoPack, HealthPack }

[System.Serializable]
public struct LootTableEntry
{
    public LootItemType itemType;
    public WeaponData weaponData; // null for non-weapons
    public int weight;
}

[CreateAssetMenu(fileName = "LootTable", menuName = "MazeRunner/LootTable")]
public class LootTable : ScriptableObject
{
    public List<LootTableEntry> entries = new List<LootTableEntry>();

    public LootTableEntry Roll(System.Random rng)
    {
        int totalWeight = 0;
        foreach (var entry in entries)
            totalWeight += entry.weight;

        if (totalWeight <= 0)
            return entries[0];

        int roll = rng.Next(0, totalWeight);
        int cumulative = 0;
        foreach (var entry in entries)
        {
            cumulative += entry.weight;
            if (roll < cumulative)
                return entry;
        }

        return entries[entries.Count - 1];
    }
}

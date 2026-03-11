using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pure static helpers for loot placement logic — testable without NetworkBehaviour.
/// </summary>
public static class LootPlacementHelper
{
    public static int CalculateLootCount(int baseLootCount, int additionalPerPlayer, int playerCount, float mazeAreaInCells)
    {
        int lootCount = baseLootCount + Mathf.Max(0, playerCount - 2) * additionalPerPlayer;
        lootCount = Mathf.Max(lootCount, baseLootCount);
        int maxCap = Mathf.Max(8, (int)(mazeAreaInCells / 15f));
        return Mathf.Min(lootCount, maxCap);
    }

    public static bool IsValidPlacement(Vector3 candidate, List<Vector3> placed, List<Vector3> spawns, Vector3 keyPos,
        float minSeparation, float minDistFromSpawns, float minDistFromKey)
    {
        foreach (var p in placed)
        {
            if (Vector3.Distance(candidate, p) < minSeparation)
                return false;
        }

        foreach (var s in spawns)
        {
            if (Vector3.Distance(candidate, s) < minDistFromSpawns)
                return false;
        }

        if (Vector3.Distance(candidate, keyPos) < minDistFromKey)
            return false;

        return true;
    }

    public static List<Vector3> BuildFallbackPositions(float minX, float maxX, float minZ, float maxZ)
    {
        float midX = (minX + maxX) / 2f;
        float midZ = (minZ + maxZ) / 2f;
        float qX = (maxX - minX) / 4f;
        float qZ = (maxZ - minZ) / 4f;

        return new List<Vector3>
        {
            new Vector3(midX - qX, 0.5f, midZ - qZ),
            new Vector3(midX + qX, 0.5f, midZ - qZ),
            new Vector3(midX - qX, 0.5f, midZ + qZ),
            new Vector3(midX + qX, 0.5f, midZ + qZ)
        };
    }
}

using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class LootPlacementTests
{
    [Test]
    public void CalculateLootCount_OnePlayer_ReturnsBase()
    {
        int result = LootPlacementHelper.CalculateLootCount(8, 3, 1, 400f);
        Assert.AreEqual(8, result);
    }

    [Test]
    public void CalculateLootCount_TwoPlayers_ReturnsBase()
    {
        int result = LootPlacementHelper.CalculateLootCount(8, 3, 2, 400f);
        Assert.AreEqual(8, result);
    }

    [Test]
    public void CalculateLootCount_FourPlayers_Returns14()
    {
        // 8 + (4-2)*3 = 14
        int result = LootPlacementHelper.CalculateLootCount(8, 3, 4, 400f);
        Assert.AreEqual(14, result);
    }

    [Test]
    public void CalculateLootCount_EightPlayers_Returns26()
    {
        // 8 + (8-2)*3 = 26
        int result = LootPlacementHelper.CalculateLootCount(8, 3, 8, 400f);
        Assert.AreEqual(26, result);
    }

    [Test]
    public void MaxCap_SmallMaze_CapsAt8()
    {
        // 100 cells / 15 = 6.67 -> max(8, 6) = 8
        int result = LootPlacementHelper.CalculateLootCount(8, 3, 4, 100f);
        Assert.AreEqual(8, result);
    }

    [Test]
    public void MaxCap_LargeMaze_DoesNotCap14()
    {
        // 400 cells / 15 = 26.67 -> max(8, 26) = 26. 14 < 26, so no cap.
        int result = LootPlacementHelper.CalculateLootCount(8, 3, 4, 400f);
        Assert.AreEqual(14, result);
    }

    [Test]
    public void IsValidPlacement_TooCloseToLootBox_Rejected()
    {
        var placed = new List<Vector3> { new Vector3(10, 0, 10) };
        var spawns = new List<Vector3>();
        var key = new Vector3(50, 0, 50);

        bool result = LootPlacementHelper.IsValidPlacement(
            new Vector3(12, 0, 10), placed, spawns, key, 8f, 12f, 8f);

        Assert.IsFalse(result);
    }

    [Test]
    public void IsValidPlacement_TooCloseToSpawn_Rejected()
    {
        var placed = new List<Vector3>();
        var spawns = new List<Vector3> { new Vector3(10, 0, 10) };
        var key = new Vector3(50, 0, 50);

        bool result = LootPlacementHelper.IsValidPlacement(
            new Vector3(15, 0, 10), placed, spawns, key, 8f, 12f, 8f);

        Assert.IsFalse(result);
    }

    [Test]
    public void IsValidPlacement_TooCloseToKey_Rejected()
    {
        var placed = new List<Vector3>();
        var spawns = new List<Vector3>();
        var key = new Vector3(10, 0, 10);

        bool result = LootPlacementHelper.IsValidPlacement(
            new Vector3(12, 0, 10), placed, spawns, key, 8f, 12f, 8f);

        Assert.IsFalse(result);
    }

    [Test]
    public void IsValidPlacement_AllDistancesSatisfied_Accepted()
    {
        var placed = new List<Vector3> { new Vector3(0, 0, 0) };
        var spawns = new List<Vector3> { new Vector3(50, 0, 50) };
        var key = new Vector3(70, 0, 70);

        bool result = LootPlacementHelper.IsValidPlacement(
            new Vector3(30, 0, 30), placed, spawns, key, 8f, 12f, 8f);

        Assert.IsTrue(result);
    }

    [Test]
    public void BuildFallbackPositions_ReturnsExactly4()
    {
        var positions = LootPlacementHelper.BuildFallbackPositions(0, 80, 0, 80);
        Assert.AreEqual(4, positions.Count);
    }

    [Test]
    public void BuildFallbackPositions_InDifferentQuadrants()
    {
        var positions = LootPlacementHelper.BuildFallbackPositions(0, 80, 0, 80);
        float midX = 40f;
        float midZ = 40f;

        // Each quadrant should have exactly one position
        int q1 = 0, q2 = 0, q3 = 0, q4 = 0;
        foreach (var p in positions)
        {
            if (p.x < midX && p.z < midZ) q1++;
            else if (p.x > midX && p.z < midZ) q2++;
            else if (p.x < midX && p.z > midZ) q3++;
            else if (p.x > midX && p.z > midZ) q4++;
        }

        Assert.AreEqual(1, q1, "Q1 (low X, low Z)");
        Assert.AreEqual(1, q2, "Q2 (high X, low Z)");
        Assert.AreEqual(1, q3, "Q3 (low X, high Z)");
        Assert.AreEqual(1, q4, "Q4 (high X, high Z)");
    }

    [Test]
    public void FiftyAttemptGuard_ImpossibleConstraints_TerminatesAndUsesFallback()
    {
        // Simulate: all positions are invalid (everything too close)
        var placed = new List<Vector3>();
        var spawns = new List<Vector3>();
        var key = Vector3.zero;

        // Fill placed with a dense grid making everything invalid
        for (int x = 0; x < 80; x += 2)
            for (int z = 0; z < 80; z += 2)
                placed.Add(new Vector3(x, 0, z));

        var rng = new System.Random(42);
        Vector3? result = null;
        int maxAttempts = 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = new Vector3(
                (float)(rng.NextDouble() * 80),
                0.5f,
                (float)(rng.NextDouble() * 80));

            if (LootPlacementHelper.IsValidPlacement(candidate, placed, spawns, key, 8f, 12f, 8f))
            {
                result = candidate;
                break;
            }
        }

        // Should not have found a valid position
        Assert.IsNull(result, "Should not find valid position with dense grid");

        // Fallback should be available
        var fallbacks = LootPlacementHelper.BuildFallbackPositions(0, 80, 0, 80);
        Assert.AreEqual(4, fallbacks.Count);
    }

    [Test]
    public void FiftyAttemptGuard_ReasonableConstraints_FindsValidPosition()
    {
        var placed = new List<Vector3>();
        var spawns = new List<Vector3> { new Vector3(5, 0, 5) };
        var key = new Vector3(75, 0, 75);

        var rng = new System.Random(42);
        Vector3? result = null;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            var candidate = new Vector3(
                (float)(rng.NextDouble() * 76 + 2),
                0.5f,
                (float)(rng.NextDouble() * 76 + 2));

            if (LootPlacementHelper.IsValidPlacement(candidate, placed, spawns, key, 8f, 12f, 8f))
            {
                result = candidate;
                break;
            }
        }

        Assert.IsNotNull(result, "Should find valid position within 50 attempts");
    }
}

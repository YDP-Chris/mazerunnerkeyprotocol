using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class LootTableTests
{
    private LootTable CreateTable(params (LootItemType type, int weight)[] entries)
    {
        var table = ScriptableObject.CreateInstance<LootTable>();
        table.entries = new List<LootTableEntry>();
        foreach (var (type, weight) in entries)
        {
            table.entries.Add(new LootTableEntry { itemType = type, weight = weight });
        }
        return table;
    }

    [Test]
    public void EvenWeights_ProduceEvenDistribution()
    {
        var table = CreateTable(
            (LootItemType.Shotgun, 10),
            (LootItemType.SMG, 10),
            (LootItemType.Rifle, 10));

        var rng = new System.Random(42);
        var counts = new Dictionary<LootItemType, int>();
        int rolls = 10000;

        for (int i = 0; i < rolls; i++)
        {
            var entry = table.Roll(rng);
            if (!counts.ContainsKey(entry.itemType)) counts[entry.itemType] = 0;
            counts[entry.itemType]++;
        }

        float expected = rolls / 3f;
        foreach (var kvp in counts)
        {
            float pct = kvp.Value / (float)rolls;
            Assert.That(pct, Is.InRange(0.28f, 0.38f),
                $"{kvp.Key} was {pct:P1}, expected ~33%");
        }
    }

    [Test]
    public void SkewedWeights_FavorHeavyEntry()
    {
        var table = CreateTable(
            (LootItemType.Shotgun, 80),
            (LootItemType.SMG, 10),
            (LootItemType.Rifle, 10));

        var rng = new System.Random(42);
        int shotgunCount = 0;
        int rolls = 10000;

        for (int i = 0; i < rolls; i++)
        {
            if (table.Roll(rng).itemType == LootItemType.Shotgun)
                shotgunCount++;
        }

        float pct = shotgunCount / (float)rolls;
        Assert.That(pct, Is.InRange(0.75f, 0.85f),
            $"Shotgun was {pct:P1}, expected 75-85%");
    }

    [Test]
    public void SingleEntry_AlwaysReturnsThatEntry()
    {
        var table = CreateTable((LootItemType.HealthPack, 10));
        var rng = new System.Random(42);

        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(LootItemType.HealthPack, table.Roll(rng).itemType);
        }
    }

    [Test]
    public void AllWeightsZero_ReturnsFirstEntry()
    {
        var table = CreateTable(
            (LootItemType.Shotgun, 0),
            (LootItemType.SMG, 0));

        var rng = new System.Random(42);
        var result = table.Roll(rng);

        Assert.AreEqual(LootItemType.Shotgun, result.itemType);
    }

    [Test]
    public void ZeroWeightEntries_NeverReturnedWhenMixedWithPositive()
    {
        var table = CreateTable(
            (LootItemType.Shotgun, 0),
            (LootItemType.SMG, 10));

        var rng = new System.Random(42);
        for (int i = 0; i < 1000; i++)
        {
            Assert.AreEqual(LootItemType.SMG, table.Roll(rng).itemType);
        }
    }

    [Test]
    public void SameSeed_ProducesIdenticalSequence()
    {
        var table = CreateTable(
            (LootItemType.Shotgun, 10),
            (LootItemType.SMG, 10),
            (LootItemType.Rifle, 10));

        var rng1 = new System.Random(12345);
        var rng2 = new System.Random(12345);

        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(table.Roll(rng1).itemType, table.Roll(rng2).itemType,
                $"Divergence at roll {i}");
        }
    }
}

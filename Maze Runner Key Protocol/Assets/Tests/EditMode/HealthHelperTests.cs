using NUnit.Framework;

[TestFixture]
public class HealthHelperTests
{
    [Test]
    public void ClampDamage_DoesNotProduceNegative()
    {
        Assert.AreEqual(0, HealthHelper.ClampDamage(50, 100));
    }

    [Test]
    public void ClampDamage_EqualToCurrentProducesZero()
    {
        Assert.AreEqual(0, HealthHelper.ClampDamage(50, 50));
    }

    [Test]
    public void ClampHeal_DoesNotExceedMax()
    {
        Assert.AreEqual(100, HealthHelper.ClampHeal(80, 100, 50));
    }

    [Test]
    public void ClampDamage_ZeroDamage_Unchanged()
    {
        Assert.AreEqual(75, HealthHelper.ClampDamage(75, 0));
    }

    [Test]
    public void ClampDamage_NegativeDamage_Unchanged()
    {
        Assert.AreEqual(75, HealthHelper.ClampDamage(75, -10));
    }

    [Test]
    public void ClampHeal_ZeroHeal_Unchanged()
    {
        Assert.AreEqual(50, HealthHelper.ClampHeal(50, 100, 0));
    }

    [Test]
    public void ClampHeal_NegativeHeal_Unchanged()
    {
        Assert.AreEqual(50, HealthHelper.ClampHeal(50, 100, -10));
    }

    [Test]
    public void ClampDamageFloat_AccumulatesCorrectly()
    {
        float health = 50f;
        health = HealthHelper.ClampDamageFloat(health, 7.5f);
        Assert.AreEqual(42.5f, health, 0.001f);
        health = HealthHelper.ClampDamageFloat(health, 7.5f);
        Assert.AreEqual(35f, health, 0.001f);
    }

    [Test]
    public void IsEliminated_ZeroHealth_True()
    {
        Assert.IsTrue(HealthHelper.IsEliminated(0));
    }

    [Test]
    public void IsEliminated_PositiveHealth_False()
    {
        Assert.IsFalse(HealthHelper.IsEliminated(1));
    }
}

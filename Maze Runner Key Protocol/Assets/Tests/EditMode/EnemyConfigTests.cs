using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class EnemyConfigTests
{
    private EnemyConfig CreateGruntConfig()
    {
        var config = ScriptableObject.CreateInstance<EnemyConfig>();
        config.health = 50f;
        config.patrolSpeed = 2f;
        config.alertSpeed = 3.5f;
        config.chaseSpeed = 5f;
        config.attackSpeed = 1f;
        config.attackRange = 2.5f;
        config.attackDamage = 10f;
        config.attackInterval = 1.5f;
        config.sightRange = 15f;
        config.sightAngle = 90f;
        config.soundDetectionRadius = 20f;
        config.searchDuration = 10f;
        config.memoryDuration = 5f;
        config.patrolBehavior = EnemyConfig.PatrolBehavior.Patrol;
        return config;
    }

    private EnemyConfig CreateGuardConfig()
    {
        var config = CreateGruntConfig();
        config.health = 80f;
        config.patrolBehavior = EnemyConfig.PatrolBehavior.Stationary;
        return config;
    }

    [Test]
    public void Grunt_HasPositiveHealth()
    {
        var config = CreateGruntConfig();
        Assert.Greater(config.health, 0f);
    }

    [Test]
    public void Grunt_HasPatrolBehavior()
    {
        var config = CreateGruntConfig();
        Assert.AreEqual(EnemyConfig.PatrolBehavior.Patrol, config.patrolBehavior);
    }

    [Test]
    public void Guard_HasStationaryBehavior()
    {
        var config = CreateGuardConfig();
        Assert.AreEqual(EnemyConfig.PatrolBehavior.Stationary, config.patrolBehavior);
    }

    [Test]
    public void Guard_HealthGreaterThanOrEqualGrunt()
    {
        var grunt = CreateGruntConfig();
        var guard = CreateGuardConfig();
        Assert.GreaterOrEqual(guard.health, grunt.health);
    }

    [Test]
    public void AllSpeeds_ArePositive()
    {
        var config = CreateGruntConfig();
        Assert.Greater(config.patrolSpeed, 0f);
        Assert.Greater(config.alertSpeed, 0f);
        Assert.Greater(config.chaseSpeed, 0f);
        Assert.Greater(config.attackSpeed, 0f);
    }

    [Test]
    public void ChaseSpeed_GreaterThanOrEqualPatrolSpeed()
    {
        var config = CreateGruntConfig();
        Assert.GreaterOrEqual(config.chaseSpeed, config.patrolSpeed);
    }

    [Test]
    public void PerceptionRanges_ArePositive()
    {
        var config = CreateGruntConfig();
        Assert.Greater(config.sightRange, 0f);
        Assert.Greater(config.sightAngle, 0f);
        Assert.Greater(config.soundDetectionRadius, 0f);
    }

    [Test]
    public void TimerDurations_ArePositive()
    {
        var config = CreateGruntConfig();
        Assert.Greater(config.searchDuration, 0f);
        Assert.Greater(config.memoryDuration, 0f);
    }

    [Test]
    public void CombatValues_ArePositive()
    {
        var config = CreateGruntConfig();
        Assert.Greater(config.attackRange, 0f);
        Assert.Greater(config.attackDamage, 0f);
        Assert.Greater(config.attackInterval, 0f);
    }
}

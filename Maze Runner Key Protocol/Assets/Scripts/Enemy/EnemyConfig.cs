using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "MazeRunner/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    public enum PatrolBehavior { Patrol, Stationary }

    [Header("Health")]
    public float health = 50f;

    [Header("Movement Speeds")]
    public float patrolSpeed = 2f;
    public float alertSpeed = 3.5f;
    public float chaseSpeed = 5f;
    public float attackSpeed = 1f;

    [Header("Combat")]
    public float attackRange = 2.5f;
    public float attackDamage = 10f;
    public float attackInterval = 1.5f;

    [Header("Perception - Sight")]
    public float sightRange = 15f;
    public float sightAngle = 90f;

    [Header("Perception - Sound")]
    public float soundDetectionRadius = 20f;

    [Header("Timers")]
    public float searchDuration = 10f;
    public float memoryDuration = 5f;

    [Header("Navigation")]
    public float waypointArrivalThreshold = 0.5f;

    [Header("Behavior")]
    public PatrolBehavior patrolBehavior = PatrolBehavior.Patrol;
}

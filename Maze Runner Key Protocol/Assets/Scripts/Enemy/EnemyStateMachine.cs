using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections.Generic;

public enum EnemyState : byte { PATROL, INVESTIGATE, CHASE, ATTACK, SEARCH }

/// <summary>
/// 5-state enemy AI. Runs on host only, syncs state to clients via NetworkVariable.
/// Behavior driven by EnemyConfig ScriptableObject.
/// </summary>
public class EnemyStateMachine : NetworkBehaviour
{
    [SerializeField] private EnemyConfig config;

    public NetworkVariable<EnemyState> CurrentState = new NetworkVariable<EnemyState>(
        EnemyState.PATROL, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private EnemyPerception perception;
    private EnemyHealth enemyHealth;
    private NavMeshAgent agent;

    // Patrol
    private List<Vector3> patrolWaypoints = new List<Vector3>();
    private int currentWaypointIndex;

    // Investigation
    private Vector3 investigateTarget;

    // Chase
    private Transform chaseTarget;
    private float losLostTimer;
    private bool hasLOS;

    // Attack
    private float attackCooldown;

    // Search
    private float searchTimer;
    private Vector3 searchOrigin;
    private int searchWaypointIndex;
    private List<Vector3> searchWaypoints = new List<Vector3>();

    // Stuck detection
    private Vector3 lastPositionCheck;
    private float stuckTimer;
    private int stuckAttempts;
    private const float STUCK_THRESHOLD = 0.1f;
    private const float STUCK_TIMEOUT = 3f;

    // Guard
    private Vector3 spawnPosition;

    public override void OnNetworkSpawn()
    {
        perception = GetComponent<EnemyPerception>();
        enemyHealth = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();

        if (!IsServer)
        {
            if (agent != null) agent.enabled = false;
            enabled = false;
            return;
        }

        spawnPosition = transform.position;
        lastPositionCheck = transform.position;

        if (enemyHealth != null)
            enemyHealth.OnDied += OnEnemyDied;

        // Subscribe to match end
        if (MatchManager.Instance != null)
            MatchManager.Instance.OnMatchEnded += OnMatchEnded;

        EnterState(EnemyState.PATROL);
    }

    public override void OnNetworkDespawn()
    {
        if (enemyHealth != null)
            enemyHealth.OnDied -= OnEnemyDied;

        if (MatchManager.Instance != null)
            MatchManager.Instance.OnMatchEnded -= OnMatchEnded;
    }

    private void Update()
    {
        if (!IsServer || enemyHealth == null || enemyHealth.IsEliminated) return;

        UpdateCurrentState();
        CheckStuck();
    }

    public void SetPatrolWaypoints(List<Vector3> waypoints)
    {
        patrolWaypoints = waypoints;
    }

    public void SetConfig(EnemyConfig newConfig)
    {
        config = newConfig;
    }

    // --- State Transitions ---

    private void TransitionTo(EnemyState newState)
    {
        if (CurrentState.Value == newState) return;

        ExitState(CurrentState.Value);
        CurrentState.Value = newState;
        EnterState(newState);
    }

    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.PATROL:
                agent.speed = config.patrolSpeed;
                if (config.patrolBehavior == EnemyConfig.PatrolBehavior.Patrol && patrolWaypoints.Count > 0)
                    SetDestinationSafe(patrolWaypoints[currentWaypointIndex]);
                else if (config.patrolBehavior == EnemyConfig.PatrolBehavior.Stationary)
                    agent.ResetPath();
                break;

            case EnemyState.INVESTIGATE:
                agent.speed = config.alertSpeed;
                SetDestinationSafe(investigateTarget);
                break;

            case EnemyState.CHASE:
                agent.speed = config.chaseSpeed;
                losLostTimer = 0f;
                hasLOS = true;
                break;

            case EnemyState.ATTACK:
                agent.speed = config.attackSpeed;
                attackCooldown = 0f;
                break;

            case EnemyState.SEARCH:
                agent.speed = config.alertSpeed;
                searchTimer = 0f;
                searchWaypointIndex = 0;
                perception.GetLastKnownPosition(out searchOrigin);
                SetDestinationSafe(searchOrigin);
                BuildSearchWaypoints();
                break;
        }
    }

    private void ExitState(EnemyState state)
    {
        // Cleanup if needed per state
    }

    // --- State Updates ---

    private void UpdateCurrentState()
    {
        switch (CurrentState.Value)
        {
            case EnemyState.PATROL: UpdatePatrol(); break;
            case EnemyState.INVESTIGATE: UpdateInvestigate(); break;
            case EnemyState.CHASE: UpdateChase(); break;
            case EnemyState.ATTACK: UpdateAttack(); break;
            case EnemyState.SEARCH: UpdateSearch(); break;
        }
    }

    private void UpdatePatrol()
    {
        perception.UpdatePerception(0.4f); // Throttled

        // Check for visible player
        var visible = perception.GetVisiblePlayer();
        if (visible != null)
        {
            chaseTarget = visible;
            TransitionTo(EnemyState.CHASE);
            return;
        }

        // Check for sound
        if (perception.GetLastKnownPosition(out Vector3 soundPos))
        {
            investigateTarget = soundPos;
            TransitionTo(EnemyState.INVESTIGATE);
            return;
        }

        // Waypoint cycling (Patrol type only)
        if (config.patrolBehavior == EnemyConfig.PatrolBehavior.Patrol && patrolWaypoints.Count > 0)
        {
            if (!agent.pathPending && agent.remainingDistance <= config.waypointArrivalThreshold)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Count;
                SetDestinationSafe(patrolWaypoints[currentWaypointIndex]);
            }
        }
    }

    private void UpdateInvestigate()
    {
        perception.UpdatePerception(0.2f);

        // Check for visible player
        var visible = perception.GetVisiblePlayer();
        if (visible != null)
        {
            chaseTarget = visible;
            TransitionTo(EnemyState.CHASE);
            return;
        }

        // Check for new sound closer than current target
        if (perception.GetLastKnownPosition(out Vector3 newSoundPos))
        {
            float currentDist = Vector3.Distance(transform.position, investigateTarget);
            float newDist = Vector3.Distance(transform.position, newSoundPos);
            if (newDist < currentDist)
            {
                investigateTarget = newSoundPos;
                SetDestinationSafe(investigateTarget);
            }
        }

        // Reached investigation target
        if (!agent.pathPending && agent.remainingDistance <= config.waypointArrivalThreshold)
        {
            perception.ClearMemory();
            TransitionTo(EnemyState.PATROL);
        }
    }

    private void UpdateChase()
    {
        perception.UpdatePerception(0f); // Every frame

        var visible = perception.GetVisiblePlayer();
        if (visible != null)
        {
            chaseTarget = visible;
            hasLOS = true;
            losLostTimer = 0f;

            // Check attack range
            float dist = Vector3.Distance(transform.position, chaseTarget.position);
            if (dist <= config.attackRange)
            {
                TransitionTo(EnemyState.ATTACK);
                return;
            }

            SetDestinationSafe(chaseTarget.position);
        }
        else
        {
            // LOS lost
            if (hasLOS)
            {
                hasLOS = false;
                losLostTimer = 0f;
            }

            losLostTimer += Time.deltaTime;

            // Navigate to last known position
            if (perception.GetLastKnownPosition(out Vector3 lastPos))
                SetDestinationSafe(lastPos);

            if (losLostTimer >= 5f)
            {
                TransitionTo(EnemyState.SEARCH);
                return;
            }
        }

        // Target eliminated
        if (chaseTarget != null)
        {
            var targetHealth = chaseTarget.GetComponent<PlayerHealth>();
            if (targetHealth != null && targetHealth.IsEliminated)
            {
                perception.ClearMemory();
                TransitionTo(EnemyState.PATROL);
            }
        }
    }

    private void UpdateAttack()
    {
        perception.UpdatePerception(0f);

        if (chaseTarget == null)
        {
            TransitionTo(EnemyState.PATROL);
            return;
        }

        // Check if target eliminated
        var targetHealth = chaseTarget.GetComponent<PlayerHealth>();
        if (targetHealth != null && targetHealth.IsEliminated)
        {
            perception.ClearMemory();
            TransitionTo(EnemyState.PATROL);
            return;
        }

        float dist = Vector3.Distance(transform.position, chaseTarget.position);

        // Out of range or LOS lost → chase
        if (dist > config.attackRange || perception.GetVisiblePlayer() == null)
        {
            TransitionTo(EnemyState.CHASE);
            return;
        }

        // Face target
        Vector3 lookDir = (chaseTarget.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(lookDir);

        // Deal damage on cooldown
        attackCooldown -= Time.deltaTime;
        if (attackCooldown <= 0f)
        {
            attackCooldown = config.attackInterval;
            if (targetHealth != null)
            {
                targetHealth.TakeDamage((int)config.attackDamage);
                Debug.Log($"[EnemyAI] {gameObject.name} attacks player for {config.attackDamage} damage");
            }
        }
    }

    private void UpdateSearch()
    {
        perception.UpdatePerception(0.1f);
        searchTimer += Time.deltaTime;

        // Found player
        var visible = perception.GetVisiblePlayer();
        if (visible != null)
        {
            chaseTarget = visible;
            TransitionTo(EnemyState.CHASE);
            return;
        }

        // Search timeout
        if (searchTimer >= config.searchDuration)
        {
            perception.ClearMemory();
            if (config.patrolBehavior == EnemyConfig.PatrolBehavior.Stationary)
            {
                // Guard returns to spawn
                SetDestinationSafe(spawnPosition);
            }
            TransitionTo(EnemyState.PATROL);
            return;
        }

        // Navigate through search waypoints
        if (!agent.pathPending && agent.remainingDistance <= config.waypointArrivalThreshold)
        {
            if (searchWaypointIndex < searchWaypoints.Count)
            {
                SetDestinationSafe(searchWaypoints[searchWaypointIndex]);
                searchWaypointIndex++;
            }
        }
    }

    // --- Helpers ---

    private void BuildSearchWaypoints()
    {
        searchWaypoints.Clear();
        float searchRadius = 10f;

        // Find up to 3 nearby patrol waypoints
        foreach (var wp in patrolWaypoints)
        {
            if (Vector3.Distance(searchOrigin, wp) <= searchRadius)
            {
                searchWaypoints.Add(wp);
                if (searchWaypoints.Count >= 3) break;
            }
        }
    }

    private void SetDestinationSafe(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh && agent.enabled)
            agent.SetDestination(destination);
    }

    private void CheckStuck()
    {
        float movedDist = Vector3.Distance(transform.position, lastPositionCheck);

        if (agent.hasPath && movedDist < STUCK_THRESHOLD)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= STUCK_TIMEOUT)
            {
                stuckAttempts++;
                Debug.LogWarning($"[EnemyAI] {gameObject.name} stuck at {transform.position}, target: {agent.destination} (attempt {stuckAttempts})");

                agent.ResetPath();

                if (stuckAttempts >= 2)
                {
                    // Fallback to nearest patrol waypoint
                    stuckAttempts = 0;
                    if (patrolWaypoints.Count > 0)
                    {
                        Vector3 nearest = patrolWaypoints[0];
                        float nearestDist = float.MaxValue;
                        foreach (var wp in patrolWaypoints)
                        {
                            float d = Vector3.Distance(transform.position, wp);
                            if (d < nearestDist) { nearestDist = d; nearest = wp; }
                        }
                        SetDestinationSafe(nearest);
                    }
                    TransitionTo(EnemyState.PATROL);
                }
                else
                {
                    // Recalculate path
                    SetDestinationSafe(agent.destination);
                }

                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
            stuckAttempts = 0;
        }

        lastPositionCheck = transform.position;
    }

    private void OnEnemyDied()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
        enabled = false;
    }

    private void OnMatchEnded(MatchManager.MatchOutcome outcome, ulong winnerId)
    {
        // Stop all AI on match end
        if (agent != null && agent.isOnNavMesh)
            agent.ResetPath();
        enabled = false;
    }
}

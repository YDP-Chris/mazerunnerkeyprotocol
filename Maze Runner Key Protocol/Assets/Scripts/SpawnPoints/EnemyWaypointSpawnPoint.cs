using UnityEngine;

public class EnemyWaypointSpawnPoint : MonoBehaviour
{
    [Tooltip("Patrol route index this waypoint belongs to")]
    public int patrolRouteIndex;

    [Tooltip("Order within the patrol route")]
    public int waypointOrder;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.4f);
    }
}

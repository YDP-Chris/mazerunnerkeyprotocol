using UnityEngine;

public class KeySpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.84f, 0f); // Gold
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}

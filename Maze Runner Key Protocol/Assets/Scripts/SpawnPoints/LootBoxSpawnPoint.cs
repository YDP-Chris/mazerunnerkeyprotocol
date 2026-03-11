using UnityEngine;

public class LootBoxSpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
        Gizmos.DrawSphere(transform.position, 0.4f);
    }
}

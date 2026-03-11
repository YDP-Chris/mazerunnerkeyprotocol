using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private int damage = 15;
    [SerializeField] private float fireRate = 0.3f;
    [SerializeField] private float range = 50f;
    [SerializeField] private LayerMask damageableMask;

    [Header("References")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject muzzleFlashObject;
    [SerializeField] private GameObject hitImpactPrefab;
    [SerializeField] private GameObject wallImpactPrefab;
    [SerializeField] private AudioClip fireSound;

    private PlayerInputActions inputActions;
    private AudioSource audioSource;
    private float nextFireTime;
    private bool isEliminated;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
        audioSource = GetComponent<AudioSource>();

        if (muzzleFlashObject != null)
            muzzleFlashObject.SetActive(false);

        var health = GetComponent<PlayerHealth>();
        if (health != null)
            health.OnDied += () => isEliminated = true;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputActions?.Player.Disable();
        inputActions?.Dispose();
    }

    private void Update()
    {
        if (!IsOwner || isEliminated) return;

        if (inputActions.Player.Fire.WasPressedThisFrame() && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Fire();
        }
    }

    private void Fire()
    {
        // Muzzle flash
        if (muzzleFlashObject != null)
            StartCoroutine(ShowMuzzleFlash());

        // Audio
        if (audioSource != null && fireSound != null)
            audioSource.PlayOneShot(fireSound);

        // Hitscan from camera center
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            // Check if we hit a damageable target
            if (((1 << hit.collider.gameObject.layer) & damageableMask) != 0)
            {
                var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (targetHealth != null)
                {
                    var targetNetObj = targetHealth.GetComponent<NetworkObject>();
                    if (targetNetObj != null)
                    {
                        DealDamageServerRpc(targetNetObj.NetworkObjectId, damage);
                    }
                }

                // Hit impact on damageable
                if (hitImpactPrefab != null)
                    SpawnImpact(hitImpactPrefab, hit.point, hit.normal);
            }
            else
            {
                // Wall/environment impact
                if (wallImpactPrefab != null)
                    SpawnImpact(wallImpactPrefab, hit.point, hit.normal);
            }
        }
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong targetNetworkObjectId, int dmg)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var targetObj))
        {
            var health = targetObj.GetComponent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(dmg);
        }
    }

    private IEnumerator ShowMuzzleFlash()
    {
        muzzleFlashObject.SetActive(true);
        yield return new WaitForSeconds(0.07f);
        muzzleFlashObject.SetActive(false);
    }

    private void SpawnImpact(GameObject prefab, Vector3 position, Vector3 normal)
    {
        var impact = Instantiate(prefab, position, Quaternion.LookRotation(normal));
        Destroy(impact, 2f);
    }
}

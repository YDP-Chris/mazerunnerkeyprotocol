using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections;

public class PlayerCombat : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private LayerMask damageableMask;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject muzzleFlashObject;
    [SerializeField] private GameObject hitImpactPrefab;
    [SerializeField] private GameObject wallImpactPrefab;
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip emptySound;

    private PlayerInputActions inputActions;
    private AudioSource audioSource;
    private WeaponInventory inventory;
    private float nextFireTime;
    private bool isEliminated;
    private bool fireHeld;

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
        inventory = GetComponent<WeaponInventory>();

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
        if (!IsOwner || isEliminated || inputActions == null || inventory == null) return;

        // Weapon switching
        HandleWeaponSwitching();

        // Firing
        var weapon = inventory.GetEquippedWeapon();
        if (weapon == null) return;

        bool firePressed = inputActions.Player.Fire.WasPressedThisFrame();
        bool fireHeldNow = inputActions.Player.Fire.IsPressed();

        if (weapon.fireMode == FireMode.Single)
        {
            if (firePressed && Time.time >= nextFireTime)
            {
                if (TryFire(weapon))
                    nextFireTime = Time.time + (1f / weapon.fireRate);
            }
        }
        else // Automatic
        {
            if (fireHeldNow && Time.time >= nextFireTime)
            {
                if (TryFire(weapon))
                    nextFireTime = Time.time + (1f / weapon.fireRate);
            }
        }
    }

    private void HandleWeaponSwitching()
    {
        if (inputActions.Player.Weapon1.WasPressedThisFrame())
        {
            inventory.SwitchToSlot(0);
            nextFireTime = 0f; // Reset cooldown on switch
        }
        else if (inputActions.Player.Weapon2.WasPressedThisFrame())
        {
            inventory.SwitchToSlot(1);
            nextFireTime = 0f;
        }
        else if (inputActions.Player.Weapon3.WasPressedThisFrame())
        {
            inventory.SwitchToSlot(2);
            nextFireTime = 0f;
        }

        // Scroll wheel
        float scroll = inputActions.Player.ScrollWeapon.ReadValue<float>();
        if (scroll > 0.1f)
        {
            inventory.CycleWeapon(1);
            nextFireTime = 0f;
        }
        else if (scroll < -0.1f)
        {
            inventory.CycleWeapon(-1);
            nextFireTime = 0f;
        }
    }

    private bool TryFire(WeaponData weapon)
    {
        int slot = inventory.equippedSlot.Value;

        // Ammo check (skip for pistol slot 0)
        if (slot != 0)
        {
            if (inventory.GetEquippedAmmo() <= 0)
            {
                // Empty weapon cue
                if (audioSource != null && emptySound != null)
                    audioSource.PlayOneShot(emptySound);
                return false;
            }
        }

        // Consume ammo
        if (slot != 0)
            inventory.ConsumeAmmo();

        // Visual/audio feedback
        if (muzzleFlashObject != null)
            StartCoroutine(ShowMuzzleFlash());
        if (audioSource != null && fireSound != null)
            audioSource.PlayOneShot(fireSound);

        // Broadcast gunshot sound for enemy AI
        if (IsServer)
            SoundEventSystem.BroadcastSound(transform.position, 20f, SoundType.Gunshot);
        else
            BroadcastGunshotServerRpc(transform.position);

        // Hitscan
        Camera cam = Camera.main;
        if (cam == null) return true;

        if (weapon.pelletCount > 1)
            FireMultiRay(cam, weapon);
        else
            FireSingleRay(cam, weapon);

        return true;
    }

    private void FireSingleRay(Camera cam, WeaponData weapon)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // Apply spread
        if (weapon.spreadAngle > 0f)
            ray.direction = ApplySpread(ray.direction, weapon.spreadAngle);

        ProcessRaycast(ray, weapon);
    }

    private void FireMultiRay(Camera cam, WeaponData weapon)
    {
        Ray baseRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        for (int i = 0; i < weapon.pelletCount; i++)
        {
            Ray pelletRay = baseRay;
            pelletRay.direction = ApplySpread(baseRay.direction, weapon.spreadAngle);
            ProcessRaycast(pelletRay, weapon);
        }
    }

    private void ProcessRaycast(Ray ray, WeaponData weapon)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, weapon.effectiveRange))
        {
            if (((1 << hit.collider.gameObject.layer) & damageableMask) != 0)
            {
                var targetNetObj = hit.collider.GetComponentInParent<NetworkObject>();
                if (targetNetObj != null)
                {
                    bool hasDamageable = targetNetObj.GetComponent<PlayerHealth>() != null
                        || targetNetObj.GetComponent<EnemyHealth>() != null;

                    if (hasDamageable)
                        DealDamageServerRpc(targetNetObj.NetworkObjectId, Mathf.RoundToInt(weapon.damage));
                }

                if (hitImpactPrefab != null)
                    SpawnImpact(hitImpactPrefab, hit.point, hit.normal);
            }
            else
            {
                if (wallImpactPrefab != null)
                    SpawnImpact(wallImpactPrefab, hit.point, hit.normal);
            }
        }
    }

    private Vector3 ApplySpread(Vector3 direction, float spreadAngle)
    {
        float halfAngle = spreadAngle * 0.5f;
        float randomAngle = Random.Range(-halfAngle, halfAngle);
        float randomRotation = Random.Range(0f, 360f);

        Quaternion spreadRotation = Quaternion.AngleAxis(randomAngle, Vector3.up) *
                                     Quaternion.AngleAxis(randomRotation, direction);
        // Proper cone spread
        Vector3 randomDir = Quaternion.AngleAxis(randomAngle,
            Quaternion.AngleAxis(randomRotation, direction) * Vector3.up) * direction;
        return randomDir.normalized;
    }

    [ServerRpc]
    private void BroadcastGunshotServerRpc(Vector3 position)
    {
        SoundEventSystem.BroadcastSound(position, 20f, SoundType.Gunshot);
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong targetNetworkObjectId, int dmg)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var targetObj))
        {
            var playerHealth = targetObj.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(dmg);
                return;
            }

            var enemyHealth = targetObj.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
                enemyHealth.TakeDamage(dmg);
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

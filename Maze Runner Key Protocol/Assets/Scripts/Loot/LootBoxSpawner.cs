using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class LootBoxSpawner : NetworkBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject lootBoxPrefab;

    [Header("Loot Table")]
    [SerializeField] private LootTable lootTable;
    [SerializeField] private AmmoPickupData ammoPickupData;
    [SerializeField] private HealthPackData healthPackData;

    [Header("Weapon References")]
    [SerializeField] private WeaponData shotgunData;
    [SerializeField] private WeaponData smgData;
    [SerializeField] private WeaponData rifleData;

    [Header("Density Settings")]
    [SerializeField] private int baseLootCount = 8;
    [SerializeField] private int additionalLootPerPlayer = 3;

    [Header("Placement Settings")]
    [SerializeField] private float minSeparation = 8f; // 2 tiles * 4 units
    [SerializeField] private float minDistFromSpawns = 12f; // 3 tiles
    [SerializeField] private float minDistFromKey = 8f; // 2 tiles
    [SerializeField] private int maxAttempts = 50;

    private float mazeMinX;
    private float mazeMaxX;
    private float mazeMinZ;
    private float mazeMaxZ;

    private List<Vector3> placedPositions = new List<Vector3>();
    private List<Vector3> fallbackPositions = new List<Vector3>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Wait for MazeGenerator to signal ready
        if (MazeGenerator.IsReady)
        {
            OnMazeReady();
        }
        else
        {
            MazeGenerator.OnMazeReady += OnMazeReady;
        }
    }

    public override void OnNetworkDespawn()
    {
        MazeGenerator.OnMazeReady -= OnMazeReady;
    }

    private void OnMazeReady()
    {
        MazeGenerator.OnMazeReady -= OnMazeReady;

        // Read bounds from MazeGenerator
        mazeMinX = MazeGenerator.MinX;
        mazeMaxX = MazeGenerator.MaxX;
        mazeMinZ = MazeGenerator.MinZ;
        mazeMaxZ = MazeGenerator.MaxZ;

        // Delay slightly for key placement to complete first
        StartCoroutine(SpawnAfterDelay());
    }

    private System.Collections.IEnumerator SpawnAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        SpawnLootBoxes();
    }

    private void SpawnLootBoxes()
    {
        if (lootBoxPrefab == null || lootTable == null) return;

        // Deterministic RNG from match seed
        int seed = UnityEngine.Random.Range(0, int.MaxValue);
        var rng = new System.Random(seed);

        // Build fallback positions (4 quadrants)
        BuildFallbackPositions();

        // Calculate loot count
        int playerCount = Mathf.Max(1, NetworkManager.Singleton.ConnectedClientsIds.Count);
        int lootCount = baseLootCount + Mathf.Max(0, playerCount - 2) * additionalLootPerPlayer;
        lootCount = Mathf.Max(lootCount, baseLootCount);

        // Max cap based on maze area
        float mazeAreaInCells = ((mazeMaxX - mazeMinX) * (mazeMaxZ - mazeMinZ)) / 16f; // ~4x4 cell size
        int maxCap = Mathf.Max(8, (int)(mazeAreaInCells / 15f));
        lootCount = Mathf.Min(lootCount, maxCap);

        // Gather exclusion zones
        List<Vector3> playerSpawns = GetPlayerSpawnPositions();
        float centerX = (mazeMinX + mazeMaxX) / 2f;
        float centerZ = (mazeMinZ + mazeMaxZ) / 2f;
        Vector3 keyPos = KeyManager.Instance != null
            ? KeyManager.Instance.KeyWorldPosition.Value
            : new Vector3(centerX, 0f, centerZ);

        int fallbackIndex = 0;

        for (int i = 0; i < lootCount; i++)
        {
            Vector3? position = null;

            // 50-attempt placement loop
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 candidate = new Vector3(
                    (float)(rng.NextDouble() * (mazeMaxX - mazeMinX) + mazeMinX),
                    0.5f,
                    (float)(rng.NextDouble() * (mazeMaxZ - mazeMinZ) + mazeMinZ));

                if (IsValidPosition(candidate, playerSpawns, keyPos))
                {
                    position = candidate;
                    break;
                }
            }

            // Fallback
            if (!position.HasValue && fallbackIndex < fallbackPositions.Count)
            {
                position = fallbackPositions[fallbackIndex];
                fallbackIndex++;
                Debug.LogWarning($"[LootBoxSpawner] Used fallback position for loot box {i}");
            }

            if (!position.HasValue)
            {
                Debug.LogWarning($"[LootBoxSpawner] Could not place loot box {i}, no fallback available");
                continue;
            }

            // Roll loot table
            var entry = lootTable.Roll(rng);

            // Spawn
            var box = Instantiate(lootBoxPrefab, position.Value, Quaternion.identity);
            var netObj = box.GetComponent<NetworkObject>();
            netObj.Spawn();

            var lootBox = box.GetComponent<LootBox>();
            if (lootBox != null)
            {
                lootBox.assignedItemType.Value = (int)entry.itemType;

                // Assign data based on type
                switch (entry.itemType)
                {
                    case LootItemType.Shotgun:
                        lootBox.weaponData = shotgunData;
                        break;
                    case LootItemType.SMG:
                        lootBox.weaponData = smgData;
                        break;
                    case LootItemType.Rifle:
                        lootBox.weaponData = rifleData;
                        break;
                    case LootItemType.AmmoPack:
                        lootBox.ammoAmount = ammoPickupData != null ? ammoPickupData.ammoAmount : 15;
                        break;
                    case LootItemType.HealthPack:
                        lootBox.healAmount = healthPackData != null ? healthPackData.healAmount : 25f;
                        break;
                }
            }

            placedPositions.Add(position.Value);
        }

        Debug.Log($"[LootBoxSpawner] Spawned {placedPositions.Count}/{lootCount} loot boxes (seed: {seed})");
    }

    private bool IsValidPosition(Vector3 pos, List<Vector3> playerSpawns, Vector3 keyPos)
    {
        // Check separation from other loot boxes
        foreach (var placed in placedPositions)
        {
            if (Vector3.Distance(pos, placed) < minSeparation)
                return false;
        }

        // Check distance from player spawns
        foreach (var spawn in playerSpawns)
        {
            if (Vector3.Distance(pos, spawn) < minDistFromSpawns)
                return false;
        }

        // Check distance from key
        if (Vector3.Distance(pos, keyPos) < minDistFromKey)
            return false;

        // Check it's a walkable position (raycast down to find floor)
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
        {
            // Check not inside a wall (cast from above)
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                return false;
        }
        else
        {
            return false; // No floor found
        }

        return true;
    }

    private void BuildFallbackPositions()
    {
        fallbackPositions.Clear();

        float midX = (mazeMinX + mazeMaxX) / 2f;
        float midZ = (mazeMinZ + mazeMaxZ) / 2f;
        float qX = (mazeMaxX - mazeMinX) / 4f;
        float qZ = (mazeMaxZ - mazeMinZ) / 4f;

        // One position per quadrant
        Vector3[] quadrants = {
            new Vector3(midX - qX, 0.5f, midZ - qZ),
            new Vector3(midX + qX, 0.5f, midZ - qZ),
            new Vector3(midX - qX, 0.5f, midZ + qZ),
            new Vector3(midX + qX, 0.5f, midZ + qZ)
        };

        foreach (var pos in quadrants)
        {
            fallbackPositions.Add(pos);
        }
    }

    private List<Vector3> GetPlayerSpawnPositions()
    {
        var positions = new List<Vector3>();
        var spawnPoints = Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        foreach (var sp in spawnPoints)
        {
            positions.Add(sp.transform.position);
        }
        return positions;
    }
}

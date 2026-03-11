using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class WeaponInventory : NetworkBehaviour
{
    [SerializeField] private WeaponData pistolData;

    public const int SlotCount = 3;

    // Slot 0 = pistol (permanent), slots 1-2 = looted weapons
    private WeaponData[] weaponSlots = new WeaponData[SlotCount];
    private int[] ammo = new int[SlotCount]; // -1 for pistol (infinite)
    private int reserveAmmo;

    public NetworkVariable<int> equippedSlot = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Synced ammo for slots (server-readable)
    public NetworkVariable<int> syncedAmmoSlot1 = new NetworkVariable<int>(0);
    public NetworkVariable<int> syncedAmmoSlot2 = new NetworkVariable<int>(0);

    // Synced weapon types for HUD (-1 = empty, 0+ = LootItemType enum cast)
    public NetworkVariable<int> syncedWeaponSlot1 = new NetworkVariable<int>(-1);
    public NetworkVariable<int> syncedWeaponSlot2 = new NetworkVariable<int>(-1);

    public System.Action OnWeaponChanged;
    public System.Action OnAmmoChanged;

    public override void OnNetworkSpawn()
    {
        weaponSlots[0] = pistolData;
        ammo[0] = -1; // infinite
        ammo[1] = 0;
        ammo[2] = 0;

        if (IsOwner)
        {
            equippedSlot.Value = 0;
        }
    }

    public WeaponData GetEquippedWeapon()
    {
        int slot = equippedSlot.Value;
        if (slot >= 0 && slot < SlotCount)
            return weaponSlots[slot];
        return pistolData;
    }

    public WeaponData GetWeaponInSlot(int slot)
    {
        if (slot >= 0 && slot < SlotCount)
            return weaponSlots[slot];
        return null;
    }

    public int GetEquippedAmmo()
    {
        int slot = equippedSlot.Value;
        return ammo[slot];
    }

    public int GetAmmoInSlot(int slot)
    {
        if (slot >= 0 && slot < SlotCount)
            return ammo[slot];
        return 0;
    }

    public bool IsSlotOccupied(int slot)
    {
        return slot >= 0 && slot < SlotCount && weaponSlots[slot] != null;
    }

    public void SwitchToSlot(int slot)
    {
        if (!IsOwner) return;
        if (slot < 0 || slot >= SlotCount) return;
        if (weaponSlots[slot] == null) return;

        equippedSlot.Value = slot;
        OnWeaponChanged?.Invoke();
    }

    public void CycleWeapon(int direction)
    {
        if (!IsOwner) return;

        int current = equippedSlot.Value;
        int next = current;
        for (int i = 0; i < SlotCount - 1; i++)
        {
            next = (next + direction + SlotCount) % SlotCount;
            if (weaponSlots[next] != null)
            {
                equippedSlot.Value = next;
                OnWeaponChanged?.Invoke();
                return;
            }
        }
    }

    public void AddWeapon(WeaponData weapon, LootItemType itemType)
    {
        // Check for duplicate - add ammo instead
        for (int i = 1; i < SlotCount; i++)
        {
            if (weaponSlots[i] != null && weaponSlots[i].weaponName == weapon.weaponName)
            {
                ammo[i] = Mathf.Min(ammo[i] + weapon.ammoCapacity, weapon.maxAmmo);
                SyncAmmo();
                OnAmmoChanged?.Invoke();
                return;
            }
        }

        // Find first empty slot
        for (int i = 1; i < SlotCount; i++)
        {
            if (weaponSlots[i] == null)
            {
                weaponSlots[i] = weapon;
                ammo[i] = weapon.ammoCapacity + reserveAmmo;
                if (weapon.maxAmmo > 0)
                    ammo[i] = Mathf.Min(ammo[i], weapon.maxAmmo);
                reserveAmmo = 0;
                SyncWeaponSlots();
                SyncAmmo();
                OnWeaponChanged?.Invoke();
                OnAmmoChanged?.Invoke();
                return;
            }
        }

        // Both slots full - swap with currently equipped looted weapon
        int equipped = equippedSlot.Value;
        if (equipped == 0) equipped = 1; // Can't swap pistol, use slot 1

        weaponSlots[equipped] = weapon;
        ammo[equipped] = weapon.ammoCapacity + reserveAmmo;
        if (weapon.maxAmmo > 0)
            ammo[equipped] = Mathf.Min(ammo[equipped], weapon.maxAmmo);
        reserveAmmo = 0;
        SyncWeaponSlots();
        SyncAmmo();
        OnWeaponChanged?.Invoke();
        OnAmmoChanged?.Invoke();
    }

    public void AddAmmo(int amount)
    {
        int slot = equippedSlot.Value;

        // If pistol equipped, find first non-pistol weapon
        if (slot == 0)
        {
            for (int i = 1; i < SlotCount; i++)
            {
                if (weaponSlots[i] != null)
                {
                    slot = i;
                    break;
                }
            }
        }

        // No non-pistol weapons - store as reserve
        if (slot == 0)
        {
            reserveAmmo += amount;
            return;
        }

        var weapon = weaponSlots[slot];
        if (weapon != null && weapon.maxAmmo > 0)
        {
            ammo[slot] = Mathf.Min(ammo[slot] + amount, weapon.maxAmmo);
        }
        else if (weapon != null)
        {
            ammo[slot] += amount;
        }

        SyncAmmo();
        OnAmmoChanged?.Invoke();
    }

    public bool ConsumeAmmo()
    {
        int slot = equippedSlot.Value;
        if (slot == 0) return true; // Pistol always fires

        if (ammo[slot] <= 0) return false;

        ammo[slot]--;
        SyncAmmo();
        OnAmmoChanged?.Invoke();
        return true;
    }

    private void SyncAmmo()
    {
        if (!IsServer) return;
        syncedAmmoSlot1.Value = ammo[1];
        syncedAmmoSlot2.Value = ammo[2];
    }

    private void SyncWeaponSlots()
    {
        if (!IsServer) return;
        syncedWeaponSlot1.Value = weaponSlots[1] != null ? (int)GetItemTypeForWeapon(weaponSlots[1]) : -1;
        syncedWeaponSlot2.Value = weaponSlots[2] != null ? (int)GetItemTypeForWeapon(weaponSlots[2]) : -1;
    }

    private LootItemType GetItemTypeForWeapon(WeaponData weapon)
    {
        if (weapon.weaponName == "Shotgun") return LootItemType.Shotgun;
        if (weapon.weaponName == "SMG") return LootItemType.SMG;
        if (weapon.weaponName == "Rifle") return LootItemType.Rifle;
        return LootItemType.Shotgun; // fallback
    }

    // Called by host to set ammo directly (for ServerRpc flows)
    public void SetAmmoOnServer(int slot, int value)
    {
        if (!IsServer) return;
        ammo[slot] = value;
        SyncAmmo();
    }
}

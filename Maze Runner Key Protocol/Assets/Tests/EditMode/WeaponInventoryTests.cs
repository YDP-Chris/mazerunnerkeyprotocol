using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests WeaponInventory slot logic using direct field manipulation.
/// Since WeaponInventory is a NetworkBehaviour, we test the logic patterns
/// by creating a test harness that mirrors the slot management.
/// </summary>
[TestFixture]
public class WeaponInventoryTests
{
    // Test harness mirroring WeaponInventory slot logic (no NetworkBehaviour)
    private class InventoryHarness
    {
        public const int SlotCount = 3;
        public WeaponData[] slots = new WeaponData[SlotCount];
        public int[] ammo = new int[SlotCount];
        public int equippedSlot = 0;
        public int reserveAmmo = 0;

        public InventoryHarness(WeaponData pistol)
        {
            slots[0] = pistol;
            ammo[0] = -1; // infinite
        }

        public void AddWeapon(WeaponData weapon)
        {
            // Duplicate check
            for (int i = 1; i < SlotCount; i++)
            {
                if (slots[i] != null && slots[i].weaponName == weapon.weaponName)
                {
                    ammo[i] = Mathf.Min(ammo[i] + weapon.ammoCapacity, weapon.maxAmmo);
                    return;
                }
            }

            // First empty slot
            for (int i = 1; i < SlotCount; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = weapon;
                    ammo[i] = weapon.ammoCapacity + reserveAmmo;
                    if (weapon.maxAmmo > 0)
                        ammo[i] = Mathf.Min(ammo[i], weapon.maxAmmo);
                    reserveAmmo = 0;
                    return;
                }
            }

            // Both full — swap equipped (or slot 1 if pistol equipped)
            int target = equippedSlot == 0 ? 1 : equippedSlot;
            slots[target] = weapon;
            ammo[target] = weapon.ammoCapacity + reserveAmmo;
            if (weapon.maxAmmo > 0)
                ammo[target] = Mathf.Min(ammo[target], weapon.maxAmmo);
            reserveAmmo = 0;
        }

        public bool ConsumeAmmo()
        {
            if (equippedSlot == 0) return true; // pistol
            if (ammo[equippedSlot] <= 0) return false;
            ammo[equippedSlot]--;
            return true;
        }

        public void AddAmmo(int amount)
        {
            int slot = equippedSlot;
            if (slot == 0)
            {
                for (int i = 1; i < SlotCount; i++)
                {
                    if (slots[i] != null) { slot = i; break; }
                }
            }
            if (slot == 0) { reserveAmmo += amount; return; }

            var w = slots[slot];
            if (w != null && w.maxAmmo > 0)
                ammo[slot] = Mathf.Min(ammo[slot] + amount, w.maxAmmo);
            else if (w != null)
                ammo[slot] += amount;
        }
    }

    private WeaponData CreateWeapon(string name, int capacity = 30, int maxAmmo = 90)
    {
        var w = ScriptableObject.CreateInstance<WeaponData>();
        w.weaponName = name;
        w.ammoCapacity = capacity;
        w.maxAmmo = maxAmmo;
        return w;
    }

    [Test]
    public void InitialState_Slot0Pistol_Slots12Empty()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var inv = new InventoryHarness(pistol);

        Assert.IsNotNull(inv.slots[0]);
        Assert.IsNull(inv.slots[1]);
        Assert.IsNull(inv.slots[2]);
        Assert.AreEqual(-1, inv.ammo[0]); // infinite
    }

    [Test]
    public void AddWeapon_FillsSlot1First()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var shotgun = CreateWeapon("Shotgun");
        var inv = new InventoryHarness(pistol);

        inv.AddWeapon(shotgun);

        Assert.AreEqual("Shotgun", inv.slots[1].weaponName);
        Assert.IsNull(inv.slots[2]);
    }

    [Test]
    public void AddWeapon_FillsSlot2WhenSlot1Occupied()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var inv = new InventoryHarness(pistol);

        inv.AddWeapon(CreateWeapon("Shotgun"));
        inv.AddWeapon(CreateWeapon("SMG"));

        Assert.AreEqual("Shotgun", inv.slots[1].weaponName);
        Assert.AreEqual("SMG", inv.slots[2].weaponName);
    }

    [Test]
    public void DuplicateWeapon_AddsAmmoInsteadOfSlot()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var shotgun = CreateWeapon("Shotgun", 8, 90);
        var inv = new InventoryHarness(pistol);

        inv.AddWeapon(shotgun);
        int ammoAfterFirst = inv.ammo[1];

        inv.AddWeapon(shotgun);

        Assert.AreEqual(ammoAfterFirst + 8, inv.ammo[1]);
        Assert.IsNull(inv.slots[2]); // no new slot used
    }

    [Test]
    public void DuplicateAmmo_CappedAtMaxAmmo()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var shotgun = CreateWeapon("Shotgun", 80, 90);
        var inv = new InventoryHarness(pistol);

        inv.AddWeapon(shotgun); // ammo = 80
        inv.AddWeapon(shotgun); // +80 but capped at 90

        Assert.AreEqual(90, inv.ammo[1]);
    }

    [Test]
    public void BothSlotsFull_SwapReplacesEquippedLootSlot()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var inv = new InventoryHarness(pistol);

        inv.AddWeapon(CreateWeapon("Shotgun"));
        inv.AddWeapon(CreateWeapon("SMG"));
        inv.equippedSlot = 2; // equip slot 2 (SMG)

        inv.AddWeapon(CreateWeapon("Rifle"));

        Assert.AreEqual("Rifle", inv.slots[2].weaponName); // replaced equipped
        Assert.AreEqual("Shotgun", inv.slots[1].weaponName); // untouched
    }

    [Test]
    public void BothSlotsFull_PistolEquipped_SwapsSlot1()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var inv = new InventoryHarness(pistol);

        inv.AddWeapon(CreateWeapon("Shotgun"));
        inv.AddWeapon(CreateWeapon("SMG"));
        inv.equippedSlot = 0; // pistol

        inv.AddWeapon(CreateWeapon("Rifle"));

        Assert.AreEqual("Rifle", inv.slots[1].weaponName);
    }

    [Test]
    public void ConsumeAmmo_PistolAlwaysFires()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var inv = new InventoryHarness(pistol);
        inv.equippedSlot = 0;

        for (int i = 0; i < 100; i++)
            Assert.IsTrue(inv.ConsumeAmmo());
    }

    [Test]
    public void ConsumeAmmo_NonPistolWithAmmo_DecrementsAndReturnsTrue()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var shotgun = CreateWeapon("Shotgun", 5, 90);
        var inv = new InventoryHarness(pistol);
        inv.AddWeapon(shotgun);
        inv.equippedSlot = 1;

        Assert.IsTrue(inv.ConsumeAmmo());
        Assert.AreEqual(4, inv.ammo[1]);
    }

    [Test]
    public void ConsumeAmmo_NonPistolNoAmmo_ReturnsFalse()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var shotgun = CreateWeapon("Shotgun", 0, 90);
        var inv = new InventoryHarness(pistol);
        inv.AddWeapon(shotgun);
        inv.ammo[1] = 0;
        inv.equippedSlot = 1;

        Assert.IsFalse(inv.ConsumeAmmo());
    }

    [Test]
    public void AddAmmo_CapsAtMaxAmmo()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var shotgun = CreateWeapon("Shotgun", 30, 90);
        var inv = new InventoryHarness(pistol);
        inv.AddWeapon(shotgun);
        inv.equippedSlot = 1;

        inv.AddAmmo(100);

        Assert.AreEqual(90, inv.ammo[1]);
    }

    [Test]
    public void AddAmmo_NoNonPistolWeapons_StoresAsReserve()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var inv = new InventoryHarness(pistol);
        inv.equippedSlot = 0;

        inv.AddAmmo(15);

        Assert.AreEqual(15, inv.reserveAmmo);
    }

    [Test]
    public void ReserveAmmo_TransfersToNewWeapon()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var inv = new InventoryHarness(pistol);
        inv.reserveAmmo = 20;

        var shotgun = CreateWeapon("Shotgun", 8, 90);
        inv.AddWeapon(shotgun);

        Assert.AreEqual(28, inv.ammo[1]); // 8 capacity + 20 reserve
        Assert.AreEqual(0, inv.reserveAmmo);
    }

    [Test]
    public void ReserveAmmo_PlusCapacity_CappedAtMaxAmmo()
    {
        var pistol = CreateWeapon("Pistol", 0, -1);
        var inv = new InventoryHarness(pistol);
        inv.reserveAmmo = 100;

        var shotgun = CreateWeapon("Shotgun", 30, 90);
        inv.AddWeapon(shotgun);

        Assert.AreEqual(90, inv.ammo[1]); // 30 + 100 = 130, capped at 90
    }
}

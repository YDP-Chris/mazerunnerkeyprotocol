using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class WeaponHUD : MonoBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Slot Indicators")]
    [SerializeField] private TextMeshProUGUI slot1Text;
    [SerializeField] private TextMeshProUGUI slot2Text;
    [SerializeField] private TextMeshProUGUI slot3Text;

    [Header("Crosshair")]
    [SerializeField] private RectTransform crosshairTop;
    [SerializeField] private RectTransform crosshairBottom;
    [SerializeField] private RectTransform crosshairLeft;
    [SerializeField] private RectTransform crosshairRight;
    [SerializeField] private float baseCrosshairSize = 10f;
    [SerializeField] private float spreadMultiplier = 3f;

    private WeaponInventory inventory;

    private void Update()
    {
        if (inventory == null)
        {
            // Find local player's inventory
            foreach (var obj in Object.FindObjectsByType<WeaponInventory>(FindObjectsSortMode.None))
            {
                if (obj.IsOwner)
                {
                    inventory = obj;
                    inventory.OnWeaponChanged += Refresh;
                    inventory.OnAmmoChanged += Refresh;
                    Refresh();
                    break;
                }
            }
            return;
        }

        // Continuous refresh for real-time ammo updates
        RefreshAmmo();
    }

    private void Refresh()
    {
        if (inventory == null) return;

        var weapon = inventory.GetEquippedWeapon();
        if (weapon == null) return;

        // Weapon name
        if (weaponNameText != null)
            weaponNameText.text = weapon.weaponName;

        RefreshAmmo();
        RefreshSlots();
        RefreshCrosshair(weapon);
    }

    private void RefreshAmmo()
    {
        if (inventory == null || ammoText == null) return;

        int slot = inventory.equippedSlot.Value;
        if (slot == 0)
        {
            ammoText.text = "INF";
        }
        else
        {
            int current = inventory.GetAmmoInSlot(slot);
            var weapon = inventory.GetEquippedWeapon();
            int max = weapon != null ? weapon.maxAmmo : 0;
            ammoText.text = $"{current} / {max}";
        }
    }

    private void RefreshSlots()
    {
        int equipped = inventory.equippedSlot.Value;
        Color active = Color.white;
        Color inactive = new Color(0.5f, 0.5f, 0.5f);
        Color empty = new Color(0.3f, 0.3f, 0.3f);

        if (slot1Text != null)
        {
            slot1Text.text = "1: Pistol";
            slot1Text.color = equipped == 0 ? active : inactive;
        }

        if (slot2Text != null)
        {
            var w2 = inventory.GetWeaponInSlot(1);
            slot2Text.text = w2 != null ? $"2: {w2.weaponName}" : "2: ---";
            slot2Text.color = w2 == null ? empty : (equipped == 1 ? active : inactive);
        }

        if (slot3Text != null)
        {
            var w3 = inventory.GetWeaponInSlot(2);
            slot3Text.text = w3 != null ? $"3: {w3.weaponName}" : "3: ---";
            slot3Text.color = w3 == null ? empty : (equipped == 2 ? active : inactive);
        }
    }

    private void RefreshCrosshair(WeaponData weapon)
    {
        if (crosshairTop == null) return;

        float spread = baseCrosshairSize + weapon.spreadAngle * spreadMultiplier;

        crosshairTop.anchoredPosition = new Vector2(0, spread);
        crosshairBottom.anchoredPosition = new Vector2(0, -spread);
        crosshairLeft.anchoredPosition = new Vector2(-spread, 0);
        crosshairRight.anchoredPosition = new Vector2(spread, 0);
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnWeaponChanged -= Refresh;
            inventory.OnAmmoChanged -= Refresh;
        }
    }
}

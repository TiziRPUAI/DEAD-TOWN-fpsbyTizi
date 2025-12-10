using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; set; }

    [Header("Ammo")]
    public TextMeshProUGUI magazineAmmoUI;
    public TextMeshProUGUI totalAmmoUI;
    public Image ammoTypeUI;

    [Header("Weapon")]
    public Image activeWeaponUI;
    public Image unActiveWeaponUI;

    [Header("Throwables")]
    public Image lethalUI;
    public TextMeshProUGUI lethalAmountUI;

    public Image tacticalUI;
    public TextMeshProUGUI tacticalAmountAmountUI;

    public Sprite emptySlot;

    // Caché de sprites para no cargar repetidamente
    private readonly Dictionary<Weapon.WeaponModel, Sprite> weaponSprites = new Dictionary<Weapon.WeaponModel, Sprite>();
    private readonly Dictionary<Weapon.WeaponModel, Sprite> ammoSprites = new Dictionary<Weapon.WeaponModel, Sprite>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        // mantener Update para refresco continuo (por seguridad), pero delega en método reutilizable
        UpdateHUD();
    }

    // Método público para forzar actualización desde WeaponManager
    public void RefreshHUD()
    {
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        // Seguridad: comprobar instancia de WeaponManager
        if (WeaponManager.Instance == null || WeaponManager.Instance.activeWeaponSlot == null)
        {
            ClearHUD();
            return;
        }

        Weapon activeWeapon = WeaponManager.Instance.activeWeaponSlot.GetComponentInChildren<Weapon>();
        var unActiveSlot = GetUnActiveWeaponSlot();
        Weapon unActiveWeapon = unActiveSlot != null ? unActiveSlot.GetComponentInChildren<Weapon>() : null;

        if (activeWeapon != null)
        {
            // Evitar división por cero: use siempre al menos 1
            int burst = Math.Max(1, activeWeapon.bulletsPerBurst);
            magazineAmmoUI.text = $"{activeWeapon.bulletsLeft / burst}";
            totalAmmoUI.text = $"{WeaponManager.Instance.CheckAmmmoLeftFor(activeWeapon.thisWeaponModel)}";

            Weapon.WeaponModel model = activeWeapon.thisWeaponModel;
            // Logs diagnósticos mínimos (comentarlos si molestan)
            //Debug.Log($"HUDManager: activeWeapon detected: {model}");

            ammoTypeUI.sprite = GetAmmoSprite(model);
            activeWeaponUI.sprite = GetWeaponSprite(model);

            if (unActiveWeapon != null)
                unActiveWeaponUI.sprite = GetWeaponSprite(unActiveWeapon.thisWeaponModel);
            else
                unActiveWeaponUI.sprite = emptySlot;
        }
        else
        {
            ClearHUD();
        }
    }

    private void ClearHUD()
    {
        magazineAmmoUI.text = "";
        totalAmmoUI.text = "";
        ammoTypeUI.sprite = emptySlot;
        activeWeaponUI.sprite = emptySlot;
        unActiveWeaponUI.sprite = emptySlot;
    }

    // Obtiene sprite de arma usando la lógica original del tutorial pero SIN Instantiate.
    private Sprite GetWeaponSprite(Weapon.WeaponModel model)
    {
        if (weaponSprites.TryGetValue(model, out var cached)) return cached;

        string resourceName = model switch
        {
            Weapon.WeaponModel.Pistol1911 => "Pistol1911_Weapon",
            Weapon.WeaponModel.M4_8 => "M4_8_Weapon",
            _ => null
        };

        if (string.IsNullOrEmpty(resourceName))
        {
            Debug.LogWarning($"GetWeaponSprite: resourceName vacío para modelo {model}");
            return emptySlot;
        }

        var sprite = LoadSpriteFromResources(resourceName);
        if (sprite != null)
        {
            weaponSprites[model] = sprite;
            return sprite;
        }

        Debug.LogWarning($"Sprite de arma no encontrado para: {model} (recurso: {resourceName})");
        return emptySlot;
    }

    // Obtiene sprite de munición usando la lógica original del tutorial pero SIN Instantiate.
    private Sprite GetAmmoSprite(Weapon.WeaponModel model)
    {
        if (ammoSprites.TryGetValue(model, out var cached)) return cached;

        string resourceName = model switch
        {
            Weapon.WeaponModel.Pistol1911 => "Pistol1911_Ammo",
            Weapon.WeaponModel.M4_8 => "M4_8_Ammo",
            _ => null
        };

        if (string.IsNullOrEmpty(resourceName))
        {
            Debug.LogWarning($"GetAmmoSprite: resourceName vacío para modelo {model}");
            return emptySlot;
        }

        var sprite = LoadSpriteFromResources(resourceName);
        if (sprite != null)
        {
            ammoSprites[model] = sprite;
            return sprite;
        }

        Debug.LogWarning($"Sprite de munición no encontrado para: {model} (recurso: {resourceName})");
        return emptySlot;
    }

    // Carga sprite intentando: Resources.Load<Sprite>, Resources.Load<GameObject> (prefab) y Resources.LoadAll<Sprite> (sprite sheet).
    private Sprite LoadSpriteFromResources(string resourceName)
    {
        // 1) Intentar cargar directamente como Sprite
        var s = Resources.Load<Sprite>(resourceName);
        if (s != null) return s;

        // 2) Intentar cargar prefab y extraer SpriteRenderer/Image sin instanciar
        var prefab = Resources.Load<GameObject>(resourceName);
        if (prefab != null)
        {
            var sprite = ExtractSpriteFromPrefab(prefab);
            if (sprite != null) return sprite;
        }

        // 3) Intentar rutas comunes dentro de Resources (subcarpetas)
        string[] attempts = new[] { "Sprites/" + resourceName, "UI/" + resourceName, "Images/" + resourceName, "Sprites/UI/" + resourceName };
        foreach (var path in attempts)
        {
            s = Resources.Load<Sprite>(path);
            if (s != null) return s;

            prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                var sprite = ExtractSpriteFromPrefab(prefab);
                if (sprite != null) return sprite;
            }
        }

        // 4) Intentar sprite sheet / atlas en Resources
        try
        {
            var sheet = Resources.LoadAll<Sprite>(resourceName);
            if (sheet != null && sheet.Length > 0) return sheet[0];
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"LoadSpriteFromResources: LoadAll failed for '{resourceName}': {ex.Message}");
        }

        return null;
    }

    // Extrae sprite desde un prefab (no instancia), busca Image o SpriteRenderer en root o hijos
    private Sprite ExtractSpriteFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        var img = prefab.GetComponent<Image>();
        if (img != null && img.sprite != null) return img.sprite;

        var sr = prefab.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null) return sr.sprite;

        var childImg = prefab.GetComponentInChildren<Image>();
        if (childImg != null && childImg.sprite != null) return childImg.sprite;

        var childSr = prefab.GetComponentInChildren<SpriteRenderer>();
        if (childSr != null && childSr.sprite != null) return childSr.sprite;

        return null;
    }

    private GameObject GetUnActiveWeaponSlot()
    {
        if (WeaponManager.Instance == null) return null;

        foreach (GameObject weaponSlot in WeaponManager.Instance.weaponSlots)
        {
            if (weaponSlot != WeaponManager.Instance.activeWeaponSlot)
            {
                return weaponSlot;
            }
        }
        return null;
    }
}

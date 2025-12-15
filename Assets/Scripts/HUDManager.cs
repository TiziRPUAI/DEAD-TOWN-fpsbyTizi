using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class HUDManager : MonoBehaviour
{
    // Función: Gestiona elementos HUD (munición, arma, lanzables y contador de oleada).
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

    [Header("Wave")]
    public TextMeshProUGUI waveUI;

    private readonly Dictionary<Weapon.WeaponModel, Sprite> weaponSprites = new Dictionary<Weapon.WeaponModel, Sprite>();
    private readonly Dictionary<Weapon.WeaponModel, Sprite> ammoSprites = new Dictionary<Weapon.WeaponModel, Sprite>();

    private ZombieSpawnController spawner;

    // Función: Inicializa la instancia singleton y cachea referencias.
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

        spawner = FindObjectOfType<ZombieSpawnController>();
    }

    // Función: Actualiza el HUD cada frame.
    private void Update()
    {
        UpdateHUD();
    }

    // Función: Forzar refresco del HUD desde controladores externos.
    public void RefreshHUD()
    {
        UpdateHUD();
    }

    // Función: Actualiza todos los elementos visibles del HUD.
    private void UpdateHUD()
    {
        if (WeaponManager.Instance == null || WeaponManager.Instance.activeWeaponSlot == null)
        {
            ClearHUD();
            UpdateWaveUI();
            return;
        }

        Weapon activeWeapon = WeaponManager.Instance.activeWeaponSlot.GetComponentInChildren<Weapon>();
        var unActiveSlot = GetUnActiveWeaponSlot();
        Weapon unActiveWeapon = unActiveSlot != null ? unActiveSlot.GetComponentInChildren<Weapon>() : null;

        if (activeWeapon != null)
        {
            int burst = Math.Max(1, activeWeapon.bulletsPerBurst);
            magazineAmmoUI.text = $"{activeWeapon.bulletsLeft / burst}";
            totalAmmoUI.text = $"{WeaponManager.Instance.CheckAmmmoLeftFor(activeWeapon.thisWeaponModel)}";

            Weapon.WeaponModel model = activeWeapon.thisWeaponModel;

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

        UpdateWaveUI();
    }

    // Función: Limpia los elementos de munición y arma del HUD.
    private void ClearHUD()
    {
        magazineAmmoUI.text = "";
        totalAmmoUI.text = "";
        ammoTypeUI.sprite = emptySlot;
        activeWeaponUI.sprite = emptySlot;
        unActiveWeaponUI.sprite = emptySlot;
    }

    // Función: Actualiza el contador de oleada usando ZombieSpawnController.
    private void UpdateWaveUI()
    {
        if (waveUI == null) return;

        if (spawner == null)
            spawner = FindObjectOfType<ZombieSpawnController>();

        int wave = (spawner != null) ? spawner.currentWave : 0;
        waveUI.text = $"Wave: {wave}";
    }

    // Función: Obtiene o carga el sprite del arma.
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

    // Función: Obtiene o carga el sprite de munición.
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

    // Función: Intenta cargar un Sprite desde Resources por varias rutas.
    private Sprite LoadSpriteFromResources(string resourceName)
    {
        var s = Resources.Load<Sprite>(resourceName);
        if (s != null) return s;

        var prefab = Resources.Load<GameObject>(resourceName);
        if (prefab != null)
        {
            var sprite = ExtractSpriteFromPrefab(prefab);
            if (sprite != null) return sprite;
        }

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

    // Función: Extrae un Sprite desde un prefab sin instanciarlo.
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

    // Función: Devuelve la otra ranura de arma (no activa).
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

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ZombieSpawnController : MonoBehaviour
{
    // Función: Controla oleadas de zombies, spawns, cooldowns y gestión de prefabs.
    public int initialZombiesPerWave = 5;    
    public int currentZombiesPerWave;

    public float spawnDelay = 0.5f;

    public int currentWave = 0;
    public float waveCooldown = 10f;

    public bool inCooldown;
    public float cooldownCounter = 0;

    public List<EnemyNavigation> currentZombiesAlive;

    public List<GameObject> zombiePrefabs;
    public GameObject zombiePrefab;

    public TextMeshProUGUI waveOverUI;
    public TextMeshProUGUI cooldownCounterUI;

    // Nuevas opciones para soportar múltiples spawners
    public List<Transform> spawnerPoints; // si está vacío se usará transform
    public bool distributeEvenly = true; // true: repartir en orden, false: elegir spawner aleatorio
    public Vector2 spawnOffsetXRange = new Vector2(-1f, 1f);
    public Vector2 spawnOffsetZRange = new Vector2(-1f, 10f);

    // Configuración de cajas de munición
    public GameObject ammoBoxPrefab;              // Prefab genérico (compatibilidad)
    public GameObject ammoBoxPistolPrefab;        // Prefab específico para PistolAmmo
    public GameObject ammoBoxRiflePrefab;         // Prefab específico para RifleAmmo
    public int wavesPerAmmoBox = 3;               // Cada N oleadas spawnea una caja

    // Nuevo: spawn en un objeto vacío determinado por el diseñador
    public Transform ammoSpawnPoint;              // asigna aquí el GameObject vacío en el inspector
    public bool useAmmoSpawnPoint = true;         // si true intentará spawnear en ammoSpawnPoint
    public bool ammoSpawnAtExactPoint = true;     // si true usa la posición exacta, si false aplica offset aleatorio

    // Función: Inicializa listas, valores y lanza la primera oleada.
    private void Start()
    {
        if (currentZombiesAlive == null)
            currentZombiesAlive = new List<EnemyNavigation>();

        currentZombiesPerWave = initialZombiesPerWave;

        if (waveOverUI != null) waveOverUI.gameObject.SetActive(false);
        if (cooldownCounterUI != null) cooldownCounterUI.gameObject.SetActive(false);

        // Asegurar al menos un spawner (este transform) si no se han asignado spawnerPoints en el inspector
        if (spawnerPoints == null)
            spawnerPoints = new List<Transform>();
        if (spawnerPoints.Count == 0)
            spawnerPoints.Add(transform);

        StartNextWave();
    }

    // Función: Prepara y comienza la siguiente oleada.
    private void StartNextWave()
    {
        currentZombiesAlive.Clear();
        currentWave++;
        StartCoroutine(SpawnWave());
    }

    // Función: Instancia los prefabs de zombies para la oleada actual.
    private IEnumerator SpawnWave()
    {
        int spawnerCount = (spawnerPoints != null && spawnerPoints.Count > 0) ? spawnerPoints.Count : 1;

        for (int i = 0; i < currentZombiesPerWave; i++)
        {
            // Elegir spawner según la configuración
            Transform chosenSpawner;
            if (spawnerPoints == null || spawnerPoints.Count == 0)
            {
                chosenSpawner = transform;
            }
            else
            {
                if (distributeEvenly)
                    chosenSpawner = spawnerPoints[i % spawnerCount];
                else
                    chosenSpawner = spawnerPoints[UnityEngine.Random.Range(0, spawnerCount)];
            }

            Vector3 spawnOffset = new Vector3(UnityEngine.Random.Range(spawnOffsetXRange.x, spawnOffsetXRange.y), 0f, UnityEngine.Random.Range(spawnOffsetZRange.x, spawnOffsetZRange.y));
            Vector3 spawnPosition = chosenSpawner.position + spawnOffset;

            GameObject prefabToSpawn = (zombiePrefabs != null && zombiePrefabs.Count > 0)
                ? zombiePrefabs[UnityEngine.Random.Range(0, zombiePrefabs.Count)]
                : zombiePrefab;

            if (prefabToSpawn == null)
            {
               
                yield return new WaitForSeconds(spawnDelay);
                continue;
            }

            var zombie = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

            EnemyNavigation zombieScript = zombie.GetComponent<EnemyNavigation>();

            currentZombiesAlive.Add(zombieScript);

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    // Función: Mantiene la lista de zombies vivos y controla el cooldown entre oleadas.
    private void Update()
    {
        List<EnemyNavigation> zombiesToRemove = new List<EnemyNavigation>();
        foreach (EnemyNavigation zombie in currentZombiesAlive)
        {
            if (zombie == null)
            {
                zombiesToRemove.Add(zombie);
            }
        }
        foreach (EnemyNavigation zombie in zombiesToRemove)
        {
            currentZombiesAlive.Remove(zombie);
        }
        zombiesToRemove.Clear();

        if (currentZombiesAlive.Count == 0 && !inCooldown)
        {
            StartCoroutine(WaveCooldown());
        }

        if (inCooldown)
        {
            cooldownCounter -= Time.unscaledDeltaTime;
            if (cooldownCounter < 0f) cooldownCounter = 0f;

            if (cooldownCounterUI != null)
            {
                if (!cooldownCounterUI.gameObject.activeSelf) cooldownCounterUI.gameObject.SetActive(true);
                cooldownCounterUI.text = Mathf.CeilToInt(cooldownCounter).ToString();
            }
        }
        else
        {
            if (cooldownCounterUI != null && cooldownCounterUI.gameObject.activeSelf)
                cooldownCounterUI.gameObject.SetActive(false);
        }
    }

    // Función auxiliar: instancia una caja de munición cerca de un spawner dado, eligiendo prefab según tipo.
    // useOffset: si true aplicará el offset aleatorio; si false usará la posición exacta del spawner.
    private void SpawnAmmoBoxOfType(AmmoBox.AmmoType type, Transform spawner, bool useOffset = true)
    {
        GameObject chosenPrefab = null;

        switch (type)
        {
            case AmmoBox.AmmoType.PistolAmmo:
                chosenPrefab = ammoBoxPistolPrefab != null ? ammoBoxPistolPrefab : ammoBoxPrefab;
                break;
            case AmmoBox.AmmoType.RifleAmmo:
                chosenPrefab = ammoBoxRiflePrefab != null ? ammoBoxRiflePrefab : ammoBoxPrefab;
                break;
            default:
                chosenPrefab = ammoBoxPrefab;
                break;
        }

        if (chosenPrefab == null) return;

        Vector3 offset = Vector3.zero;
        if (useOffset)
            offset = new Vector3(UnityEngine.Random.Range(spawnOffsetXRange.x, spawnOffsetXRange.y), 0f, UnityEngine.Random.Range(spawnOffsetZRange.x, spawnOffsetZRange.y));

        Vector3 spawnPos = (spawner != null ? spawner.position : transform.position) + offset;

        var go = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
        var ammoBox = go.GetComponent<AmmoBox>();
        if (ammoBox != null)
        {
            ammoBox.ammoType = type; // asegurar tipo correcto aunque el prefab tenga otro
        }
    }

    // Función: Gestiona el periodo de cooldown entre oleadas y lanza la siguiente.
    private IEnumerator WaveCooldown()
    {
        inCooldown = true;
        cooldownCounter = waveCooldown;

        if (waveOverUI != null)
        {
            waveOverUI.text = $"Wave {currentWave} terminada";
            waveOverUI.gameObject.SetActive(true);
        }

        if (cooldownCounterUI != null)
            cooldownCounterUI.gameObject.SetActive(true);

        // Si la oleada actual es múltiplo de wavesPerAmmoBox, spawnear una caja para que el jugador la recoja durante el cooldown
        if ((ammoBoxPrefab != null || ammoBoxPistolPrefab != null || ammoBoxRiflePrefab != null) && wavesPerAmmoBox > 0 && currentWave % wavesPerAmmoBox == 0)
        {
            // decidir donde spawnear la caja: si useAmmoSpawnPoint y ammoSpawnPoint asignado, usarlo;
            // si no, elegir un spawner aleatorio como antes.
            Transform chosenSpawner = null;
            bool useOffsetForSpawn = true;

            if (useAmmoSpawnPoint && ammoSpawnPoint != null)
            {
                chosenSpawner = ammoSpawnPoint;
                useOffsetForSpawn = !ammoSpawnAtExactPoint; // si queremos exacto, no usar offset
            }
            else
            {
                chosenSpawner = (spawnerPoints != null && spawnerPoints.Count > 0)
                    ? spawnerPoints[UnityEngine.Random.Range(0, spawnerPoints.Count)]
                    : transform;
                useOffsetForSpawn = true;
            }

            // Alternar tipo por aparición: (0 → Pistol, 1 → Rifle, 0 → Pistol, ...)
            int appearanceIndex = (currentWave / wavesPerAmmoBox) - 1; // -1 porque currentWave ya se incrementó al empezar la oleada
            AmmoBox.AmmoType chosenType = (appearanceIndex % 2 == 0) ? AmmoBox.AmmoType.PistolAmmo : AmmoBox.AmmoType.RifleAmmo;

            SpawnAmmoBoxOfType(chosenType, chosenSpawner, useOffsetForSpawn);
        }

        while (cooldownCounter > 0f)
        {
            yield return null;
        }

        inCooldown = false;

        if (waveOverUI != null) waveOverUI.gameObject.SetActive(false);
        if (cooldownCounterUI != null) cooldownCounterUI.gameObject.SetActive(false);

        currentZombiesPerWave *= 2;

        StartNextWave();
    }
}

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class ZombieSpawnController : MonoBehaviour
{
    public int initialZombiesPerWave = 5;    
    public int currentZombiesPerWave;

    public float spawnDelay = 0.5f;

    public int currentWave = 0;
    public float waveCooldown = 10f;

    public bool inCooldown;
    public float cooldownCounter = 0;

    public List<EnemyNavigation> currentZombiesAlive;

    public GameObject zombiePrefab;

    public TextMeshProUGUI waveOverUI;
    public TextMeshProUGUI cooldownCounterUI;

    private void Start()
    {
        if (currentZombiesAlive == null)
            currentZombiesAlive = new List<EnemyNavigation>();

        currentZombiesPerWave = initialZombiesPerWave;

        if (waveOverUI != null) waveOverUI.gameObject.SetActive(false);
        if (cooldownCounterUI != null) cooldownCounterUI.gameObject.SetActive(false);

        StartNextWave();
    }

    private void StartNextWave()
    {
        currentZombiesAlive.Clear();
        currentWave++;
        StartCoroutine(SpawnWave());
    }
    private IEnumerator SpawnWave()
    {
        for (int i = 0; i < currentZombiesPerWave; i++)
        {
            Vector3 spawnOffset = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 10f));
            Vector3 spawnPosition = transform.position + spawnOffset;

            var zombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);

            EnemyNavigation zombieScript = zombie.GetComponent<EnemyNavigation>();

            currentZombiesAlive.Add(zombieScript);

            yield return new WaitForSeconds(spawnDelay);
        }
    }
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
            // Usar tiempo no escalado para que el cooldown avance aunque Time.timeScale == 0
            cooldownCounter -= Time.unscaledDeltaTime;
            if (cooldownCounter < 0f) cooldownCounter = 0f;

            if (cooldownCounterUI != null)
            {
                if (!cooldownCounterUI.gameObject.activeSelf) cooldownCounterUI.gameObject.SetActive(true);
                // Muestra segundos restantes como entero. Cambia formato si quieres decimales ("F1").
                cooldownCounterUI.text = Mathf.CeilToInt(cooldownCounter).ToString();
            }
        }
        else
        {
            if (cooldownCounterUI != null && cooldownCounterUI.gameObject.activeSelf)
                cooldownCounterUI.gameObject.SetActive(false);
        }
    }

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

        // Esperamos hasta que Update reduzca cooldownCounter a 0 (ahora usando unscaledDeltaTime)
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

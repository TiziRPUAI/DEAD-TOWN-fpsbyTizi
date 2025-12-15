using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public UIManager uiManager;

    [Header("Settings")]
    [Tooltip("Si true, el GameManager persiste entre escenas")]
    public bool persistBetweenScenes = true;

    [Header("Death / Menu")]
    [Tooltip("Segundos que esperan antes de volver al MainMenu tras la muerte")]
    public float returnToMenuDelay = 3f;

    private int enemiesRemaining;

    private Coroutine victoryCheckCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (persistBetweenScenes) DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CountEnemies();
        uiManager?.HideAll();
    }

    // Recalcula el conteo actual (útil al iniciar escena)
    public void CountEnemies()
    {
        var zombies = FindObjectsOfType<Zombie>();
        enemiesRemaining = zombies != null ? zombies.Length : 0;
        Debug.Log($"GameManager: enemiesRemaining = {enemiesRemaining}");
    }

    // Llamar desde el zombie al instanciarse para incrementar el contador
    public void RegisterEnemy()
    {
        enemiesRemaining++;
        Debug.Log($"GameManager: RegisterEnemy called. Remaining = {enemiesRemaining}");
        // Si había una comprobación de victoria en curso, cancelarla porque llegó un nuevo enemigo
        if (victoryCheckCoroutine != null)
        {
            StopCoroutine(victoryCheckCoroutine);
            victoryCheckCoroutine = null;
        }
    }

    public void EnemyDied()
    {
        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        Debug.Log($"GameManager: Enemy died. Remaining = {enemiesRemaining}");
        if (enemiesRemaining <= 0)
        {
            // Esperar un pequeño intervalo antes de declarar victoria para evitar carreras con spawners
            if (victoryCheckCoroutine == null)
                victoryCheckCoroutine = StartCoroutine(DelayedVictoryCheck());
        }
    }

    private IEnumerator DelayedVictoryCheck()
    {
        // Espera en tiempo real (no afectada por timeScale), suficiente para que spawners registren nuevos enemigos
        yield return new WaitForSecondsRealtime(0.2f);
        victoryCheckCoroutine = null;
       
    }

    // Llamado cuando el jugador muere. Guarda el mejor wave y vuelve al MainMenu pasado un retardo.
    public void PlayerDied()
    {
        // Obtener la wave actual desde el controlador de spawns (si existe)
        var spawner = FindObjectOfType<ZombieSpawnController>();
        int currentWave = 0;
        if (spawner != null)
            currentWave = spawner.currentWave;

        // Guardar si es mejor que el highscore almacenado
        if (SaveLoadManager.Instance != null)
        {
            int savedBest = SaveLoadManager.Instance.LoadHighScore();
            if (currentWave > savedBest)
            {
                SaveLoadManager.Instance.SaveHighScore(currentWave);
                Debug.Log($"GameManager: New best wave saved: {currentWave}");
            }
            else
            {
                Debug.Log($"GameManager: Player died at wave {currentWave}. Best remains {savedBest}");
            }
        }
        else
        {
            Debug.LogWarning("GameManager.PlayerDied: SaveLoadManager.Instance es null, no se pudo guardar el highscore.");
        }

        // Lanzar la transición a MainMenu tras un retardo (tiempo real)
        StartCoroutine(ReturnToMainMenuAfterDelay());
    }

    private IEnumerator ReturnToMainMenuAfterDelay()
    {
        // Asegura que el juego no esté pausado (opcional)
        Time.timeScale = 1f;

        yield return new WaitForSecondsRealtime(returnToMenuDelay);

        // Cargar la escena MainMenu
        Debug.Log("GameManager: Loading MainMenu scene...");
        SceneManager.LoadScene("MainMenu");
    }

    private void OnVictory()
    {
      
    }

    public void RestartLevel()
    {
 
    }

    public void QuitToMenu()
    {
     
    }
}
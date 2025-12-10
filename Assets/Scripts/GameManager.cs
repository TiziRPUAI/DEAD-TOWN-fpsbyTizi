
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

    public void PlayerDied()
    {
        
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
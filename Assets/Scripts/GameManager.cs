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

    public void CountEnemies()
    {
        var zombies = FindObjectsOfType<Zombie>();
        enemiesRemaining = zombies != null ? zombies.Length : 0;
        Debug.Log($"GameManager: enemiesRemaining = {enemiesRemaining}");
    }

    public void EnemyDied()
    {
        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        Debug.Log($"GameManager: Enemy died. Remaining = {enemiesRemaining}");
        if (enemiesRemaining <= 0)
        {
            OnVictory();
        }
    }

    public void PlayerDied()
    {
        Debug.Log("GameManager: Player died.");
        Time.timeScale = 0f;
        uiManager?.ShowDeathPanel();
    }

    private void OnVictory()
    {
        Debug.Log("GameManager: Victory!");
        Time.timeScale = 0f;
        uiManager?.ShowVictoryPanel();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        // Asume escena 0 como menú; cámbialo si tu proyecto tiene otro índice
        SceneManager.LoadScene(0);
    }
}

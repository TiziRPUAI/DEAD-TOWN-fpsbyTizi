using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    // Función: Gestiona la UI y acciones del menú principal.
    public TMP_Text highScoreUI;

    [Tooltip("Nombre exacto de la escena (sin .unity)")]
    public string newGameSceneName = "FPSbyTizi";

    [Tooltip("Si la escena no está en la lista o prefieres usar índice, pon aquí el Build Index (o -1 para ignorar)")]
    public int newGameSceneBuildIndex = -1;

    public AudioClip bg_music;
    public AudioSource main_channel;

    // Función: Inicializa el menú y asegura cursor visible/desbloqueado.
    void Start()
    {
        main_channel.PlayOneShot(bg_music);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpdateHighScoreUI();
    }

    // Función: Actualiza el texto del highscore desde SaveLoadManager.
    public void UpdateHighScoreUI()
    {
        int highScore = 0;
        if (SaveLoadManager.Instance != null)
            highScore = SaveLoadManager.Instance.LoadHighScore();
        else
            Debug.LogWarning("MainMenu: SaveLoadManager.Instance es null. HighScore mostrado como 0.");

        if (highScoreUI != null)
            highScoreUI.text = "Best Wave: " + highScore.ToString();
        else
            Debug.LogWarning("MainMenu: highScoreUI no asignado en el Inspector.");
    }

    // Función: Guarda el highscore (usa SaveLoadManager).
    public void SaveHighScore(int score)
    {
        if (SaveLoadManager.Instance == null)
        {
            Debug.LogWarning("MainMenu.SaveHighScore: SaveLoadManager.Instance es null. No se puede guardar.");
            return;
        }

        SaveLoadManager.Instance.SaveHighScore(score);
        Debug.Log($"MainMenu: HighScore guardado: {score}");
        UpdateHighScoreUI();
    }

    // Función: Guarda solo si el score es mejor que el almacenado.
    public void TrySaveHighScore(int score)
    {
        if (SaveLoadManager.Instance == null)
        {
            Debug.LogWarning("MainMenu.TrySaveHighScore: SaveLoadManager.Instance es null. No se puede comprobar/guardar.");
            return;
        }

        int current = SaveLoadManager.Instance.LoadHighScore();
        if (score > current)
        {
            SaveHighScore(score);
            Debug.Log($"MainMenu: Nuevo best wave guardado: {score} (superó {current})");
        }
        else
        {
            Debug.Log($"MainMenu: Score {score} no supera best {current}, no se guarda.");
        }
    }

    // Función: Inicia la partida (carga la escena).
    public void StartNewGame()
    {
        main_channel.Stop();
        Debug.Log($"MainMenu: StartNewGame called. name='{newGameSceneName}' buildIndex={newGameSceneBuildIndex}");

        Time.timeScale = 1f;

#if UNITY_EDITOR
        bool found = false;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(s.path);
                if (fileName == newGameSceneName)
                {
                    found = true;
                    break;
                }
            }
        }

        if (!found && newGameSceneBuildIndex < 0)
        {
            Debug.LogError($"MainMenu: La escena '{newGameSceneName}' no está añadida en __Build Settings__ y no se proporcionó un índice válido. Abre __File > Build Settings__ y añade la escena.");
            return;
        }
#endif

        if (newGameSceneBuildIndex >= 0)
        {
            Debug.Log($"MainMenu: Cargando escena por índice {newGameSceneBuildIndex}...");
            SceneManager.LoadScene(newGameSceneBuildIndex);
        }
        else
        {
            Debug.Log($"MainMenu: Cargando escena por nombre '{newGameSceneName}'...");
            SceneManager.LoadScene(newGameSceneName);
        }
    }

    // Función: Cierra la aplicación o detiene el Play en Editor.
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

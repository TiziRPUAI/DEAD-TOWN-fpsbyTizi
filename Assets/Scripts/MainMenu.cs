using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    public TMP_Text highScoreUI;

    [Tooltip("Nombre exacto de la escena (sin .unity)")]
    public string newGameSceneName = "FPSbyTizi";

    [Tooltip("Si la escena no está en la lista o prefieres usar índice, pon aquí el Build Index (o -1 para ignorar)")]
    public int newGameSceneBuildIndex = -1;

    void Start()
    {
        int highScore = SaveLoadManager.Instance.LoadHighScore();
        if (highScoreUI != null)
            highScoreUI.text = "Best Wave: " + highScore.ToString();
        else
            Debug.LogWarning("MainMenu: highScoreUI no asignado en el Inspector.");
    }

    public void StartNewGame()
    {
        Debug.Log($"MainMenu: StartNewGame called. name='{newGameSceneName}' buildIndex={newGameSceneBuildIndex}");

        // Normalizar timeScale por si viniera de una escena pausada
        Time.timeScale = 1f;

#if UNITY_EDITOR
        // Validación útil en el Editor: comprobar si la escena se encuentra en Build Settings
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

        // Priorizar carga por índice si se ha establecido
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

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

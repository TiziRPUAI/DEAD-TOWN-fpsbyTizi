using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject deathPanel;
    public GameObject victoryPanel;

    [Header("Optional UI")]
    public Text healthText; // si quieres actualizar vida

    private PlayerHealth playerHealth;

    void Start()
    {
        HideAll();

        // Buscar PlayerHealth en la escena (si hay varios players, asigna manualmente desde el Inspector)
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("UIManager: PlayerHealth no encontrado en la escena. Añade el componente PlayerHealth al GameObject del jugador.");
        }
        else
        {
            UpdateHealthText();
        }
    }

    void Update()
    {
        // Polling simple para mantener la UI actualizada
        if (healthText != null && playerHealth != null)
        {
            UpdateHealthText();
        }
    }

    private void UpdateHealthText()
    {
        float current = playerHealth.GetCurrentHealth();
        float max = playerHealth.maxHealth;
        healthText.text = $"{current:0}/{max:0}";
    }

    public void HideAll()
    {
        if (deathPanel != null) deathPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    public void ShowDeathPanel()
    {
        if (deathPanel != null) deathPanel.SetActive(true);
    }

    public void ShowVictoryPanel()
    {
        if (victoryPanel != null) victoryPanel.SetActive(true);
    }

    // Métodos públicos para botones
    public void OnRestartButton()
    {
        GameManager.Instance?.RestartLevel();
    }

    public void OnQuitButton()
    {
        GameManager.Instance?.QuitToMenu();
    }

    // Permite asignar PlayerHealth por código si lo necesitas
    public void SetPlayerHealth(PlayerHealth ph)
    {
        playerHealth = ph;
        UpdateHealthText();
    }
}

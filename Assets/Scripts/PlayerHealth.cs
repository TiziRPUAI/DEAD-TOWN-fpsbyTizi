using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IHittable
{
    [Header("Player Health")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Damage control")]
    [Tooltip("Tiempo mínimo entre daños consecutivos (para evitar multihit)")]
    public float damageCooldown = 0.1f;
    private float lastDamageTime = -999f;

    [Header("Optional events")]
    public UnityEvent<float> onDamage; 
    public UnityEvent onDeath;
    public GameObject bloodyScreen;
    private BloodyScreenEffect bloodyEffect;
    public GameObject deathScreen; 
    private DeathScreen deathScreenCtrl;
    public TextMeshProUGUI playerHealthUI;
    public bool isDead { get; private set; }

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;

        // Mantener el GameObject activo para que el componente pueda inicializarse.
        if (bloodyScreen != null)
        {
            bloodyScreen.SetActive(true);
            bloodyEffect = bloodyScreen.GetComponent<BloodyScreenEffect>();
            if (bloodyEffect == null)
                Debug.LogWarning($"{name}: BloodyScreenEffect no encontrado en bloodyScreen.");
        }

        if (deathScreen != null)
        {
            deathScreen.SetActive(true);
            deathScreenCtrl = deathScreen.GetComponent<DeathScreen>();
            if (deathScreenCtrl == null)
                Debug.LogWarning($"{name}: DeathScreen no encontrado en deathScreen.");
        }

        UpdateHealthUI();
    }

    // IHittable
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        if (Time.time - lastDamageTime < damageCooldown) return;
        lastDamageTime = Time.time;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"{name} recibió {amount} de daño. HP: {currentHealth}/{maxHealth}");
        onDamage?.Invoke(currentHealth);

        // Trigger overlay: usar la referencia cacheada si existe, sino buscarla al vuelo
        if (bloodyEffect == null && bloodyScreen != null)
            bloodyEffect = bloodyScreen.GetComponent<BloodyScreenEffect>();

        if (bloodyEffect != null)
            bloodyEffect.Trigger(amount / maxHealth);

        UpdateHealthUI();

        if (currentHealth <= 0f)
            Die();
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // Utilidades públicas
    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        onDamage?.Invoke(currentHealth);
        UpdateHealthUI();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"{name} ha muerto.");
        onDeath?.Invoke();

        // Notificar al GameManager
        GameManager.Instance?.PlayerDied();

        // Desactivar componentes del jugador para evitar seguir moviéndose/disparando
        var pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        var weapon = GetComponentInChildren<Weapon>();
        if (weapon != null) weapon.enabled = false;

        // Activar el Animator que esté en los hijos para reproducir la animación de muerte
        var animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.enabled = true;

        // Mostrar blackout + "YOU DIED"
        if (deathScreenCtrl != null)
            deathScreenCtrl.Show();

        // Asegurar que la UI muestre 0 al morir
        UpdateHealthUI();

        // Puedes añadir aquí lógica de respawn, pantalla de Game Over, etc.
    }

    // Actualiza el texto del contador de vida (muestra valores enteros)
    private void UpdateHealthUI()
    {
        if (playerHealthUI == null) return;
        int current = Mathf.RoundToInt(currentHealth);
        int max = Mathf.RoundToInt(maxHealth);
        playerHealthUI.text = $"Health:{current}/{max}";
    }
}

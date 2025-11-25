using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IHittable
{
    [Header("Player Health")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Damage control")]
    [Tooltip("Tiempo mínimo entre daños consecutivos (para evitar multihit)")]
    public float damageCooldown = 0.05f;
    private float lastDamageTime = -999f;

    [Header("Optional events")]
    public UnityEvent<float> onDamage; // parámetro: currentHealth
    public UnityEvent onDeath;

    public bool isDead { get; private set; }

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
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

        // Puedes añadir aquí lógica de respawn, pantalla de Game Over, etc.
    }
}

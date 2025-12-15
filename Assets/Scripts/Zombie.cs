using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour, IHittable
{
    // Función: Controla salud, ataque, movimiento y muerte del zombie.
    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;
    public bool isDead = false;

    [Header("Death")]
    public float destroyAfterSeconds = 2f;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1.2f;

    [Header("Movement")]
    public float movementSpeed = 3.5f;

    [Header("Animator parameters")]
    public string attackTriggerName = "Attack";
    public string dieTriggerName = "Die";
        
    [Header("Fallback")]
    public string attackStateName = "";
    public bool forceAlwaysAnimate = true;

    // Sonido: intervalo entre reproducciones del sonido de persecución/movimiento
    [Header("Sound")]
    public float chaseSoundInterval = 1.0f;

    private NavMeshAgent navAgent;
    private Collider[] colliders;
    private Animator animator;

    private Transform player;
    private IHittable playerHittable;
    private float lastAttackTime = -999f;

    // Sonidos: control interno
    private Coroutine chaseSoundCoroutine;

    // Función: Inicializa componentes, asigna velocidad y registra en GameManager.
    void Start()
    {
        currentHealth = maxHealth;
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null && forceAlwaysAnimate)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        colliders = GetComponents<Collider>();

        if (navAgent != null)
        {
            navAgent.speed = movementSpeed;
        }

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;

            playerHittable = playerGO.GetComponent<IHittable>();
            if (playerHittable == null)
                playerHittable = playerGO.GetComponentInChildren<IHittable>();
            if (playerHittable == null)
                playerHittable = playerGO.GetComponentInParent<IHittable>();

        }

        GameManager.Instance?.RegisterEnemy();
    }

    // Función: Lógica por frame para detener/mover al zombie y gestionar ataques.
    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            if (navAgent != null && navAgent.enabled) navAgent.isStopped = true;

            // Al entrar en rango de ataque, detener sonido de persecución
            StopChaseSound();

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                DoAttack();
                lastAttackTime = Time.time;
            }
        }
        else
        {
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.isStopped = false;
                // Si el agente se mueve hacia el jugador, iniciar sonido de persecución si no está ya
                StartChaseSound();
            }
        }
    }

    // Función: Ejecuta la animación de ataque y aplica daño al IHittable objetivo.
    private void DoAttack()
    {
        if (animator != null)
        {
            bool hasAttackParam = false;
            foreach (var p in animator.parameters)
            {
                if (p != null && p.name == attackTriggerName)
                {
                    hasAttackParam = true;
                    break;
                }
            }
            if (hasAttackParam) animator.SetTrigger(attackTriggerName);
            else if (!string.IsNullOrEmpty(attackStateName))
            {
                int stateHash = Animator.StringToHash(attackStateName);
                for (int layer = 0; layer < animator.layerCount; layer++)
                {
                    if (animator.HasState(layer, stateHash))
                    {
                        animator.CrossFade(stateHash, 0.1f, layer, 0f);
                        break;
                    }
                }
            }
        }

        // Reproducir sonido de ataque a través del SoundManager (serializado, no solapado)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayZombieAttack();
        }

        if (playerHittable != null)
        {
            Debug.Log($"{name} ataca a {player.name} con {attackDamage} daño.");
            playerHittable.TakeDamage(attackDamage);
        }
        else
        {
            Debug.LogWarning($"{name} intentó atacar pero playerHittable es null.");
        }
    }

    // Función: Aplicar daño al zombie y comprobar muerte.
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // Reproducir sonido de daño de forma inmediata y prioritaria (interrumpe cola de zombies)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayZombieSoundImmediate(SoundManager.Instance.zombieHurt);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // Función: Ejecutar la muerte (animación, deshabilitar colliders/agent y notificar).
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Detener cualquier sonido de persecución en curso
        StopChaseSound();

        GetComponent<EnemyNavigation>()?.Die();

        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }

        if (colliders != null)
        {
            foreach (var c in colliders)
                if (c != null) c.enabled = false;
        }

        // Reproducir sonido de muerte de forma inmediata (vacía cola y lo reproduce)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayZombieSoundImmediate(SoundManager.Instance.zombieDeath);
        }

        if (animator != null)
        {
            bool hasDieParam = false;
            foreach (var p in animator.parameters)
            {
                if (p != null && p.name == dieTriggerName)
                {
                    hasDieParam = true;
                    break;
                }
            }

            if (hasDieParam) animator.SetTrigger(dieTriggerName);
            else transform.Rotate(Vector3.right, 90f);
        }
        else transform.Rotate(Vector3.right, 90f);

        GameManager.Instance?.EnemyDied();

        StartCoroutine(DestroyAfterSecondsRealtime());
    }

    // Función: Destruye el GameObject tras un tiempo no escalado.
    private System.Collections.IEnumerator DestroyAfterSecondsRealtime()
    {
        yield return new WaitForSecondsRealtime(destroyAfterSeconds);
        Destroy(gameObject);
    }

    // Función: Devuelve la salud actual.
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // Inicia la coroutine que reproduce periódicamente el sonido de persecución/movimiento.
    private void StartChaseSound()
    {
        if (isDead) return;
        if (chaseSoundCoroutine != null) return;
        if (SoundManager.Instance == null || SoundManager.Instance.zombieChannel == null || SoundManager.Instance.zombieChase == null) return;

        chaseSoundCoroutine = StartCoroutine(ChaseSoundLoop());
    }

    // Detiene la coroutine de sonido de persecución.
    private void StopChaseSound()
    {
        if (chaseSoundCoroutine != null)
        {
            StopCoroutine(chaseSoundCoroutine);
            chaseSoundCoroutine = null;
        }
    }

    // Reproduce el clip de persecución cada 'chaseSoundInterval' segundos mientras el zombie se esté moviendo.
    private System.Collections.IEnumerator ChaseSoundLoop()
    {
        while (true)
        {
            // Si el zombie dejó de moverse, terminar
            if (isDead) yield break;
            if (navAgent == null || !navAgent.enabled || navAgent.isStopped) yield break;

            // Si el agente está muy cerca del destino, deja de sonar
            if (navAgent.remainingDistance <= navAgent.stoppingDistance) yield break;

            // Encolar sonido de persecución a través del SoundManager (evita solapamiento)
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayZombieChase();
            }

            yield return new WaitForSeconds(chaseSoundInterval);
        }
    }
}
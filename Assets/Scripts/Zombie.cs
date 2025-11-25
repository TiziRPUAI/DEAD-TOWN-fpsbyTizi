using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour, IHittable
{
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

    [Header("Animator parameters")]
    public string attackTriggerName = "Attack";
    public string dieTriggerName = "Die";

    [Header("Fallback")]
    public string attackStateName = "";
    public bool forceAlwaysAnimate = true;

    private NavMeshAgent navAgent;
    private Collider[] colliders;
    private Animator animator;

    private Transform player;
    private IHittable playerHittable;
    private float lastAttackTime = -999f;

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

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerHittable = playerGO.GetComponent<IHittable>();
        }

        // Asegurarse de que GameManager tenga el conteo correcto si el zombie se instancia runtime
        GameManager.Instance?.CountEnemies();
    }

    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            if (navAgent != null && navAgent.enabled) navAgent.isStopped = true;

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                DoAttack();
                lastAttackTime = Time.time;
            }
        }
        else
        {
            if (navAgent != null && navAgent.enabled) navAgent.isStopped = false;
        }
    }

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

        if (playerHittable != null)
            playerHittable.TakeDamage(attackDamage);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

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

        // Notificar al GameManager que un enemigo murió
        GameManager.Instance?.EnemyDied();

        Destroy(gameObject, destroyAfterSeconds);
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}

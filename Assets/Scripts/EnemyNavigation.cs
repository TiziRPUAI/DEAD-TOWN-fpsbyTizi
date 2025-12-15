using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavigation : MonoBehaviour
{
    // Función: Gestiona la navegación del enemigo hacia el jugador usando NavMeshAgent.
    public NavMeshAgent agent;
    public Transform player;
    public float initialDelay;
    public float interval;

    // Función: Inicializa el agente y programa la actualización periódica del destino.
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (agent != null && player != null)
        {
            InvokeRepeating(nameof(SetDestination), initialDelay, interval);
        }
    }

    // Función: Ejecuta la lógica de muerte del enemigo (detener agente, desactivar collider y destruir).
    public void Die()
    {
        if (agent != null)
            agent.isStopped = true;

        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        transform.Rotate(90, 0, 0);

        Destroy(gameObject, 2f);
    }

    // Función: Actualiza el destino del NavMeshAgent al jugador si es posible.
    public void SetDestination()
    {
        if (agent != null && player != null && agent.enabled)
            agent.destination = player.position;
    }
}
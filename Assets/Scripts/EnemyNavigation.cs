using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavigation : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public float initialDelay;
    public float interval;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (agent != null && player != null)
        {
            InvokeRepeating(nameof(SetDestination), initialDelay, interval);
        }
    }

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

    public void SetDestination()
    {
        if (agent != null && player != null && agent.enabled)
            agent.destination = player.position;
    }
}
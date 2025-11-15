using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalSenses : MonoBehaviour
{
    private AnimalEventHub eventHub;
    private NavMeshAgent agent;

    private void Awake()
    {
        eventHub.GetComponent<AnimalEventHub>();
        agent = GetComponent<NavMeshAgent>();
    }


}

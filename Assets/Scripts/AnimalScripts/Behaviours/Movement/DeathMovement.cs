using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class DeathMovement : BaseMovementScript, IAnimalMovement
{
    public Vector3? MoveTargetPosition => null;
    public Vector3? LookTargetPosition => null;
    public bool? LookAtTarget { get; private set; }

    NavMeshAgent agent;
    AnimalEventHub eventHub;

    public DeathMovement(NavMeshAgent agent, AnimalEventHub eventHub)
    {
        this.agent = agent;
        this.eventHub = eventHub;
        AssignMovementStats();
    }

    protected override void AssignMovementStats()
    {
        AssignBaseMovementStats(agent);
    }

    public void Enter()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    public void Update()
    {
    //pobranie pozycji jeœli cia³o jest trzymane
    }
    public void Exit() { }
}

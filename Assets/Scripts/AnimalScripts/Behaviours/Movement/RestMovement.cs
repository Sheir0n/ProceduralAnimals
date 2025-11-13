using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RestMovement : IAnimalMovement
{
    private NavMeshAgent agent;
    private Transform transform;
    public Vector3? MoveTargetPosition { get; private set; }
    public Vector3? LookTargetPosition { get; private set; }

    public bool? LookAtTarget { get; private set; }
    public RestMovement(NavMeshAgent agent, Transform transform)
    {
        this.agent = agent;
        this.transform = transform;
    }

    public void Enter()
    {
        LookAtTarget = false;
        MoveTargetPosition = transform.position;
        LookAtTarget = false;
    }

    public void Update()
    {
        
    }

    public void Exit()
    {


    }
}

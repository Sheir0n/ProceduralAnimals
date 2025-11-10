using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerControlledBehavior : IAnimalBehavior
{
    private NavMeshAgent agent;
    private Transform transform;

    public Vector3? MoveTargetPosition { get; private set; }
    public Vector3? LookTargetPosition { get; private set; }
    public bool? LookAtTarget { get; private set; }

    public PlayerControlledBehavior(NavMeshAgent agent, Transform transform)
    {
        this.agent = agent;
        this.transform = transform;
    }

    public void Enter()
    {
        MoveTargetPosition = transform.position;
    }

    public void Update()
    {
        if (Input.GetMouseButton(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitInfo))
            {
                MoveTargetPosition = hitInfo.point;
                agent.SetDestination(MoveTargetPosition.Value);
            }
        }

        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitInfo))
            {
                LookTargetPosition = hitInfo.point;
                LookAtTarget = true;
            }
        }
        else
            LookAtTarget = false;
    }

    public void Exit()
    {
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EmptyMovement : MovementScript, IAnimalMovement
{
    public Vector3? MoveTargetPosition => null;
    public Vector3? LookTargetPosition => null;
    public bool? LookAtTarget { get; private set; }
    protected override void AssignExtraMovementStats(NavMeshAgent agent) { }

    public void Enter()
    {
        agent.ResetPath();
    }

    public void Update()
    {

    }
    public void Exit() { }

    protected override void AssignBaseMovementStats(NavMeshAgent agent)
    {
        baseStats.Speed = 0;
        baseStats.AngularSpeed = 0;
        baseStats.Acceleration = 0;
        baseStats.StoppingDistance = 0;
    }
}

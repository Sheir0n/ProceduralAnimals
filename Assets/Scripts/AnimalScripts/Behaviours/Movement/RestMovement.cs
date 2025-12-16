using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;



[CreateAssetMenu(fileName = "RestMovement", menuName = "AI/Movement/RestMovement")]
public class RestMovement : MovementScript, IAnimalMovement
{
    public Vector3? MoveTargetPosition { get; private set; }
    public Vector3? LookTargetPosition { get; private set; }
    public bool? LookAtTarget { get; private set; }

    protected override void AssignExtraMovementStats(NavMeshAgent agent) { }

    public void Enter()
    {
        agent.isStopped = false;
        LookAtTarget = false;

        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        MoveTargetPosition = transform.position + flatForward * 0.5f;
        agent.SetDestination((Vector3)MoveTargetPosition);
    }

    public void Update()
    {
        SmoothAssignMovementStats(agent, baseStats, lerpSpeed: 0.5f);
    }

    public void Exit()
    {
    }
}

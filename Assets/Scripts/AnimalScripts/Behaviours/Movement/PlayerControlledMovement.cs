using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "PlayerMovement", menuName = "AI/Movement/PlayerMovement")]
public class PlayerControlledMovement : MovementScript, IAnimalMovement
{
    public Vector3? MoveTargetPosition { get; private set; }
    public Vector3? LookTargetPosition { get; private set; }
    public bool? LookAtTarget { get; private set; }

    protected override void AssignExtraMovementStats(NavMeshAgent agent){}

    public void Enter()
    {
        MoveTargetPosition = transform.position;
    }

    public void Update()
    {
        SmoothAssignMovementStats(agent, baseStats, lerpSpeed: 5f);

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

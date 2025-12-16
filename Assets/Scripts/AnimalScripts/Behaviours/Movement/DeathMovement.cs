using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "DeathMovement", menuName = "AI/Movement/DeathMovement")]
public class DeathMovement : BaseMovementScript, IAnimalMovement
{
    public Vector3? MoveTargetPosition => null;
    public Vector3? LookTargetPosition => null;
    public bool? LookAtTarget { get; private set; }

    protected override void AssignExtraMovementStats(NavMeshAgent agent) { }

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

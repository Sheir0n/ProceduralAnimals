using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RestMovement : BaseMovementScript, IAnimalMovement
{
    private NavMeshAgent agent;
    private Transform transform;
    public Vector3? MoveTargetPosition { get; private set; }
    public Vector3? LookTargetPosition { get; private set; }

    public bool? LookAtTarget { get; private set; }

    private MovementStats slowDownStats;
    public RestMovement(NavMeshAgent agent, Transform transform, IReadOnlyAnimalStats generalStatsHook)
    {
        this.agent = agent;
        this.transform = transform;
        AssignMovementStats();
    }

    protected override void AssignMovementStats()
    {
        AssignBaseMovementStats(agent);
        slowDownStats = new MovementStats(BaseStats);
        slowDownStats.Speed *= 0.1f;
    }

    public void Enter()
    {
        agent.isStopped = false;
        LookAtTarget = false;
        MoveTargetPosition = transform.position;
        LookAtTarget = false;
    }

    public void Update()
    {
        SmoothAssignMovementStats(agent, slowDownStats, lerpSpeed: 0.5f);
    }

    public void Exit()
    {
    }
}

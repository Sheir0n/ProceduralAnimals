using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ChaseFoodMovement : BaseMovementScript, IAnimalMovement
{
    private NavMeshAgent agent;
    private AnimalEventHub eventHub;
    private MovementStats chaseStats;

    private Vector3 moveTargetPos;
    private Vector3 lookTargetPos;
    public Vector3? MoveTargetPosition => moveTargetPos;
    public Vector3? LookTargetPosition => lookTargetPos;
    public bool? LookAtTarget { get; private set; }

    private Transform chaseTarget = null;

    private float updateAgentTargetingTimerMs = 0f;
    private const int updateAgentTargetingTimeMs = 500;

    public ChaseFoodMovement(NavMeshAgent agent, AnimalEventHub eventHub)
    {
        this.agent = agent;
        this.eventHub = eventHub;
        AssignMovementStats();
    }

    protected override void AssignMovementStats()
    {
        AssignBaseMovementStats(agent);
        chaseStats = new MovementStats(BaseStats);
    }

    public void Enter() {
        updateAgentTargetingTimerMs = 0f;
    }

    public void Update() {
        SmoothAssignMovementStats(agent, chaseStats, lerpSpeed: 5f);

        Transform newTarget = eventHub.FindNearestHuntTarget();
        if(newTarget != null && chaseTarget != newTarget)
        {
            chaseTarget = newTarget;
            updateAgentTargetingTimerMs = updateAgentTargetingTimeMs;
        }

        updateAgentTargetingTimerMs += Time.deltaTime * 1000f;
        if (updateAgentTargetingTimerMs >= updateAgentTargetingTimeMs)
        {
            agent.SetDestination(chaseTarget.position);
        }
    }
    public void Exit() {
        chaseTarget = null;
    } 
}

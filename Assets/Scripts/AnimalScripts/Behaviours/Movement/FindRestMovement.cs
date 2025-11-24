using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class FindRestSpotMovement : BaseMovementScript, IAnimalMovement
{
    private NavMeshAgent agent;
    private Transform transform;
    public Vector3? MoveTargetPosition { get; private set; }
    public Vector3? LookTargetPosition { get; private set; }
    public bool? LookAtTarget { get; private set; }

    private MovementStats searchRestStats;
    private FindRestSpotMovementSettings searchRestMovementSettings;
    private AnimalEventHub eventHub;
    private Transform nearestRestSpot;
    private bool isOnRestSpot;

    public FindRestSpotMovement(NavMeshAgent agent, FindRestSpotMovementSettings settings, Transform transform, AnimalEventHub eventHub, IReadOnlyAnimalStats generalStatsHook)
    {
        this.agent = agent;
        this.searchRestMovementSettings = settings;
        this.animalStatsHook = generalStatsHook;
        this.transform = transform;
        this.eventHub = eventHub;
        AssignMovementStats();
    }

    protected override void AssignMovementStats()
    {
        AssignBaseMovementStats(agent);
        searchRestStats = new MovementStats(BaseStats);

        float modifier = searchRestMovementSettings.vigorWalkSpeedVariationModifier;
        float speedMultiplier = Mathf.Lerp(1f - modifier, 1f + modifier, animalStatsHook.StatVigor);

        searchRestStats.Speed *= searchRestMovementSettings.agentBaseWalkSpeedMultiplier * speedMultiplier;
        searchRestStats.Acceleration *= searchRestMovementSettings.agentBaseWalkSpeedMultiplier * speedMultiplier;
    }

    public void Enter()
    {
        eventHub.OnIsOnRestSpotRequest += CheckIsOnRestSpot;
        nearestRestSpot = eventHub.FindNearestRestSpot();
        if (nearestRestSpot == null)
            return;

        SetAgentDestination(nearestRestSpot);
        isOnRestSpot = false;
    }

    public void Update() {
        if (nearestRestSpot == null)
            return;

        Vector3 agentPos = transform.position;
        Vector3 spotPos = nearestRestSpot.position;

        float radius = nearestRestSpot.lossyScale.x * 0.6f;

        Vector3 diff = agentPos - spotPos;
        diff.y = 0f;

        if (diff.sqrMagnitude <= radius * radius)
        {
            isOnRestSpot = true;
            Debug.Log("Agent is near resting spot!");
        }
    }
    public void Exit()
    {
        nearestRestSpot = null;
        eventHub.OnIsOnRestSpotRequest -= CheckIsOnRestSpot;
    }

    private void SetAgentDestination(Transform newRestSpot)
    {
        Vector3 spotPos = newRestSpot.position;

        Vector2 randomDir2D = Random.insideUnitCircle.normalized;
        Vector3 randomDir = new Vector3(randomDir2D.x, 0f, randomDir2D.y);

        float offset = newRestSpot.lossyScale.x * 0.5f;

        Vector3 finalTarget = spotPos + randomDir * offset;

        agent.SetDestination(finalTarget);
    }

    private bool CheckIsOnRestSpot()
    {
        return isOnRestSpot;
    }
}

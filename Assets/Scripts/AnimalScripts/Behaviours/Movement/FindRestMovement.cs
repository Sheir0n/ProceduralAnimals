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

    private float restSpotScale = 0.5f;
    private float restSpotCheckRangeBonus = 0.1f;

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

        SmoothAssignMovementStats(agent, searchRestStats, lerpSpeed: 5f);

        Vector3 agentPos = transform.position;
        Vector3 spotPos = nearestRestSpot.position;

        float radius = nearestRestSpot.lossyScale.x * (restSpotScale + restSpotCheckRangeBonus);

        Vector3 diff = agentPos - spotPos;
        diff.y = 0f;

        if (diff.sqrMagnitude <= radius * radius)
            isOnRestSpot = true;
        DrawDebugCircle(nearestRestSpot.position, radius);
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

        float offset = newRestSpot.lossyScale.x * restSpotScale;

        Vector3 finalTarget = spotPos + randomDir * offset;

        agent.SetDestination(finalTarget);
    }

    private bool CheckIsOnRestSpot()
    {
        return isOnRestSpot;
    }

    public void DrawDebugCircle(Vector3 center, float radius, int segments = 32)
    {
        Vector3 prev = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            Debug.DrawLine(prev, next, Color.red);
            prev = next;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "FindRestSpotMovement", menuName = "AI/Movement/FindRestSpotMovement")]
public class FindRestSpotMovement : MovementScript, IAnimalMovement
{
    public Vector3? MoveTargetPosition { get; private set; }
    public Vector3? LookTargetPosition { get; private set; }
    public bool? LookAtTarget { get; private set; }

    [Header("Global Stats Sensitivity Variables")]
    public float vigorWalkSpeedVariationModifier = 0f;
    [Range(0, 1)] public float lowHealthSpeedPenalityThreshold = 0f;
    [Range(0f, 1)] public float healthSlowdownMaxPenality = 0f;

    MovementStats penalisedStats;

    private Transform nearestRestSpot;
    private bool isOnRestSpot;

    private float restSpotScale = 0.55f;
    private float restSpotCheckRangeBonus = 1f;
    private bool pathSet = false;

    public override void OnInstantiate(NavMeshAgent agent, Transform transform, AnimalEventHub eventHub, IReadOnlyAnimalStats statsHook)
    {
        base.OnInstantiate(agent, transform, eventHub, statsHook);
    }

    protected override void AssignExtraMovementStats(NavMeshAgent agent)
    {
        penalisedStats = new MovementStats(baseStats);
    }

    public void Enter()
    {
        eventHub.OnIsOnRestSpotRequest += CheckIsOnRestSpot;
        nearestRestSpot = eventHub.FindNearestRestSpot();
        if (nearestRestSpot == null)
            return;
        pathSet = false;
        SetAgentDestination(nearestRestSpot);
        isOnRestSpot = false;
    }

    public void Update()
    {
        if (!pathSet)
        {
            nearestRestSpot = eventHub.FindNearestRestSpot();
            if (nearestRestSpot == null)
                return;
            SetAgentDestination(nearestRestSpot);
        }

        penalisedStats = CalculateHealthStatPenality(baseStats);
        SmoothAssignMovementStats(agent, penalisedStats, lerpSpeed: 5f);

        Vector3 agentPos = transform.position;
        Vector3 spotPos = nearestRestSpot.position;
        agentPos.y = 0;
        spotPos.y = 0;

        float radius = nearestRestSpot.lossyScale.x * restSpotScale + restSpotCheckRangeBonus;

        Vector3 diff = agentPos - spotPos;
        diff.y = 0f;

        if (diff.sqrMagnitude <= radius * radius)
            isOnRestSpot = true;

#if UNITY_EDITOR
        DrawDebugCircle(nearestRestSpot.position, radius);
#endif
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
        NavMeshPath path = new NavMeshPath();

        if (agent.CalculatePath(finalTarget, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            agent.SetDestination(finalTarget);
            pathSet = true;
        }
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

    private MovementStats CalculateHealthStatPenality(MovementStats baseStats)
    {
        float speedMultiplier = 1f;

        float currentHealthNormalized = animalStatsHook.Health / animalStatsHook.MaxHealth;
        if (currentHealthNormalized <= lowHealthSpeedPenalityThreshold)
        {
            float t = 1f - (currentHealthNormalized / lowHealthSpeedPenalityThreshold);
            t = Mathf.Clamp01(t);

            speedMultiplier = 1f - t * healthSlowdownMaxPenality;
        }

        MovementStats modifiedStats = baseStats;
        modifiedStats.Speed = baseStats.Speed * speedMultiplier;
        modifiedStats.Acceleration = baseStats.Acceleration * speedMultiplier;

        return modifiedStats;
    }
}

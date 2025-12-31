using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "FleeMovement", menuName = "AI/Movement/FleeMovement")]
public class FleeMovement : MovementScript, IAnimalMovement
{
    [Header("Flee Wander Randomness Variables")]
    [SerializeField] private float selectNewTargetCooldownMs = 250;
    [SerializeField] private float fleeWanderCircleDistance = 2f;
    [SerializeField] private float fleeWanderCircleRadius = 1.5f;
    [SerializeField] private float fleeWanderJitter = 0.1f;

    [Header("Global Stats Sensitivity Variables")]
    [SerializeField][Range(0, 1)] private float lowHealthSpeedPenalityThreshold = 0f;
    [SerializeField][Range(0f, 1)] private float healthSlowdownMaxPenality = 0f;

    private bool wanderFallback = false;
    private float selectNewTargetTimerMs = 0;
    private float fallbackTimerMs = 0;
    private const float fallbackCooldownMs = 500;
    public Vector3? MoveTargetPosition { get; private set; }
    public Vector3? LookTargetPosition { get; private set; }
    public bool? LookAtTarget { get; private set; }

    MovementStats penalisedStats;

    protected override void AssignExtraMovementStats(NavMeshAgent agent)
    {
        penalisedStats = new MovementStats(baseStats);
    }

    public void Enter()
    {
        fallbackTimerMs = fallbackCooldownMs;
        wanderFallback = false;
        selectNewTargetTimerMs = 0;
        LookAtTarget = true;
    }

    public void Update()
    {
        penalisedStats = CalculateHealthStatPenality(baseStats);

        SmoothAssignMovementStats(agent, penalisedStats, lerpSpeed: 5f);
        if (fallbackCooldownMs < fallbackTimerMs && selectNewTargetTimerMs >= selectNewTargetCooldownMs)
        {
            selectNewTargetTimerMs = 0;
            Vector3 newPos = GetNewWanderTarget();
            MoveTargetPosition = newPos;
            LookTargetPosition = newPos;
            agent.SetDestination((Vector3)MoveTargetPosition);
        }

        UpdateTimer();
    }

    public void Exit() { }

    private Vector3 GetNewWanderTarget()
    {
        const int maxTries = 5;
        TrackedWithScore fearOrigin = eventHub.RequestTrackedFear();
        if(fearOrigin.tracked == null)
            fearOrigin.tracked = transform;
        for (int i = 0; i < maxTries; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * Random.Range(0f, fleeWanderJitter);
            Vector3 wanderTarget = Vector3.Normalize(new Vector3(randomOffset.x, 0, randomOffset.z)) * fleeWanderCircleRadius * (0.5f + animalStatsHook.StatCuriosity);

            Vector3 fleeDirection = (agent.transform.position - fearOrigin.tracked.transform.position).normalized;
            Vector3 circleCenter = fleeDirection * fleeWanderCircleDistance;
            Vector3 targetPos = agent.transform.position + circleCenter + wanderTarget;
            targetPos.y = 0;

            if (NavMeshUtils.IsPointOnNavMesh(targetPos, out Vector3 validPos, 0.25f) && NavMeshUtils.CanReachTarget(agent, validPos))
            {
                SetWanderFallback(false);
                return validPos;
            }
        }

        //Fallback function
        //Search for any valid position
        const float searchRadius = 5f;

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * searchRadius;
            Vector3 candidate = agent.transform.position + new Vector3(randomDir.x, 0, randomDir.y);
            candidate.y = 0;

            if (NavMeshUtils.IsPointOnNavMesh(candidate, out Vector3 candidateValidPos, 1.5f) && NavMeshUtils.CanReachTarget(agent, candidateValidPos))
            {
                SetWanderFallback(true);
                return candidateValidPos;
            }
        }
        return agent.transform.position;
    }

    private void SetWanderFallback(bool fallback)
    {
        if (fallback != wanderFallback)
        {
            wanderFallback = fallback;
        }
    }

    private void UpdateTimer()
    {
        selectNewTargetTimerMs += Time.deltaTime * 1000 * Random.Range(0f, 2f);
        fallbackTimerMs += Time.deltaTime * 1000;
    }

    private MovementStats CalculateHealthStatPenality(MovementStats stats)
    {
        float speedMultiplier = 1f;

        float currentHealthNormalized = animalStatsHook.Health / animalStatsHook.MaxHealth;
        if (currentHealthNormalized <= lowHealthSpeedPenalityThreshold)
        {
            float t = 1f - (currentHealthNormalized / lowHealthSpeedPenalityThreshold);
            t = Mathf.Clamp01(t);

            speedMultiplier = 1f - t * healthSlowdownMaxPenality;
        }

        MovementStats modifiedStats = stats;
        modifiedStats.Speed = stats.Speed * speedMultiplier;
        modifiedStats.Acceleration = stats.Acceleration * speedMultiplier;

        return modifiedStats;
    }
}

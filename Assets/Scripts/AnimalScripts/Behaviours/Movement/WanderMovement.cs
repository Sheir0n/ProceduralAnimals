using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class WanderMovement : BaseMovementScript, IAnimalMovement
{
    private NavMeshAgent agent;

    private WanderMovementSettings wanderSettings;

    private bool showTargetingLogs = false;

    private bool wanderFallback = false;
    private float selectNewTargetTimerMs = 0;
    private float fallbackTimerMs = 0;
    private const float fallbackCooldownMs = 500;

    private Vector3 moveTargetPos;
    private Vector3 lookTargetPos;
    public Vector3? MoveTargetPosition => moveTargetPos;
    public Vector3? LookTargetPosition => lookTargetPos;
    public bool? LookAtTarget { get; private set; }

    private MovementStats walkStats;
    public WanderMovement(NavMeshAgent agent, WanderMovementSettings settings, IReadOnlyAnimalStats generalStatsHook)
    {
        this.agent = agent;
        this.wanderSettings = settings;
        this.animalStatsHook = generalStatsHook;
        AssignMovementStats();
    }

    protected override void AssignMovementStats()
    {
        AssignBaseMovementStats(agent);
        walkStats = new MovementStats(BaseStats);

        float modifier = wanderSettings.vigorWalkSpeedVariationModifier;
        float speedMultiplier = Mathf.Lerp(1f - modifier, 1f + modifier, animalStatsHook.StatVigor);

        walkStats.Speed *= wanderSettings.agentBaseWalkSpeedMultiplier * speedMultiplier;
        walkStats.Acceleration *= wanderSettings.agentBaseWalkSpeedMultiplier * speedMultiplier;
    }

    public void Enter()
    {
        fallbackTimerMs = fallbackCooldownMs;
        wanderFallback = false;
        selectNewTargetTimerMs = 0;

        //tymczasowo
        LookAtTarget = false;
    }

    public void Update()
    {
        MovementStats penalizedStats = CalculateHealthStatPenality(walkStats);

        SmoothAssignMovementStats(agent, penalizedStats, lerpSpeed: 5f);
        if (fallbackCooldownMs < fallbackTimerMs && selectNewTargetTimerMs >= wanderSettings.selectNewTargetCooldownMs)
        {
            selectNewTargetTimerMs = 0;
            Vector3 newPos = GetNewWanderTarget();
            moveTargetPos = newPos;
            lookTargetPos = newPos;
            agent.SetDestination(moveTargetPos);
        }

        UpdateTimer();
    }

    public void Exit() 
    {
    }

    private Vector3 GetNewWanderTarget()
    {
        const int maxTries = 5;

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * Random.Range(0f, wanderSettings.wanderJitter);
            Vector3 wanderTarget = Vector3.Normalize(new Vector3(randomOffset.x, 0, randomOffset.z)) * wanderSettings.wanderCircleRadius;

            Vector3 circleCenter = agent.transform.forward * wanderSettings.wanderCircleDistance;
            Vector3 targetPos = agent.transform.position + circleCenter + wanderTarget;
            targetPos.y = 0;

            if (NavMeshUtils.IsPointOnNavMesh(targetPos, out Vector3 validPos, 0.25f) && NavMeshUtils.CanReachTarget(agent, validPos))
            {
                if (showTargetingLogs)
                    Debug.Log($"[Wander] Target found on attempt {i + 1}");
                SetWanderFallbackSpeed(false);
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
                if (showTargetingLogs)
                    Debug.Log($"[Wander] Fallback target found on attempt {i + 1}");
                SetWanderFallbackSpeed(true);
                return candidateValidPos;
            }
        }
        return agent.transform.position;
    }

    private void SetWanderFallbackSpeed(bool fallback)
    {
        if (fallback != wanderFallback)
        {
            if (fallback)
            {
                walkStats.AngularSpeed = wanderSettings.agentFallbackAngularSpeed;
                fallbackTimerMs = 0;
            }
            else
                walkStats.AngularSpeed = wanderSettings.agentWanderAngularSpeed;

            wanderFallback = fallback;
        }
    }

    private void UpdateTimer()
    {
        selectNewTargetTimerMs += Time.deltaTime * 1000 * Random.Range(0f, 2f);
        fallbackTimerMs += Time.deltaTime * 1000;
    }

    private MovementStats CalculateHealthStatPenality(MovementStats baseStats)
    {
        float speedMultiplier = 1f;

        float currentHealthNormalized = animalStatsHook.Health / animalStatsHook.MaxHealth;
        if (currentHealthNormalized <= wanderSettings.lowHealthSpeedPenalityThreshold)
        {
            float t = 1f - (currentHealthNormalized / wanderSettings.lowHealthSpeedPenalityThreshold);
            t = Mathf.Clamp01(t);

            speedMultiplier = 1f - t * wanderSettings.healthSlowdownMaxPenality;
        }

        MovementStats modifiedStats = baseStats;
        modifiedStats.Speed = baseStats.Speed * speedMultiplier;
        modifiedStats.Acceleration = baseStats.Acceleration * speedMultiplier;

        return modifiedStats;
    }
}

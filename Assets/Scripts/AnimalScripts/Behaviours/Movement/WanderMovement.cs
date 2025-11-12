using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class WanderMovement : IAnimalMovement
{
    private NavMeshAgent agent;

    private WanderMovementSettings settings;

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

    public WanderMovement(NavMeshAgent agent, WanderMovementSettings settings)
    {
        this.agent = agent;
        this.settings = settings;
    }

    public void Enter()
    {
        fallbackTimerMs = fallbackCooldownMs;
        wanderFallback = false;
        selectNewTargetTimerMs = 0;

        //tymczasowo
        LookAtTarget = true;
    }

    public void Update()
    {
        if (fallbackCooldownMs < fallbackTimerMs && selectNewTargetTimerMs >= settings.selectNewTargetCooldownMs)
        {
            selectNewTargetTimerMs = 0;
            Vector3 newPos = GetNewWanderTarget();
            moveTargetPos = newPos;
            lookTargetPos = newPos;
            agent.SetDestination(moveTargetPos);
        }

        UpdateTimer();
    }

    public void Exit() { }

    private Vector3 GetNewWanderTarget()
    {
        const int maxTries = 5;

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * Random.Range(0f, settings.wanderJitter);
            Vector3 wanderTarget = Vector3.Normalize(new Vector3(randomOffset.x, 0, randomOffset.z)) * settings.wanderCircleRadius;

            Vector3 circleCenter = agent.transform.forward * settings.wanderCircleDistance;
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
                agent.angularSpeed = settings.agentFallbackAngularSpeed;
                fallbackTimerMs = 0;
            }
            else
                agent.angularSpeed = settings.agentWanderAngularSpeed;

            wanderFallback = fallback;
        }
    }

    private void UpdateTimer()
    {
        selectNewTargetTimerMs += Time.deltaTime * 1000 * Random.Range(0f, 2f);
        fallbackTimerMs += Time.deltaTime * 1000;
    }
}

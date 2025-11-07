using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class PathfindController : MonoBehaviour
{
    [Header("Nav Mesh Agent Component")]
    private NavMeshAgent agent;
    [SerializeField] bool enableMouseFollow = false;
    [SerializeField] bool enableMouseLook = false;
    public Vector3 moveTargetPos { get; private set; } = Vector3.zero;
    public Vector3 lookTargetPos { get; private set; } = Vector3.zero;
    public bool lookAtTarget { get; private set; } = false;

    private Quaternion lastRotation;
    public float agentCurrAngularSpeed { get; private set; } = 0;

    [Header("Wander Variables")]
    [SerializeField] private float selectNewTargetCooldownMs = 250;
    private float selectNewTargetTimerMs = 0;
    private const float fallbackCooldownMs = 500;
    private float fallbackTimerMs = 0;

    [SerializeField] private float wanderCircleDistance = 2f;
    [SerializeField] private float wanderCircleRadius = 1.5f;
    [SerializeField] private float wanderJitter = 0.1f;

    [Header("Wander Speed Variables")]
    [SerializeField] private float agentWanderAngularSpeed = 90;
    [SerializeField] private float agentFallbackAngularSpeed = 240;
    private bool wanderFallback = false;

    [Header("Debug Comments")]
    [SerializeField] private bool showTargetingLogs = false;

    void Start()
    {
        agent = transform.GetComponentInParent<NavMeshAgent>();
        lastRotation = transform.rotation;
        fallbackTimerMs = fallbackCooldownMs;
    }
    void Update()
    {
        if (Input.GetMouseButton(1) && enableMouseFollow)
        {
            Ray targetMovePos = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(targetMovePos, out var hitInfo))
            {
                SetWanderSpeedFallback(false);
                moveTargetPos = hitInfo.point;
                agent.SetDestination(moveTargetPos);
            }
        }
        else
        {
                if (fallbackCooldownMs < fallbackTimerMs && selectNewTargetTimerMs >= selectNewTargetCooldownMs)
                {
                    selectNewTargetTimerMs = 0;
                    moveTargetPos = GetNewWanderTarget();
                    agent.SetDestination(moveTargetPos);
                }
        }

        if (Input.GetMouseButton(0) && enableMouseLook)
        {
            Ray targetLookPos = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(targetLookPos, out var hitInfo))
            {
                lookTargetPos = hitInfo.point;
                lookAtTarget = true;
            }
        }
        else
        {
            lookTargetPos = moveTargetPos;
        }

        CalculateAngularSpeed();
        UpdateTimer();
    }

    private Vector3 GetNewWanderTarget()
    {
        const int maxTries = 5;

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * Random.Range(0f, wanderJitter);
            Vector3 wanderTarget = Vector3.Normalize(new Vector3(randomOffset.x, 0, randomOffset.z)) * wanderCircleRadius;

            Vector3 circleCenter = agent.transform.forward * wanderCircleDistance;
            Vector3 targetPos = agent.transform.position + circleCenter + wanderTarget;
            targetPos.y = 0;

            if (IsPointOnNavMesh(targetPos, out Vector3 validPos, 0.25f) && CanReachTarget(agent, validPos))
            {
                if (showTargetingLogs)
                    Debug.Log($"[Wander] Target found on attempt {i + 1}");
                SetWanderSpeedFallback(false);
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

            if (IsPointOnNavMesh(candidate, out Vector3 candidateValidPos, 1.5f) && CanReachTarget(agent, candidateValidPos))
            {
                if (showTargetingLogs)
                    Debug.Log($"[Wander] Fallback target found on attempt {i + 1}");
                SetWanderSpeedFallback(true);
                return candidateValidPos;
            }
        }
        return agent.transform.position;
    }

    private void UpdateTimer()
    {
        selectNewTargetTimerMs += Time.deltaTime * 1000 * Random.Range(0f, 2f);
        fallbackTimerMs += Time.deltaTime * 1000;
    }

    private void SetWanderSpeedFallback(bool fallback)
    {
        if (fallback != wanderFallback)
        {
            if (fallback)
            {
                agent.angularSpeed = agentFallbackAngularSpeed;
                fallbackTimerMs = 0;
            }
            else
                agent.angularSpeed = agentWanderAngularSpeed;

            wanderFallback = fallback;
        }
    }

    public bool IsMoving() => agent.velocity.sqrMagnitude > 0.01f;
    public float GetVelocity() => agent.velocity.magnitude;

    private void CalculateAngularSpeed()
    {
        Quaternion delta = transform.rotation * Quaternion.Inverse(lastRotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        agentCurrAngularSpeed = angle / Time.deltaTime;
        lastRotation = transform.rotation;
    }

    private bool IsPointOnNavMesh(Vector3 point, out Vector3 validPoint, float maxDistance = 1f)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(point, out hit, maxDistance, NavMesh.AllAreas))
        {
            validPoint = hit.position;
            return true;
        }

        validPoint = Vector3.zero;
        return false;
    }

    private bool CanReachTarget(NavMeshAgent agent, Vector3 targetPos)
    {
        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(targetPos, path))
            return false;
        if (path.status != NavMeshPathStatus.PathComplete)
            return false;
        if (path.corners.Length < 2)
            return false;

        return path.status == NavMeshPathStatus.PathComplete;
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class NavMeshUtils
{
    public static bool IsPointOnNavMesh(Vector3 point, out Vector3 validPoint, float maxDistance = 1f)
    {
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            validPoint = hit.position;
            return true;
        }

        validPoint = Vector3.zero;
        return false;
    }

    public static bool CanReachTarget(NavMeshAgent agent, Vector3 targetPos)
    {
        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(targetPos, path))
            return false;
        if (path.status != NavMeshPathStatus.PathComplete)
            return false;
        if (path.corners.Length < 2)
            return false;

        return true;
    }
}

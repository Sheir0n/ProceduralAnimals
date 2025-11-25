using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using static AnimalAI;

public class AnimalEventHub : MonoBehaviour
{
    public event Action<IReadOnlyAnimalStats> OnInitializeStats;
    public event Action<AIAction> OnActionChanged;
    public event Action<Vector3> OnSegmentCollision;
    public event Action OnFoundFirstRestSpot;
    public event Action<Transform> OnNewRestSpotFound;
    public event Action<Transform> OnNewInterestSpotFound;

    public event Func<float> OnAngularSpeedRequest;
    public event Func<LookTarget> OnPathfingingLookTargetRequest; // animator <= movement
    public event Func<LookTarget> OnInterestLookTargetRequest; // animator <= UtilityAI interest targets
    public event Func<LerpedLookData> OnLookConeSetCenterRequest; // senses <= animator (current lerped look for vision cone)
    public event Func<Transform> OnNearestRestSpotRequest;
    public event Func<bool> OnIsOnRestSpotRequest;
    

    private const int pushEventLimit = 5;
    private int currPushEventCount = 0;

    private void LateUpdate()
    {
        currPushEventCount = 0;
    }

    public void SendInitializeRequest(IReadOnlyAnimalStats stats) => OnInitializeStats?.Invoke(stats);
    public void SendAIStateChange(AIAction newAction) => OnActionChanged?.Invoke(newAction);
    public void PushAgentOnSegmentCollision(Vector3 pushVector)
    {
        if (currPushEventCount >= pushEventLimit) return;
        OnSegmentCollision?.Invoke(pushVector);
        currPushEventCount++;
    }
    public void FoundFirstRestSpot() => OnFoundFirstRestSpot?.Invoke();

    public float GetAngularSpeed() => OnAngularSpeedRequest?.Invoke() ?? 0f;
    public LookTarget RequestPathfindingLookTargetData()
    {
        return OnPathfingingLookTargetRequest?.Invoke() ?? new LookTarget(Vector3.zero, false);
    }

    public LookTarget RequestInterestLookTargetData()
    {
        return OnInterestLookTargetRequest?.Invoke() ?? new LookTarget(Vector3.zero, false);
    }

    public LerpedLookData RequestLookConeSetCenter()
    {
        return OnLookConeSetCenterRequest?.Invoke() ?? new LerpedLookData(transform.position, transform.forward);
    }
    public Transform FindNearestRestSpot() => OnNearestRestSpotRequest?.Invoke();
    public bool IsOnRestSpot() => OnIsOnRestSpotRequest?.Invoke() ?? false;

    public void NewRestSpotFound(Transform restTransform) => OnNewRestSpotFound?.Invoke(restTransform);
    public void NewInterestSpotFound(Transform interestTransform) => OnNewInterestSpotFound?.Invoke(interestTransform);
}

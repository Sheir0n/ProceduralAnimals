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
    public event Action<Transform> OnNewRestSpotFound;
    public event Action<Transform> OnNewInterestSpotFound;
    public event Action<Transform> OnNewHuntTargetFound;
    public event Action<Collider> OnAttemptBite;

    public event Func<float> OnAngularSpeedRequest;
    public event Func<LookTarget> OnPathfingingLookTargetRequest; // animator <= movement
    public event Func<LookTarget> OnInterestLookTargetRequest; // animator <= UtilityAI interest targets
    public event Func<HeadCenterData> OnHeadDataRequest; // senses <= animator (current lerped look for vision cone)
    public event Func<Transform> OnNearestRestSpotRequest;
    public event Func<bool> OnIsOnRestSpotRequest;
    public event Func<Transform> OnNearestHuntTargetRequest;


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

    public float GetAngularSpeed() => OnAngularSpeedRequest?.Invoke() ?? 0f;
    public LookTarget RequestPathfindingLookTargetData() => OnPathfingingLookTargetRequest?.Invoke() ?? new LookTarget(Vector3.zero, false);
    public LookTarget RequestInterestLookTargetData() => OnInterestLookTargetRequest?.Invoke() ?? new LookTarget(Vector3.zero, false);
    public HeadCenterData RequestHeadData() => OnHeadDataRequest?.Invoke() ?? new HeadCenterData(transform.position, transform.forward);
    public Transform FindNearestRestSpot() => OnNearestRestSpotRequest?.Invoke();
    public bool IsOnRestSpot() => OnIsOnRestSpotRequest?.Invoke() ?? false;
    public void NewRestSpotFound(Transform restTransform) => OnNewRestSpotFound?.Invoke(restTransform);
    public void NewInterestSpotFound(Transform interestTransform) => OnNewInterestSpotFound?.Invoke(interestTransform);
    public void NewHuntTargetFound(Transform interestTransform) => OnNewHuntTargetFound?.Invoke(interestTransform);
    public Transform FindNearestHuntTarget() => OnNearestHuntTargetRequest?.Invoke();

    public void AttemptBite(Collider other) => OnAttemptBite?.Invoke(other);
}

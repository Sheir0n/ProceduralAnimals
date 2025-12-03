using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using static AnimalAI;
using static ChaseFoodController;

public class AnimalEventHub : MonoBehaviour
{
    public event Action<IReadOnlyAnimalStats> OnInitializeStats;
    public event Action<AIAction> OnActionChanged;
    public event Action<Vector3> OnSegmentCollision;
    public event Action<Transform> OnNewRestSpotFound;
    public event Action<Transform> OnNewInterestSpotFound;
    public event Action<Transform> OnNewHuntTargetFound;
    public event Action<Collider> OnAttemptBite;
    public event Action<BiteAttackStage> OnBiteAttack;
    public event Action<IDamageable> OnAnnouncePreyCaught;

    public event Func<float> OnAngularSpeedRequest;
    public event Func<LookTarget> OnPathfindScriptLookTarget; // animator <= movement
    public event Func<LookTarget> OnInterestLookTarget; // animator <= UtilityAI interest targets
    public event Func<HeadCenterData> OnHeadDataRequest; // senses <= animator (current lerped look for vision cone)
    public event Func<Transform> OnNearestRestSpotRequest;
    public event Func<bool> OnIsOnRestSpotRequest;
    public event Func<Transform> OnNearestHuntTargetRequest;
    public event Func<AnimalMouthCollider> OnMouthHookRequest;

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

    public LookTarget RequestPathfindingLookTargetData() => OnPathfindScriptLookTarget?.Invoke() ?? new LookTarget(Vector3.zero, false);
    public LookTarget RequestInterestLookTargetData() => OnInterestLookTarget?.Invoke() ?? new LookTarget(Vector3.zero, false);
    public HeadCenterData RequestHeadData() => OnHeadDataRequest?.Invoke() ?? new HeadCenterData(transform.position, transform.forward);
    public Transform FindNearestRestSpot() => OnNearestRestSpotRequest?.Invoke();
    public bool IsOnRestSpot() => OnIsOnRestSpotRequest?.Invoke() ?? false;
    public void NewRestSpotFound(Transform restTransform) => OnNewRestSpotFound?.Invoke(restTransform);
    public void NewInterestSpotFound(Transform interestTransform) => OnNewInterestSpotFound?.Invoke(interestTransform);
    public void NewHuntTargetFound(Transform interestTransform) => OnNewHuntTargetFound?.Invoke(interestTransform);
    public Transform FindNearestHuntTarget() => OnNearestHuntTargetRequest?.Invoke();
    public void AttemptBite(Collider other) => OnAttemptBite?.Invoke(other);
    public AnimalMouthCollider GetAnimalMouth() => OnMouthHookRequest?.Invoke();
    public void AnnounceBiteAttack(BiteAttackStage attackStage) => OnBiteAttack?.Invoke(attackStage);
    public void AnnouncePreyCaught(IDamageable prey) => OnAnnouncePreyCaught?.Invoke(prey);
}

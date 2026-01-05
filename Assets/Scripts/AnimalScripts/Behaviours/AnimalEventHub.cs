using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AnimalAI;
using static ChaseFoodController;

public class AnimalEventHub : MonoBehaviour
{
    public event Action OnBodyGenerated;
    public event Action<IReadOnlyAnimalStats> OnInitializeStats;
    public event Action<ActionID> OnActionChanged;
    public event Action<Vector3> OnSegmentCollision;
    public event Action<List<Transform>> OnNewInterestFound;
    public event Action<Collider> OnAttemptBite;
    public event Action<BiteAttackStage> OnBiteAttack;
    public event Action OnDeath;
    public event Action<float> OnDeathFade;

    public event Func<float> OnAngularSpeedRequest;
    public event Func<LookTarget> OnPathfindScriptLookTarget; // animator <= movement
    public event Func<LookTarget> OnInterestLookTarget; // animator <= UtilityAI interest targets
    public event Func<HeadCenterData> OnHeadDataRequest; // senses <= animator (current lerped look for vision cone)
    public event Func<Transform> OnNearestRestSpotRequest;
    public event Func<bool> OnIsOnRestSpotRequest;
    public event Func<AnimalMouthCollider> OnMouthHookRequest;
    public event Func<TrackedWithScore> OnTrackedPreyRequest;
    public event Func<TrackedWithScore> OnTrackedFearRequest;
    public event Func<TrackerDatas> OnTrackerDatasRequest;

    private const int pushEventLimit = 5;
    private int currPushEventCount = 0;

    private void LateUpdate()
    {
        currPushEventCount = 0;
    }

    public void AnnounceBodyGenerated() => OnBodyGenerated?.Invoke();
    public void SendInitializeRequest(IReadOnlyAnimalStats stats) => OnInitializeStats?.Invoke(stats);
    public void SendAIStateChange(ActionID newAction) => OnActionChanged?.Invoke(newAction);
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
    public void NewInterestsFound(List<Transform> interestsTransform) => OnNewInterestFound?.Invoke(interestsTransform);
    public void AttemptBite(Collider other) => OnAttemptBite?.Invoke(other);
    public AnimalMouthCollider GetAnimalMouth() => OnMouthHookRequest?.Invoke();
    public void AnnounceBiteAttack(BiteAttackStage attackStage) => OnBiteAttack?.Invoke(attackStage);
    public TrackedWithScore RequestTrackedPrey() => OnTrackedPreyRequest?.Invoke() ?? new TrackedWithScore(null,0);
    public TrackedWithScore RequestTrackedFear() => OnTrackedFearRequest?.Invoke() ?? new TrackedWithScore(null, 0);
    public TrackerDatas RequestTrackerDataToInitialize() => OnTrackerDatasRequest?.Invoke() ?? new TrackerDatas();
    public void AnnounceDeath() => OnDeath?.Invoke();
    public void DeathFade(float fadeValue) => OnDeathFade?.Invoke(fadeValue);
}

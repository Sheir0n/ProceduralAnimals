using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AnimalAI;

public class AnimalAnimator : MonoBehaviour
{
    [Header("Animal Joints")]
    protected List<AnimalJoint> joints;
    protected List<AnimalLimb> limbs;
    protected AnimalHead head;

    [Header("Movement Controller")]
    protected Vector3 prevHeadPosition;

    protected AnimalEventHub eventHub;
    [SerializeField] private bool isAnimalDisabled;

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        eventHub.OnActionChanged += OnActionChanged;
    }
    public void SetBody(List<AnimalJoint> spineJoints, List<AnimalLimb> limbs, AnimalHead head)
    {
        this.joints = spineJoints;
        this.limbs = limbs;
        this.head = head;
    }
    public void SetJoints(List<AnimalJoint> _segments) => joints = _segments;
    public void SetLimbs(List<AnimalLimb> _limbs) => limbs = _limbs;
    public void SetHead(AnimalHead _head) => head = _head;

    protected virtual void CalculateRootSegmentTransform()
    {
        if (joints != null && joints.Count > 0 && joints[0] != null)
        {
            joints[0].SetPosition(transform.position);
            joints[0].SetRotation(RotateUp(transform.rotation));
            joints[0].UpdateSegmentTransform();
        }
        else
            Debug.LogWarning("Animal Animator: segment[0] not found!");
    }

    protected virtual void CalculateMainBodyTransform(List<AnimalJoint> jointList, int minSegmentId, int maxSegmentId)
    {
        if (jointList == null || jointList.Count == 0)
        {
            Debug.LogWarning("Animal Animator: joints list is empty or null!");
            return;
        }

        if (minSegmentId < 1 || maxSegmentId > jointList.Count)
        {
            Debug.LogWarning($"Animal Animator: _minSegmentId ({minSegmentId}) or _maxSegmentId ({maxSegmentId}) out of range. List count: {jointList.Count}");
            return;
        }

        for (int i = minSegmentId; i < maxSegmentId; i++)
        {
            AnimalJoint prevSegment = jointList[i - 1];
            AnimalJoint currSegment = jointList[i];
            Vector3 toPrev = prevSegment.segmentPosition - currSegment.segmentPosition;
            float newLocalY = GetYAngleConstrained(toPrev, prevSegment, prevSegment.prefferedAngle);

            Vector3 idealDir = Quaternion.Euler(0f, newLocalY, 0f) * Vector3.forward;

            Vector3 targetPos = prevSegment.segmentPosition - idealDir * currSegment.distanceConstraint;
            float pushRadius = 0.25f * currSegment.segmentScale.x;
            if (SegmentHitsObstacle(targetPos, radius: pushRadius))
            {
                Vector3 pushedPos = PushBodyFromObstacle(prevSegment, targetPos, radius: pushRadius, pushFactor: 0.25f);

                targetPos = prevSegment.segmentPosition + (pushedPos - prevSegment.segmentPosition).normalized * currSegment.distanceConstraint;
            }

            currSegment.SetRotation(Quaternion.Euler(90f, newLocalY, 0f));
            currSegment.SetPosition(targetPos);

            currSegment.UpdateSegmentTransform();
        }
    }

    protected virtual void CalculateLimbsTransform()
    {
        int chainPullCount = 10;
        foreach (AnimalLimb currLimb in limbs)
        {
            currLimb.UpdateTargetingVariables(deltaMs: Time.deltaTime * 1000);
            CalculateLimbsTargetPosition(currLimb);
            CalculateFabrikTransforms(jointChain: currLimb.joints, parentJoint: joints[currLimb.limbData.parentJointId], targetPos: currLimb.targetLerpPosition, rootOffset: currLimb.parentLocalOffset, pulls: chainPullCount, doLerp: true);
        }
    }

    protected void CalculateHeadTransform()
    {
        if (head is null)
            return;

        if (!isAnimalDisabled)
        {
            LookTarget lookData = eventHub.RequestPathfindingLookTargetData();
            if (lookData.isLooking)
                head.LookAt(lookData);
            else
            {
                lookData = eventHub.RequestInterestLookTargetData();
                head.LookAt(lookData);
            }
        }
        int chainPullCount = 10;

        CalculateFabrikTransforms(jointChain: head.headJoints, parentJoint: head.parentJoint, targetPos: head.targetPosition, rootOffset: head.headLocalOffset, pulls: chainPullCount, doLerp: false);

        AnimalJoint end = head.headJoints.Last();
        AnimalJoint prev = head.headJoints[head.headJoints.Count - 2];

        float radius = 0.15f;

        if (SegmentHitsObstacle(end.segmentPosition, radius))
            PushBodyFromObstacle(prev, end.segmentPosition, radius, pushFactor: 0.45f);
    }

    protected void CalculateFabrikTransforms(List<AnimalJoint> jointChain, AnimalJoint parentJoint, Vector3 targetPos, Vector3 rootOffset, int pulls, bool doLerp)
    {
        if (isAnimalDisabled)
            pulls = 1;

        for (int i = 0; i < pulls; i++)
        {
            if (!isAnimalDisabled)
                ForwardPass(jointChain, targetPos);
            BackwardPass(jointChain, parentJoint, rootOffset);
        }

        if (doLerp)
            LerpUpdateChain(jointChain);
        else
            DirectUpdateChain(jointChain);
    }

    private void ForwardPass(List<AnimalJoint> chain, Vector3 targetPos)
    {
        AnimalJoint tip = chain[^1];
        tip.SetPosition(targetPos);

        float angleY = GetYAngle(targetPos - tip.segmentPosition);
        tip.SetRotation(Quaternion.Euler(90f, angleY, 0f));

        for (int i = chain.Count - 1; i > 0; i--)
        {
            AnimalJoint next = chain[i];
            AnimalJoint curr = chain[i - 1];

            SolveJoint(anchor: next, segment: curr, constraintJoint: curr, curr.prefferedAngle);
        }
    }

    private void BackwardPass(List<AnimalJoint> chain, AnimalJoint parent, Vector3 rootOffset)
    {
        AnimalJoint root = chain[0];
        Vector3 rootPos = parent.segmentPosition + parent.segmentRotation * rootOffset;

        root.SetPosition(rootPos);
        float angleY = GetYAngle(parent.segmentPosition - root.segmentPosition);
        root.SetRotation(Quaternion.Euler(90f, angleY, 0f));
        root.UpdateSegmentTransform();

        for (int i = 1; i < chain.Count; i++)
        {
            AnimalJoint prev = chain[i - 1];
            AnimalJoint curr = chain[i];

            SolveJoint(anchor: prev, segment: curr, constraintJoint: prev, -prev.prefferedAngle);
        }
    }

    private void SolveJoint(AnimalJoint anchor, AnimalJoint segment, AnimalJoint constraintJoint,float prefferedAngle)
    {
        Vector3 direction = anchor.segmentPosition - segment.segmentPosition;
        float newLocalY = GetYAngleConstrained(direction, constraintJoint, prefferedAngle);
        segment.SetRotation(Quaternion.Euler(90f, newLocalY, 0f));
        Vector3 allowedDir = Quaternion.Euler(0f, newLocalY, 0f) * Vector3.forward;
        segment.SetPosition(anchor.segmentPosition - allowedDir * segment.distanceConstraint);
    }

    private void LerpUpdateChain(List<AnimalJoint> chain)
    {
        const float lerpSpeed = 25f;

        for (int i = 1; i < chain.Count; i++)
        {
            var prev = chain[i - 1];
            var curr = chain[i];

            curr.UpdateLerpRotation(lerpSpeed);
            curr.UpdateLerpPosition(prev.segmentLerpPosition);
            curr.UpdateSegmentLerpTransform();
        }
    }

    private void DirectUpdateChain(List<AnimalJoint> chain)
    {
        for (int i = 1; i < chain.Count; i++)
            chain[i].UpdateSegmentTransform();
    }


    protected float GetYAngleConstrained(Vector3 vecToTarget, AnimalJoint targetJoint, float prefferedAngle)
    {
        Vector3 flatToTarget = new Vector3(vecToTarget.x, 0f, vecToTarget.z);
        flatToTarget.Normalize();

        float targetYAngle = Mathf.Atan2(flatToTarget.x, flatToTarget.z) * Mathf.Rad2Deg;
        float prevLocalY = targetJoint.segmentRotation.eulerAngles.y;
        float deltaY = Mathf.DeltaAngle(prevLocalY, targetYAngle);
        float maxAngle = targetJoint.angularConstraint;
        float clampedY = Mathf.Clamp(deltaY, -maxAngle - prefferedAngle, maxAngle - prefferedAngle);
        float newLocalY = prevLocalY + clampedY;
        return newLocalY;
    }

    protected float GetYAngle(Vector3 toTarget)
    {
        Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
        flatToTarget.Normalize();
        return Mathf.Atan2(flatToTarget.x, flatToTarget.z) * Mathf.Rad2Deg;
    }

    protected virtual void CalculateLimbsTargetPosition(AnimalLimb currLimb)
    {
        Vector3 targetPos = currLimb.targetPosition;
        Vector3 newTargetPosition = currLimb.GetNewTargetPos();
        float maxDistance = currLimb.limbData.maxReachDistance;
        float newTargetDistance = Vector3.Distance(newTargetPosition, targetPos);
        currLimb.CalculateTargetLerp();

        if (newTargetDistance > maxDistance)
        {
            currLimb.UpdateLimbTarget(lerp: true);
        }
    }

    protected Quaternion RotateUp(Quaternion rotation)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.x = 90f;
        return Quaternion.Euler(euler);
    }

    protected bool SegmentHitsObstacle(Vector3 pos, float radius)
    {
        LayerMask mask = LayerMask.GetMask("Obstacles");
        bool hit = Physics.CheckSphere(pos, radius, mask, QueryTriggerInteraction.Ignore);
        return hit;
    }

    protected Vector3 PushBodyFromObstacle(AnimalJoint prevSegment, Vector3 targetPos, float radius = 0.15f, float pushFactor = 0.45f)
    {
        Vector3 from = prevSegment.segmentPosition;
        float segmentLength = prevSegment.distanceConstraint;

        Collider[] hits = Physics.OverlapSphere(targetPos, radius, LayerMask.GetMask("Obstacles"));
        if (hits.Length == 0)
            return targetPos;

        Vector3 totalPush = Vector3.zero;
        const float MIN_PUSH = 0.05f;

        foreach (var hit in hits)
        {
            Vector3 closest = hit.ClosestPoint(targetPos);
            Vector3 dirAway = targetPos - closest;

            if (dirAway.sqrMagnitude < 0.0001f)
                dirAway = (targetPos - hit.bounds.center).sqrMagnitude < 0.0001f ? Vector3.up : targetPos - hit.bounds.center;

            dirAway.Normalize();

            float penetration = Mathf.Max(0f, radius - Vector3.Distance(targetPos, closest));
            totalPush += dirAway * Mathf.Max(MIN_PUSH, penetration);
        }

        Vector3 idealDir = (targetPos - from).normalized;
        Vector3 perpPush = Vector3.ProjectOnPlane(totalPush * pushFactor, idealDir);

        Vector3 pushedPos = from + (targetPos + perpPush - from).normalized * segmentLength;

        if (totalPush.sqrMagnitude > 0.0001f)
            eventHub.PushAgentOnSegmentCollision(totalPush);

        return pushedPos;
    }

    protected virtual void OnActionChanged(AIAction newAction)
    {
        Debug.Log("Animator recived: " + newAction);
        if (newAction == AIAction.Death)
        {
            Debug.Log("Animal disable recived");
            isAnimalDisabled = true;
        }
    }
}

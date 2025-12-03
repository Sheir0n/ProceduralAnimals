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

    protected virtual void Update()
    {
        if (joints == null || joints.Count == 0)
        {
            Debug.LogWarning("Animal Animator: joints list is empty or null!");
            return;
        }

        CalculateRootSegmentTransform();
        CalculateMainBodyTransform(joints, minSegmentId: 1, joints.Count);
        CalculateLimbsTransform();
        CalculateHeadTransform();
    }


    public void SetBody(List<AnimalJoint> spineJoints, List<AnimalLimb> limbs, AnimalHead head)
    {
        this.joints = spineJoints;
        this.limbs = limbs;
        this.head = head;
    }

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

            currSegment.SetRotation(Quaternion.Euler(90f, newLocalY, 0f));
            currSegment.SetPosition(targetPos);

            float baseRadius = 0.25f;
            float pushFactor = 0.25f;
            float radius = currSegment.segmentScale.magnitude * baseRadius;

            if (SegmentHitsObstacle(currSegment.segmentPosition, radius))
            {
                Vector3 pushed = PushBodyFromObstacle(prevSegment, currSegment.segmentPosition, radius, pushFactor, callEvent: true);
                Vector3 corrected = prevSegment.segmentPosition + (pushed - prevSegment.segmentPosition).normalized * currSegment.distanceConstraint;
                currSegment.SetPosition(corrected);
            }
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
    }

    protected void CalculateFabrikTransforms(List<AnimalJoint> jointChain, AnimalJoint parentJoint, Vector3 targetPos, Vector3 rootOffset, int pulls, bool doLerp)
    {
        if (isAnimalDisabled)
            pulls = 1;

        float collisionRadius = 0.15f;
        float pushFactor = 1f;

        for (int i = 0; i < pulls; i++)
        {
            if (!isAnimalDisabled)
                ForwardPass(jointChain, targetPos);
            BackwardPass(jointChain, parentJoint, rootOffset);
            ResolveAllSegmentCollisions(jointChain, collisionRadius, pushFactor);
            RebuildChainDistancesSafe(jointChain, collisionRadius);
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

            float radius = curr.segmentScale.magnitude * 0.12f;
            Vector3 pushOffset = CalculatePushOffset(next, curr, radius, pushFactor: 0.25f);
            Vector3 newPos = curr.segmentPosition + pushOffset;
            newPos.y = curr.segmentPosition.y;
            curr.SetPosition(newPos);
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

            float radius = curr.segmentScale.magnitude * 0.12f;
            Vector3 pushOffset = CalculatePushOffset(prev, curr, radius, pushFactor: 0.25f);
            Vector3 newPos = curr.segmentPosition + pushOffset;
            newPos.y = curr.segmentPosition.y;
            curr.SetPosition(newPos);
        }
    }

    private void SolveJoint(AnimalJoint anchor, AnimalJoint segment, AnimalJoint constraintJoint, float prefferedAngle)
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
        AnimalJoint tipSegment = currLimb.joints[^1];
        float distanceToTarget = Vector3.Distance(tipSegment.segmentPosition, currLimb.targetLerpPosition);

        if (distanceToTarget > 0.01f)
        {
            Vector3 newTargetPosition = currLimb.GetNewTargetPos();
            float maxDistance = currLimb.limbData.maxReachDistance;
            float newTargetDistance = Vector3.Distance(newTargetPosition, currLimb.targetPosition);

            if (newTargetDistance > maxDistance)
            {
                currLimb.UpdateLimbTarget(lerp: true);
            }
        }
        currLimb.CalculateTargetLerp();
    }

    protected Quaternion RotateUp(Quaternion rotation)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.x = 90f;
        return Quaternion.Euler(euler);
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

    // COLLISION DETECTION FUNCTIONS

    protected bool SegmentHitsObstacle(Vector3 pos, float radius)
    {
        LayerMask mask = LayerMask.GetMask("Obstacles");
        bool hit = Physics.CheckSphere(pos, radius, mask, QueryTriggerInteraction.Ignore);
        return hit;
    }

    protected Vector3 PushBodyFromObstacle(AnimalJoint prev, Vector3 targetPos, float radius, float pushFactor, bool callEvent = false)
    {
        Vector3 from = prev.segmentPosition;
        Collider[] hits = Physics.OverlapSphere(targetPos, radius, LayerMask.GetMask("Obstacles"));

        if (hits.Length == 0)
            return targetPos;

        const float MIN_PUSH = 0.05f;
        Vector3 totalPush = Vector3.zero;

        foreach (var hit in hits)
        {
            Vector3 closest = hit.ClosestPoint(targetPos);
            Vector3 dir = targetPos - closest;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = targetPos - hit.bounds.center;
                dir.y = 0f;
            }
            dir.Normalize();

            Vector2 targetXZ = new Vector2(targetPos.x, targetPos.z);
            Vector2 closestXZ = new Vector2(closest.x, closest.z);
            float penetration = radius - Vector2.Distance(targetXZ, closestXZ);
            if (penetration < 0f)
                continue;

            totalPush += dir * Mathf.Max(MIN_PUSH, penetration);
        }

        if (totalPush.sqrMagnitude < 0.0001f)
            return targetPos;

        Vector3 along = (targetPos - from).normalized;
        Vector3 perpPush = Vector3.ProjectOnPlane(totalPush, along) * pushFactor;
        Vector3 finalPos = from + along * prev.distanceConstraint + perpPush;

        if (callEvent)
            eventHub.PushAgentOnSegmentCollision(totalPush);

        return finalPos;
    }

    private Vector3 CalculatePushOffset(AnimalJoint prevSegment, AnimalJoint currSegment, float radius, float pushFactor)
    {
        Vector3 checkPos = currSegment.segmentPosition;
        Collider[] hits = Physics.OverlapSphere(checkPos, radius, LayerMask.GetMask("Obstacles"));
        if (hits.Length == 0) return Vector3.zero;

        Vector3 totalPush = Vector3.zero;
        const float MIN_PUSH = 0.25f;

        foreach (var hit in hits)
        {
            Vector3 closest = hit.ClosestPoint(checkPos);
            Vector3 dir = checkPos - closest;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = checkPos - hit.bounds.center;
                dir.y = 0f;
            }
            dir.Normalize();

            Vector2 posXZ = new Vector2(checkPos.x, checkPos.z);
            Vector2 closestXZ = new Vector2(closest.x, closest.z);
            float penetration = radius - Vector2.Distance(posXZ, closestXZ);
            if (penetration <= 0f) continue;

            totalPush += dir * Mathf.Max(MIN_PUSH, penetration);
        }

        Vector3 along = (currSegment.segmentPosition - prevSegment.segmentPosition).normalized;
        return Vector3.ProjectOnPlane(totalPush, along) * pushFactor;
    }

    private void ResolveAllSegmentCollisions(List<AnimalJoint> chain, float radius, float pushFactor)
    {
        for (int i = 0; i < chain.Count; i++)
        {
            AnimalJoint curr = chain[i];
            Collider[] hits = Physics.OverlapSphere(curr.segmentPosition, radius, LayerMask.GetMask("Obstacles"));
            if (hits.Length == 0) continue;

            Vector3 totalPush = Vector3.zero;

            foreach (var hit in hits)
            {
                Vector3 closest = hit.ClosestPoint(curr.segmentPosition);
                Vector3 dir = curr.segmentPosition - closest;
                dir.y = 0f;

                if (dir.sqrMagnitude < 0.0001f)
                {
                    dir = curr.segmentPosition - hit.bounds.center;
                    dir.y = 0f;
                }

                Vector2 currXZ = new Vector2(curr.segmentPosition.x, curr.segmentPosition.z);
                Vector2 closestXZ = new Vector2(closest.x, closest.z);
                float penetration = radius - Vector2.Distance(currXZ, closestXZ);
                if (penetration > 0f)
                    totalPush += dir.normalized * penetration;
            }

            if (totalPush.sqrMagnitude > 0.0001f)
            {
                Vector3 newPos = curr.segmentPosition + totalPush * pushFactor;
                curr.SetPosition(Vector3.Lerp(curr.segmentPosition, newPos, 0.35f));
            }
        }
    }

    private void RebuildChainDistancesSafe(List<AnimalJoint> chain, float radius)
    {
        LayerMask mask = LayerMask.GetMask("Obstacles");

        for (int i = 1; i < chain.Count; i++)
        {
            AnimalJoint prev = chain[i - 1];
            AnimalJoint curr = chain[i];

            Vector3 dir = curr.segmentPosition - prev.segmentPosition;
            dir.y = 0f;
            dir = dir.normalized;

            Vector3 newPos = prev.segmentPosition + dir * curr.distanceConstraint;
            // Y BLOKOWANY DO PREV:
            newPos.y = prev.segmentPosition.y;
            curr.SetPosition(newPos);

            int safety = 0;
            while (Physics.CheckSphere(curr.segmentPosition, radius, mask))
            {
                Collider[] hits = Physics.OverlapSphere(curr.segmentPosition, radius, mask);

                Vector3 correction = Vector3.zero;

                foreach (var h in hits)
                {
                    Vector3 closest = h.ClosestPoint(curr.segmentPosition);
                    Vector3 push = curr.segmentPosition - closest;
                    push.y = 0f;

                    if (push.sqrMagnitude < 0.0001f)
                    {
                        push = curr.segmentPosition - h.bounds.center;
                        push.y = 0f;
                    }

                    Vector2 currXZ = new Vector2(curr.segmentPosition.x, curr.segmentPosition.z);
                    Vector2 closestXZ = new Vector2(closest.x, closest.z);
                    float penetration = radius - Vector2.Distance(currXZ, closestXZ);
                    if (penetration > 0f)
                        correction += push.normalized * penetration;
                }

                curr.SetPosition(curr.segmentPosition + correction);

                dir = curr.segmentPosition - prev.segmentPosition;
                dir.y = 0f;
                dir = dir.normalized;

                Vector3 fixedPos = prev.segmentPosition + dir * curr.distanceConstraint;
                fixedPos.y = prev.segmentPosition.y;
                curr.SetPosition(fixedPos);

                safety++;
                if (safety > 6) break;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnimalAnimator : MonoBehaviour
{
    [Header("Animal Joints")]
    protected List<AnimalJoint> joints;
    protected List<AnimalLimb> limbs;
    protected AnimalHead head;

    [Header("Movement Controller")]
    [SerializeField] protected PathfindController movementController;
    protected Vector3 prevHeadPosition;

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

    protected virtual void CalculateMainBodyTransform(int _minSegmentId, int _maxSegmentId)
    {
        if (joints == null || joints.Count == 0)
        {
            Debug.LogWarning("Animal Animator: joints list is empty or null!");
            return;
        }

        if (_minSegmentId < 1 || _maxSegmentId > joints.Count)
        {
            Debug.LogWarning($"Animal Animator: _minSegmentId ({_minSegmentId}) or _maxSegmentId ({_maxSegmentId}) out of range. List count: {joints.Count}");
            return;
        }

        for (int i = _minSegmentId; i < _maxSegmentId; i++)
        {
            AnimalJoint prevSegment = joints[i - 1];
            AnimalJoint currSegment = joints[i];

            Vector3 toPrev = prevSegment.segmentPosition - currSegment.segmentPosition;
            float newLocalY = GetYAngleConstrained(vecToTarget: toPrev, targetJoint: prevSegment, prevSegment.prefferedAngle);
            currSegment.SetRotation(Quaternion.Euler(90f, newLocalY, 0f));

            Vector3 allowedDir = Quaternion.Euler(0f, newLocalY, 0f) * Vector3.forward;
            currSegment.SetPosition(prevSegment.segmentPosition - allowedDir * currSegment.distanceConstraint);

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
        head.LookAt(movementController.lookTargetPos, movementController.lookAtTarget);
        int chainPullCount = 10;
        CalculateFabrikTransforms(jointChain: head.headJoints, parentJoint: head.parentJoint, targetPos: head.targetPosition, rootOffset: head.headLocalOffset, pulls: chainPullCount, doLerp: false);
    }

    protected void CalculateFabrikTransforms(List<AnimalJoint> jointChain, AnimalJoint parentJoint, Vector3 targetPos, Vector3 rootOffset, int pulls, bool doLerp)
    {
        for (int pullId = 0; pullId < pulls; pullId++)
        {
            AnimalJoint currJoint = jointChain.Last();
            currJoint.SetPosition(targetPos);

            float angleY = GetYAngle(targetPos - currJoint.segmentPosition);
            currJoint.SetRotation(Quaternion.Euler(90f, angleY, 0f));
            //currJoint.UpdateSegmentTransform();
            for (int i = jointChain.Count() - 1; i > 0; i--)
            {
                AnimalJoint nextSegment = jointChain[i];
                AnimalJoint currSegment = jointChain[i - 1];
                Vector3 toNext = nextSegment.segmentPosition - currSegment.segmentPosition;
                float newLocalY = GetYAngleConstrained(vecToTarget: toNext, targetJoint: currSegment, currSegment.prefferedAngle);
                currSegment.SetRotation(Quaternion.Euler(90f, newLocalY, 0f));

                Vector3 allowedDir = Quaternion.Euler(0f, newLocalY, 0f) * Vector3.forward;
                currSegment.SetPosition(nextSegment.segmentPosition - allowedDir * currSegment.distanceConstraint);
            }

            currJoint = jointChain[0];
            Vector3 rootPosition = parentJoint.segmentPosition + parentJoint.segmentRotation * rootOffset;

            currJoint.SetPosition(rootPosition);
            angleY = GetYAngle(toTarget: parentJoint.segmentPosition - currJoint.segmentPosition);
            currJoint.SetRotation(Quaternion.Euler(90f, angleY, 0f));
            currJoint.UpdateSegmentTransform();

            for (int i = 1; i < jointChain.Count; i++)
            {
                AnimalJoint prevSegment = jointChain[i - 1];
                AnimalJoint currSegment = jointChain[i];

                Vector3 toPrev = prevSegment.segmentPosition - currSegment.segmentPosition;
                float newLocalY = GetYAngleConstrained(vecToTarget: toPrev, targetJoint: prevSegment, -prevSegment.prefferedAngle);
                currSegment.SetRotation(Quaternion.Euler(90f, newLocalY, 0f));

                Vector3 allowedDir = Quaternion.Euler(0f, newLocalY, 0f) * Vector3.forward;
                currSegment.SetPosition(prevSegment.segmentPosition - allowedDir * currSegment.distanceConstraint);
            }
        }

        if (doLerp)
        {
            for (int i = 1; i < jointChain.Count; i++)
            {
                float lerpSpeed = 25;
                AnimalJoint prevSegment = jointChain[i - 1];
                AnimalJoint currSegment = jointChain[i];

                jointChain[i].UpdateLerpRotation(lerpSpeed);
                jointChain[i].UpdateLerpPosition(prevSegment.segmentLerpPosition);
                jointChain[i].UpdateSegmentLerpTransform();
            }
        }
        else
        {
            for (int i = 1; i < jointChain.Count; i++)
            {
                jointChain[i].UpdateSegmentTransform();
            }
        }
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
}

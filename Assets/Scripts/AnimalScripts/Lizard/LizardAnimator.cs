using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LizardAnimator : AnimalAnimator
{
    private float angularSpeedUnlinkLimbPairs = 40f;
    private bool calculateTailLean = false;

    private float tailLerpSpeed = 10f;
    private float currentTailLeanLerp = 0f;
    private int randomTailDir = 0;

    protected override void Awake()
    {
        base.Awake();
        eventHub.OnHeadDataRequest += GetLookCenter;
        eventHub.OnMouthTransformRequest += GetMouthSegmentPos;
    }

    private void Update()
    {
        if (joints == null || joints.Count == 0)
        {
            Debug.LogWarning("Animal Animator: joints list is empty or null!");
            return;
        }

        CalculateRootSegmentTransform();

        if (calculateTailLean)
        {
            CalculateMainBodyTransform(joints, 1, joints.Count);
            currentTailLeanLerp = Mathf.Lerp(currentTailLeanLerp, 1, tailLerpSpeed * Time.deltaTime);
            ForceSegmentsLean(6, joints.Count, randomTailDir);
        }
        else
            CalculateMainBodyTransform(joints, 1, joints.Count);

        CalculateLimbsTransform();
        CalculateHeadTransform();
    }

    protected override void CalculateLimbsTargetPosition(AnimalLimb currLimb)
    {
        if (limbs.Count < 4)
            return;

        Vector3 targetPos = currLimb.targetPosition;
        Vector3 newTargetPosition = currLimb.GetNewTargetPos();
        float maxDistance = currLimb.limbData.maxReachDistance;
        currLimb.CalculateTargetLerp();

        float newTargetDistance = Vector3.Distance(newTargetPosition, targetPos);
        if (newTargetDistance > maxDistance)
        {
            float angularSpeed = eventHub.GetAngularSpeed();
            if (angularSpeedUnlinkLimbPairs > angularSpeed)
            {
                if (currLimb.limbId == 0 || currLimb.limbId == 3)
                {
                    limbs[0].UpdateLimbTarget(lerp: true);
                    limbs[3].UpdateLimbTarget(lerp: true);
                }
                else if (currLimb.limbId == 1 || currLimb.limbId == 2)
                {
                    limbs[1].UpdateLimbTarget(lerp: true);
                    limbs[2].UpdateLimbTarget(lerp: true);
                }
            }
            else
                currLimb.UpdateLimbTarget(lerp: true);
        }
    }

    private HeadCenterData GetLookCenter() => head.GetLerpedLook(segmentId: 2);

    protected override void OnActionChanged(AnimalAI.AIAction newAction)
    {
        base.OnActionChanged(newAction);
        if (joints != null && joints.Count > 0)
        {
            if (newAction == AnimalAI.AIAction.Rest)
            {
                calculateTailLean = true;
                currentTailLeanLerp = 0f;
                randomTailDir = CalculateTailLeanDirection();
            }
            else
            {
                calculateTailLean = false;
            }
        }
    }


    private int CalculateTailLeanDirection()
    {
        float deltaY = Mathf.DeltaAngle(joints[5].segmentRotation.eulerAngles.y, joints[6].segmentRotation.eulerAngles.y);
        int directionalAmount = deltaY >= 0 ? 1 : -1;

        return directionalAmount;
    }


    protected void ForceSegmentsLean(int minSegmentId, int maxSegmentId, float directionalAmount, float leanSpeed = 200f)
    {
        if (joints == null || joints.Count == 0)
            return;

        if (minSegmentId < 1 || maxSegmentId > joints.Count)
            return;

        for (int i = minSegmentId; i < maxSegmentId; i++)
        {
            AnimalJoint prev = joints[i - 1];
            AnimalJoint curr = joints[i];

            float prevY = prev.segmentRotation.eulerAngles.y;
            float targetDelta = Mathf.Abs(prev.angularConstraint) * currentTailLeanLerp * directionalAmount;
            float targetY = prevY + targetDelta;

            float newY = Mathf.MoveTowardsAngle(curr.segmentRotation.eulerAngles.y, targetY, leanSpeed * Time.deltaTime);

            Quaternion globalRot = Quaternion.Euler(90f, newY, 0f);
            Vector3 allowedDir = Quaternion.Euler(0f, newY, 0f) * Vector3.forward;

            Vector3 targetPos = prev.segmentPosition - allowedDir * curr.distanceConstraint;

            float pushRadius = 0.25f * curr.segmentScale.x;
            if (SegmentHitsObstacle(targetPos, pushRadius))
            {
                targetPos = PushBodyFromObstacle(prev, targetPos, pushRadius, 0.25f);
            }

            curr.SetRotation(globalRot);
            curr.SetPosition(targetPos);
            curr.UpdateSegmentTransform();
        }
    }

    private Transform GetMouthSegmentPos() => head.headJoints.Last().transform;
}

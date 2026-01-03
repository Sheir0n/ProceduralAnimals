using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class LizardAnimator : AnimalAnimator
{
    private float angularSpeedUnlinkLimbPairs = 40f;
    private bool calculateTailLean = false;

    private float tailLerpSpeed = 10f;
    private float currentTailLeanLerp = 0f;
    private int randomTailDir = 0;

    [Header("ID zachowania ze zwiniêciem ogona")]
    [SerializeField] private ActionID tailRestActionID = null;

    protected override void Update()
    {
        if (!isBodyReady)
            return;

        if (joints == null || joints.Count == 0)
        {
            Debug.LogWarning("LizardAnimator: Lista stawów jest pusta!",this);
            return;
        }

        CalculateRootSegmentTransform();

        if (calculateTailLean)
        {
            CalculateMainBodyTransform(joints, minSegmentId: 1, joints.Count);
            currentTailLeanLerp = Mathf.Lerp(currentTailLeanLerp, 1, tailLerpSpeed * Time.deltaTime);
            ForceSegmentsLean(minSegmentId: 6, joints.Count, randomTailDir);
        }
        else
            CalculateMainBodyTransform(joints, 1, joints.Count);

        CalculateLimbsTransform();
        CalculateHeadTransform();
        UpdateMesh(bodyColor);
    }

    protected override void CalculateLimbsTargetPosition(AnimalLimb currLimb)
    {
        if (limbs.Count < 4)
            return;

        AnimalJoint tipSegment = currLimb.joints[^1];
        float distanceToTarget = Vector3.Distance(tipSegment.segmentPosition, currLimb.targetLerpPosition);
        if (distanceToTarget < 0.01f)
        {
            currLimb.CalculateTargetLerp();
            return;
        }

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
            {
                currLimb.UpdateLimbTarget(lerp: true);
            }
        }
    }

    protected override void OnActionChanged(ActionID newAction)
    {
        base.OnActionChanged(newAction);
        if (joints != null && joints.Count > 0)
        {
            if (newAction == tailRestActionID)
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
        float deltaY = Mathf.DeltaAngle(joints[5].yaw, joints[6].yaw);
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

            float prevYaw = prev.yaw;
            float targetDelta = Mathf.Abs(prev.angularConstraint) * currentTailLeanLerp * directionalAmount;
            float targetYaw = prevYaw + targetDelta;

            float newYaw = Mathf.MoveTowardsAngle(curr.yaw, targetYaw, leanSpeed * Time.deltaTime);

            Vector3 allowedDir = new Vector3(Mathf.Sin(newYaw * Mathf.Deg2Rad), 0f, Mathf.Cos(newYaw * Mathf.Deg2Rad));
            Vector3 targetPos = prev.segmentPosition - allowedDir * curr.distanceConstraint;

            float pushRadius = 0.25f * curr.segmentScale.x;
            if (SegmentHitsObstacle(targetPos, pushRadius))
            {
                targetPos = PushBodyFromObstacle(prev, targetPos, pushRadius, pushFactor: 0.25f, callEvent: true);
            }

            curr.SetRotation(newYaw);
            curr.SetPosition(targetPos);
            curr.UpdateSegmentTransform();
        }
    }
}

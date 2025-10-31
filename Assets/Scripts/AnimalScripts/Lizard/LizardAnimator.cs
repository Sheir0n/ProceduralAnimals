using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LizardAnimator : AnimalAnimator
{
    private float angularSpeedUnlinkLimbPairs = 40f;

    private void Update()
    {
        if (joints == null || joints.Count == 0)
        {
            Debug.LogWarning("Animal Animator: joints list is empty or null!");
            return;
        }

        CalculateRootSegmentTransform();
        CalculateMainBodyTransform(1, joints.Count);
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
            if (angularSpeedUnlinkLimbPairs < movementController.agentAngularSpeed)
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
}

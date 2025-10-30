using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LizardAnimator : AnimalAnimator
{
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
        Vector3 limbEndPosition = currLimb.joints.Last().transform.position;
        float maxDistance = currLimb.limbData.maxReachDistance;
        currLimb.CalculateTargetLerp();

        float limbEndDistance = Vector3.Distance(limbEndPosition, targetPos);

        if (limbEndDistance > maxDistance)
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

        //if (limbEndDistance > maxDistance)
        //{
        //    currLimb.UpdateLimbTarget(lerp: true);
        //}
    }
}

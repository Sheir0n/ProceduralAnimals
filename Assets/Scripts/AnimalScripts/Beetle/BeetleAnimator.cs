using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeetleAnimator : AnimalAnimator
{ 
    private void Update()
    {
        if (joints == null || joints.Count == 0)
        {
            Debug.LogWarning("Animal Animator: joints list is empty or null!");
            return;
        }

        CalculateRootSegmentTransform();
        CalculateMainBodyTransform(joints, 1, joints.Count);
        CalculateLimbsTransform();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

public class SnakeAnimator : AnimalAnimator
{
    public void SetJoints(List<SnakeJoint> _segments)
    {
        joints = new List<AnimalJoint>(_segments);
    }

    private void Update()
    {
        if (joints == null || joints.Count == 0)
        {
            Debug.LogWarning("Animal Animator: joints list is empty or null!");
            return;
        }

        CalculateHeadSegmentTransform();
        CalculateMainBodyTransform(1, joints.Count);
    }

}

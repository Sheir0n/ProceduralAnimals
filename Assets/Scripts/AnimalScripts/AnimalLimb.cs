using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

[System.Serializable]
public class AnimalLimbData
{
    public string limbName;
    public List<SegmentData> joints = new List<SegmentData>();
    public int parentJointId;
    public Vector3 parentPositionOffset;
    public Vector3 targetPosOffset;
    public float maxReachDistance;
}

public class AnimalLimb
{
    public AnimalLimbData limbData { get; private set; }
    public List<AnimalJoint> joints { get; private set; }
    public Vector3 targetPosition = Vector3.zero;
    public Vector3 targetLerpPosition = Vector3.zero;
    public int limbId;

    public AnimalLimb(AnimalLimbData _limbData, List<AnimalJoint> _joints, int limbId)
    {
        limbData = _limbData;
        joints = _joints;
        UpdateLimbTarget(lerp: false);
        this.limbId = limbId;
    }

    public void UpdateLimbTarget(bool lerp)
    {
        AnimalJoint rootJoint = joints[0];
        Vector3 prevPosition = rootJoint.transform.position;
        float rootRotdirection = Mathf.Sign(limbData.parentPositionOffset.x);
        Vector3 newTargetPosition = rootJoint.transform.position + rootJoint.segmentRotation * Quaternion.Euler(0f, 0f, rootRotdirection * -90f) * limbData.targetPosOffset;

        if (!lerp)
            targetLerpPosition = newTargetPosition;
        targetPosition = newTargetPosition;
    }

    public void CalculateTargetLerp()
    {
        float speed = 35;
        targetLerpPosition = Vector3.Lerp(targetLerpPosition, targetPosition, speed * Time.deltaTime);
    }
}

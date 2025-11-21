using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using static UnityEngine.RuleTile.TilingRuleOutput;


[System.Serializable]
public class AnimalHeadData
{
    public string headName;
    public List<SegmentData> joints = new List<SegmentData>();
    public float maxLookAngle = 0f;
    public Vector3 headParentOffset = Vector3.zero;
}

public struct LookTarget
{
    public Vector3 target;
    public bool isLooking;
    public LookTarget(Vector3 target, bool isLooking)
    {
        this.target = target;
        this.isLooking = isLooking;
    }
}

public class AnimalHead
{
    public AnimalHeadData headData { get; private set; }
    public List<AnimalJoint> headJoints { get; private set; }
    public int neckSegmentId { get; private set; }
    public Vector3 headLocalOffset { get; private set; }

    public Vector3 targetPosition { get; private set; } = Vector3.zero;
    private float lookLerpAngle = 0f;
    private float lerpSpeed = 15f;
    public AnimalJoint parentJoint { get; private set; }
    public AnimalHead(List<AnimalJoint> _joints, AnimalJoint _parentJoint, AnimalHeadData _data)
    {
        headData = _data;
        headJoints = _joints;
        parentJoint = _parentJoint;
        // przekszta³cenie z rotacji globalnej na lokaln¹
        headLocalOffset = Quaternion.Euler(-90f, 0f, 0f) * _data.headParentOffset;
    }

    public void LookAt(LookTarget lookData)
    {
        Vector3 toTarget;
        if (lookData.isLooking)
        {
            toTarget = lookData.target - parentJoint.transform.position;
        }
        else
        {
            toTarget = parentJoint.transform.up * 5;
        }

        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        float currentY = parentJoint.transform.rotation.eulerAngles.y;
        float targetY = Quaternion.LookRotation(toTarget, Vector3.up).eulerAngles.y;
        float deltaY = Mathf.DeltaAngle(currentY, targetY);

        float clampedY = Mathf.Clamp(deltaY, -headData.maxLookAngle, headData.maxLookAngle);

        lookLerpAngle = Mathf.Lerp(lookLerpAngle, clampedY, Time.deltaTime * lerpSpeed);

        Vector3 forward = parentJoint.transform.up;
        Vector3 rotatedForward = Quaternion.AngleAxis(lookLerpAngle, Vector3.up) * forward;
        targetPosition = parentJoint.transform.position + rotatedForward * 5;
    }

    public LerpedLookData GetLerpedLook(int segmentId)
    {
        return new LerpedLookData(headJoints[segmentId].segmentPosition, headJoints.Last().segmentLerpRotation * -Vector3.up);
    }

}


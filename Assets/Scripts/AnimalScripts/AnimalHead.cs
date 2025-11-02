using System.Collections;
using System.Collections.Generic;
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

    public void LookAt(Vector3 targetPos, bool doLook)
    {
        Vector3 toTarget;
        if (doLook)
        {
            toTarget = targetPos - parentJoint.transform.position;
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
}


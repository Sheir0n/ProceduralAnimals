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
}

public class AnimalHead
{
    public List<AnimalJoint> headJoints { get; private set; }
    public int neckSegmentId;
    private float maxLookAngle;

    public Vector3 targetPosition { get; private set; } = Vector3.zero;
    private float lookLerpAngle = 0f;
    private float lerpSpeed = 20f;
    public AnimalJoint parentJoint { get; private set; }
    public AnimalHead(List<AnimalJoint> _joints, AnimalJoint _parentJoint, AnimalHeadData _data)
    {
        headJoints = _joints;
        parentJoint = _parentJoint;
        maxLookAngle = _data.maxLookAngle;
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

        float clampedY = Mathf.Clamp(deltaY, -maxLookAngle, maxLookAngle);
        //Debug.Log($"currentY: {currentY:F1}, targetY: {targetY:F1}, deltaY: {deltaY:F1}");

        lookLerpAngle = Mathf.Lerp(lookLerpAngle, clampedY, Time.deltaTime * lerpSpeed);

        Vector3 forward = parentJoint.transform.up;
        Vector3 rotatedForward = Quaternion.AngleAxis(lookLerpAngle, Vector3.up) * forward;
        targetPosition = parentJoint.transform.position + rotatedForward * 5;
    }
}


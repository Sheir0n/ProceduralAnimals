using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEditor;
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
    public Vector3 targetPosition { get; private set; } = Vector3.zero;
    public Vector3 targetLerpPosition { get; private set; } = Vector3.zero;
    public Vector3 parentLocalOffset { get; private set; } = Vector3.zero;
    public AnimalJoint parentJoint { get; private set; }
    public int limbId { get; private set; }

    private Vector3 lastRootPos = Vector3.zero;
    private Vector3 lastMoveDir = Vector3.zero;
    private float limbTargetingCooldownInMs = 250;
    private float currLimbTargetingTimeMs = 0;
    private float targetLerpSpeed = 50;
    private float absTargetingMaxRotation = 75;

    public AnimalLimb(AnimalLimbData _limbData, List<AnimalJoint> _joints, AnimalJoint _parentJoint, int _limbId)
    {
        limbData = _limbData;
        joints = _joints;
        UpdateLimbTarget(lerp: false);
        limbId = _limbId;
        lastRootPos = _joints[0].segmentPosition;
        parentJoint = _parentJoint;
        parentLocalOffset = Quaternion.Euler(-90f, 0f, 0f) * _limbData.parentPositionOffset;
    }

    public void UpdateLimbTarget(bool lerp)
    {
        AnimalJoint rootJoint = joints[0];
        if (currLimbTargetingTimeMs >= limbTargetingCooldownInMs)
        {
            Vector3 newTargetPosition = GetNewTargetPos();
            currLimbTargetingTimeMs = 0;


            if (!lerp)
                targetLerpPosition = newTargetPosition;
            targetPosition = newTargetPosition;
        }
    }

    public Vector3 GetNewTargetPos()
    {
        AnimalJoint rootJoint = joints[0];
        Vector3 pivot = rootJoint.segmentPosition;
        Vector3 moveDir = pivot - lastRootPos;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 0.001f)
            moveDir.Normalize();
        else
        {
            moveDir = lastMoveDir;
            pivot = lastRootPos;
        }

        float moveAngleY = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
        Quaternion moveRotation = Quaternion.Euler(0f, moveAngleY, 0f);
        Quaternion parentRotation = Quaternion.Euler(0f, parentJoint.segmentRotation.eulerAngles.y, 0f);
        float moveAngleDifference = Mathf.Clamp(GetSignedAngle(parentRotation, moveRotation, Vector3.up), -absTargetingMaxRotation, absTargetingMaxRotation);

        Vector3 newTargetPosition = pivot + (parentRotation * Quaternion.Euler(0, moveAngleDifference, 0) * limbData.targetPosOffset);

        lastRootPos = pivot;
        lastMoveDir = moveDir;
        return newTargetPosition;
    }

    public void CalculateTargetLerp()
    {
        targetLerpPosition = Vector3.Lerp(targetLerpPosition, targetPosition, targetLerpSpeed * Time.deltaTime);
    }

    public void UpdateTargetingVariables(float deltaMs)
    {
        currLimbTargetingTimeMs += deltaMs;
    }

    private float GetSignedAngle(Quaternion from, Quaternion to, Vector3 axis)
    {
        Vector3 fromDir = from * Vector3.forward;
        Vector3 toDir = to * Vector3.forward;
        float angle = Vector3.Angle(fromDir, toDir);
        float sign = Mathf.Sign(Vector3.Dot(Vector3.Cross(fromDir, toDir), axis));
        return angle * sign;
    }
}

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

    private Vector3 lastRootPos = Vector3.zero;
    private Vector3 lastMoveDir = Vector3.zero;
    private float limbTargetingCooldownInMs = 250;
    private float currLimbTargetingTimeMs = 0;

    public AnimalLimb(AnimalLimbData _limbData, List<AnimalJoint> _joints, int limbId)
    {
        limbData = _limbData;
        joints = _joints;
        UpdateLimbTarget(lerp: false);
        this.limbId = limbId;
        lastRootPos = joints[0].transform.position;
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
        Vector3 pivot = rootJoint.transform.position;
        Vector3 moveDir = pivot - lastRootPos;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 0.0001f)
            moveDir.Normalize();
        else
            moveDir = lastMoveDir;

        float moveAngleY = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
        Quaternion moveRotation = Quaternion.Euler(0f, moveAngleY, 0f);

        float rootRotdirection = Mathf.Sign(limbData.parentPositionOffset.x);
        Vector3 newTargetPosition = rootJoint.transform.position + (moveRotation * limbData.targetPosOffset);


        lastRootPos = pivot;
        lastMoveDir = moveDir;
        return newTargetPosition;
    }

    public void CalculateTargetLerp()
    {
        float speed = 20;
        targetLerpPosition = Vector3.Lerp(targetLerpPosition, targetPosition, speed * Time.deltaTime);
    }

    public void UpdateTargetingTime(float deltaMs)
    {
        currLimbTargetingTimeMs += deltaMs;
    }
}

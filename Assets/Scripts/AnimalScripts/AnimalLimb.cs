using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEngine;

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
    private float limbTargetingCooldownInMs = 150;
    private float currLimbTargetingTimeMs = 0;
    private float targetLerpSpeed = 45;
    private float absTargetingMaxRotation = 75;

    private const float targetReachThreshold = 0.05f;
    private const float targetAngleThreshold = 1f;

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
        AnimalJoint tipSegment = joints[^1];
        float distanceToTip = Vector3.Distance(tipSegment.segmentPosition, targetPosition);

        if (distanceToTip < targetReachThreshold)
        {
            CalculateTargetLerp();
            return;
        }

        if (currLimbTargetingTimeMs >= limbTargetingCooldownInMs)
        {
            Vector3 newTargetPosition = GetNewTargetPos();
            float distanceDiff = Vector3.Distance(newTargetPosition, targetPosition);
            float angleDiff = Vector3.Angle((newTargetPosition - joints[0].segmentPosition).normalized,
                                            (targetPosition - joints[0].segmentPosition).normalized);

            if (distanceDiff > targetReachThreshold || angleDiff > targetAngleThreshold)
            {
                targetPosition = newTargetPosition;

                if (!lerp)
                    targetLerpPosition = newTargetPosition;

                currLimbTargetingTimeMs = 0;
            }
        }
        DetectInaccesibleTargetPoint();
        CalculateTargetLerp();
    }

    public Vector3 GetNewTargetPos()
    {
        Vector3 pivot = joints[0].segmentPosition;
        Vector3 moveDir = pivot - lastRootPos;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude < 0.001f)
            moveDir = lastMoveDir;

        float moveAngleY = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
        Quaternion moveRotation = Quaternion.Euler(0f, moveAngleY, 0f);
        Quaternion parentRotation = Quaternion.Euler(0f, parentJoint.yaw, 0f);

        float moveAngleDifference = Mathf.Clamp(
            GetSignedAngle(parentRotation, moveRotation, Vector3.up),
            -absTargetingMaxRotation,
            absTargetingMaxRotation
        );

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


    private void DetectInaccesibleTargetPoint()
    {
        float overlapRadius = 0.05f;
        var hits = Physics.OverlapSphere(targetPosition, overlapRadius, LayerMask.GetMask("Obstacles"));
        if (hits.Length == 0)
            return;

        foreach (var hit in hits)
        {
            if (hit is CapsuleCollider capsule)
            {
                targetPosition = FixTargetPoint(capsule, targetPosition);
            }
        }
    }

    private Vector3 FixTargetPoint(CapsuleCollider capsule, Vector3 pos)
    {
        float origY = pos.y;

        Vector3 rootXZ = new Vector3(parentJoint.segmentPosition.x, 0, parentJoint.segmentPosition.z);
        Vector3 capCenterXZ = new Vector3(capsule.transform.position.x, 0f, capsule.transform.position.z); ;
        Vector3 posXZ = new Vector3(pos.x, 0f, pos.z);
        float capsuleRadiusXZ = capsule.radius * Mathf.Max(capsule.transform.lossyScale.x, capsule.transform.lossyScale.z);

        Vector3 dir = (rootXZ - capCenterXZ);
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.forward;

        dir.Normalize();
        Vector3 targetOnEdge = capCenterXZ + dir * capsuleRadiusXZ;
        targetOnEdge.y = origY;

        return targetOnEdge;
    }

    public void DrawGizmos()
    {
        if (joints == null || joints.Count == 0)
            return;

        Vector3 root = joints[0].segmentPosition;
        Gizmos.color = UnityEngine.Color.yellow;
        Gizmos.DrawLine(root, targetPosition);

        Gizmos.color = UnityEngine.Color.red;
        Gizmos.DrawSphere(targetPosition, 0.05f);

        Gizmos.color = UnityEngine.Color.cyan;
        Gizmos.DrawLine(root, targetLerpPosition);
        Gizmos.DrawSphere(targetLerpPosition, 0.04f);
    }
}

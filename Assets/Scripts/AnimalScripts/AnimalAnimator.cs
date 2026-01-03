using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using static AnimalAI;

public class AnimalAnimator : MonoBehaviour
{
    [Header("Animal Joints")]
    protected List<AnimalJoint> joints;
    protected List<AnimalLimb> limbs;
    protected AnimalHead head;

    [Header("Movement Controller")]
    protected Vector3 prevHeadPosition;

    protected AnimalEventHub eventHub;
    private bool isAnimalDisabled = false;
    protected bool isBodyReady { get; private set; } = false;

    protected Color bodyColor = Color.white;
    private Mesh bodyMesh;
    private MeshFilter bodyMeshFilter;
    private MeshRenderer bodyMeshRenderer;

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        eventHub.OnActionChanged += OnActionChanged;
        eventHub.OnHeadDataRequest += GetLookCenter;
        eventHub.OnDeath += OnDeath;
    }

    protected virtual void Update()
    {
        if (!isBodyReady)
            return;

        if (joints == null || joints.Count == 0)
        {
            Debug.LogWarning("AnimalAnimator: Lista stawów nie jest przypisana lub jest pusta! SprawdŸ ustawienia AnimalCreator!", this);
            return;
        }

        CalculateRootSegmentTransform();
        CalculateMainBodyTransform(joints, minSegmentId: 1, joints.Count);
        CalculateLimbsTransform();
        CalculateHeadTransform();
        UpdateMesh(bodyColor);
    }


    public void SetBody(List<AnimalJoint> spineJoints, List<AnimalLimb> limbs, AnimalHead head, Color bodyColor)
    {
        this.joints = spineJoints;
        this.limbs = limbs;
        this.head = head;
        this.bodyColor = bodyColor;
        isBodyReady = true;
        InitializeMesh();
    }

    protected virtual void CalculateRootSegmentTransform()
    {
        if (joints != null && joints.Count > 0 && joints[0] != null)
        {
            joints[0].SetPosition(transform.position);
            joints[0].SetRotation(transform.rotation.eulerAngles.y);
            joints[0].UpdateSegmentTransform();
        }
        else
            Debug.LogWarning("Animal Animator: segment[0] not found!");
    }

    protected virtual void CalculateMainBodyTransform(List<AnimalJoint> jointList, int minSegmentId, int maxSegmentId)
    {
        if (jointList == null || jointList.Count == 0)
        {
            Debug.LogWarning("Animal Animator: joints list is empty or null!");
            return;
        }

        if (minSegmentId < 1 || maxSegmentId > jointList.Count)
        {
            Debug.LogWarning($"Animal Animator: _minSegmentId ({minSegmentId}) or _maxSegmentId ({maxSegmentId}) out of range. List count: {jointList.Count}");
            return;
        }

        for (int i = minSegmentId; i < maxSegmentId; i++)
        {
            AnimalJoint prevSegment = jointList[i - 1];
            AnimalJoint currSegment = jointList[i];

            SolveJoint(anchor: prevSegment, segment: currSegment, constraintJoint: prevSegment, prevSegment.prefferedAngle);

            float baseRadius = 0.25f;
            float pushFactor = 0.25f;
            float radius = currSegment.segmentScale.magnitude * baseRadius;

            if (SegmentHitsObstacle(currSegment.segmentPosition, radius))
            {
                Vector3 pushed = PushBodyFromObstacle(prevSegment, currSegment.segmentPosition, radius, pushFactor, callEvent: true);
                Vector3 corrected = prevSegment.segmentPosition + (pushed - prevSegment.segmentPosition).normalized * currSegment.distanceConstraint;
                currSegment.SetPosition(corrected);
            }
            currSegment.UpdateSegmentTransform();
        }
    }

    protected virtual void CalculateLimbsTransform()
    {
        int chainPullCount = 3;
        foreach (AnimalLimb currLimb in limbs)
        {
            currLimb.UpdateTargetingVariables(deltaMs: Time.deltaTime * 1000);
            CalculateLimbsTargetPosition(currLimb);
            CalculateFabrikTransforms(jointChain: currLimb.joints, parentJoint: joints[currLimb.limbData.parentJointId], targetPos: currLimb.targetLerpPosition, rootOffset: currLimb.parentLocalOffset, pulls: chainPullCount, doLerp: true);
        }
    }

    protected void CalculateHeadTransform()
    {
        if (head is null)
            return;

        if (!isAnimalDisabled)
        {
            LookTarget lookData = eventHub.RequestPathfindingLookTargetData();
            if (lookData.isLooking)
                head.LookAt(lookData);
            else
            {
                lookData = eventHub.RequestInterestLookTargetData();
                head.LookAt(lookData);
            }
        }
        int chainPullCount = 3;

        CalculateFabrikTransforms(jointChain: head.headJoints, parentJoint: head.parentJoint, targetPos: head.targetPosition, rootOffset: head.headLocalOffset, pulls: chainPullCount, doLerp: false);

        //odepchniêcie g³owy
        for (int i = 1; i < head.headJoints.Count; i++)
        {
            AnimalJoint prevSegment = head.headJoints[i - 1];
            AnimalJoint currSegment = head.headJoints[i];

            float baseRadius = 0.25f;
            float pushFactor = 0.25f;
            float radius = currSegment.segmentScale.magnitude * baseRadius;

            if (SegmentHitsObstacle(currSegment.segmentPosition, radius))
            {
                Vector3 pushed = PushBodyFromObstacle(prevSegment, currSegment.segmentPosition, radius, pushFactor, callEvent: true);
            }
        }
    }

    protected void CalculateFabrikTransforms(List<AnimalJoint> jointChain, AnimalJoint parentJoint, Vector3 targetPos, Vector3 rootOffset, int pulls, bool doLerp)
    {
        if (isAnimalDisabled)
            pulls = 1;

        for (int i = 0; i < pulls; i++)
        {
            if (!isAnimalDisabled)
                ForwardPass(jointChain, targetPos);
            BackwardPass(jointChain, parentJoint, rootOffset);
        }
        FixChainCollision(jointChain);

        if (doLerp)
            LerpUpdateChain(jointChain);
        else
            DirectUpdateChain(jointChain);
    }

    private void ForwardPass(List<AnimalJoint> chain, Vector3 targetPos)
    {
        AnimalJoint tip = chain[^1];
        tip.SetPosition(targetPos);

        float angleY = GetYAngle(targetPos - tip.segmentPosition);
        tip.SetRotation(angleY);

        for (int i = chain.Count - 1; i > 0; i--)
        {
            AnimalJoint next = chain[i];
            AnimalJoint curr = chain[i - 1];

            SolveJoint(anchor: next, segment: curr, constraintJoint: curr, curr.prefferedAngle);
        }
    }

    private void BackwardPass(List<AnimalJoint> chain, AnimalJoint parent, Vector3 rootOffset)
    {
        AnimalJoint root = chain[0];
        Vector3 rootPos = parent.segmentPosition + parent.segmentRotation * rootOffset;

        root.SetPosition(rootPos);
        float angleY = GetYAngle(parent.segmentPosition - root.segmentPosition);
        root.SetRotation(angleY);
        root.UpdateSegmentTransform();

        for (int i = 1; i < chain.Count; i++)
        {
            AnimalJoint prev = chain[i - 1];
            AnimalJoint curr = chain[i];

            SolveJoint(anchor: prev, segment: curr, constraintJoint: prev, -prev.prefferedAngle);
        }
    }

    private void SolveJoint(AnimalJoint anchor, AnimalJoint segment, AnimalJoint constraintJoint, float prefferedAngle)
    {
        Vector3 direction = anchor.segmentPosition - segment.segmentPosition;
        float newLocalY = GetYAngleConstrained(direction, constraintJoint, prefferedAngle);
        segment.SetRotation(newLocalY);

        float rad = newLocalY * Mathf.Deg2Rad;
        Vector3 allowedDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

        segment.SetPosition(anchor.segmentPosition - allowedDir * segment.distanceConstraint);
    }

    private void LerpUpdateChain(List<AnimalJoint> chain)
    {
        const float lerpSpeed = 25f;

        for (int i = 1; i < chain.Count; i++)
        {
            var prev = chain[i - 1];
            var curr = chain[i];

            curr.UpdateLerpRotation(lerpSpeed);
            curr.UpdateLerpPosition(prev.segmentLerpPosition);
            curr.UpdateSegmentLerpTransform();
        }
    }

    private void DirectUpdateChain(List<AnimalJoint> chain)
    {
        for (int i = 1; i < chain.Count; i++)
            chain[i].UpdateSegmentTransform();
    }

    protected float GetYAngleConstrained(Vector3 vecToTarget, AnimalJoint joint, float preferredAngle)
    {
        float targetYAngle = Mathf.Atan2(vecToTarget.x, vecToTarget.z) * Mathf.Rad2Deg;

        float prevY = joint.yaw;
        float deltaY = Mathf.DeltaAngle(prevY, targetYAngle);
        float max = joint.angularConstraint;
        deltaY = Mathf.Clamp(deltaY, -max - preferredAngle, max - preferredAngle);

        return prevY + deltaY;
    }

    protected float GetYAngle(Vector3 toTarget)
    {
        Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
        flatToTarget.Normalize();
        return Mathf.Atan2(flatToTarget.x, flatToTarget.z) * Mathf.Rad2Deg;
    }

    protected virtual void CalculateLimbsTargetPosition(AnimalLimb currLimb)
    {
        AnimalJoint tipSegment = currLimb.joints[^1];
        float distanceToTarget = Vector3.Distance(tipSegment.segmentPosition, currLimb.targetLerpPosition);

        if (distanceToTarget > 0.01f)
        {
            Vector3 newTargetPosition = currLimb.GetNewTargetPos();
            float maxDistance = currLimb.limbData.maxReachDistance;
            float newTargetDistance = Vector3.Distance(newTargetPosition, currLimb.targetPosition);

            if (newTargetDistance > maxDistance)
            {
                currLimb.UpdateLimbTarget(lerp: true);
            }
        }
        currLimb.CalculateTargetLerp();
    }

    protected Quaternion RotateUp(Quaternion rotation)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.x = 90f;
        return Quaternion.Euler(euler);
    }

    protected virtual void OnActionChanged(ActionID newAction) { }

    private void OnDeath()
    {
        Debug.Log("Animal disable recived");
        isAnimalDisabled = true;
    }

    // COLLISION DETECTION FUNCTIONS

    protected bool SegmentHitsObstacle(Vector3 pos, float radius)
    {
        LayerMask mask = LayerMask.GetMask("Obstacles");
        bool hit = Physics.CheckSphere(pos, radius, mask, QueryTriggerInteraction.Ignore);
        return hit;
    }

    protected Vector3 PushBodyFromObstacle(AnimalJoint prev, Vector3 targetPos, float radius, float pushFactor, bool callEvent = false)
    {
        Vector3 from = prev.segmentPosition;
        Collider[] hits = Physics.OverlapSphere(targetPos, radius, LayerMask.GetMask("Obstacles"));

        if (hits.Length == 0)
            return targetPos;

        const float MIN_PUSH = 0.05f;
        Vector3 totalPush = Vector3.zero;

        foreach (var hit in hits)
        {
            Vector3 closest = hit.ClosestPoint(targetPos);
            Vector3 dir = targetPos - closest;

            if (dir.sqrMagnitude < 0.0001f)
                dir = targetPos - hit.bounds.center;
            dir.Normalize();

            float penetration = radius - Vector3.Distance(targetPos, closest);
            if (penetration < 0f)
                continue;

            totalPush += dir * Mathf.Max(MIN_PUSH, penetration);
        }

        if (totalPush.sqrMagnitude < 0.0001f)
            return targetPos;

        Vector3 along = (targetPos - from).normalized;
        Vector3 perpPush = Vector3.ProjectOnPlane(totalPush, along) * pushFactor;
        Vector3 finalPos = from + along * prev.distanceConstraint + perpPush;

        if (callEvent)
            eventHub.PushAgentOnSegmentCollision(totalPush);

        return finalPos;
    }

    private void FixChainCollision(List<AnimalJoint> jointChain)
    {
        if (jointChain.Count == 0)
            return;

        float lerpSpeed = 8f;
        float minPenetration = 0.02f;
        int iterations = 3;
        float t = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime / iterations);

        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = 1; i < jointChain.Count; i++)
            {
                AnimalJoint curr = jointChain[i];
                AnimalJoint prev = jointChain[i - 1];

                Vector3 targetPos = GetPushPosition(curr);
                Vector3 diff = targetPos - curr.segmentPosition;
                if (diff.magnitude > minPenetration)
                {
                    Vector3 move = Vector3.Lerp(Vector3.zero, diff, Mathf.Clamp01(t));
                    curr.SetPosition(curr.segmentPosition + move);
                }

                Vector3 dir = prev.segmentPosition - curr.segmentPosition;
                float targetY = GetYAngleConstrained(dir, prev, -prev.prefferedAngle);
                curr.SetRotation(targetY);
            }
        }
    }

    private Vector3 GetPushPosition(AnimalJoint joint)
    {
        if (joint == null) return Vector3.zero;
        Vector3 pos = joint.segmentPosition;
        float radius = joint.segmentScale.x * 0.25f;
        float origY = pos.y;

        var hits = Physics.OverlapSphere(pos, radius, LayerMask.GetMask("Obstacles"));
        if (hits.Length == 0)
            return pos;

        foreach (var hit in hits)
        {
            if (hit is CapsuleCollider capsule)
            {
                pos = PushOutsideCapsule(capsule, pos, radius);
            }
        }
        pos.y = origY;
        return pos;
    }

    private Vector3 PushOutsideCapsule(CapsuleCollider capsule, Vector3 pos, float segmentRadius)
    {
        float origY = pos.y;

        Vector3 centerXZ = new Vector3(capsule.transform.position.x, 0f, capsule.transform.position.z);
        Vector3 posXZ = new Vector3(pos.x, 0f, pos.z);

        float radiusXZ = capsule.radius * Mathf.Max(capsule.transform.lossyScale.x, capsule.transform.lossyScale.z);

        Vector3 dir = posXZ - centerXZ;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.forward;

        dir.Normalize();
        Vector3 newPos = centerXZ + dir * (radiusXZ + segmentRadius);
        newPos.y = origY;

        return newPos;
    }

    private HeadCenterData GetLookCenter()
    {
        if (head == null)
            return new HeadCenterData(transform.position, transform.forward);
        return head.GetLerpedLook();
    }

    //RYSOWANIE MESH
    private void InitializeMesh()
    {
        GameObject go = new GameObject("SpineMesh");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        bodyMeshFilter = go.AddComponent<MeshFilter>();
        bodyMeshRenderer = go.AddComponent<MeshRenderer>();

        bodyMesh = new Mesh();
        bodyMesh.MarkDynamic();
        bodyMeshFilter.mesh = bodyMesh;

        bodyMeshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bodyMeshRenderer.material.color = bodyColor;
    }

    protected void UpdateMesh(Color color)
    {
        if (bodyMesh == null)
            return;
        bodyMesh.Clear();

        CreateRegularMeshFromChain(joints, color);

        if (head != null)
            CreateRegularMeshFromChain(head.headJoints, color);

        foreach (AnimalLimb limb in limbs)
            CreateRegularMeshFromChain(limb.joints, color);

        bodyMesh.RecalculateNormals();
        bodyMesh.RecalculateBounds();
    }

    private void CreateRegularMeshFromChain(List<AnimalJoint> chainJoints, Color color)
    {
        if (chainJoints == null || joints.Count == 0)
            return;

        //float rightAngle = 0f;
        //float leftAngle = Mathf.PI;

        List<Vector3> pointPositions = new List<Vector3>();
        //for (int i = 0; i < chainJoints.Count; i++)
        //{
        //    pointPositions.Add(GetPoint(chainJoints[i], rightAngle));
        //    pointPositions.Add(GetPoint(chainJoints[i], leftAngle));
        //}

        for (int i = 0; i < chainJoints.Count-1; i++)
        {
            Vector3 direction = (chainJoints[i].segmentPosition - chainJoints[i+1].segmentPosition).normalized;

            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 left = -right;
            AnimalJoint firstJoint = chainJoints[i];
            AnimalJoint secondJoint = chainJoints[i+1];
            float radiusFirst = firstJoint.segmentScale.x * 0.5f;
            float radiusSecond = secondJoint.segmentScale.x * 0.5f;

            Vector3 rightFirst = firstJoint.transform.position + right * radiusFirst;
            Vector3 leftFirst = firstJoint.transform.position + left * radiusFirst;
            Vector3 rightSecond = secondJoint.transform.position + right * radiusSecond;
            Vector3 leftSecond = secondJoint.transform.position + left * radiusSecond;

            pointPositions.Add(rightFirst);
            pointPositions.Add(leftFirst);
            pointPositions.Add(rightSecond);
            pointPositions.Add(leftSecond);
        }

        if (pointPositions.Count < 4)
            return;

        Vector3 localOffset = bodyMeshFilter.transform.position;
        Quaternion invRot = Quaternion.Inverse(bodyMeshFilter.transform.rotation);
        for (int j = 0; j < pointPositions.Count; j++)
        {
            pointPositions[j] = invRot * (pointPositions[j] - localOffset);
        }
        CreateRibbonMesh(pointPositions, color);
    }

    private void CreateLimbMeshFromChain(List<AnimalJoint> chainJoints, Color color)
    {
        //if (chainJoints == null || joints.Count == 0)
        //    return;

        //List<Vector3> pointPositions = new List<Vector3>();

        //Vector3 localOffset = bodyMeshFilter.transform.position;
        //Quaternion invRot = Quaternion.Inverse(bodyMeshFilter.transform.rotation);
        //for (int i = 0; i < chainJoints.Count-1; i++)
        //{
        //    List<Vector3> newPoints = GetSegmentEdgePoints(chainJoints[i], chainJoints[i + 1]);
        //    for (int j = 0; j < newPoints.Count; j++)
        //    {
        //        newPoints[j] = invRot * (newPoints[j] - localOffset);
        //    }
        //    CreateRibbonMesh(newPoints, color);
        //}
    }

    private List<Vector3> GetSegmentEdgePoints(AnimalJoint firstJoint, AnimalJoint secondJoint)
    {
        List<Vector3> points = new List<Vector3>();

        Vector3 direction = (firstJoint.segmentPosition - secondJoint.segmentPosition).normalized;
        Vector3 up = Vector3.up;

        // prostopad³y wektor do kierunku segmentu
        Vector3 right = Vector3.Cross(up, direction).normalized;
        Vector3 left = -right;

        float radiusFirst = firstJoint.segmentScale.x * 0.5f;
        float radiusSecond = secondJoint.segmentScale.x * 0.5f;

        // punkty pierwszego segmentu
        Vector3 frontFirst = firstJoint.segmentPosition + direction * radiusFirst;
        Vector3 rightFirst = firstJoint.segmentPosition + right * radiusFirst;
        Vector3 leftFirst = firstJoint.segmentPosition + left * radiusFirst;

        // punkty drugiego segmentu
        Vector3 backSecond = secondJoint.segmentPosition - direction * radiusSecond;
        Vector3 rightSecond = secondJoint.segmentPosition + right * radiusSecond;
        Vector3 leftSecond = secondJoint.segmentPosition + left * radiusSecond;

        // dodajemy w kolejnoœci wskazówek zegara;
        points.Add(rightFirst);
        points.Add(rightSecond);
        points.Add(leftSecond);
        points.Add(leftFirst);

        return points;
    }


    private void CreateRibbonMesh(List<Vector3> pointPositions, Color color)
    {
        if (pointPositions == null || pointPositions.Count < 2)
            return;

        List<Vector3> verts = pointPositions;
        List<int> tris = new List<int>();

        for (int i = 0; i < pointPositions.Count-2; i+=4)
        {
            tris.Add(i);
            tris.Add(i + 2);
            tris.Add(i + 1);

            tris.Add(i + 2);
            tris.Add(i + 3);
            tris.Add(i + 1);
        }

        Vector3[] oldVerts = bodyMesh.vertices;
        int[] oldTris = bodyMesh.triangles;

        int vertOffset = oldVerts.Length;

        bodyMesh.vertices = oldVerts.Concat(verts).ToArray();
        bodyMesh.triangles = oldTris.Concat(tris.Select(t => t + vertOffset)).ToArray();
    }

    private Vector3 GetPoint(AnimalJoint joint, float angleRad)
    {
        float radius = joint.segmentScale.x * 0.5f;
        Vector3 localPoint = new Vector3(Mathf.Cos(angleRad) * radius, 0f, Mathf.Sin(angleRad) * radius);
        Quaternion rotation = Quaternion.Euler(0f, joint.yaw, 0f);
        Vector3 rotatedPoint = rotation * localPoint;
        Vector3 worldPoint = joint.transform.position + rotatedPoint;
        return worldPoint;
    }

    public void OnDrawGizmos()
    {
        if (limbs == null)
            return;

        foreach (var limb in limbs)
            limb?.DrawGizmos();
    }
}

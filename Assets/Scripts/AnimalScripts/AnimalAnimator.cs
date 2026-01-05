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

    //dane mesh
    protected Color bodyColor = Color.white;
    protected float colorFadeAmount = 0f;
    private Mesh bodyMesh;
    private MeshFilter bodyMeshFilter;
    private MeshRenderer bodyMeshRenderer;

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        eventHub.OnActionChanged += OnActionChanged;
        eventHub.OnHeadDataRequest += GetLookCenter;
        eventHub.OnDeath += OnDeath;
        eventHub.OnDeathFade += ApplyColorFade;
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
        CreateMeshObject(ref bodyMesh, ref bodyMeshFilter,ref bodyMeshRenderer ,bodyColor, "BodyMesh");
        if(head != null)
            CreateMeshObject(ref head.bodyMesh, ref head.bodyMeshFilter, ref head.bodyMeshRenderer, head.bodyColor, "HeadMesh");
        foreach(AnimalLimb limb in limbs)
            CreateMeshObject(ref limb.bodyMesh, ref limb.bodyMeshFilter, ref limb.bodyMeshRenderer, limb.bodyColor, "LimbMesh");
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
    private void CreateMeshObject(ref Mesh mesh, ref MeshFilter filter, ref MeshRenderer renderer, Color meshColor, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        filter = go.AddComponent<MeshFilter>();
        renderer = go.AddComponent<MeshRenderer>();

        mesh = new Mesh();
        mesh.MarkDynamic();
        filter.mesh = mesh;

        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = meshColor;
    }

    protected void UpdateMesh(Color color)
    {
        if (bodyMesh != null)
        {
            bodyMesh.Clear();

            CreateRegularMeshFromChain(joints, ref bodyMesh, ref bodyMeshFilter);
            bodyMesh.RecalculateNormals();
            bodyMesh.RecalculateBounds();
        }

        if (head != null && head.bodyMesh != null)
        {
            head.bodyMesh.Clear();
            CreateRegularMeshFromChain(head.headJoints, ref head.bodyMesh, ref head.bodyMeshFilter);
            head.bodyMesh.RecalculateNormals();
            head.bodyMesh.RecalculateBounds();
        }

        foreach (AnimalLimb limb in limbs)
        {
            if (limb.bodyMesh != null)
            {
                limb.bodyMesh.Clear();
                CreateRegularMeshFromChain(limb.joints, ref limb.bodyMesh, ref limb.bodyMeshFilter);
                limb.bodyMesh.RecalculateNormals();
                limb.bodyMesh.RecalculateBounds();
            }
        }
    }

    private void CreateRegularMeshFromChain(List<AnimalJoint> chainJoints, ref Mesh mesh, ref MeshFilter filter)
    {
        if (chainJoints == null || joints.Count == 0)
            return;
        List<Vector3> pointPositions = new List<Vector3>();

        for (int i = 0; i < chainJoints.Count - 1; i++)
        {
            Vector3 direction = (chainJoints[i].segmentPosition - chainJoints[i + 1].segmentPosition).normalized;

            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 left = -right;
            AnimalJoint firstJoint = chainJoints[i];
            AnimalJoint secondJoint = chainJoints[i + 1];
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

        Transform root = transform;

        for (int j = 0; j < pointPositions.Count; j++)
        {
            pointPositions[j] = root.InverseTransformPoint(pointPositions[j]);
        }
        CreateRibbonMesh(pointPositions, ref mesh);
    }
    private void CreateRibbonMesh(List<Vector3> pointPositions, ref Mesh mesh)
    {
        if (pointPositions == null || pointPositions.Count < 2)
            return;

        List<Vector3> verts = pointPositions;
        List<int> tris = new List<int>();

        for (int i = 0; i < pointPositions.Count - 2; i += 4)
        {
            tris.Add(i);
            tris.Add(i + 2);
            tris.Add(i + 1);

            tris.Add(i + 2);
            tris.Add(i + 3);
            tris.Add(i + 1);
        }

        Vector3[] oldVerts = mesh.vertices;
        int[] oldTris = mesh.triangles;

        int vertOffset = oldVerts.Length;

        mesh.vertices = oldVerts.Concat(verts).ToArray();
        mesh.triangles = oldTris.Concat(tris.Select(t => t + vertOffset)).ToArray();
    }

    public void OnDrawGizmos()
    {
        if (limbs == null)
            return;

        foreach (var limb in limbs)
            limb?.DrawGizmos();
    }

    private void ApplyColorFade(float amount)
    {
        foreach (AnimalJoint joint in joints)
        {
            joint.SetColorFade(amount);
            amount = Mathf.Clamp01(amount);
            Color resultColor = Color.Lerp(bodyColor, Color.gray, amount);
            bodyMeshRenderer.material.color = resultColor;
        }

        foreach (AnimalLimb limb in limbs)
            limb.SetColorFade(amount);

        if (head != null)
            head.SetColorFade(amount);
    }

    private void OnDestroy()
    {
        if(joints != null)
            joints.Clear();
        if (limbs != null)
            limbs.Clear();
        head = null;
    }
}

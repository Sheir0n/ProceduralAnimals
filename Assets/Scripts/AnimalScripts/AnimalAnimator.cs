using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnimalAnimator : MonoBehaviour
{
    protected List<AnimalJoint> joints;
    protected List<AnimalLimb> limbs;


    [SerializeField] protected PathfindController movementController;

    protected Vector3 prevHeadPosition;


    public void SetJoints(List<AnimalJoint> _segments)
    {
        joints = _segments;
    }

    public void SetLimbs(List<AnimalLimb> _limbs)
    {
        limbs = _limbs;
    }

    protected virtual void CalculateHeadSegmentTransform()
    {
        if (joints != null && joints.Count > 0 && joints[0] != null)
        {
            joints[0].SetPosition(transform.position);
            joints[0].SetRotation(RotateUp(transform.rotation));
            joints[0].UpdateSegmentTransform();
        }
        else
            Debug.LogWarning("Animal Animator: segment[0] not found!");
    }

    protected virtual void CalculateMainBodyTransform(int _minSegmentId, int _maxSegmentId)
    {
        if (joints == null || joints.Count == 0)
        {
            Debug.LogWarning("Animal Animator: joints list is empty or null!");
            return;
        }

        if (_minSegmentId < 1 || _maxSegmentId > joints.Count)
        {
            Debug.LogWarning($"Animal Animator: _minSegmentId ({_minSegmentId}) or _maxSegmentId ({_maxSegmentId}) out of range. List count: {joints.Count}");
            return;
        }

        for (int i = _minSegmentId; i < _maxSegmentId; i++)
        {
            AnimalJoint prevSegment = joints[i - 1];
            AnimalJoint currSegment = joints[i];

            Vector3 toPrev = prevSegment.segmentPosition - currSegment.segmentPosition;
            Vector3 flatToPrev = new Vector3(toPrev.x, 0f, toPrev.z);

            if (flatToPrev.sqrMagnitude < 0.0001f)
                continue;

            flatToPrev.Normalize();

            // Docelowy k¹t wzglêdem œwiata
            float targetYAngle = Mathf.Atan2(flatToPrev.x, flatToPrev.z) * Mathf.Rad2Deg;

            // Obecny lokalny k¹t Y poprzedniego segmentu
            float prevLocalY = prevSegment.segmentRotation.eulerAngles.y;

            // Chcemy ograniczyæ ró¿nicê k¹ta miêdzy prev a curr wzglêdem lokalnego uk³adu
            float deltaY = Mathf.DeltaAngle(prevLocalY, targetYAngle);

            // Zastosuj constraint
            float maxAngle = prevSegment.angularConstraint;
            float clampedY = Mathf.Clamp(deltaY, -maxAngle, maxAngle);

            // Nowy lokalny k¹t bie¿¹cego segmentu = k¹t poprzedniego + ograniczony offset
            float newLocalY = prevLocalY + clampedY;

            // Bazowy X=90 (le¿y p³asko), Y=nowy k¹t, Z=0
            currSegment.SetRotation(Quaternion.Euler(90f, newLocalY, 0f));

            // Ustaw pozycjê wzd³u¿ kierunku wynikaj¹cego z nowej rotacji
            Vector3 allowedDir = Quaternion.Euler(0f, newLocalY, 0f) * Vector3.forward;
            currSegment.SetPosition(prevSegment.segmentPosition - allowedDir * currSegment.distanceConstraint);

            currSegment.UpdateSegmentTransform();
        }
    }

    protected virtual void CalculateLimbsTransform()
    {
        foreach (AnimalLimb currLimb in limbs)
        {
            int chainPullCount = 5;
            for (int pullId = 0; pullId < chainPullCount; pullId++)
            {
                //obliczenie pozycji ostatniego stawu
                AnimalJoint currJoint = currLimb.joints.Last();
                AnimalLimbData currLimbData = currLimb.limbData;
                Vector3 targetPos = currLimb.targetPosition;

                currJoint.SetPosition(targetPos);

                Vector3 toPrev = targetPos - currJoint.segmentPosition;
                Vector3 flatToPrev = new Vector3(toPrev.x, 0f, toPrev.z);
                flatToPrev.Normalize();
                float angleY = Mathf.Atan2(flatToPrev.x, flatToPrev.z) * Mathf.Rad2Deg;
                currJoint.SetRotation(Quaternion.Euler(90f, angleY, 0f));
                currJoint.UpdateSegmentTransform();


                for (int i = currLimb.joints.Count - 1; i > 0; i--)
                {
                    AnimalJoint nextSegment = currLimb.joints[i];
                    AnimalJoint currSegment = currLimb.joints[i - 1]; // ustawiamy wzglêdem nastêpnego

                    Vector3 toNext = nextSegment.segmentPosition - currSegment.segmentPosition;
                    Vector3 flatToNext = new Vector3(toNext.x, 0f, toNext.z);

                    if (flatToNext.sqrMagnitude < 0.0001f)
                        continue;

                    flatToNext.Normalize();

                    float targetYAngle = Mathf.Atan2(flatToNext.x, flatToNext.z) * Mathf.Rad2Deg;
                    float currLocalY = currSegment.segmentRotation.eulerAngles.y;
                    float deltaY = Mathf.DeltaAngle(currLocalY, targetYAngle);

                    float maxAngle = currSegment.angularConstraint;
                    float clampedY = Mathf.Clamp(deltaY, -maxAngle, maxAngle);
                    float newLocalY = currLocalY + clampedY;

                    currSegment.SetRotation(Quaternion.Euler(90f, newLocalY, 0f));

                    Vector3 allowedDir = Quaternion.Euler(0f, newLocalY, 0f) * Vector3.forward;
                    currSegment.SetPosition(nextSegment.segmentPosition - allowedDir * currSegment.distanceConstraint);

                    currSegment.UpdateSegmentTransform();
                }

                // obliczenie pozycji 1 stawu
                currJoint = currLimb.joints[0];
                currLimbData = currLimb.limbData;
                AnimalJoint parentJoint = joints[currLimbData.parentJointId];

                Vector3 rootPosition = parentJoint.segmentPosition + parentJoint.segmentRotation * currLimbData.parentPositionOffset;

                currJoint.SetPosition(rootPosition);

                toPrev = parentJoint.segmentPosition - currJoint.segmentPosition;
                flatToPrev = new Vector3(toPrev.x, 0f, toPrev.z);
                flatToPrev.Normalize();
                angleY = Mathf.Atan2(flatToPrev.x, flatToPrev.z) * Mathf.Rad2Deg;
                currJoint.SetRotation(Quaternion.Euler(90f, angleY, 0f));
                currJoint.UpdateSegmentTransform();

                for (int i = 1; i < currLimb.joints.Count; i++)
                {
                    AnimalJoint prevSegment = currLimb.joints[i - 1];
                    AnimalJoint currSegment = currLimb.joints[i];

                    toPrev = prevSegment.segmentPosition - currSegment.segmentPosition;
                    flatToPrev = new Vector3(toPrev.x, 0f, toPrev.z);

                    if (flatToPrev.sqrMagnitude < 0.0001f)
                        continue;

                    flatToPrev.Normalize();

                    // Docelowy k¹t wzglêdem œwiata
                    float targetYAngle = Mathf.Atan2(flatToPrev.x, flatToPrev.z) * Mathf.Rad2Deg;

                    // Obecny lokalny k¹t Y poprzedniego segmentu
                    float prevLocalY = prevSegment.segmentRotation.eulerAngles.y;

                    // Chcemy ograniczyæ ró¿nicê k¹ta miêdzy prev a curr wzglêdem lokalnego uk³adu
                    float deltaY = Mathf.DeltaAngle(prevLocalY, targetYAngle);

                    // Zastosuj constraint
                    float maxAngle = prevSegment.angularConstraint;
                    float clampedY = Mathf.Clamp(deltaY, -maxAngle, maxAngle);

                    // Nowy lokalny k¹t bie¿¹cego segmentu = k¹t poprzedniego + ograniczony offset
                    float newLocalY = prevLocalY + clampedY;

                    // Bazowy X=90 (le¿y p³asko), Y=nowy k¹t, Z=0
                    currSegment.SetRotation(Quaternion.Euler(90f, newLocalY, 0f));

                    // Ustaw pozycjê wzd³u¿ kierunku wynikaj¹cego z nowej rotacji
                    Vector3 allowedDir = Quaternion.Euler(0f, newLocalY, 0f) * Vector3.forward;
                    currSegment.SetPosition(prevSegment.segmentPosition - allowedDir * currSegment.distanceConstraint);

                    currSegment.UpdateSegmentTransform();
                }
            }
        }
    }


    protected Quaternion RotateUp(Quaternion rotation)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.x = 90f;
        return Quaternion.Euler(euler);
    }
}

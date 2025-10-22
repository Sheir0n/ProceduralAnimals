using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

public class SnakeAnimator : MonoBehaviour
{
    private List<SnakeJoint> segments;
    [SerializeField] PathfindController movementController;

    private Vector3 prevHeadPosition;

    public void SetSegments(List<SnakeJoint> _segments)
    {
        segments = _segments;
    }

    private void Update()
    {
        CalculateHeadSegmentTransform();
        CalculateBodySegmentTransform();

        //foreach (var segment in segments)
        //    UpdateSegmentTransform(segment);
    }

    private void CalculateHeadSegmentTransform()
    {
        segments[0].SetPosition(transform.position);
        segments[0].SetRotation(RotateUp(transform.rotation));
        UpdateSegmentTransform(segments[0]);
    }

    //private void CalculateBodySegmentTransform()
    //{
    //    for (int i = 1; i < segments.Count; i++)
    //    {
    //        SnakeSegment prevSegment = segments[i - 1];
    //        SnakeSegment currSegment = segments[i];

    //        Vector3 direction = prevSegment.segmentPosition - currSegment.segmentPosition;

    //        if (direction.sqrMagnitude > 0.0001f)
    //        {
    //            currSegment.SetRotation(Quaternion.LookRotation(direction) * Quaternion.Euler(90, 0, 0)); 

    //            currSegment.SetPosition(prevSegment.segmentPosition - direction.normalized * currSegment.distanceConstraint);
    //        }
    //        UpdateSegmentTransform(currSegment);
    //    }
    //}

    private void CalculateBodySegmentTransform()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            SnakeJoint prev = segments[i - 1];
            SnakeJoint curr = segments[i];

            Vector3 toPrev = prev.segmentPosition - curr.segmentPosition;
            Vector3 flatToPrev = new Vector3(toPrev.x, 0f, toPrev.z);

            if (flatToPrev.sqrMagnitude < 0.0001f)
                continue;

            flatToPrev.Normalize();

            // Docelowy k¹t wzglêdem œwiata
            float targetYAngle = Mathf.Atan2(flatToPrev.x, flatToPrev.z) * Mathf.Rad2Deg;

            // Obecny lokalny k¹t Y poprzedniego segmentu
            float prevLocalY = prev.segmentRotation.eulerAngles.y;

            // Chcemy ograniczyæ ró¿nicê k¹ta miêdzy prev a curr wzglêdem lokalnego uk³adu
            float deltaY = Mathf.DeltaAngle(prevLocalY, targetYAngle);

            // Zastosuj constraint
            float maxAngle = curr.angularConstraint;
            float clampedY = Mathf.Clamp(deltaY, -maxAngle, maxAngle);

            // Nowy lokalny k¹t bie¿¹cego segmentu = k¹t poprzedniego + ograniczony offset
            float newLocalY = prevLocalY + clampedY;

            // Bazowy X=90 (le¿y p³asko), Y=nowy k¹t, Z=0
            curr.SetRotation(Quaternion.Euler(90f, newLocalY, 0f));

            // Ustaw pozycjê wzd³u¿ kierunku wynikaj¹cego z nowej rotacji
            Vector3 allowedDir = Quaternion.Euler(0f, newLocalY, 0f) * Vector3.forward;
            curr.SetPosition(prev.segmentPosition - allowedDir * curr.distanceConstraint);

            UpdateSegmentTransform(curr);
        }
    }


    private Quaternion RotateUp(Quaternion rotation)
    {
        Vector3 euler = rotation.eulerAngles;
        euler.x = 90f;
        return Quaternion.Euler(euler);
    }


    void UpdateSegmentTransform(SnakeJoint _segment)
    {
        _segment.transform.rotation = _segment.segmentRotation;
        _segment.transform.position = _segment.segmentPosition;
        _segment.transform.localScale = _segment.segmentScale;
    }
}

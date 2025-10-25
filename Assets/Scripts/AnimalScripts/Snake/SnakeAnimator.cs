using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

public class SnakeAnimator : AnimalAnimator
{
    [Header("Snake Wriggle Settings")]
    public float wriggleAmplitude = 15f;      // stopnie
    public float wriggleFrequency = 5f;       // cykle/s
    public float wrigglePhaseOffset = 0.25f;  // przesuniêcie fazy miêdzy segmentami

    [Header("Movement Detection")]
    public float movementThreshold = 0.001f;  // minimalna prêdkoœæ ruchu g³owy
    private Vector3 lastHeadPosition;
    private bool isMoving;


    public void SetJoints(List<SnakeJoint> _segments)
    {
        joints = new List<AnimalJoint>(_segments);
    }

    private void Update()
    {
        if (joints == null || joints.Count == 0)
        {
            Debug.LogWarning("Animal Animator: joints list is empty or null!");
            return;
        }

        CalculateHeadSegmentTransform();
        CalculateMainBodyTransform(1, joints.Count);
    }

    protected override void CalculateMainBodyTransform(int _minSegmentId, int _maxSegmentId)
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

        // --- Detekcja ruchu g³owy ---
        Vector3 headPos = joints[0].segmentPosition;
        Vector3 headDelta = headPos - lastHeadPosition;
        float headSpeed = headDelta.magnitude / Time.deltaTime;
        isMoving = headSpeed > movementThreshold;
        lastHeadPosition = headPos;

        float speedFactor = Mathf.Clamp(headSpeed, 0.5f, 3f);

        // odwrócenie kierunku fali: liczymy fazê od koñca wê¿a
        int totalSegments = _maxSegmentId - _minSegmentId;

        for (int i = _minSegmentId; i < _maxSegmentId; i++)
        {
            AnimalJoint prevSegment = joints[i - 1];
            AnimalJoint currSegment = joints[i];

            Vector3 toPrev = prevSegment.segmentPosition - currSegment.segmentPosition;
            Vector3 flatToPrev = new Vector3(toPrev.x, 0f, toPrev.z);

            if (flatToPrev.sqrMagnitude < 0.0001f)
                continue;

            flatToPrev.Normalize();

            float targetYAngle = Mathf.Atan2(flatToPrev.x, flatToPrev.z) * Mathf.Rad2Deg;
            float prevLocalY = prevSegment.segmentRotation.eulerAngles.y;
            float deltaY = Mathf.DeltaAngle(prevLocalY, targetYAngle);

            float maxAngle = prevSegment.angularConstraint;
            float clampedDeltaY = Mathf.Clamp(deltaY, -maxAngle, maxAngle);

            float newLocalY = prevLocalY + clampedDeltaY;

            if (isMoving)
            {
                // odwrócona faza segmentu: fala idzie od ogona do g³owy
                int reversedIndex = totalSegments - 1 - (i - _minSegmentId);
                float phase = reversedIndex * wrigglePhaseOffset * speedFactor;

                // gradient amplitudy wzd³u¿ cia³a
                float segmentFactor = 1f - ((float)(i - _minSegmentId) / totalSegments);

                float wriggleOffset = Mathf.Sin(Time.time * wriggleFrequency * speedFactor + phase) * wriggleAmplitude * segmentFactor;

                newLocalY += wriggleOffset;
            }

            currSegment.SetRotation(Quaternion.Euler(90f, newLocalY, 0f));

            Vector3 allowedDir = Quaternion.Euler(0f, newLocalY, 0f) * Vector3.forward;
            currSegment.SetPosition(prevSegment.segmentPosition - allowedDir * currSegment.distanceConstraint);

            currSegment.UpdateSegmentTransform();
        }
    }

}

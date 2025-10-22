using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.U2D;
using static UnityEngine.Rendering.DebugUI.Table;

public class SnakeCreator : MonoBehaviour, IGenerateAnimal
{
    [SerializeField] private GameObject bodySegmentPrefab;
    [SerializeField] private int segmentCount = 8;
    [SerializeField] private AnimationCurve sizeCurve;
    [SerializeField] private float sizeMultiplier = 1f;


    [SerializeField] SnakeAnimator animatorScript;

    private List<SnakeJoint> joints = new List<SnakeJoint>();
    

    void Start()
    {
        GenerateBody();
    }

    //public void GenerateBody()
    //{
    //    Transform masterTransform = transform;

    //    Vector3 rot = masterTransform.rotation.eulerAngles;
    //    rot.x = 90f;
    //    Quaternion headRotation = Quaternion.Euler(rot);
    //    float distanceConstraint = 0.75f;
    //    Vector3 startPosOffset = Vector3.zero;

    //    for (int i = 0; i < segmentCount; i++)
    //    {
    //        float xValue = (float)i / (float)segmentCount;
    //        float segmentScale = sizeCurve.Evaluate(xValue);

    //        GameObject newSegment = Instantiate(bodySegmentPrefab, masterTransform);
    //        newSegment.transform.localScale = Vector3.one * segmentScale;
    //        newSegment.transform.rotation = masterTransform.rotation;
    //        newSegment.transform.localPosition += startPosOffset;
    //        startPosOffset += new Vector3(0, -1f* segmentScale, 0);
    //        newSegment.name = "Segment " + i;

    //        SnakeSegment segmentScript = newSegment.GetComponent<SnakeSegment>();
    //        segmentScript.AfterInstantiate(distanceConstraint, i);
    //        segments.Add(newSegment.GetComponent<SnakeSegment>());
    //    }
    //    animatorScript.SetSegments(segments);
    //}

    public void GenerateBody()
    {
        Transform masterTransform = transform;
        Vector3 postionOffset = Vector3.zero;
        float distanceConstraint = 0.75f;
        float angularConstraint = 40f;

        for (int i = 0; i < segmentCount; i++)
        {
            float xValue = (float)i / (float)segmentCount;
            float segmentScale = sizeCurve.Evaluate(xValue);

            GameObject newSegment = Instantiate(bodySegmentPrefab, masterTransform);
            newSegment.transform.localScale = Vector3.one*segmentScale;
            newSegment.transform.position += postionOffset;
            postionOffset += new Vector3(0, 0, -1f*segmentScale);

            newSegment.name = "Segment " + i;
            SnakeJoint segmentScript = newSegment.GetComponent<SnakeJoint>();
            segmentScript.AfterInstantiate(distanceConstraint * segmentScale, angularConstraint, i);
            joints.Add(newSegment.GetComponent<SnakeJoint>());
        }
        animatorScript.SetSegments(joints);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct SegmentData
{
    public string segmentName;

    public GameObject bodySegmentPrefab;
    public int jointCount;
    public AnimationCurve sizeCurve;
    public float distanceConstraint;
    public float angularConstraint;
}


public class AnimalCreator : MonoBehaviour
{
    [SerializeField] AnimalAnimator animatorScript;

    [SerializeField] protected List<SegmentData> segments = new List<SegmentData>();
    private List<AnimalJoint> joints = new List<AnimalJoint>();
    public void GenerateBody()
    {
        Transform masterTransform = transform;
        Vector3 postionOffset = Vector3.zero;

        foreach (SegmentData currSegment in segments) {
            for (int i = 0; i < currSegment.jointCount; i++)
            {
                float xValue = (float)i / (float)currSegment.jointCount;
                float segmentScale = currSegment.sizeCurve.Evaluate(xValue);

                GameObject newSegment = Instantiate(currSegment.bodySegmentPrefab, masterTransform);
                newSegment.transform.localScale = Vector3.one * segmentScale;
                newSegment.transform.position += postionOffset;
                postionOffset += new Vector3(0, 0, -1f * segmentScale);

                newSegment.name = "Segment " + i;
                SnakeJoint segmentScript = newSegment.GetComponent<SnakeJoint>();
                segmentScript.AfterInstantiate(currSegment.distanceConstraint * segmentScale, currSegment.angularConstraint, i);
                joints.Add(newSegment.GetComponent<SnakeJoint>());
            }
            animatorScript.SetJoints(joints);
        }
    }
}

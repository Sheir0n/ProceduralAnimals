using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SegmentData
{
    public string segmentName;

    public GameObject bodySegmentPrefab;
    public int jointCount;
    public AnimationCurve sizeCurve;
    public float distanceConstraint;
    [Range(0, 180)] public float angularConstraint;
    [Range(-180, 180)] public float prefferedAngle;
}

public class AnimalCreator : MonoBehaviour
{
    [SerializeField] AnimalAnimator animatorScript;

    [SerializeField] protected List<SegmentData> spineSegmentData = new List<SegmentData>();
    [SerializeField] protected List<AnimalLimbData> animalLimbData = new List<AnimalLimbData>();
    [SerializeField] protected AnimalHeadData animalHeadData;

    private List<AnimalJoint> spineJoints = new List<AnimalJoint>();
    private List<AnimalLimb> limbs = new List<AnimalLimb>();
    private AnimalHead animalHead;
    public void GenerateBody()
    {
        Transform masterTransform = transform;
        Vector3 positionOffset = Vector3.zero;
        int nameId = 0;

        foreach (SegmentData currSegmentData in spineSegmentData)
        {
            for (int i = 0; i < currSegmentData.jointCount; i++)
            {
                float xValue = (float)i / (float)currSegmentData.jointCount;
                float segmentScale = currSegmentData.sizeCurve.Evaluate(xValue);
                string name = currSegmentData.segmentName + " Spine Segment " + nameId++;

                spineJoints.Add(GenerateSegment(segmentData: currSegmentData, iteration: i, masterTransform, positionOffset, segmentScale, name));
                positionOffset += new Vector3(0, 0, -1f * segmentScale * currSegmentData.distanceConstraint);
            }
            animatorScript.SetJoints(spineJoints);
        }
    }

    public void GenerateHead()
    {
        Transform masterTransform = transform;
        Vector3 positionOffset = animalHeadData.headParentOffset;
        List<AnimalJoint> headJoints = new List<AnimalJoint>();

        int nameId = 0;
        foreach (SegmentData currSegmentData in animalHeadData.joints)
        {
            for (int i = 0; i < currSegmentData.jointCount; i++)
            {
                float xValue = (float)i / (float)currSegmentData.jointCount;
                float segmentScale = currSegmentData.sizeCurve.Evaluate(xValue);
                string name = currSegmentData.segmentName + " Head Segment " + nameId++;
                headJoints.Add(GenerateSegment(currSegmentData, iteration: i, masterTransform, positionOffset, segmentScale, name));
                positionOffset += new Vector3(0, 0, 1f * segmentScale * currSegmentData.distanceConstraint);
            }
        }
        animalHead = new AnimalHead(headJoints, spineJoints[0], animalHeadData);
        animatorScript.SetHead(animalHead);
    }

    public void GenerateLimbs()
    {
        Transform masterTransform = transform;
        int limbId = 0;

        foreach (AnimalLimbData currLimbData in animalLimbData)
        {
            Vector3 positionOffset = spineJoints[currLimbData.parentJointId].segmentPosition + currLimbData.parentPositionOffset - masterTransform.position;

            List<AnimalJoint> limbJoints = new List<AnimalJoint>();

            foreach (SegmentData currSegmentData in currLimbData.joints)
            {
                int nameId = 0;
                for (int i = 0; i < currSegmentData.jointCount; i++)
                {
                    float xValue = (float)i / (float)currSegmentData.jointCount;
                    float segmentScale = currSegmentData.sizeCurve.Evaluate(xValue);

                    string name = currLimbData.limbName + " " + currSegmentData.segmentName + " Segment " + nameId++;

                    limbJoints.Add(GenerateSegment(currSegmentData, iteration: i, masterTransform, positionOffset, segmentScale, name));
                    float offsetDirection = (currLimbData.parentPositionOffset.x >= 0f) ? 1 : -1;
                    positionOffset += new Vector3(offsetDirection * segmentScale * currSegmentData.distanceConstraint, 0, 0);
                }
            }
            limbs.Add(new AnimalLimb(currLimbData, limbJoints, spineJoints[currLimbData.parentJointId], limbId++));
        }
        animatorScript.SetLimbs(limbs);
    }

    protected AnimalJoint GenerateSegment(SegmentData segmentData, int iteration, Transform masterTransform, Vector3 positionOffset, float segmentScale, string name)
    {
        GameObject newSegment = Instantiate(segmentData.bodySegmentPrefab, masterTransform);
        newSegment.transform.localScale = Vector3.one * segmentScale;

        //newSegment.transform.position = new Vector3(masterTransform.position.x, masterTransform.position.y, masterTransform.position.z);
        newSegment.transform.position += positionOffset;
        newSegment.name = name;

        AnimalJoint segmentScript = newSegment.GetComponent<AnimalJoint>();
        segmentScript.AfterInstantiate(segmentData.distanceConstraint * segmentScale, segmentData.angularConstraint, segmentData.prefferedAngle,iteration);
        return newSegment.GetComponent<AnimalJoint>();
    }
}

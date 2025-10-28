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
    public float angularConstraint;
}

public class AnimalCreator : MonoBehaviour
{
    [SerializeField] AnimalAnimator animatorScript;

    [SerializeField] protected List<SegmentData> spineSegmentData = new List<SegmentData>();
    [SerializeField] protected List<AnimalLimbData> animalLimbData = new List<AnimalLimbData>();

    private List<AnimalJoint> spineJoints = new List<AnimalJoint>();
    private List<AnimalLimb> limbs = new List<AnimalLimb>();
    public void GenerateBody()
    {
        Transform masterTransform = transform;
        Vector3 positionOffset = Vector3.zero;
        int nameId = 0;

        foreach (SegmentData currSegment in spineSegmentData)
        {
            for (int i = 0; i < currSegment.jointCount; i++)
            {
                float xValue = (float)i / (float)currSegment.jointCount;
                float segmentScale = currSegment.sizeCurve.Evaluate(xValue);

                GameObject newSegment = Instantiate(currSegment.bodySegmentPrefab, masterTransform);
                newSegment.transform.localScale = Vector3.one * segmentScale;
                newSegment.transform.position = new Vector3(masterTransform.position.x,0,masterTransform.position.z);
                newSegment.transform.position += positionOffset;
                positionOffset += new Vector3(0, 0, -1f * segmentScale);
                newSegment.name = currSegment.segmentName + " Spine Segment " + nameId++;
                AnimalJoint segmentScript = newSegment.GetComponent<AnimalJoint>();
                segmentScript.AfterInstantiate(currSegment.distanceConstraint * segmentScale, currSegment.angularConstraint, i);
                spineJoints.Add(newSegment.GetComponent<AnimalJoint>());
            }
            animatorScript.SetJoints(spineJoints);
        }
    }

    public void GenerateLimbs()
    {
        Transform masterTransform = transform;
        int limbId = 0;

        foreach (AnimalLimbData currLimbData in animalLimbData)
        {
            Vector3 positionOffset;
            positionOffset = spineJoints[currLimbData.parentJointId].segmentPosition * spineJoints[currLimbData.parentJointId].segmentScale.x;
            if (spineJoints != null && currLimbData.parentJointId >= 0 && currLimbData.parentJointId < spineJoints.Count && spineJoints[currLimbData.parentJointId] != null)
            {
                Vector3 parentOffset = spineJoints[currLimbData.parentJointId].segmentPosition;
                float parentScale = spineJoints[currLimbData.parentJointId].segmentScale.x;
                positionOffset = parentOffset * parentScale + currLimbData.parentPositionOffset;
            }
            else
            {
                positionOffset = currLimbData.parentPositionOffset;
                Debug.LogWarning("Animal Creator: Parent for limb - " + currLimbData.limbName + " not found! Using default offset");
            }


            List<AnimalJoint> limbJoints = new List<AnimalJoint>();

            foreach (SegmentData currJoint in currLimbData.joints)
            {
                int nameId = 0;
                for (int i = 0; i < currJoint.jointCount; i++)
                {
                    float xValue = (float)i / (float)currJoint.jointCount;
                    float segmentScale = currJoint.sizeCurve.Evaluate(xValue);

                    GameObject newSegment = Instantiate(currJoint.bodySegmentPrefab, masterTransform);
                    newSegment.transform.localScale = Vector3.one * segmentScale;
                    newSegment.transform.position += positionOffset;

                    float offsetDirection = (currLimbData.parentPositionOffset.x >= 0f) ? 1 : -1;
                    positionOffset += new Vector3(offsetDirection * segmentScale, 0, 0);

                    newSegment.name = currLimbData.limbName + " " + currJoint.segmentName + " Segment " + nameId++;
                    AnimalJoint segmentScript = newSegment.GetComponent<AnimalJoint>();
                    segmentScript.AfterInstantiate(currJoint.distanceConstraint * segmentScale, currJoint.angularConstraint, i);
                    limbJoints.Add(newSegment.GetComponent<AnimalJoint>());
                }
            }
            limbs.Add(new AnimalLimb(currLimbData, limbJoints, limbId++));
        }
        animatorScript.SetLimbs(limbs);
    }
}

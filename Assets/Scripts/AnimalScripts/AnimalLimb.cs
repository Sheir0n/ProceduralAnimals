using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

[System.Serializable]
public class AnimalLimbData
{
    public string limbName;
    public List<SegmentData> joints = new List<SegmentData>();
    public int parentJointId;
    public Vector3 parentPositionOffset;
}

public class AnimalLimb
{
    public AnimalLimbData limbData { get; private set; }
    public List<AnimalJoint> joints { get; private set; }
    public Vector3 targetPosition;

    public AnimalLimb(AnimalLimbData _limbData, List<AnimalJoint> _joints)
    {
        limbData = _limbData;
        joints = _joints;
        targetPosition = _joints.Last().segmentPosition;
    }
}

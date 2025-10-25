using System.Collections;
using System.Collections.Generic;
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
    private AnimalLimbData limbData;
    private List<AnimalJoint> joints;

    public AnimalLimb(AnimalLimbData _limbData, List<AnimalJoint> _joints)
    {
        limbData = _limbData;
        joints = _joints;
    }
}

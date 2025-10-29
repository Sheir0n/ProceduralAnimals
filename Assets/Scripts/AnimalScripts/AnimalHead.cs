using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class AnimalHeadData
{
    public string headName;
    public List<SegmentData> joints = new List<SegmentData>();
}

public class AnimalHead
{
    public List<AnimalJoint> headJoints { get; private set; }
    public Vector3 targetLookPosition = Vector3.zero;
    public int neckSegmentId;

    public AnimalHead(List<AnimalJoint> _joints)
    {
        headJoints = _joints;
    }
}


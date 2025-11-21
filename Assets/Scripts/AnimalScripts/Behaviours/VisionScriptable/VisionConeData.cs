using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VisionConeSettings", menuName = "AI/Behavior/Senses/Vision Cone Settings")]
public class VisionConeData : ScriptableObject
{
    public float coneSize = 15;
    public float coneAngleRange = 30f;
}

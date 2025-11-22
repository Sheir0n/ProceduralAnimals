using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SearchRestMovementSettings", menuName = "AI/Behavior/Movement/Search Rest Settings")]
public class FindRestSpotMovementSettings : ScriptableObject
{
    [Header("Search Speed Variables")]
    public float agentBaseWalkSpeedMultiplier = 0.5f;

    [Header("Global Stats Sensitivity Variables")]
    public float vigorWalkSpeedVariationModifier = 0f;
    [Range(0, 1)] public float lowHealthSpeedPenalityThreshold = 0f;
    [Range(0f, 1)] public float healthSlowdownMaxPenality = 0f;
}

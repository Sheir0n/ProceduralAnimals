using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WanderMovementSettings", menuName = "AI/Behavior/Movement/Wander Settings")]
public class WanderMovementSettings : ScriptableObject
{
    [Header("Wander Variables")]
    public float selectNewTargetCooldownMs = 250;
    public float wanderCircleDistance = 2f;
    public float wanderCircleRadius = 1.5f;
    public float wanderJitter = 0.1f;

    [Header("Wander Speed Variables")]
    public float agentWanderAngularSpeed = 90;
    public float agentFallbackAngularSpeed = 240;
    public float agentBaseWalkSpeedMultiplier = 0.5f;

    [Header("Global Stats Sensitivity Variables")]
    public float vigorWalkSpeedVariationModifier = 0f;
    [Range(0, 1)] public float lowHealthSpeedPenalityThreshold = 0f;
    [Range(0f, 1)] public float healthSlowdownMaxPenality = 0f;
}
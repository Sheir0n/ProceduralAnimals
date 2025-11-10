using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WanderBehaviorSettings", menuName = "AI/Behavior/Wander Settings")]
public class WanderBehaviorSettings : ScriptableObject
{
    [Header("Wander Variables")]
    public float selectNewTargetCooldownMs = 250;
    public float wanderCircleDistance = 2f;
    public float wanderCircleRadius = 1.5f;
    public float wanderJitter = 0.1f;

    [Header("Wander Speed Variables")]
    public float agentWanderAngularSpeed = 90;
    public float agentFallbackAngularSpeed = 240;
}
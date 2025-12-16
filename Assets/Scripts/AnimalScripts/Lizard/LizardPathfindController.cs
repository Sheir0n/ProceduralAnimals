using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AnimalAI;
using UnityEngine.AI;

public class LizardPathfindController : PathfindController
{
    [Header("Scriptable Behavior Settings")]
    [SerializeField] private WanderMovementSettings wanderBehaviorSettings;
    [SerializeField] private FindRestSpotMovementSettings findRestSpotBehaviorSettings;

    [SerializeField] private ActionID playerControlled;
    [SerializeField] private ActionID wander;
    [SerializeField] private ActionID rest;
    [SerializeField] private ActionID findRest;
    [SerializeField] private ActionID chase;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void InitializeWithStatsHook(IReadOnlyAnimalStats statsHook)
    {
        base.InitializeWithStatsHook(statsHook);
        agent.baseOffset = 0.2f;

        AddNewMovementBehavior(ScriptableObject.CreateInstance<PlayerControlledMovement>(), playerControlled, statsHook);
        AddNewMovementBehavior(ScriptableObject.CreateInstance<WanderMovement>(), wander, statsHook);
        AddNewMovementBehavior(ScriptableObject.CreateInstance<RestMovement>(), rest, statsHook);
        AddNewMovementBehavior(ScriptableObject.CreateInstance<FindRestSpotMovement>(), findRest, statsHook);
        AddNewMovementBehavior(ScriptableObject.CreateInstance<ChaseFoodMovement>(), chase, statsHook);
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static AnimalAI;

public class BeetlePathfindingController : PathfindController
{
    [Header("Scriptable Behavior Settings")]
    [SerializeField] private WanderMovementSettings wanderBehaviorSettings;

    [SerializeField] private ActionID playercontrolled;
    [SerializeField] private ActionID wander;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void InitializeWithStatsHook(IReadOnlyAnimalStats statsHook)
    {
        base.InitializeWithStatsHook(statsHook);
        agent.baseOffset = 0f;
        AddNewMovementBehavior(new PlayerControlledMovement(agent, transform, statsHook), playercontrolled);
        AddNewMovementBehavior(new WanderMovement(agent, wanderBehaviorSettings, eventHub, statsHook), wander);
    }
}

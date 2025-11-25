using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static AnimalAI;

public class BeetlePathfindingController : PathfindController
{
    [Header("Scriptable Behavior Settings")]
    [SerializeField] private WanderMovementSettings wanderBehaviorSettings;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void InitializeWithStatsHook(IReadOnlyAnimalStats statsHook)
    {
        base.InitializeWithStatsHook(statsHook);

        AddNewMovementBehavior(new PlayerControlledMovement(agent, transform, statsHook), AIAction.PlayerControlled);
        AddNewMovementBehavior(new WanderMovement(agent, wanderBehaviorSettings, eventHub, statsHook), AIAction.Wander);
    }
}

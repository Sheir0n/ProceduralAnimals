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

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void InitializeWithStatsHook(IReadOnlyAnimalStats statsHook)
    {
        base.InitializeWithStatsHook(statsHook);
        agent.height = 2;

        AddNewMovementBehavior(new PlayerControlledMovement(agent, transform, statsHook), AIAction.PlayerControlled);
        AddNewMovementBehavior(new WanderMovement(agent, wanderBehaviorSettings, eventHub, statsHook), AIAction.Wander);
        AddNewMovementBehavior(new RestMovement(agent, transform), AIAction.Rest);
        AddNewMovementBehavior(new FindRestSpotMovement(agent, findRestSpotBehaviorSettings, transform, eventHub, statsHook), AIAction.FindRestSpot);
        AddNewMovementBehavior(new ChaseFoodMovement(agent, eventHub), AIAction.ChaseFood);
    }
}

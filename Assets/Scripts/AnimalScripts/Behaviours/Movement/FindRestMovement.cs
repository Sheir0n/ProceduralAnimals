using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class FindRestSpotMovement : BaseMovementScript, IAnimalMovement
{
    private NavMeshAgent agent;
    private Transform transform;
    public Vector3? MoveTargetPosition { get; private set; }
    public Vector3? LookTargetPosition { get; private set; }
    public bool? LookAtTarget { get; private set; }

    private MovementStats searchRestStats;
    private FindRestSpotMovementSettings searchRestMovementSettings;

    public FindRestSpotMovement(NavMeshAgent agent, FindRestSpotMovementSettings settings, IReadOnlyAnimalStats generalStatsHook)
    {
        this.agent = agent;
        this.searchRestMovementSettings = settings;
        this.animalStatsHook = generalStatsHook;
        AssignMovementStats();
    }

    protected override void AssignMovementStats()
    {
        AssignBaseMovementStats(agent);
        searchRestStats = new MovementStats(BaseStats);

        float modifier = searchRestMovementSettings.vigorWalkSpeedVariationModifier;
        float speedMultiplier = Mathf.Lerp(1f - modifier, 1f + modifier, animalStatsHook.StatVigor);

        searchRestStats.Speed *= searchRestMovementSettings.agentBaseWalkSpeedMultiplier * speedMultiplier;
        searchRestStats.Acceleration *= searchRestMovementSettings.agentBaseWalkSpeedMultiplier * speedMultiplier;
    }

    public void Enter() { }
    public void Update() { }
    public void Exit() { }

}

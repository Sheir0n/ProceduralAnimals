using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.AI;
using static ChaseFoodController;

[CreateAssetMenu(fileName = "ChaseMovement", menuName = "AI/Movement/ChaseMovement")]
public class ChaseFoodMovement : BaseMovementScript, IAnimalMovement
{
    [Header("Attack Dash Settings")]
    [SerializeField] private MovementStatsModifiers slowdownStatsModifiers = new MovementStatsModifiers();
    [SerializeField] private MovementStatsModifiers noMovementStatsModifiers = new MovementStatsModifiers();

    private MovementStats slowdownStats;
    private MovementStats noMovementStats;
    private MovementStats currStatsSet;

    private Vector3 dashPushVector = Vector3.zero;
    private Vector3 dashPushTargetVector = Vector3.zero;

    private Vector3 lookTargetPos;
    public Vector3? MoveTargetPosition => null;
    public Vector3? LookTargetPosition => lookTargetPos;
    public bool? LookAtTarget { get; private set; }

    private Transform chaseTarget = null;

    private float updateAgentTargetingTimerMs = 0f;
    private const int updateAgentTargetingTimeMs = 500;
    private float dashSpeed = 8f;
    private float dashLerpSpeed = 2f;
    private float dashSlowdownLerp = 8f;

    public override void OnInstantiate(NavMeshAgent agent, Transform transform, AnimalEventHub eventHub, IReadOnlyAnimalStats statsHook)
    {
        base.OnInstantiate(agent, transform, eventHub, statsHook);
    }

    protected override void AssignExtraMovementStats(NavMeshAgent agent)
    {
        slowdownStats = CalculateStatsWithModifiers(agent, slowdownStatsModifiers);
        noMovementStats = CalculateStatsWithModifiers(agent, noMovementStatsModifiers);
    }

    //protected override void AssignMovementStats()
    //{
    //    baseStats = new MovementStats(base.baseStats);

    //    slowdownStats = new MovementStats(base.baseStats);
    //    slowdownStats.Speed = base.baseStats.Speed * 0.1f;
    //    slowdownStats.AngularSpeed = base.baseStats.AngularSpeed * 0.1f;
    //    slowdownStats.Acceleration = base.baseStats.AngularSpeed * 0.25f;

    //    noMovementStats = new MovementStats(base.baseStats);
    //    noMovementStats.Speed = 0f;
    //    noMovementStats.AngularSpeed = 0.125f;

    //    currStatsSet = base.baseStats;
    //}

    public void Enter()
    {
        currStatsSet = baseStats;
        dashPushVector = Vector3.zero;
        dashPushTargetVector = Vector3.zero;
        updateAgentTargetingTimerMs = 0f;
        LookAtTarget = true;
        eventHub.OnBiteAttack += UpdateMovementOnAttackStageChange;
    }

    public void Update()
    {
        SmoothAssignMovementStats(agent, currStatsSet, lerpSpeed: 5f);
        if (dashPushTargetVector != Vector3.zero)
        {
            dashPushVector = Vector3.Lerp(dashPushVector, dashPushTargetVector, dashLerpSpeed * Time.deltaTime
            );
            agent.Move(dashPushVector * dashSpeed * Time.deltaTime);
        }
        else
        {
            dashPushVector = Vector3.Lerp(dashPushVector, Vector3.zero, dashSlowdownLerp * Time.deltaTime);
            agent.Move(dashPushVector * dashSpeed * Time.deltaTime);
        }

        Transform newTarget = eventHub.RequestTrackedPrey().tracked;
        if (newTarget != null && chaseTarget != newTarget)
        {
            chaseTarget = newTarget;
            updateAgentTargetingTimerMs = updateAgentTargetingTimeMs;
        }

        updateAgentTargetingTimerMs += Time.deltaTime * 1000f;
        if (updateAgentTargetingTimerMs >= updateAgentTargetingTimeMs)
        {
            if (chaseTarget != null)
                agent.SetDestination(chaseTarget.position);
        }
        lookTargetPos = chaseTarget.transform.position;
    }
    public void Exit()
    {
        chaseTarget = null;
        eventHub.OnBiteAttack -= UpdateMovementOnAttackStageChange;
    }

    public void UpdateMovementOnAttackStageChange(BiteAttackStage currAttackStage)
    {
        switch (currAttackStage)
        {
            case BiteAttackStage.Windup:
                currStatsSet = slowdownStats;
                break;
            case BiteAttackStage.Dash:
                if (chaseTarget != null)
                    dashPushTargetVector = (chaseTarget.position - transform.position).normalized;
                else
                    dashPushTargetVector = Vector3.zero;

                dashPushVector = Vector3.zero;
                currStatsSet = noMovementStats;
                break;
            case BiteAttackStage.Finished:
                currStatsSet = baseStats;
                dashPushTargetVector = Vector3.zero;
                break;
        }
    }
}

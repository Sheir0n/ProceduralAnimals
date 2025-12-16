using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class MovementScript : ScriptableObject
{
    protected struct MovementStats
    {
        public float Speed;
        public float AngularSpeed;
        public float Acceleration;
        public float StoppingDistance;

        public MovementStats(float baseAgentSpeed, float baseAgentAngularSpeed, float baseAgentAcceleration, float baseStoppingDistance)
        {
            Speed = baseAgentSpeed;
            AngularSpeed = baseAgentAngularSpeed;
            Acceleration = baseAgentAcceleration;
            StoppingDistance = baseStoppingDistance;
        }

        public MovementStats(MovementStats other)
        {
            Speed = other.Speed;
            AngularSpeed = other.AngularSpeed;
            Acceleration = other.Acceleration;
            StoppingDistance = other.StoppingDistance;
        }
    }

    [System.Serializable]
    protected class MovementStatsModifiers
    {
        public float SpeedModifier = 1f;
        public float AngularSpeedModifier = 1f;
        public float AccelerationModifier = 1f;
        public float StoppingDistanceModifier = 1f;
    }

    protected NavMeshAgent agent;
    protected Transform transform;
    protected AnimalEventHub eventHub;
    protected IReadOnlyAnimalStats animalStatsHook;

    [Header("Base stats modifiers")]
    [SerializeField] private MovementStatsModifiers baseStatsModifiers;
    protected MovementStats baseStats;

    [Header("Connected ActionID")]
    [SerializeField] public ActionID connectedId = null;

    public virtual void OnInstantiate(NavMeshAgent agent, Transform transform, AnimalEventHub eventHub, IReadOnlyAnimalStats statsHook)
    {
        this.agent = agent;
        this.transform = transform;
        this.eventHub = eventHub;
        this.animalStatsHook = statsHook;
        AssignBaseMovementStats(agent);
        AssignExtraMovementStats(agent);
    }

    protected void SmoothAssignMovementStats(NavMeshAgent agent, MovementStats targetStats, float lerpSpeed)
    {
        if (Mathf.Abs(agent.speed - targetStats.Speed) > 0.01f)
            agent.speed = Mathf.Lerp(agent.speed, targetStats.Speed, lerpSpeed * Time.deltaTime);

        if (Mathf.Abs(agent.angularSpeed - targetStats.AngularSpeed) > 0.01f)
            agent.angularSpeed = Mathf.Lerp(agent.angularSpeed, targetStats.AngularSpeed, lerpSpeed * Time.deltaTime);

        if (Mathf.Abs(agent.acceleration - targetStats.Acceleration) > 0.01f)
            agent.acceleration = Mathf.Lerp(agent.acceleration, targetStats.Acceleration, lerpSpeed * Time.deltaTime);

        if (Mathf.Abs(agent.stoppingDistance - targetStats.StoppingDistance) > 0.01f)
            agent.stoppingDistance = Mathf.Lerp(agent.stoppingDistance, targetStats.StoppingDistance, lerpSpeed * Time.deltaTime);
    }

    protected virtual void AssignBaseMovementStats(NavMeshAgent agent)
    {
        baseStats = CalculateStatsWithModifiers(agent, baseStatsModifiers);
    }

    protected MovementStats CalculateStatsWithModifiers(NavMeshAgent agent, MovementStatsModifiers modifiers)
    {
        MovementStats modifiedStats = new MovementStats(
            agent.speed * modifiers.SpeedModifier,
            agent.angularSpeed * modifiers.AngularSpeedModifier,
            agent.acceleration * modifiers.AccelerationModifier,
            agent.stoppingDistance * modifiers.StoppingDistanceModifier);
        return modifiedStats;
    }

    protected abstract void AssignExtraMovementStats(NavMeshAgent agent);
}

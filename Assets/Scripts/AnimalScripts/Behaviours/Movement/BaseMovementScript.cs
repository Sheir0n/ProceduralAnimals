using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class BaseMovementScript
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

    protected MovementStats BaseStats { private set; get; }
    protected IReadOnlyAnimalStats animalStatsHook;


    protected void AssignBaseMovementStats(NavMeshAgent agent)
    {
        BaseStats = new MovementStats(agent.speed, agent.angularSpeed, agent.acceleration, agent.stoppingDistance);
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

    protected abstract void AssignMovementStats();
}

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static AnimalAI;

public class PathfindController : MonoBehaviour, IAnimalObserver
{
    [Header("Nav Mesh Agent Component")]
    private NavMeshAgent agent;
    public Vector3 moveTargetPos { get; private set; } = Vector3.zero;
    public Vector3 lookTargetPos { get; private set; } = Vector3.zero;
    public bool lookAtTarget { get; private set; } = false;

    private Quaternion lastRotation;
    public float agentCurrAngularSpeed { get; private set; } = 0;

    private WanderMovement wanderMovement;
    private PlayerControlledMovement playerControledMovement;
    private RestMovement restMovement;
    private IAnimalMovement currentBehavior;

    [Header("Scriptable Behavior Settings")]
    [SerializeField] WanderMovementSettings wanderBehaviorSettings;

    AIAction currAction;

    private void Awake()
    {
        agent = transform.GetComponentInParent<NavMeshAgent>();
        wanderMovement = new WanderMovement(agent, wanderBehaviorSettings);
        playerControledMovement = new PlayerControlledMovement(agent, transform);
        restMovement = new RestMovement(agent,transform);
    }

    void Start()
    {
        lastRotation = transform.rotation;
    }

    void Update()
    {
        currentBehavior.Update();
        lookTargetPos = currentBehavior.LookTargetPosition ?? transform.position;
        lookAtTarget = currentBehavior.LookAtTarget ?? false;
        CalculateAngularSpeed();
    }

    public bool IsMoving() => agent.velocity.sqrMagnitude > 0.01f;
    public float GetVelocity() => agent.velocity.magnitude;

    private void CalculateAngularSpeed()
    {
        Quaternion delta = transform.rotation * Quaternion.Inverse(lastRotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        agentCurrAngularSpeed = angle / Time.deltaTime;
        lastRotation = transform.rotation;
    }

    public void OnActionChanged(AIAction newAction) { 
        Debug.Log("recived new action! " + newAction);

        if (currAction == newAction)
            return;

        currentBehavior?.Exit();

        currAction = newAction;

        switch (newAction)
        {
            case AnimalAI.AIAction.Rest:
                currentBehavior = restMovement;
                break;
            case AnimalAI.AIAction.Wander:
                currentBehavior = wanderMovement;
                break;
            case AnimalAI.AIAction.PlayerControlled:
                currentBehavior = playerControledMovement;
                break;
            default:
                currentBehavior = restMovement;
                break;
        }

        currentBehavior.Enter();

        Debug.Log($"Action changed to {newAction}");
    }
}


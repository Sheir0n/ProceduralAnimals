using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class PathfindController : MonoBehaviour
{
    [Header("Nav Mesh Agent Component")]
    private NavMeshAgent agent;
    [SerializeField] bool enablePlayerControl = false;
    public Vector3 moveTargetPos { get; private set; } = Vector3.zero;
    public Vector3 lookTargetPos { get; private set; } = Vector3.zero;
    public bool lookAtTarget { get; private set; } = false;

    private Quaternion lastRotation;
    public float agentCurrAngularSpeed { get; private set; } = 0;

    private WanderBehavior wanderBehavior;
    private PlayerControlledBehavior playerControledBehavior;
    private IAnimalBehavior currentBehavior;

    [Header("Scriptable Behavior Settings")]
    [SerializeField] WanderBehaviorSettings wanderBehaviorSettings;

    private void Awake()
    {
        agent = transform.GetComponentInParent<NavMeshAgent>();
        wanderBehavior = new WanderBehavior(agent, wanderBehaviorSettings);
        playerControledBehavior = new PlayerControlledBehavior(agent, transform);

        currentBehavior = wanderBehavior;
        currentBehavior.Enter();
    }
    void Start()
    {
        lastRotation = transform.rotation;
    }

    void Update()
    {
        if (enablePlayerControl && playerControledBehavior != null)
        {
            if (currentBehavior != playerControledBehavior) {
                currentBehavior.Exit();
                currentBehavior = playerControledBehavior;
                currentBehavior.Enter();
            }
        }
        else
        {
            if (currentBehavior != wanderBehavior && wanderBehavior != null)
            {
                currentBehavior.Exit();
                currentBehavior = wanderBehavior;
                currentBehavior.Enter();
            }
        }
        
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
}


using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static AnimalAI;

public class PathfindController : MonoBehaviour
{
    [Header("Nav Mesh Agent Component")]
    protected NavMeshAgent agent;
    public Vector3 lookTargetPos { get; private set; } = Vector3.zero;
    public bool lookAtTarget { get; private set; } = false;

    private Quaternion lastRotation;
    protected float agentCurrAngularSpeed { get; private set; } = 0;

    protected List<IAnimalMovement> movements = new List<IAnimalMovement>();
    protected Dictionary<AIAction, IAnimalMovement> movementByEnum;
    protected Dictionary<IAnimalMovement, AIAction> enumByMovement;
    protected IAnimalMovement currentBehavior;

    protected AnimalEventHub eventHub;
    private Vector3 pendingPush;
    private bool pushedThisFrame = false;

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        eventHub.OnInitializeStats += InitializeWithStatsHook;
        eventHub.OnActionChanged += OnActionChanged;
        eventHub.OnSegmentCollision += PushAgent;

        //external data requests
        eventHub.OnAngularSpeedRequest += GetAngularSpeed;
        eventHub.OnLookTargetRequest += GetLookTarget;
    }

    protected virtual void InitializeWithStatsHook(IReadOnlyAnimalStats statsHook)
    {
        Debug.Log("Initialized stats!");
        agent = transform.GetComponentInParent<NavMeshAgent>();

        movementByEnum = new Dictionary<AIAction, IAnimalMovement>();
        enumByMovement = new Dictionary<IAnimalMovement, AIAction>();
        lastRotation = transform.rotation;
    }

    void Update()
    {
        currentBehavior.Update();
        lookTargetPos = currentBehavior.LookTargetPosition ?? transform.position;
        lookAtTarget = currentBehavior.LookAtTarget ?? false;

        if (pendingPush.magnitude > 0.01f)
            if (pendingPush.magnitude > 0.01f)
            {
                float basePushSpeed = 1.5f;
                float scaledSpeed = basePushSpeed * pendingPush.magnitude;

                float moveStep = Mathf.Min(pendingPush.magnitude, scaledSpeed * Time.deltaTime);
                Vector3 moveDir = pendingPush.normalized * moveStep;
                agent.Move(moveDir);
                pendingPush -= moveDir;
            }

        if (!pushedThisFrame)
            StopAgentPush();


        CalculateAngularSpeed();
    }

    private void LateUpdate()
    {
        pushedThisFrame = false;
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

    protected void AddNewMovementBehavior(IAnimalMovement movement, AIAction actionEnum)
    {
        movements.Add(movement);
        enumByMovement.Add(movement, actionEnum);
        movementByEnum.Add(actionEnum, movement);
    }

    public void OnActionChanged(AIAction newAction)
    {
        Debug.Log("Recived new action! " + newAction);

        if (!movementByEnum.ContainsKey(newAction))
        {
            Debug.LogWarning("Couldnt find corresponding movmenet action! " + newAction);
            return;
        }

        IAnimalMovement newMovementBehavior = movementByEnum[newAction];

        if (currentBehavior == newMovementBehavior)
            return;

        currentBehavior?.Exit();
        currentBehavior = newMovementBehavior;
        newMovementBehavior.Enter();

        Debug.Log($"Action changed to {newAction}");
    }

    private void PushAgent(Vector3 pushVector)
    {
        pushedThisFrame = true;
        float pushAmount = 15f;
        float redirectAmount = 10f;
        pendingPush += pushVector * pushAmount * Time.deltaTime;

        float stopDistance = 2f;
        Vector3 currDestination = agent.destination;
        float distanceToDestination = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(currDestination.x, currDestination.z));

        //redirect agent
        if (distanceToDestination < stopDistance)
        {
            agent.SetDestination(currDestination + pushVector * redirectAmount * Time.deltaTime);
        }
    }

    private void StopAgentPush()
    {
        if (pendingPush.sqrMagnitude > 0.00001f)
        {
            pendingPush *= 0.80f;
        }
    }

    public float GetAngularSpeed() => agentCurrAngularSpeed;

    public LookTarget GetLookTarget() => new LookTarget(lookTargetPos, lookAtTarget);
}


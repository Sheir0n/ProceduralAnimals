using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.AI;
using static AnimalAI;

public class PathfindController : MonoBehaviour
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

    [Header("Scriptable Behavior Settings")]
    [SerializeField] WanderMovementSettings wanderBehaviorSettings;

    protected List<IAnimalMovement> movements = new List<IAnimalMovement>();
    protected Dictionary<AIAction, IAnimalMovement> movementByEnum;
    protected Dictionary<IAnimalMovement, AIAction> enumByMovement;
    private IAnimalMovement currentBehavior;

    private AnimalEventHub eventHub;

    void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        eventHub.OnInitializeStats += InitializeWithStatsHook;
        eventHub.OnActionChanged += OnActionChanged;
    }

    public void InitializeWithStatsHook(IReadOnlyAnimalStats statsHook)
    {
        Debug.Log("Initialized stats!");
        agent = transform.GetComponentInParent<NavMeshAgent>();

        movementByEnum = new Dictionary<AIAction, IAnimalMovement>();
        enumByMovement = new Dictionary<IAnimalMovement, AIAction>();
        AddNewMovementBehavior(new PlayerControlledMovement(agent, transform, statsHook), AIAction.PlayerControlled);
        AddNewMovementBehavior(new WanderMovement(agent, wanderBehaviorSettings, statsHook), AIAction.Wander);
        AddNewMovementBehavior(new RestMovement(agent, transform, statsHook), AIAction.Rest);


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

    protected void AddNewMovementBehavior(IAnimalMovement movement, AIAction actionEnum)
    {
        movements.Add(movement);
        enumByMovement.Add(movement, actionEnum);
        movementByEnum.Add(actionEnum, movement);
    }

    public void OnActionChanged(AIAction newAction) { 
        Debug.Log("Recived new action! " + newAction);

        if(!movementByEnum.ContainsKey(newAction)) {
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
}


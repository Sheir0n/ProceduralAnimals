using System;
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
    [Header("Agent znajdywania œcie¿ki")]
    protected NavMeshAgent agent;
    public Vector3 lookTargetPos { get; private set; } = Vector3.zero;
    public bool lookAtTarget { get; private set; } = false;
    private Quaternion lastRotation;
    protected float agentCurrAngularSpeed { get; private set; } = 0;

    [Header("Domyœlny ruch sterowania agenta poprzez u¿ytkownika")]
    [SerializeField] protected PlayerControlledMovement playerControlledMovement;
    [Header("Lista zachowañ ruchowych - powinna odpowiadaæ zachowaniom UtilityAI")]
    [SerializeField] protected List<MovementScript> avalibleMovementsScriptables;

    protected List<IAnimalMovement> movements = new List<IAnimalMovement>();
    protected Dictionary<ActionID, IAnimalMovement> movementByID;
    protected Dictionary<IAnimalMovement, ActionID> IDByMovement;
    protected IAnimalMovement currentBehavior;

    protected AnimalEventHub eventHub;
    private Vector3 pendingPush;
    private bool pushedThisFrame = false;

    [Header("Globalne ID statycznych zachowañ ruchowych")]
    [SerializeField] protected ActionID deathActionSharedID;
    [SerializeField] protected ActionID emptyActionSharedID;
    [SerializeField] protected ActionID playerControlledActionSharedID;

    [Header("Pozycja agenta w osi Y, zachowañ unikalne by unikn¹æ zjawiska Z-fighting ")]
    [SerializeField] private float agentHeightBaseOffset = 0.5f;

    [Header("Runtime Debug: lista parametrów kopii zachowañ")]
    [SerializeField] private List<ScriptableObject> runtimeMovementsDebug;

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        eventHub.OnInitializeStats += InitializeMovements;
        eventHub.OnActionChanged += OnActionChanged;
        eventHub.OnSegmentCollision += PushAgent;

        //external data requests
        eventHub.OnAngularSpeedRequest += GetAngularSpeed;
        eventHub.OnPathfindScriptLookTarget += GetLookTarget;
    }

    private void InitializeMovements(IReadOnlyAnimalStats statsHook)
    {
        agent = transform.GetComponentInParent<NavMeshAgent>();
        agent.baseOffset = agentHeightBaseOffset;

        movementByID = new Dictionary<ActionID, IAnimalMovement>();
        IDByMovement = new Dictionary<IAnimalMovement, ActionID>();
        lastRotation = transform.rotation;

        foreach (MovementScript movement in avalibleMovementsScriptables)
        {
            if (movement is IAnimalMovement utilityAction)
            {
                AddNewMovementBehavior(movement, movement.connectedId, statsHook);
            }
        }

        if (playerControlledMovement != null)
            AddNewMovementBehavior(playerControlledMovement, playerControlledActionSharedID, statsHook);
        else
            Debug.LogWarning("PathfindController: Nie przypisano skryptu kontrolera gracza! U¿ytkownik nie bêdzie móg³ obj¹æ kontroli nad agentem!", this);

        EmptyMovement emptyController = ScriptableObject.CreateInstance<EmptyMovement>();
        AddNewMovementBehavior(emptyController, emptyActionSharedID, statsHook);

        DeathMovement deathController = ScriptableObject.CreateInstance<DeathMovement>();
        AddNewMovementBehavior(deathController, deathActionSharedID, statsHook);
    }

    void Update()
    {
        if (currentBehavior != null)
        {
            currentBehavior.Update();
            lookTargetPos = currentBehavior.LookTargetPosition ?? transform.position;
            lookAtTarget = currentBehavior.LookAtTarget ?? false;
        }

        if (pendingPush.magnitude > 0.01f)
            if (pendingPush.magnitude > 0.01f)
            {
                float basePushSpeed = 2f;
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

    protected void AddNewMovementBehavior(MovementScript movement, ActionID id, IReadOnlyAnimalStats statsHook)
    {
        var instanceObj = Instantiate(movement);

        if (instanceObj is not IAnimalMovement instance)
        {
            Debug.LogError($"PathfindController: {movement.name} nie implementuje interfejsu IAnimalMovement!", this);
            return;
        }
        if (id == null)
        {
            Debug.LogError($"PathfindController: {movement.name} Nie ma podano tagu zachowania!", this);
            return;
        }

        if (movementByID.ContainsKey(id))
        {
            IAnimalMovement duplicate = movementByID[id];
            Debug.LogWarning($"PathfindController: {movement.name} ma duplikat ID istniej¹cego ju¿ zachowania: " + duplicate, this);
            return;
        }

        movements.Add(instance);
        instance.OnInstantiate(agent, transform, eventHub, statsHook);
        IDByMovement.Add(instance, id);
        movementByID.Add(id, instance);
        runtimeMovementsDebug.Add(instanceObj);
    }

    protected void OnActionChanged(ActionID newAction)
    {
        IAnimalMovement newMovementBehavior = movementByID[emptyActionSharedID];
        if (!movementByID.ContainsKey(newAction))
            Debug.LogWarning("PathfindController: Nie znaleziono zachowania odpowiadaj¹cemu id: " + newAction + "! Przejœcie na domyœlne zachowanie EmptyMovement!", this);
        else
            newMovementBehavior = movementByID[newAction];

        if (currentBehavior == newMovementBehavior)
            return;

        currentBehavior?.Exit();
        currentBehavior = newMovementBehavior;
        newMovementBehavior.Enter();
    }

    private void PushAgent(Vector3 pushVector)
    {
        pushedThisFrame = true;
        float pushAmount = 25f;
        float redirectAmount = 10f;
        pendingPush += pushVector * pushAmount * Time.deltaTime;

        float stopDistance = 2f;
        Vector3 currDestination = agent.destination;
        float distanceToDestination = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(currDestination.x, currDestination.z));

        //redirect agent
        if (distanceToDestination < stopDistance && IDByMovement[currentBehavior] != deathActionSharedID)
        {
            agent.SetDestination(currDestination + pushVector * redirectAmount * Time.deltaTime);
        }
    }

    private void StopAgentPush()
    {
        if (pendingPush.sqrMagnitude > 0.00001f)
        {
            pendingPush *= 0.75f;
        }
    }

    public float GetAngularSpeed() => agentCurrAngularSpeed;

    public LookTarget GetLookTarget() => new LookTarget(lookTargetPos, lookAtTarget);

    private void OnDestroy()
    {
        
    }
}


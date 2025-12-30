using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnimalAI : MonoBehaviour, IDamageable
{
    [SerializeField] protected AnimalStats stats;
    protected readonly float energyDrainRate = 0.075f;
    protected readonly float saturationDrainRate = 0.045f;

    [Header("Show StateChange logs")]
    [SerializeField] private bool showStateChangeLogs = false;

    //Datas
    [SerializeField] protected TrackerDatas trackersData;
    protected InterestTracker interestTracker;
    protected PreyTracker preyTracker;
    protected FearTracker fearTracker;

    //UtilityAction controllers
    protected AnimalAnimator animator;
    protected PathfindController pathfindController;

    //UtilityAI Actions
    protected List<IUtilityAction> actions = new List<IUtilityAction>();
    protected IUtilityAction currAction;

    [SerializeField] protected float defaultPenality = 1f;
    [SerializeField] protected float penalityDrainSpeed = 4f;
    [SerializeField] protected float hysteresis = 0.1f;

    [SerializeField, ReadOnly] protected Dictionary<IUtilityAction, float> actionPenalities;
    [SerializeField] protected string actionDebugDisplay;

    protected Dictionary<ActionID, IUtilityAction> actionByID;
    protected Dictionary<IUtilityAction, ActionID> IDByAction;

    [SerializeField] private bool isPlayerControlled = false;
    [SerializeField] private bool showAIPoints = false;

    protected AnimalEventHub eventHub;
    private Transform snatchTransform;

    [Header("Static action IDS")]
    [SerializeField] protected List<ActionController> actionAssetList = new List<ActionController>();
    [SerializeField] protected ActionID emptyActionSharedID;
    [SerializeField] protected ActionID deathActionSharedID;
    [SerializeField] protected ActionID playerControlledActionSharedID;


    [Header("Runtime action settings clones")]
    [SerializeField] private List<ScriptableObject> runtimeActionDebug;
    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        animator = GetComponent<AnimalAnimator>();
        pathfindController = GetComponent<PathfindController>();

        stats.GenerateStats();

        LoadActionList();

        if (trackersData == null)
            trackersData = ScriptableObject.CreateInstance<TrackerDatas>();
        interestTracker = new InterestTracker(trackersData.lookTrackerTags, transform, eventHub, stats);
        preyTracker = new PreyTracker(trackersData.foodTrackerTags, transform, eventHub, stats);
        fearTracker = new FearTracker(trackersData.fearTrackerTags, transform, eventHub, stats);

        eventHub.OnTrackerDatasRequest += () => trackersData;
    }


    private void LoadActionList()
    {
        actionByID = new Dictionary<ActionID, IUtilityAction>();
        IDByAction = new Dictionary<IUtilityAction, ActionID>();

        EmptyController emptyController = ScriptableObject.CreateInstance<EmptyController>();
        emptyController.InitializeShared(emptyActionSharedID);
        AddNewAction(emptyController);

        PlayerControlledController playerController = ScriptableObject.CreateInstance<PlayerControlledController>();
        playerController.InitializeShared(playerControlledActionSharedID);
        AddNewAction(playerController);

        if (actionAssetList.Count == 0)
            Debug.Log("Action asset list is empty! Defaulting to EmptyController", this);

        foreach (ActionController action in actionAssetList)
        {
            if (action is IUtilityAction utilityAction)
            {
                AddNewAction(action);
            }
        }

        DeathController deathController = ScriptableObject.CreateInstance<DeathController>();
        deathController.InitializeShared(deathActionSharedID);
        AddNewAction(deathController);

        actionPenalities = new Dictionary<IUtilityAction, float>();
        foreach (IUtilityAction action in actions)
            actionPenalities.Add(action, 0);

        currAction = actionByID[emptyActionSharedID];
    }


    protected virtual void Start()
    {
        eventHub.SendInitializeRequest(stats);
        eventHub.SendAIStateChange(IDByAction[currAction]);
    }

    protected virtual void Update()
    {
        if (currAction == actionByID[deathActionSharedID] || CheckDeath())
        {
            return;
        }

        CalculateStatsAndPenalities();

        IUtilityAction newAction = GetHighestUtilityAction();
        if (newAction != currAction)
        {
            if (showStateChangeLogs)
                Debug.Log("Animal State Change: " + currAction.ActionTag + " => " + newAction.ActionTag);

            currAction.Exit();
            actionPenalities[currAction] += defaultPenality;

            currAction = newAction;
            newAction.Enter();
            eventHub.SendAIStateChange(IDByAction[newAction]);
            Debug.Log(newAction);
        }

        foreach (IUtilityAction action in actions)
        {
            action.AlwaysUpdate();
        }

        currAction.Update();
        interestTracker.OnUpdate();
        preyTracker.OnUpdate();
        fearTracker.OnUpdate();

        actionDebugDisplay = currAction.ActionTag.actionName;
    }

    protected void AddNewAction(ActionController action)
    {
        var instanceObj = Instantiate(action);

        if (instanceObj is not IUtilityAction instance)
        {
            Debug.LogError($"{action.name} does not implement IUtilityAction");
            return;
        }

        if (actionByID.ContainsKey(instance.ActionTag))
        {
            IUtilityAction duplicate = actionByID[instance.ActionTag];
            Debug.LogWarning($"{instance.ActionTag} has duplicate id to existing ai script: " + duplicate);
            return;
        }

        actions.Add(instance);
        instance.OnInstantiate(transform, eventHub, animator, energyDrainRate, saturationDrainRate);

        IDByAction.Add(instance, instance.ActionTag);
        actionByID.Add(instance.ActionTag, instance);
        runtimeActionDebug.Add(instanceObj);
    }

    protected virtual IUtilityAction GetHighestUtilityAction()
    {
        IUtilityAction bestAction = currAction;
        float highscore = -Mathf.Infinity;

        if (isPlayerControlled && actionByID.ContainsKey(playerControlledActionSharedID))
            bestAction = actionByID[playerControlledActionSharedID];
        else
        {
            foreach (IUtilityAction action in actions)
            {
                if ((actionByID.ContainsKey(playerControlledActionSharedID) && action == actionByID[playerControlledActionSharedID]) || action == actionByID[deathActionSharedID])
                    continue;

                float actionScore = action.GetUtilityScore(stats, currAction) - actionPenalities[action];
                if (action == currAction)
                    actionScore += hysteresis;

                if (actionScore > highscore)
                {
                    bestAction = action;
                    highscore = actionScore;
                }

                if (showAIPoints)
                    Debug.Log(action.ActionTag + " " + actionScore);
            }
        }
        return bestAction;
    }

    protected virtual void CalculateStatsAndPenalities()
    {
        currAction.CalculateStats(stats);

        foreach (var key in actionPenalities.Keys.ToList())
        {
            actionPenalities[key] = Mathf.Lerp(actionPenalities[key], 0, penalityDrainSpeed * Time.deltaTime);
        }
    }

    protected float GetRandom(System.Random rand, float min, float max)
    {
        return (float)(rand.NextDouble() * (max - min) + min);
    }

    private bool CheckDeath()
    {
        if (stats.health <= 0)
        {
            Debug.Log(stats.health);
            currAction = actionByID[deathActionSharedID];
            eventHub.SendAIStateChange(deathActionSharedID);
            eventHub.AnnounceDeath();
            actionDebugDisplay = currAction.ActionTag.actionName;
            return true;
        }
        return false;
    }

    public void TakeDamage(int amount)
    {
        if (stats.health > 0)
            stats.health = Math.Clamp(stats.health - amount, 0, stats.maxHealth);
    }

    public float GetHealth() { return stats.health; }

    public void OnSnatchAttachTo(Transform other)
    {
        //temporary - move to movement
        if (snatchTransform != other)
        {
            snatchTransform = other;
            transform.position = new Vector3(snatchTransform.position.x, transform.position.y, snatchTransform.position.z);
            Debug.Log(transform.position + " " + snatchTransform.position);
            Debug.Log("snatched on death!");

            //transform.GetComponent<Collider>().enabled = false;
        }
    }


}

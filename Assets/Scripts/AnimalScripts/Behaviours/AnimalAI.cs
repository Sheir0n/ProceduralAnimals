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
    [SerializeField] TrackerDatas trackersData;
    InterestTracker interestTracker;
    PreyTracker preyTracker;


    //UtilityAction controllers
    protected AnimalAnimator animator;
    protected PathfindController pathfindController;

    //UtilityAI Actions
    protected List<IUtilityAction> actions = new List<IUtilityAction>();
    protected IUtilityAction currAction;

    public enum AIAction { EmptyController, PlayerControlled, FindRestSpot, Rest, Wander, ChaseFood, Death };
    [SerializeField] protected float defaultPenality = 1f;
    [SerializeField] protected float penalityDrainSpeed = 4f;
    [SerializeField] protected float hysteresis = 0.1f;

    [SerializeField, ReadOnly] protected Dictionary<IUtilityAction, float> actionPenalities;
    [SerializeField, ReadOnly] protected AIAction actionDebugDisplay;

    protected Dictionary<AIAction, IUtilityAction> actionByEnum;
    protected Dictionary<IUtilityAction, AIAction> enumByAction;

    [SerializeField] private bool isPlayerControlled = false;
    [SerializeField] private bool showAIPoints = false;

    protected AnimalEventHub eventHub;
    private Transform snatchTransform;

    [SerializeField] protected List<ActionController> actionAssetList = new List<ActionController>();
    [SerializeField] protected AIAction defaultAction = AIAction.EmptyController;

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
    }


    private void LoadActionList()
    {
        actionByEnum = new Dictionary<AIAction, IUtilityAction>();
        enumByAction = new Dictionary<IUtilityAction, AIAction>();

        AddNewAction(ScriptableObject.CreateInstance<EmptyController>(), AIAction.EmptyController);

        if (actionAssetList.Count == 0)
            Debug.Log("Action asset list is empty! Defaulting to EmptyController", this);

        foreach (ActionController action in actionAssetList)
        {
            if (action is IUtilityAction utilityAction)
            {
                AddNewAction(utilityAction, utilityAction.AIAction);
            }
        }

        AddNewAction(ScriptableObject.CreateInstance<DeathController>(), AIAction.Death);

        actionPenalities = new Dictionary<IUtilityAction, float>();
        foreach (IUtilityAction action in actions)
            actionPenalities.Add(action, 0);

        if (actionByEnum.TryGetValue(defaultAction, out var defaultActionInstance))
        {
            currAction = defaultActionInstance;
        }
        else
        {
            defaultAction = AIAction.EmptyController;
            currAction = actionByEnum[defaultAction];
            Debug.Log("Specified default action not present, defaulting to EmptyController.", this);
        }
    }

    protected virtual void Start()
    {
        eventHub.SendInitializeRequest(stats);
        eventHub.SendAIStateChange(enumByAction[currAction]);
    }

    protected virtual void Update()
    {
        if (currAction == actionByEnum[AIAction.Death] || CheckDeath())
        {
            return;
        }

        CalculateStatsAndPenalities();

        IUtilityAction newAction = GetHighestUtilityAction();
        if (newAction != currAction)
        {

            if (showStateChangeLogs)
                Debug.Log("Animal State Change: " + currAction.DebugName() + " => " + newAction.DebugName());
            currAction.Exit();
            actionPenalities[currAction] += defaultPenality;

            currAction = newAction;
            newAction.Enter();
            eventHub.SendAIStateChange(enumByAction[newAction]);
            actionDebugDisplay = enumByAction[newAction];
        }

        foreach (IUtilityAction action in actions)
        {
            action.AlwaysUpdate();
        }

        currAction.Update();
        interestTracker.OnUpdate();
        preyTracker.OnUpdate();
    }

    protected void AddNewAction(IUtilityAction action, AIAction actionEnum)
    {
        var instance = Instantiate((ScriptableObject)action) as IUtilityAction;
        actions.Add(instance);
        instance.OnInstantiate(transform, eventHub, animator, energyDrainRate, saturationDrainRate);
        enumByAction.Add(instance, actionEnum);
        actionByEnum.Add(actionEnum, instance);
    }

    protected virtual IUtilityAction GetHighestUtilityAction()
    {
        IUtilityAction bestAction = currAction;
        float highscore = -Mathf.Infinity;

        if (isPlayerControlled && actionByEnum.ContainsKey(AIAction.PlayerControlled))
        {
            bestAction = actionByEnum[AIAction.PlayerControlled];
        }
        else
        {
            foreach (IUtilityAction action in actions)
            {
                if ((actionByEnum.ContainsKey(AIAction.PlayerControlled) && action == actionByEnum[AIAction.PlayerControlled]) || action == actionByEnum[AIAction.Death])
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
                    Debug.Log(action.DebugName() + " " + actionScore);
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
            currAction = actionByEnum[AIAction.Death];
            eventHub.SendAIStateChange(AIAction.Death);
            actionDebugDisplay = AIAction.Death;
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

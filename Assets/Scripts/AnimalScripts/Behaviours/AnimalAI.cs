using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AnimalAI : MonoBehaviour, IDamageable
{
    [Header("Statystyki domyœlne agenta")]
    [SerializeField] protected AnimalStats stats;
    protected readonly float energyDrainRate = 0.075f;
    protected readonly float saturationDrainRate = 0.045f;

    [Header("Poka¿ logi zmiany zachowania")]
    [SerializeField] private bool showStateChangeLogs = false;

    [Header("Scriptable listy œledzonych obiektów")]
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

    [Header("Ustawienia histerezy przy zmianie stanu")]
    [SerializeField] protected float defaultPenality = 1f;
    [SerializeField] protected float penalityDrainSpeed = 4f;
    [SerializeField] protected float hysteresis = 0.1f;

    protected Dictionary<IUtilityAction, float> actionPenalities;

    protected Dictionary<ActionID, IUtilityAction> actionByID;
    protected Dictionary<IUtilityAction, ActionID> IDByAction;

    protected AnimalEventHub eventHub;
    private Transform snatchTransform;

    [Header("Szybkoœæ straty ¿ycia przy œmierci g³odowej")]
    [SerializeField] private float StarveDmgPerSec = 0.2f;

    [Header("Czas œmierci agenta")]
    [SerializeField] private float DeathDurationInSec = 10f;

    [Header("ID zachowañ statycznych (ogólnodostêpnych w ka¿dym osobniku)")]
    [SerializeField] protected List<ActionController> actionAssetList = new List<ActionController>();
    [SerializeField] protected ActionID emptyActionSharedID;
    [SerializeField] protected ActionID deathActionSharedID;
    [SerializeField] protected ActionID playerControlledActionSharedID;

    [Header("Runtime Debug: Ustawienia komunikatów")]
    [SerializeField] private bool isPlayerControlled = false;
    [SerializeField] private bool showAIPoints = false;

    [Header("Runtime Debug: Aktualny stan zachowania")]
    [SerializeField] protected ActionID actionDebugDisplay;

    [Header("Runtime Debug: Lista sklonowanych zachowañ")]
    [SerializeField] private List<ScriptableObject> runtimeActionDebug;

    private bool startedDeath = false;

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        animator = GetComponent<AnimalAnimator>();
        pathfindController = GetComponent<PathfindController>();

        stats.GenerateStats(name);

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
            Debug.LogError("AnimalAI: Lista zachowañ jest pusta!", this);

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

        if (emptyActionSharedID != null)
            currAction = actionByID[emptyActionSharedID];
    }

    protected virtual void Start()
    {
        eventHub.SendInitializeRequest(stats);
        if (currAction != null)
            eventHub.SendAIStateChange(IDByAction[currAction]);
    }

    protected virtual void Update()
    {
        if (currAction == null || currAction == actionByID[deathActionSharedID] || CheckDeath())
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
        }

        foreach (IUtilityAction action in actions)
        {
            action.AlwaysUpdate();
        }

        currAction.Update();
        interestTracker.OnUpdate();
        preyTracker.OnUpdate();
        fearTracker.OnUpdate();

        if (stats.saturation <= 0f)
            Starve();
        actionDebugDisplay = currAction.ActionTag;
    }

    protected void LateUpdate()
    {
        if (currAction == null)
            return;
        if (IDByAction[currAction] == deathActionSharedID && !startedDeath)
        {
            startedDeath = true;
            if(stats.saturation <= 0f)
                Debug.Log("Œmieræ g³odowa agenta: " + name);
            else
                Debug.Log("Œmieræ agenta: " + name);

            _ = DeathDelayAsync(intervalSec: 0.1f);
        }
    }

    protected void AddNewAction(ActionController action)
    {
        var instanceObj = Instantiate(action);

        if (instanceObj is not IUtilityAction instance)
        {
            Debug.LogError($"AnimalAI: {action.name} nie implementuje IUtilityAction");
            return;
        }

        if (instance.ActionTag == null)
        {
            Debug.LogError($"AnimalAI: {action.name} Nie ma podano tagu zachowania!", this);
            return;
        }

        if (actionByID.ContainsKey(instance.ActionTag))
        {
            IUtilityAction duplicate = actionByID[instance.ActionTag];
            Debug.LogWarning($"AnimalAI: {instance.ActionTag} jest ma zduplikowany tag z przypisanym ju¿ skryptem: " + duplicate, this);
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
            currAction = actionByID[deathActionSharedID];
            eventHub.SendAIStateChange(deathActionSharedID);
            eventHub.AnnounceDeath();
            actionDebugDisplay = currAction.ActionTag;
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
        if (snatchTransform != other)
        {
            snatchTransform = other;
            transform.position = new Vector3(snatchTransform.position.x, transform.position.y, snatchTransform.position.z);
        }
    }

    private async Task DeathDelayAsync(float intervalSec)
    {
        float elapsed = 0f;
        float intervalTime = 0f;

        while (elapsed < DeathDurationInSec)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= intervalTime)
            {
                intervalTime += intervalSec;
                eventHub.DeathFade(elapsed / DeathDurationInSec);
            }
            await Task.Yield();
        }
        Destroy(gameObject);
    }

    protected void Starve()
    {
        if(stats.health > 0)
            stats.health -= Time.deltaTime * StarveDmgPerSec;
    }

    private void OnDestroy()
    {
        stats = null;
        trackersData = null;
        animator = null;
        pathfindController = null;
        actions = null;
        actionByID = null;
        IDByAction = null;
        actionPenalities = null;
        snatchTransform = null;
    }
}

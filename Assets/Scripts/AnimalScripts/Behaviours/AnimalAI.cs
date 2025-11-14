using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UI;
using UnityEngine;

public interface IReadOnlyAnimalStats
{
    float Health { get; }
    float Saturation { get; }
    float Energy { get; }

    float MaxHealth { get; }
    float MaxSaturation { get; }
    float MaxEnergy { get; }

    float StatVigor { get; }
    float StatAggressiveness { get; }
    float StatCuriosity { get; }
    float StatDominance { get; }
}


[System.Serializable]
public class AnimalStats : IReadOnlyAnimalStats
{
    [Header("General Variables")]
    public float health;
    public float saturation;
    public float energy;

    [Header("Variable limits")]
    public float maxHealth;
    public float maxSaturation;
    public float maxEnergy;

    [Header("Behaviour modifiers (0-1)")]
    [Range(0.01f, 1)] public float statVigor;
    [Range(0.01f, 1)] public float statAggressiveness;
    [Range(0.01f, 1)] public float statCuriosity;
    [Range(0.01f, 1)] public float statDominance;

    float IReadOnlyAnimalStats.Health => health;
    float IReadOnlyAnimalStats.Saturation => saturation;
    float IReadOnlyAnimalStats.Energy => energy;

    float IReadOnlyAnimalStats.MaxHealth => maxHealth;
    float IReadOnlyAnimalStats.MaxSaturation => maxSaturation;
    float IReadOnlyAnimalStats.MaxEnergy => maxEnergy;

    float IReadOnlyAnimalStats.StatVigor => statVigor;
    float IReadOnlyAnimalStats.StatAggressiveness => statAggressiveness;
    float IReadOnlyAnimalStats.StatCuriosity => statCuriosity;
    float IReadOnlyAnimalStats.StatDominance => statDominance;
}

public class AnimalAI : MonoBehaviour
{
    [SerializeField] protected float statMultiplierMaxRandomness = 0.2f;
    [SerializeField] protected AnimalStats stats;
    [SerializeField] protected int staticSeed = 12345;

    [Header("Use predetermined or generate new seed")]
    [SerializeField] protected bool useStaticSeed = true;
    [Header("Use custom stats instead of seed")]
    [SerializeField] protected bool ignoreSeed = true;
    private int seed = 0;
    public AnimalStats Stats => stats;
    public int Seed => seed;

    [Header("Show StateChange logs")]
    [SerializeField] private bool showStateChangeLogs = false;


    //UtilityAction controllers
    [SerializeField] private AnimalAnimator animator;
    [SerializeField] private PathfindController pathfindController;

    //UtilityAI Actions
    protected List<IUtilityAction> actions = new List<IUtilityAction>();
    IUtilityAction currAction;

    
    public enum AIAction { PlayerControlled, Rest, Wander };
    [SerializeField] protected float defaultPenality = 1f;
    [SerializeField] protected float penalityDrainSpeed = 4f;
    [SerializeField] protected float hysteresis = 0.1f;

    [SerializeField, ReadOnly] protected Dictionary<IUtilityAction, float> actionPenalities;
    [SerializeField, ReadOnly] private AIAction actionDebugDisplay;

    Dictionary<AIAction, IUtilityAction> actionByEnum = new Dictionary<AIAction, IUtilityAction>();
    Dictionary<IUtilityAction, AIAction> enumByAction = new Dictionary<IUtilityAction, AIAction>();

    [SerializeField] private bool isPlayerControlled = false;

    //observer handling
    protected List<IAnimalObserver> observers = new List<IAnimalObserver>();
    //TODO: remove observers on death

    protected virtual void Awake()
    {
        GenerateStats();

        actionByEnum = new Dictionary<AIAction, IUtilityAction>();
        enumByAction = new Dictionary<IUtilityAction, AIAction>();
        AddNewAction(new PlayerControlledController(pathfindController, animator), AIAction.PlayerControlled);
        AddNewAction(new RestController(pathfindController, animator), AIAction.Rest);
        AddNewAction(new WanderController(pathfindController, animator), AIAction.Wander);

        actionPenalities = new Dictionary<IUtilityAction, float>();
        foreach (IUtilityAction action in actions)
            actionPenalities.Add(action, 0);

        currAction = actions[(int)AIAction.Rest];
        actionDebugDisplay = AIAction.Rest;

        RegisterObserver(animator);
        RegisterObserver(pathfindController);

        foreach (var observer in observers)
            observer.OnAnimalAIInitialize(stats);
    }

    protected virtual void Start()
    {
        foreach (var observer in observers)
            observer.OnActionChanged(enumByAction[currAction]);
    }

    protected virtual void Update()
    {
        CalculateStats();

        IUtilityAction newAction = GetHighestUtilityAction();
        if(newAction != currAction)
        {
            actionDebugDisplay = enumByAction[newAction];

            if (showStateChangeLogs)
                Debug.Log("Animal State Change: " + currAction.DebugName() + " => " + newAction.DebugName());
            currAction.Exit();
            actionPenalities[currAction] += defaultPenality;

            foreach (var observer in observers)
                observer.OnActionChanged(enumByAction[newAction]);

            currAction = newAction;
            newAction.Enter();
            newAction.Update();
        }
    }

    protected void AddNewAction(IUtilityAction action, AIAction actionEnum)
    {
        actions.Add(action);
        enumByAction.Add(action, actionEnum);
        actionByEnum.Add(actionEnum, action);
    }

    protected void GenerateStats()
    {
        if (!ignoreSeed)
        {
            if (useStaticSeed)
            {
                seed = staticSeed;
                Debug.Log("Created new animal with static seed: " + seed);
            }
            else
            {
                seed = UnityEngine.Random.Range(1, 99999);
                Debug.Log("Created new animal with seed: " + seed);
            }

            System.Random rng = new System.Random(seed);
            stats.statVigor = GetRandom(rng, 0.01f, 1);
            stats.statAggressiveness = GetRandom(rng, 0.01f, 1);
            stats.statCuriosity = GetRandom(rng, 0.01f, 1);
            stats.statDominance = GetRandom(rng, 0.01f, 1);

            stats.maxHealth *= 1 + statMultiplierMaxRandomness * GetRandom(rng, -1, 1);
            stats.maxSaturation *= 1 + statMultiplierMaxRandomness * GetRandom(rng, -1, 1);
            stats.maxEnergy *= 1 + statMultiplierMaxRandomness * GetRandom(rng, -1, 1);
        }
        else
        {
            Debug.Log("Created new animal without seed");
            seed = staticSeed;
        }

        stats.health = stats.maxHealth;
        stats.saturation = stats.maxSaturation;
        stats.energy = stats.maxEnergy;

        Debug.Log(
            $"=== Stats ===\n" +
            $"Vigor: {stats.statVigor:F2}\n" +
            $"Aggressiveness: {stats.statAggressiveness:F2}\n" +
            $"Curiosity: {stats.statCuriosity:F2}\n" +
            $"Dominance: {stats.statDominance:F2}"
        );
    }


    protected virtual IUtilityAction GetHighestUtilityAction()
    {
        IUtilityAction bestAction = actionByEnum[AIAction.Rest];
        float highscore = 0;

        if (isPlayerControlled)
        {
            bestAction = actions[(int)AIAction.PlayerControlled];
        }
        else
        {
            foreach (IUtilityAction action in actions)
            {
                float actionScore = action.GetUtilityScore(stats, currAction) - actionPenalities[action];
                if (action == currAction)
                    actionScore += hysteresis;

                if (actionScore > highscore)
                {
                    bestAction = action;
                    highscore = actionScore;
                }
            }
        }

        return bestAction;
    }

    protected virtual void CalculateStats()
    {
        currAction.CalculateStats(stats);

        foreach (var key in actionPenalities.Keys.ToList()) 
        {
            actionPenalities[key] = Mathf.Lerp(actionPenalities[key], 0, penalityDrainSpeed * Time.deltaTime);
        }
    }

    public void RegisterObserver(IAnimalObserver observer) => observers.Add(observer);
    public void UnregisterObserver(IAnimalObserver observer) => observers.Remove(observer);

    protected float GetRandom(System.Random rand, float min, float max)
    {
        return (float)(rand.NextDouble() * (max - min) + min);
    }
}

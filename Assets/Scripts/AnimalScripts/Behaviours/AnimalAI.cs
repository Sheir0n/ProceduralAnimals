using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.AI;

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

public class AnimalAI : MonoBehaviour, IDamageable
{
    [SerializeField] protected float statMultiplierMaxRandomness = 0.2f;
    [SerializeField] protected AnimalStats stats;

    [Header("Use predetermined or generate new seed")]
    [SerializeField] protected bool useStaticSeed = true;
    [Header("Use custom stats instead of seed")]
    [SerializeField] protected bool ignoreSeed = true;
    [SerializeField] private int seed = 12345;

    [Header("Show StateChange logs")]
    [SerializeField] private bool showStateChangeLogs = false;


    //UtilityAction controllers
    protected AnimalAnimator animator;
    protected PathfindController pathfindController;

    //UtilityAI Actions
    protected List<IUtilityAction> actions = new List<IUtilityAction>();
    protected IUtilityAction currAction;

    public enum AIAction { PlayerControlled, FindRestSpot, Rest, Wander, ChaseFood, Death };
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

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        animator = GetComponent<AnimalAnimator>();
        pathfindController = GetComponent<PathfindController>();

        GenerateStats();

        actionByEnum = new Dictionary<AIAction, IUtilityAction>();
        enumByAction = new Dictionary<IUtilityAction, AIAction>();

        AddNewAction(new DeathController(pathfindController, animator), AIAction.Death);
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

        CalculateStats();

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
                Debug.Log("Created new animal with static seed: " + seed);
            }
            else
            {
                seed = UnityEngine.Random.Range(1, 99999);
                Debug.Log("Created new animal with seed: " + seed);
            }

            System.Random rng = new System.Random(seed);
            stats.statVigor = GetRandom(rng, 0.01f, 0.99f);
            stats.statAggressiveness = GetRandom(rng, 0.01f, 0.99f);
            stats.statCuriosity = GetRandom(rng, 0.01f, 0.99f);
            stats.statDominance = GetRandom(rng, 0.01f, 0.99f);

            stats.maxHealth *= 1 + statMultiplierMaxRandomness * GetRandom(rng, -1, 1);
            stats.maxSaturation *= 1 + statMultiplierMaxRandomness * GetRandom(rng, -1, 1);
            stats.maxEnergy *= 1 + statMultiplierMaxRandomness * GetRandom(rng, -1, 1);
        }
        else
        {
            Debug.Log("Created new animal without seed");
            seed = 0;
        }

        stats.health = stats.maxHealth;
        stats.saturation = stats.maxSaturation;
        stats.energy = stats.maxEnergy;

        //Debug.Log(
        //    $"=== Stats ===\n" +
        //    $"Vigor: {stats.statVigor:F2}\n" +
        //    $"Aggressiveness: {stats.statAggressiveness:F2}\n" +
        //    $"Curiosity: {stats.statCuriosity:F2}\n" +
        //    $"Dominance: {stats.statDominance:F2}"
        //);
    }


    protected virtual IUtilityAction GetHighestUtilityAction()
    {
        IUtilityAction bestAction = currAction;
        float highscore = 0;

        if (isPlayerControlled)
        {

            bestAction = actionByEnum[AIAction.PlayerControlled];
        }
        else
        {
            foreach (IUtilityAction action in actions)
            {
                if (action == actionByEnum[AIAction.PlayerControlled] || action == actionByEnum[AIAction.Death])
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

    protected virtual void CalculateStats()
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

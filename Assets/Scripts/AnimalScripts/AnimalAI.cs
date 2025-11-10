using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;

[System.Serializable]
public class AnimalStats
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
}

public class AnimalAI : MonoBehaviour
{
    [SerializeField] private float statMultiplierMaxRandomness = 0.2f;
    [SerializeField] private AnimalStats stats;
    [SerializeField] private int staticSeed = 12345;

    [Header("Use predetermined or generate new seed")]
    [SerializeField] private bool useStaticSeed = true;
    [Header("Use custom stats instead of seed")]
    [SerializeField] private bool ignoreSeed = true;
    private int seed = 0;

    [Header("Show StateChange logs")]
    [SerializeField] private bool showStateChangeLogs = false;


    //UtilityAI Actions
    private enum AIAction { Rest, Wander };
    [SerializeField, ReadOnly] private AIAction currAction = AIAction.Rest;
    [SerializeField] private float defaultPenality = 1f;
    [SerializeField] private float penalityDrainSpeed = 4f;
    [SerializeField, ReadOnly] private List<float> actionPenalities;
    [SerializeField] private float hysteresis = 0.1f;



    void Awake()
    {
        GenerateStats();
        actionPenalities = new List<float>() { 0, 0 };
    }

    void Update()
    {
        CalculateStats();
        GetHighestUtilityAction();
    }

    private void GenerateStats()
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
                seed = Random.Range(1, 99999);
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

    private void CalculateStats()
    {
        switch (currAction)
        {
            case AIAction.Rest:
                float defaultEnergyRegen = 1;
                stats.energy = Mathf.Clamp(stats.energy + (stats.statVigor - 0.5f + defaultEnergyRegen) * Time.deltaTime, 0, stats.maxEnergy);
                break;
            case AIAction.Wander:
                float defaultEnergyDrain = 0.15f;
                stats.energy = Mathf.Clamp(stats.energy - (0.5f * (1 - stats.statVigor) + defaultEnergyDrain) * Time.deltaTime, 0, stats.maxEnergy);
                break;
        }

        for (int i = 0; i < actionPenalities.Count; i++)
        {
            actionPenalities[i] = Mathf.Lerp(actionPenalities[i], 0, penalityDrainSpeed * Time.deltaTime);
        }
    }

    private void GetHighestUtilityAction()
    {
        AIAction newBestAction = AIAction.Rest;
        float highestNeed = 0f;

        float restUtility = GetRestUtility();
        float wanderUtility = GetWanderUtility();

        switch (currAction)
        {
            case AIAction.Rest:
                restUtility += hysteresis;
                break;
            case AIAction.Wander:
                wanderUtility += hysteresis;
                break;
        }


        if (restUtility > highestNeed)
        {
            highestNeed = restUtility;
            newBestAction = AIAction.Rest;
        }
        if (wanderUtility > highestNeed)
        {
            highestNeed = restUtility;
            newBestAction = AIAction.Wander;
        }

        if (newBestAction != currAction)
        {
            if (showStateChangeLogs)
                Debug.Log("Changing AI State: " + currAction + " => " + newBestAction);

            actionPenalities[(int)currAction] += defaultPenality;
            currAction = newBestAction;
        }
    }

    private float GetRestUtility()
    {
        float normalizedEnergy = stats.energy / stats.maxEnergy;
        float baseUtility;

        if (currAction == AIAction.Rest)
            baseUtility = Mathf.Pow(1 - normalizedEnergy, 0.5f * stats.statVigor);
        else
            baseUtility = Mathf.Pow(1 - normalizedEnergy, 6f * (1 - stats.statVigor/2));
        return baseUtility - actionPenalities[(int)AIAction.Rest];
    }

    private float GetWanderUtility()
    {
        return Mathf.Pow(stats.energy / stats.maxEnergy, 2f) - actionPenalities[(int)AIAction.Wander];
    }


    private float GetRandom(System.Random rand, float min, float max)
    {
        return (float)(rand.NextDouble() * (max - min) + min);
    }

    public AnimalStats Stats => stats;
    public int Seed => seed;
}

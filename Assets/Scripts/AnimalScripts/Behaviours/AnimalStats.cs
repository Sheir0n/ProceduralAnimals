using System.Collections;
using System.Collections.Generic;
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
    float StatBravery { get; }
}


[System.Serializable]
public class AnimalStats : IReadOnlyAnimalStats
{
    public StatData baseMaxStats;

    [Header("General Variables")]
    public float health;
    public float saturation;
    public float energy;

    [Header("Variable limits")]
    public float maxHealth;
    public float maxSaturation;
    public float maxEnergy;

    [Header("Behaviour modifiers (0-1)")]
    [Range(0.01f, 0.99f)] public float statVigor;
    [Range(0.01f, 0.99f)] public float statAggressiveness;
    [Range(0.01f, 0.99f)] public float statCuriosity;
    [Range(0.01f, 0.99f)] public float statBravery;

    float IReadOnlyAnimalStats.Health => health;
    float IReadOnlyAnimalStats.Saturation => saturation;
    float IReadOnlyAnimalStats.Energy => energy;

    float IReadOnlyAnimalStats.MaxHealth => maxHealth;
    float IReadOnlyAnimalStats.MaxSaturation => maxSaturation;
    float IReadOnlyAnimalStats.MaxEnergy => maxEnergy;

    float IReadOnlyAnimalStats.StatVigor => statVigor;
    float IReadOnlyAnimalStats.StatAggressiveness => statAggressiveness;
    float IReadOnlyAnimalStats.StatCuriosity => statCuriosity;
    float IReadOnlyAnimalStats.StatBravery => statBravery;


    public bool ignoreSeed = false;
    public bool useStaticSeed = false;
    [Range(0f, 99999f)]
    public int seed = 12345;

    public void GenerateStats()
    {
        maxHealth = baseMaxStats.maxHealth;
        maxSaturation = baseMaxStats.maxSaturation;
        maxEnergy = baseMaxStats.maxEnergy;

        float randomnessMultiplier = baseMaxStats.randomnessAmount;

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
            statVigor = GetRandom(rng, 0.01f, 0.99f);
            statAggressiveness = GetRandom(rng, 0.01f, 0.99f);
            statCuriosity = GetRandom(rng, 0.01f, 0.99f);
            statBravery = GetRandom(rng, 0.01f, 0.99f);

            maxHealth *= 1 + randomnessMultiplier * GetRandom(rng, -1, 1);
            maxSaturation *= 1 + randomnessMultiplier * GetRandom(rng, -1, 1);
            maxEnergy *= 1 + randomnessMultiplier * GetRandom(rng, -1, 1);
        }
        else
        {
            Debug.Log("Created new animal without seed");
            seed = 0;
        }

        health = maxHealth;
        saturation = maxSaturation;
        energy = maxEnergy;
    }

    protected float GetRandom(System.Random rand, float min, float max)
    {
        return (float)(rand.NextDouble() * (max - min) + min);
    }
}
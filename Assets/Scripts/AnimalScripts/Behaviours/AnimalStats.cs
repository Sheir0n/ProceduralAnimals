using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
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
    [Header("Statystyki bazowe")]
    public StatData baseMaxStats;

    [Header("Statyczne parametry zmienne")]
    public float health;
    public float saturation;
    public float energy;

    [Header("Statyczne zakresy parametrów zmiennych")]
    public float maxHealth;
    public float maxSaturation;
    public float maxEnergy;

    [Header("Statyczne modyfikatory zachowania")]
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

    [Header("Ustawienia statycznego generowania statystyk")]
    public bool ignoreSeed = false;
    public bool useStaticSeed = false;
    [Range(0f, 99999f)]
    public int seed = 12345;

    public void GenerateStats()
    {
        float randomnessMultiplier;
        if (baseMaxStats == null)
        {
            Debug.LogWarning("AnimalStats: Nie przypisano bazowych statystyk zwierzêcia!");
            maxHealth = 1;
            maxSaturation = 1;
            maxEnergy = 1;
            randomnessMultiplier = 0;
        }
        else
        {
            maxHealth = baseMaxStats.maxHealth;
            maxSaturation = baseMaxStats.maxSaturation;
            maxEnergy = baseMaxStats.maxEnergy;
            randomnessMultiplier = baseMaxStats.randomnessAmount;
        }

        if (!ignoreSeed)
        {
            if (!useStaticSeed)
                seed = UnityEngine.Random.Range(1, 99999);

            System.Random rng = new System.Random(seed);
            statVigor = GetRandom(rng, 0.01f, 0.99f);
            statAggressiveness = GetRandom(rng, 0.01f, 0.99f);
            statCuriosity = GetRandom(rng, 0.01f, 0.99f);
            statBravery = GetRandom(rng, 0.01f, 0.99f);

            maxHealth *= 1 + randomnessMultiplier * GetRandom(rng, -1, 1);
            maxSaturation *= 1 + randomnessMultiplier * GetRandom(rng, -1, 1);
            maxEnergy *= 1 + randomnessMultiplier * GetRandom(rng, -1, 1);

            Debug.Log(
                "Utworzono nowe zwierze z parametrami:\n" +
                "Seed: " + seed + "\n" +
                "Wigor: " + statVigor + "\n" +
                "Agresja: " + statAggressiveness + "\n" +
                "Ciekawoœæ: " + statCuriosity + "\n" +
                "Odwaga: " + statBravery
            );
        }
        else
        {
            Debug.Log(
                    "Utworzono nowe zwierze bez wykorzystania seeda:\n" +
                    "Seed: null\n" +
                    "Wigor: " + statVigor + "\n" +
                    "Agresja: " + statAggressiveness + "\n" +
                    "Ciekawoœæ: " + statCuriosity + "\n" +
                    "Odwaga: " + statBravery
                );
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
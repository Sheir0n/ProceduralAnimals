using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class RestController : ScriptableObject, IUtilityAction
{
    private AnimalEventHub eventHub;

    private float saturationDrainRate = 0;


    [Header("Rest settings")]
    [SerializeField] private float energyRegenRate = 0.5f;
    [SerializeField] private float restHealthRegenRate = 0.05f;
    [SerializeField] private float healthRegenSaturationDrain = 0.05f;
    [SerializeField] private float saturationRegenThreshold = 0.5f;

    bool applyRestPenality = false;

    const float restPenalityDefaultModifier = 0.5f;
    private float currentRestModifier = 1f;

    public void OnInstantiate(Transform transform, AnimalEventHub eventHub, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate)
    {
        this.eventHub = eventHub;
        this.saturationDrainRate = saturationDrainRate;
    }
    public string DebugName() => "Rest";
    public void Enter()
    {
        applyRestPenality = !eventHub.IsOnRestSpot();
        if (applyRestPenality)
        {
            Debug.Log("resting with penality");
            currentRestModifier = restPenalityDefaultModifier;
        }
        else
        {
            Debug.Log("resting without penality");
            currentRestModifier = 1f;
        }
    }

    public void Update() { }
    public void AlwaysUpdate() { }
    public void Exit() { }
    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        float normalizedEnergy = stats.energy / stats.maxEnergy;
        float normalizedSaturation = stats.saturation / stats.maxSaturation;
        float normalizedHealth = stats.health / stats.maxHealth;

        float utilityScore;
        if (stats.energy < 0.1)
            utilityScore = 1;
        else if (currAction == this && !applyRestPenality && normalizedSaturation > saturationRegenThreshold && normalizedHealth < 0.75f)
            utilityScore = Mathf.Pow(1 - normalizedHealth, 1f);
        else if (currAction == this || eventHub.IsOnRestSpot())
            utilityScore = Mathf.Pow(1 - normalizedEnergy, (3f + 4 * stats.statVigor) / 5) * 4 / 5;
        else
        {
            utilityScore = 1 - ((5 + stats.statVigor) * normalizedEnergy);
        }

        return utilityScore;
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy + (0.75f + 0.25f * stats.statVigor) * energyRegenRate * currentRestModifier * Time.deltaTime, 0, stats.maxEnergy);

        float normalizedSaturation = stats.saturation / stats.maxSaturation;
        float normalisedHealth = stats.health / stats.maxHealth;
        if (!applyRestPenality && normalizedSaturation > saturationRegenThreshold && normalisedHealth < 1f)
        {
            stats.health = Mathf.Clamp(stats.health + restHealthRegenRate * Time.deltaTime, 0, stats.maxHealth);
            stats.saturation = Mathf.Clamp(stats.saturation - (healthRegenSaturationDrain + saturationDrainRate) * Time.deltaTime, 0, stats.maxSaturation);
        }
        else
            stats.saturation = Mathf.Clamp(stats.saturation - saturationDrainRate * Time.deltaTime, 0, stats.maxSaturation);
    }
}

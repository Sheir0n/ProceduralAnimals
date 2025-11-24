using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class RestController : IUtilityAction
{
    private readonly PathfindController controller;
    private readonly AnimalAnimator animator;
    private AnimalEventHub eventHub;

    private float energyRegenRate = 0;
    private float restHealthRegenRate = 0;
    private float saturationDrainRate = 0;
    private float healthRegenSaturationDrain = 0;
    private float saturationRegenThreshold = 0;
    bool applyRestPenality = false;

    const float restPenalityDefaultModifier = 0.5f;
    private float currentRestModifier = 1f;

    public RestController(PathfindController controller, AnimalAnimator animator, AnimalEventHub eventHub, float energyRegenRate, float saturationDrainRate, float restHealthRegenRate, float healthRegenSaturationDrain, float saturationRegenThreshold)
    {
        this.controller = controller;
        this.animator = animator;
        this.eventHub = eventHub;
        this.energyRegenRate = energyRegenRate;
        this.saturationDrainRate = saturationDrainRate;
        this.restHealthRegenRate = restHealthRegenRate;
        this.healthRegenSaturationDrain = healthRegenSaturationDrain;
        this.saturationRegenThreshold = saturationRegenThreshold;
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
            utilityScore = Mathf.Pow(1 - normalizedHealth, 0.1f) * 2 / 3;
        else if (currAction == this || eventHub.IsOnRestSpot())
            utilityScore = Mathf.Pow(1 - normalizedEnergy, 0.1f) * 2 / 3;
        else
        {
            utilityScore = 1 - ((5 + stats.statVigor) * normalizedEnergy);
        }

        return utilityScore;
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy + (stats.statVigor - 0.5f + energyRegenRate) * currentRestModifier * Time.deltaTime, 0, stats.maxEnergy);

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

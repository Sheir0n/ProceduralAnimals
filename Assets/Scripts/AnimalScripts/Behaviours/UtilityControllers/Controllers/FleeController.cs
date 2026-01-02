using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FleeController", menuName = "AI/Actions/FleeController")]
public class FleeController : ActionController, IUtilityAction
{
    private AnimalEventHub eventHub;

    [Header("Drain rate modifiers")]
    [SerializeField] private float energyDrainRateModifier = 1;
    [SerializeField] private float saturationDrainRateModifier = 1;

    public ActionID ActionTag => actionID;

    TrackedWithScore highestFear;

    public void OnInstantiate(Transform transform, AnimalEventHub eventHub, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate)
    {
        this.energyDrainRate = energyDrainRate;
        this.saturationDrainRate = saturationDrainRate;
        this.eventHub = eventHub;
        highestFear = new TrackedWithScore(null, 0);
    }

    public void Enter() { }
    public void Update() { }
    public void AlwaysUpdate()
    {
        highestFear = eventHub.RequestTrackedFear();
    }

    public void Exit() { }

    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        float fleeTargetScore = highestFear.score;
        Transform targetTransform = highestFear.tracked;
        if (targetTransform == null)
            return -Mathf.Infinity;
        else
            return Mathf.Pow(fleeTargetScore, 0.5f + 0.5f * stats.statBravery) - 0.01f;
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy - (0.75f + 0.25f * (1 - stats.statVigor)) * energyDrainRate * energyDrainRateModifier * Time.deltaTime, 0, stats.maxEnergy);

        stats.saturation = Mathf.Clamp(stats.saturation - saturationDrainRate * saturationDrainRateModifier * Time.deltaTime, 0, stats.maxSaturation);
    }
}

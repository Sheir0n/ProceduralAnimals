using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaseFoodController : IUtilityAction
{
    private readonly PathfindController controller;
    private readonly AnimalAnimator animator;
    private readonly AnimalEventHub eventHub;
    private float energyDrainRate = 0;
    private float saturationDrainRate = 0;

    public ChaseFoodController(PathfindController controller, AnimalAnimator animator, AnimalEventHub eventHub, float energyDrainRate, float saturationDrainRate)
    {
        this.controller = controller;
        this.animator = animator;
        this.eventHub = eventHub;
        this.energyDrainRate = energyDrainRate;
        this.saturationDrainRate = saturationDrainRate;
    }

    public string DebugName() => "Chase Food";

    public void Enter() { }
    public void Update() { }
    public void Exit() { }
    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        Transform huntTarget = eventHub.FindNearestHuntTarget();
        if (huntTarget == null)
            return 0;
        else
        {
            float normalisedSaturation = stats.saturation / stats.maxSaturation;
            return Mathf.Pow(1 - normalisedSaturation, (1 - stats.statAggressiveness) / 3) * 2 / 3;
        }
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy - (0.75f + 0.25f * (1 - stats.statVigor)) * energyDrainRate * Time.deltaTime, 0, stats.maxEnergy);

        stats.saturation = Mathf.Clamp(stats.saturation - saturationDrainRate * Time.deltaTime, 0, stats.maxSaturation);
    }
}

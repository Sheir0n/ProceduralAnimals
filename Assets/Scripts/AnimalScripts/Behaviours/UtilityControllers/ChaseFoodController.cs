using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaseFoodController : IUtilityAction
{
    private readonly PathfindController controller;
    private readonly AnimalAnimator animator;
    private float energyDrainRate = 0;
    private float saturationDrainRate = 0;

    public ChaseFoodController(PathfindController controller, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate)
    {
        this.controller = controller;
        this.animator = animator;
        this.energyDrainRate = energyDrainRate;
        this.saturationDrainRate = saturationDrainRate;
    }

    public string DebugName() => "Chase Food";

    public void Enter() { }
    public void Update() { }
    public void Exit() { }
    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        return 0;
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy - (0.25f * (1 - stats.statVigor) * energyDrainRate) * Time.deltaTime, 0, stats.maxEnergy);

        stats.saturation = Mathf.Clamp(stats.saturation - saturationDrainRate * Time.deltaTime, 0, stats.maxSaturation);
    }
}

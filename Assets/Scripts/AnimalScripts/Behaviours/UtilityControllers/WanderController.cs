using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;

public class WanderController : IUtilityAction
{
    private readonly PathfindController controller;
    private readonly AnimalAnimator animator;
    private float energyDrainRate = 0;
    private float saturationDrainRate = 0;

    public WanderController(PathfindController controller, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate)
    {
        this.controller = controller;
        this.animator = animator;
        this.energyDrainRate = energyDrainRate;
        this.saturationDrainRate = saturationDrainRate;
    }

    public string DebugName() => "Wander";
    public void Enter() { }
    public void Update() { }
    public void AlwaysUpdate() { }
    public void Exit() { }
    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        float normalizedEnergy = stats.energy / stats.maxEnergy;
        float utilityScore = Mathf.Min(Mathf.Pow(normalizedEnergy, 2f) * 2 / 3, 0.5f);
        return utilityScore;
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy - (0.75f + 0.25f * (1 - stats.statVigor)) * energyDrainRate * Time.deltaTime, 0, stats.maxEnergy);

        stats.saturation = Mathf.Clamp(stats.saturation - saturationDrainRate * Time.deltaTime, 0, stats.maxSaturation);
    }
}

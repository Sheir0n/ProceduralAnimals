using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WanderController : IUtilityAction
{
    private readonly PathfindController controller;
    private readonly AnimalAnimator animator;

    public WanderController(PathfindController controller, AnimalAnimator animator)
    {
        this.controller = controller;
        this.animator = animator;
    }

    public string DebugName() => "Wander";
    public void Enter() { }
    public void Update() { }
    public void Exit() { }
    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction) 
    {
        return Mathf.Pow(stats.energy / stats.maxEnergy, 2f);
    }

    public void CalculateStats(AnimalStats stats)
    {
        float defaultEnergyDrain = 0.15f;
        stats.energy = Mathf.Clamp(stats.energy - (0.5f * (1 - stats.statVigor) + defaultEnergyDrain) * Time.deltaTime, 0, stats.maxEnergy);
    }
}

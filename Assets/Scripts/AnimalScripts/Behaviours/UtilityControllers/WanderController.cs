using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WanderController : IUtilityAction
{
    private readonly PathfindController controller;
    private readonly AnimalAnimator animator;
    private float energyDrainRate;

    public WanderController(PathfindController controller, AnimalAnimator animator, float energyDrainRate)
    {
        this.controller = controller;
        this.animator = animator;
        this.energyDrainRate = energyDrainRate;
    }

    public string DebugName() => "Wander";
    public void Enter() { }
    public void Update() { }
    public void Exit() { }
    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        float utilityScore;
        if (currAction == this)
            utilityScore = Mathf.Pow(stats.energy / stats.maxEnergy, 2f) * 2 / 3;
        else
            utilityScore = Mathf.Pow(stats.energy / stats.maxEnergy, 6f) * 2 / 3;

        //Debug.Log("Wanders score: " + utilityScore);
        return utilityScore;
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy - (0.5f * (1 - stats.statVigor) + energyDrainRate) * Time.deltaTime, 0, stats.maxEnergy);
    }
}

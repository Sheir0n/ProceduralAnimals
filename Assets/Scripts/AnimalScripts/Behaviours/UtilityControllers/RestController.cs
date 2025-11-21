using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestController : IUtilityAction
{
    private readonly PathfindController controller;
    private readonly AnimalAnimator animator;
    private float energyRegenRate;

    public RestController(PathfindController controller, AnimalAnimator animator, float energyRegenRate)
    {
        this.controller = controller;
        this.animator = animator;
        this.energyRegenRate = energyRegenRate;
    }

    public string DebugName() => "Rest";
    public void Enter() { }
    public void Update() { }
    public void Exit() { }
    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction) {
        float normalizedEnergy = stats.energy / stats.maxEnergy;
        float utilityScore;

        if (currAction == this)
            utilityScore = Mathf.Pow(1 - normalizedEnergy, 0.5f * stats.statVigor) * 2 / 3;
        else
            utilityScore = Mathf.Pow(1 - normalizedEnergy, 6f * (1 - stats.statVigor / 2)) * 2 / 3;

        //Debug.Log("RestScore: " + utilityScore);
        return utilityScore;
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy + (stats.statVigor - 0.5f + energyRegenRate) * Time.deltaTime, 0, stats.maxEnergy);
    }
}

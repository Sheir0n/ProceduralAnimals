using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindRestSpotController : IUtilityAction
{
    private readonly PathfindController controller;
    private readonly AnimalAnimator animator;
    private float energyDrainRate = 0;
    private float saturationDrainRate = 0;
    private bool enableScore = false;
    public FindRestSpotController(PathfindController controller, AnimalAnimator animator, AnimalEventHub eventHub, float energyDrainRate, float saturationDrainRate)
    {
        this.controller = controller;
        this.animator = animator;
        this.energyDrainRate = energyDrainRate;
        this.saturationDrainRate = saturationDrainRate;
        eventHub.OnFoundFirstRestSpot += EnableScoreOnFirstFoundSpot;
    }

    public string DebugName() => "FindRestSpot";
    public void Enter() { }
    public void Update() { }
    public void Exit() { }

    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        if (!enableScore)
            return 0;

        float normalizedEnergy = stats.energy / stats.maxEnergy;
        float utilityScore = Mathf.Pow(1 - normalizedEnergy, 2f + stats.statVigor) * 2 / 3;

        return utilityScore;
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy - (0.5f * (1 - stats.statVigor) + energyDrainRate) * Time.deltaTime, 0, stats.maxEnergy);
        stats.saturation = Mathf.Clamp(stats.saturation - saturationDrainRate * Time.deltaTime, 0, stats.maxSaturation);
    }

    private void EnableScoreOnFirstFoundSpot()
    {
        enableScore = true;
        Debug.Log("Found first spot! Find Rest scoring enabled!");
    }
}

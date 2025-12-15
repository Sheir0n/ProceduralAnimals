using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathController : IUtilityAction
{
    PathfindController controller;
    AnimalAnimator animator;
    public DeathController(PathfindController controller, AnimalAnimator animator)
    {
        this.controller = controller;
        this.animator = animator;
    }

    public string DebugName() => "DeathDisabled";

    public void OnInstantiate(Transform transform, AnimalEventHub eventHub, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate)
    {
        this.animator = animator;
    }

    public void Enter() { }
    public void Update() { }
    public void AlwaysUpdate() { }
    public void Exit() { }

    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        return -100f;
    }

    public void CalculateStats(AnimalStats stats) { }
}
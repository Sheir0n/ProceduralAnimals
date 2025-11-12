using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControlledController : IUtilityAction
{
    private readonly PathfindController controller;
    private readonly AnimalAnimator animator;

    public PlayerControlledController(PathfindController controller, AnimalAnimator animator)
    {
        this.controller = controller;
        this.animator = animator;
    }

    public string DebugName() => "PlayerControlled";
    public void Enter() { }
    public void Update() { }
    public void Exit() { }
    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction) { return 0; }
    public void CalculateStats(AnimalStats stats) { }
}

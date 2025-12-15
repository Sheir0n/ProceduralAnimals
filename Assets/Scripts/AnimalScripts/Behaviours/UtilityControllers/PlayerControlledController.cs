using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerControlledController", menuName = "AI/Actions/PlayerControlledController")]
public class PlayerControlledController : ActionController, IUtilityAction
{
    private AnimalAnimator animator;

    public string DebugName() => "PlayerControlled";

    public AnimalAI.AIAction AIAction => AnimalAI.AIAction.PlayerControlled;
    public void OnInstantiate(Transform transform, AnimalEventHub eventHub, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate)
    {
        this.animator = animator;
    }

    public void Enter() { }
    public void Update() { }
    public void AlwaysUpdate() { }
    public void Exit() { }
    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction) { return 0; }
    public void CalculateStats(AnimalStats stats) { }
}

using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline;
using UnityEngine;

public class EmptyController : ActionController, IUtilityAction
{
    public string DebugName() => "Empty Default";

    public AnimalAI.AIAction AIAction => AnimalAI.AIAction.EmptyController;
    public void OnInstantiate(Transform transform, AnimalEventHub eventHub, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate) { 
    }
    public void Enter() { }
    public void AlwaysUpdate() { }
    public void Update() { }
    public void Exit() { }

    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction) => -Mathf.Infinity;

    public void CalculateStats(AnimalStats stats) { }
}

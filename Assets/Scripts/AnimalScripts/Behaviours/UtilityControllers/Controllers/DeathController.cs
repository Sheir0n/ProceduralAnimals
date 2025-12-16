using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathController : ActionController, IUtilityAction
{
    private static ActionID sharedID;
    public AnimalAI.AIAction AIAction => AnimalAI.AIAction.Death;
    public ActionID ActionTag => sharedID; 

    public void InitializeShared(ActionID deathId)
    {
        sharedID = deathId;
    }

    public void OnInstantiate(Transform transform, AnimalEventHub eventHub, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate) {}

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
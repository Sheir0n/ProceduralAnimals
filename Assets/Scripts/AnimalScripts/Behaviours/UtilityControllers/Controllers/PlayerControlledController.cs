using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControlledController : ActionController, IUtilityAction
{

    private static ActionID sharedID;
    public ActionID ActionTag => sharedID;

    public void InitializeShared(ActionID playerSharedId)
    {
        sharedID = playerSharedId;
    }

    public void OnInstantiate(Transform transform, AnimalEventHub eventHub, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate)
    {}

    public void Enter() { }
    public void Update() { }
    public void AlwaysUpdate() { }
    public void Exit() { }
    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction) { return 0; }
    public void CalculateStats(AnimalStats stats) { }
}

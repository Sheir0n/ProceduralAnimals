using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;

public class LizardAI : AnimalAI
{
    private float energyDrainRate = 0.05f;
    private float saturationDrainRate = 0.005f;

    private float energyRegenRate = 1f;
    private float restHealthRegenRate = 0.05f;
    private float healthRegenSaturationDrain = 0.05f;
    private float saturationRegenThreshold = 0.5f;

    protected override void Awake()
    {
        base.Awake();
        AddNewAction(new PlayerControlledController(pathfindController, animator), AIAction.PlayerControlled);
        AddNewAction(new RestController(pathfindController, animator, eventHub, energyRegenRate, saturationDrainRate * 0.5f, restHealthRegenRate, healthRegenSaturationDrain, saturationRegenThreshold), AIAction.Rest);
        AddNewAction(new WanderController(pathfindController, animator, energyDrainRate, saturationDrainRate), AIAction.Wander);
        AddNewAction(new FindRestSpotController(pathfindController, animator, eventHub, energyDrainRate * 0.25f, saturationDrainRate * 0.75f), AIAction.FindRestSpot);

        actionPenalities = new Dictionary<IUtilityAction, float>();
        foreach (IUtilityAction action in actions)
            actionPenalities.Add(action, 0);

        currAction = actions[(int)AIAction.Rest];
        actionDebugDisplay = AIAction.Rest;
    }
}

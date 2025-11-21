using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;

public class LizardAI : AnimalAI
{
    private float wanderEnergyDrainRate = 0.05f;
    private float restEnergyRegenRate = 0.5f;
    protected override void Awake()
    {
        base.Awake();
        AddNewAction(new PlayerControlledController(pathfindController, animator), AIAction.PlayerControlled);
        AddNewAction(new RestController(pathfindController, animator, restEnergyRegenRate), AIAction.Rest);
        AddNewAction(new WanderController(pathfindController, animator, wanderEnergyDrainRate), AIAction.Wander);

        actionPenalities = new Dictionary<IUtilityAction, float>();
        foreach (IUtilityAction action in actions)
            actionPenalities.Add(action, 0);

        currAction = actions[(int)AIAction.Rest];
        actionDebugDisplay = AIAction.Rest;
    }
}

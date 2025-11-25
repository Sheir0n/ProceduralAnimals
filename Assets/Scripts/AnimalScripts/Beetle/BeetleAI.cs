using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeetleAI : AnimalAI
{
    private float energyDrainRate = 0f;
    private float saturationDrainRate = 0f;

    protected override void Awake()
    {
        base.Awake();
        AddNewAction(new PlayerControlledController(pathfindController, animator), AIAction.PlayerControlled);
        AddNewAction(new WanderController(pathfindController, animator, energyDrainRate, saturationDrainRate), AIAction.Wander);

        actionPenalities = new Dictionary<IUtilityAction, float>();
        foreach (IUtilityAction action in actions)
            actionPenalities.Add(action, 0);

        currAction = actionByEnum[AIAction.Wander];
    }
}

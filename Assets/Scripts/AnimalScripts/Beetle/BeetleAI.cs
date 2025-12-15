using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeetleAI : AnimalAI
{
    protected override void Awake()
    {
        base.Awake();
        AddNewAction(new PlayerControlledController(), AIAction.PlayerControlled);
        //AddNewAction(new WanderController(pathfindController, animator, energyDrainRate, saturationDrainRate), AIAction.Wander);

        actionPenalities = new Dictionary<IUtilityAction, float>();
        foreach (IUtilityAction action in actions)
            actionPenalities.Add(action, 0);

        currAction = actionByEnum[AIAction.Wander];
    }
}

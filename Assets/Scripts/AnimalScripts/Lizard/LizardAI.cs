using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class LizardAI : AnimalAI
{
    private float energyRegenRate = 0.5f;
    private float restHealthRegenRate = 0.05f;
    private float healthRegenSaturationDrain = 0.05f;
    private float saturationRegenThreshold = 0.5f;

    private int biteCooldownMs = 1500;
    private int biteWindupMs = 2000;
    private int biteDamage = 2;
    private int biteDashDuration = 1500;

    private bool clearedMemoryOnDeath = false;

    private IDamageable carriedPrey;

    protected override void Awake()
    {
        base.Awake();
        AddNewAction(new PlayerControlledController(), AIAction.PlayerControlled);
        AddNewAction(new RestController(), AIAction.Rest);
        AddNewAction(new WanderController(), AIAction.Wander);
        AddNewAction(new FindRestSpotController(), AIAction.FindRestSpot);
        AddNewAction(new ChaseFoodController(), AIAction.ChaseFood);

        actionPenalities = new Dictionary<IUtilityAction, float>();
        foreach (IUtilityAction action in actions)
            actionPenalities.Add(action, 0);

        currAction = actionByEnum[AIAction.Rest];

        eventHub.OnAnnouncePreyCaught += OnPreyCaught;
    }

    protected override void Update()
    {
        base.Update();
    }

    protected void LateUpdate()
    {
        if (enumByAction[currAction] == AIAction.Death && !clearedMemoryOnDeath)
        {
            clearedMemoryOnDeath = true;
        }
    }

    private void OnPreyCaught(IDamageable prey)
    {
        if (prey == null)
            return;

        stats.saturation = stats.maxSaturation;
        carriedPrey = prey;
    }
}

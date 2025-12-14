using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class LizardAI : AnimalAI
{
    private float energyDrainRate = 0.075f;
    private float saturationDrainRate = 0.045f;

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
        AddNewAction(new PlayerControlledController(pathfindController, animator), AIAction.PlayerControlled);
        AddNewAction(new RestController(pathfindController, animator, eventHub, energyRegenRate, saturationDrainRate * 0.5f, restHealthRegenRate, healthRegenSaturationDrain, saturationRegenThreshold), AIAction.Rest);
        AddNewAction(new WanderController(pathfindController, animator, energyDrainRate, saturationDrainRate), AIAction.Wander);
        AddNewAction(new FindRestSpotController(pathfindController, animator, transform, eventHub, energyDrainRate * 0.25f, saturationDrainRate * 0.75f), AIAction.FindRestSpot);
        AddNewAction(new ChaseFoodController(pathfindController, animator, transform, eventHub, energyDrainRate * 3f, saturationDrainRate * 1.5f, biteCooldownMs, biteWindupMs, biteDashDuration, biteDamage), AIAction.ChaseFood);

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

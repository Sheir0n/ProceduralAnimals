using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class LizardAI : AnimalAI
{
    private bool clearedMemoryOnDeath = false;

    private IDamageable carriedPrey;

    protected override void Awake()
    {
        base.Awake();
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;


public class LizardAI : AnimalAI
{
    private float energyDrainRate = 0.075f;
    private float saturationDrainRate = 0.045f;

    private float energyRegenRate = 0.5f;
    private float restHealthRegenRate = 0.05f;
    private float healthRegenSaturationDrain = 0.05f;
    private float saturationRegenThreshold = 0.5f;

    private int biteCooldownMs = 500;
    private int biteDamage = 2;

    private List<Transform> interestSpots = new List<Transform>();

    private float interestSpotResetTimer = 0;

    private bool clearedMemoryOnDeath = false;
    protected override void Awake()
    {
        base.Awake();
        AddNewAction(new PlayerControlledController(pathfindController, animator), AIAction.PlayerControlled);
        AddNewAction(new RestController(pathfindController, animator, eventHub, energyRegenRate, saturationDrainRate * 0.5f, restHealthRegenRate, healthRegenSaturationDrain, saturationRegenThreshold), AIAction.Rest);
        AddNewAction(new WanderController(pathfindController, animator, energyDrainRate, saturationDrainRate), AIAction.Wander);
        AddNewAction(new FindRestSpotController(pathfindController, animator, transform, eventHub, energyDrainRate * 0.25f, saturationDrainRate * 0.75f), AIAction.FindRestSpot);
        AddNewAction(new ChaseFoodController(pathfindController, animator, transform, eventHub, energyDrainRate * 3f, saturationDrainRate * 1.5f, biteCooldownMs, biteDamage), AIAction.ChaseFood);

        actionPenalities = new Dictionary<IUtilityAction, float>();
        foreach (IUtilityAction action in actions)
            actionPenalities.Add(action, 0);

        currAction = actionByEnum[AIAction.Rest];

        eventHub.OnNewInterestSpotFound += AddNewInterestSpot;
        eventHub.OnInterestLookTargetRequest += GetBestInterestSpot;
    }

    protected override void Update()
    {
        base.Update();
        GetBestInterestSpot();
    }

    protected void LateUpdate()
    {
        if (enumByAction[currAction] == AIAction.Death && clearedMemoryOnDeath)
        {
            clearedMemoryOnDeath = true;
            interestSpots.Clear();
        }
        else
            ResetInterestOnIntervalMs(intervalInMs: 750);
    }

    private void AddNewInterestSpot(Transform interestPoint)
    {
        if (interestSpots.Contains(interestPoint))
            return;

        interestSpots.Add(interestPoint);
    }

    private void ResetInterestOnIntervalMs(int intervalInMs)
    {
        if (interestSpots.Count() > 0)
            interestSpotResetTimer += Time.deltaTime * 1000;
        else
        {
            interestSpotResetTimer = 0;
            return;
        }

        if (interestSpotResetTimer > intervalInMs)
        {
            interestSpotResetTimer = 0;
            interestSpots.Clear();
        }
    }

    private LookTarget GetBestInterestSpot()
    {
        Transform best = null;
        float highscore = 0;
        foreach (Transform spot in interestSpots)
        {
            float distance = Mathf.Sqrt((transform.position - spot.position).sqrMagnitude);
            float score = Mathf.Clamp(1f / (distance + 0.0001f), 0, 10);
            if (spot.CompareTag("Rock"))
            {
                score += 10 * (1 - (stats.energy / stats.maxEnergy)) * (0.5f + (1 - stats.statVigor));
            }
            else if (spot.CompareTag("Lizard"))
            {
                score += 15 * (0.5f + stats.statAggressiveness);
            }
            else if (spot.CompareTag("Beetle"))
            {
                score += 20 * (0.5f + stats.saturation / stats.maxSaturation);
            }

            if (score > highscore && score > 5 - (5 * stats.statCuriosity))
            {
                highscore = score;
                best = spot;
            }
        }

        if (best == null)
        {
            LookTarget target = new LookTarget(transform.position, isLooking: false);
            return target;
        }
        else
        {
            LookTarget target = new LookTarget(best.position, isLooking: true);
            return target;
        }
    }
}

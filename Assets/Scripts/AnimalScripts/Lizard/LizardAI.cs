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

    private int biteCooldownMs = 1500;
    private int biteWindupMs = 2000;
    private int biteDamage = 2;
    private int biteDashDuration = 1500;

    private List<Transform> interestSpots = new List<Transform>();

    private float interestSpotResetTimer = 0;
    private const float interestResetCooldownMs = 450;

    private bool clearedMemoryOnDeath = false;
    private const int maxInterestDistance = 20;

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

        eventHub.OnNewInterestSpotFound += AddNewInterestSpot;
        eventHub.OnInterestLookTarget += GetBestInterestSpot;
        eventHub.OnAnnouncePreyCaught += OnPreyCaught;
    }

    protected override void Update()
    {
        base.Update();
        GetBestInterestSpot();
    }

    protected void LateUpdate()
    {
        if (enumByAction[currAction] == AIAction.Death && !clearedMemoryOnDeath)
        {
            clearedMemoryOnDeath = true;
            interestSpots.Clear();
        }
        else
            ResetInterestOnIntervalMs();
    }

    private void AddNewInterestSpot(Transform interestPoint)
    {
        if (interestSpots.Contains(interestPoint))
            return;

        interestSpots.Add(interestPoint);
    }

    private void ResetInterestOnIntervalMs()
    {
        if (interestSpots.Count() > 0)
            interestSpotResetTimer += Time.deltaTime * 1000;
        else
        {
            interestSpotResetTimer = 0;
            return;
        }

        if (interestSpotResetTimer > interestResetCooldownMs)
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
            Vector3 position = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 spotPosition = new Vector3(spot.position.x, 0, spot.position.z);

            float distance = Mathf.Sqrt((transform.position - spotPosition).sqrMagnitude);
            float score = Mathf.Clamp((maxInterestDistance - distance) / 2, 0, 10);

            if (spot.CompareTag("Rock"))
            {
                score += 8 * (1 - (stats.energy / stats.maxEnergy)) * (0.5f + (1 - stats.statVigor));
            }
            else if (spot.CompareTag("Lizard"))
            {
                score += 8 * (0.5f + stats.statAggressiveness);
            }
            else if (spot.CompareTag("Beetle"))
            {
                score += 10 * (0.5f + (1 - (stats.saturation / stats.maxSaturation)));
            }

            if (score > highscore && score > 15 - (5 * stats.statCuriosity))
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

    private void OnPreyCaught(IDamageable prey)
    {
        if (prey == null)
            return;

        stats.saturation = stats.maxSaturation;
        carriedPrey = prey;
    }
}

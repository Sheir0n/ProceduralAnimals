using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static UnityEditor.PlayerSettings;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class LizardAI : AnimalAI
{
    private float energyDrainRate = 0.05f;
    private float saturationDrainRate = 0.005f;

    private float energyRegenRate = 1f;
    private float restHealthRegenRate = 0.05f;
    private float healthRegenSaturationDrain = 0.05f;
    private float saturationRegenThreshold = 0.5f;

    private List<Transform> restingSpots = new List<Transform>();
    private List<Transform> interestSpots = new List<Transform>();
    private const int maxSpotMemorySlots = 5;
    private bool foundFirstSpot = false;
    private float interestSpotResetTimer = 0;

    [SerializeField] private bool showDebugInterestSpots = false;

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


        eventHub.OnNearestRestSpotRequest += GetNearestRestingSpot;
        eventHub.OnNewRestSpotFound += AddNewRestSpot;
        eventHub.OnNewInterestSpotFound += AddNewInterestSpot;
        eventHub.OnInterestLookTargetRequest += GetBestInterestSpot;
    }

    protected override void Update()
    {
        base.Update();
        ResetInterestOnIntervalMs(intervalInMs: 2000);
        GetBestInterestSpot();
    }

    private void AddNewRestSpot(Transform restSpot)
    {
        if (restingSpots.Contains(restSpot))
            return;

        if (!foundFirstSpot)
        {
            foundFirstSpot = true;
            eventHub.FoundFirstRestSpot();
        }

        restingSpots.Add(restSpot);
        if (restingSpots.Count > maxSpotMemorySlots)
        {
            Transform farthest = restingSpots
                .OrderByDescending(r => Vector3.Distance(transform.position, r.position))
                .First();

            restingSpots.Remove(farthest);
        }
    }
    private Transform GetNearestRestingSpot()
    {
        if (restingSpots == null || restingSpots.Count == 0)
            return null;

        Vector3 currentPos = transform.position;
        Transform nearest = restingSpots
            .OrderBy(spot => (spot.position - currentPos).sqrMagnitude)
            .First();

        return nearest;
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
            interestSpots.Clear();
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

            if (score > highscore)
            {
                highscore = score;
                best = spot;
            }
        }

        if (showDebugInterestSpots)
            Debug.Log("bestSpot: " + best + " score: " + highscore);

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

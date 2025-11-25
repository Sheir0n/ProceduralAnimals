using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;


public class HuntTarget
{
    public Transform target;
    public float memoryTimeMs;
    private float defaultMemoryMs = 5000;

    public HuntTarget(Transform target)
    {
        this.target = target;
        memoryTimeMs = defaultMemoryMs;
    }

    public void ResetMemoryTime()
    {
        memoryTimeMs = defaultMemoryMs;
    }
}


public class LizardAI : AnimalAI
{
    private float energyDrainRate = 0.075f;
    private float saturationDrainRate = 0.045f;

    private float energyRegenRate = 0.5f;
    private float restHealthRegenRate = 0.05f;
    private float healthRegenSaturationDrain = 0.05f;
    private float saturationRegenThreshold = 0.5f;

    private List<Transform> restingSpots = new List<Transform>();
    private List<Transform> interestSpots = new List<Transform>();
    private List<HuntTarget> huntTargets = new List<HuntTarget>();

    private const int maxSpotMemorySlots = 5;
    private bool foundFirstSpot = false;
    private float interestSpotResetTimer = 0;

    [SerializeField] private bool showDebugInterestSpots = false;
    [SerializeField] private bool showDebugHuntTargets = false;
    protected override void Awake()
    {
        base.Awake();
        AddNewAction(new PlayerControlledController(pathfindController, animator), AIAction.PlayerControlled);
        AddNewAction(new RestController(pathfindController, animator, eventHub, energyRegenRate, saturationDrainRate * 0.5f, restHealthRegenRate, healthRegenSaturationDrain, saturationRegenThreshold), AIAction.Rest);
        AddNewAction(new WanderController(pathfindController, animator, energyDrainRate, saturationDrainRate), AIAction.Wander);
        AddNewAction(new FindRestSpotController(pathfindController, animator, eventHub, energyDrainRate * 0.25f, saturationDrainRate * 0.75f), AIAction.FindRestSpot);
        AddNewAction(new ChaseFoodController(pathfindController, animator, eventHub, energyDrainRate * 3f, saturationDrainRate * 1.5f), AIAction.ChaseFood);

        actionPenalities = new Dictionary<IUtilityAction, float>();
        foreach (IUtilityAction action in actions)
            actionPenalities.Add(action, 0);

        currAction = actionByEnum[AIAction.Rest];

        eventHub.OnNearestRestSpotRequest += GetNearestRestingSpot;
        eventHub.OnNewRestSpotFound += AddNewRestSpot;
        eventHub.OnNewInterestSpotFound += AddNewInterestSpot;
        eventHub.OnInterestLookTargetRequest += GetBestInterestSpot;
        eventHub.OnNewHuntTargetFound += AddHuntTarget;
        eventHub.OnNearestHuntTargetRequest += GetNearestHuntTarget;
    }

    protected override void Update()
    {
        base.Update();
        GetBestInterestSpot();
        UpdateHuntTargetMemory();
    }

    protected void LateUpdate()
    {
        ResetInterestOnIntervalMs(intervalInMs: 750);
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

    private void AddHuntTarget(Transform target)
    {
        if (target == null)
            return;

        for (int i = 0; i < huntTargets.Count; i++)
        {
            if (huntTargets[i].target == target)
            {
                huntTargets[i].ResetMemoryTime();
                return;
            }
        }

        huntTargets.Add(new HuntTarget(target));
    }

    private void UpdateHuntTargetMemory()
    {
        for (int i = huntTargets.Count - 1; i >= 0; i--)
        {
            HuntTarget huntTarget = huntTargets[i];

            huntTarget.memoryTimeMs -= Time.deltaTime * 1000f;

            if (huntTarget.memoryTimeMs < 0)
            {
                huntTargets.RemoveAt(i);
            }
        }

        if (showDebugHuntTargets)
            Debug.Log(huntTargets.Count);
    }

    private Transform GetNearestHuntTarget()
    {
        if (huntTargets.Count == 0)
            return null;

        Vector3 currentPos = transform.position;
        HuntTarget nearest = huntTargets
            .OrderBy(t => (t.target.position - currentPos).sqrMagnitude)
            .First();

        return nearest.target;
    }
}

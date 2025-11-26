using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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

public class ChaseFoodController : IUtilityAction
{
    private readonly PathfindController controller;
    private readonly AnimalAnimator animator;
    private readonly AnimalEventHub eventHub;
    private readonly Transform transform;
    private float energyDrainRate = 0;
    private float saturationDrainRate = 0;

    private const float huntTargetMemoryDecayMinDistance = 4f;

    private List<HuntTarget> huntTargets = new List<HuntTarget>();

    private bool hasBittern = false;
    private Collider biteTarget;
    private int biteCooldownMs = 500;
    private int randomisedCooldownMs;
    private float biteTimerMs = 0;
    private int biteDamage = 1;
    public ChaseFoodController(PathfindController controller, AnimalAnimator animator, Transform transform, AnimalEventHub eventHub, float energyDrainRate, float saturationDrainRate, int biteCooldownMs, int biteDamage)
    {
        this.controller = controller;
        this.animator = animator;
        this.transform = transform;
        this.eventHub = eventHub;
        this.energyDrainRate = energyDrainRate;
        this.saturationDrainRate = saturationDrainRate;
        this.biteCooldownMs = biteCooldownMs;
        this.biteDamage = biteDamage;


        eventHub.OnNewHuntTargetFound += AddHuntTarget;
        eventHub.OnNearestHuntTargetRequest += GetNearestHuntTarget;
    }

    public string DebugName() => "Chase Food";

    public void Enter() {
        eventHub.OnAttemptBite += AttemptBite;
        hasBittern = false;
        biteTimerMs = biteCooldownMs;
    }

    public void Update() 
    {
        if (!hasBittern && biteTarget != null && biteTimerMs >= randomisedCooldownMs)
        {
            Bite(biteTarget);
            biteTimerMs = 0;
            randomisedCooldownMs = (int)(biteCooldownMs * Random.Range(0.75f, 1.25f));
        }
        biteTimerMs += Time.deltaTime * 1000f;
    }

    public void AlwaysUpdate() {
        UpdateHuntTargetMemory();
    }

    public void Exit() {
        eventHub.OnAttemptBite -= AttemptBite;
        biteTarget = null;
    }

    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        Transform huntTarget = eventHub.FindNearestHuntTarget();
        if (huntTarget == null)
            return 0;
        else
        {
            float normalisedSaturation = stats.saturation / stats.maxSaturation;
            return Mathf.Pow(1 - normalisedSaturation, (1 - stats.statAggressiveness) / 3) * 2 / 3;
        }
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy - (0.75f + 0.25f * (1 - stats.statVigor)) * energyDrainRate * Time.deltaTime, 0, stats.maxEnergy);

        stats.saturation = Mathf.Clamp(stats.saturation - saturationDrainRate * Time.deltaTime, 0, stats.maxSaturation);
    }

    private void AddHuntTarget(Transform target)
    {
        if (target == null)
            return;

        for (int i = 0; i < huntTargets.Count; i++)
            if (huntTargets[i].target == target)
            {
                huntTargets[i].ResetMemoryTime();
                return;
            }
        huntTargets.Add(new HuntTarget(target));
    }

    private void UpdateHuntTargetMemory()
    {
        for (int i = huntTargets.Count - 1; i >= 0; i--)
        {
            Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosXZ = new Vector3(huntTargets[i].target.position.x, 0, huntTargets[i].target.position.z);


            HuntTarget huntTarget = huntTargets[i];

            if (huntTarget.memoryTimeMs < 0)
            {
                huntTargets.RemoveAt(i);
            }
            else if (Vector3.Distance(currentPosXZ, targetPosXZ) > huntTargetMemoryDecayMinDistance)
                huntTarget.memoryTimeMs -= Time.deltaTime * 1000f;
        }
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

    private void AttemptBite(Collider other)
    {
        if (!hasBittern)
        {
            biteTarget = other;
        }
    }

    private void Bite(Collider other)
    {
        Debug.Log("Succesful bite for " + biteDamage + " damage!");
    }
}

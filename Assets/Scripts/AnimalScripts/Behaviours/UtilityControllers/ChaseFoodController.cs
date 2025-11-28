using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
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

    private bool bitePrepared = false;

    private Collider biteTarget;
    IDamageable targetInterface;

    private int biteCooldownMs = 500;
    private int randomisedCooldownMs;
    private float biteTimerMs = 0;

    private int biteWindupMs = 100;

    private int biteDashDuration = 500;
    private int biteDamage = 1;
    private bool preyCaught = false;

    public enum BiteAttackStage {Windup, Dash, Finished}

    private CancellationTokenSource biteCancelToken;
    public ChaseFoodController(PathfindController controller, AnimalAnimator animator, Transform transform, AnimalEventHub eventHub, float energyDrainRate, float saturationDrainRate, int biteCooldownMs, int biteWindupMs, int biteDashDuration, int biteDamage)
    {
        this.controller = controller;
        this.animator = animator;
        this.transform = transform;
        this.eventHub = eventHub;
        this.energyDrainRate = energyDrainRate;
        this.saturationDrainRate = saturationDrainRate;
        this.biteCooldownMs = biteCooldownMs;
        this.biteWindupMs = biteWindupMs;
        this.biteDashDuration = biteDashDuration;
        this.biteDamage = biteDamage;


        eventHub.OnNewHuntTargetFound += AddHuntTarget;
        eventHub.OnNearestHuntTargetRequest += GetNearestHuntTarget;
    }

    public string DebugName() => "Chase Food";

    public void Enter()
    {
        eventHub.OnAttemptBite += AttemptBite;
        bitePrepared = false;
        biteTimerMs = biteCooldownMs;
        randomisedCooldownMs = (int)(biteCooldownMs * UnityEngine.Random.Range(0.75f, 1.25f));
        preyCaught = false;
    }

    public void Update()
    {
        biteTimerMs += Time.deltaTime * 1000;
    }

    public void AlwaysUpdate()
    {
        UpdateHuntTargetMemory();
    }

    public void Exit()
    {
        eventHub.OnAttemptBite -= AttemptBite;
        biteTarget = null;
        targetInterface = null;
        biteCancelToken?.Cancel();
    }

    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        Transform huntTarget = eventHub.FindNearestHuntTarget();
        //TODO do zmiany, na razie zapobiega blokadzie
        //ma sie odblokowac po zaniesieniu ofiary na spawn
        if (huntTarget == null || preyCaught)
            return 0;
        else
        {
            float normalisedSaturation = stats.saturation / stats.maxSaturation;
            return Mathf.Pow(1 - normalisedSaturation, (1.5f - stats.statAggressiveness) / 2) * 2 / 3;
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
        if (!bitePrepared && !preyCaught && biteTimerMs >= randomisedCooldownMs)
        {
            biteTarget = other;
            bitePrepared = true;
            targetInterface = other.gameObject.GetComponent<IDamageable>();

            biteTimerMs = 0;
            randomisedCooldownMs = (int)(biteCooldownMs * UnityEngine.Random.Range(0.75f, 1.25f));

            biteCancelToken = new CancellationTokenSource();
            _ = BiteAfterDelay(other, biteCancelToken.Token);
        }
    }

    private async Task BiteAfterDelay(Collider other, CancellationToken token)
    {
        try
        {
            eventHub.AnnounceBiteAttack(BiteAttackStage.Windup);
            Debug.Log("bite windup start " + biteWindupMs);
            await Task.Delay(biteWindupMs, token);

            eventHub.AnnounceBiteAttack(BiteAttackStage.Dash);
            Debug.Log("bite dash started");

            float elapsedMs = 0f;
            while (elapsedMs < biteDashDuration)
            {
                token.ThrowIfCancellationRequested();
                if(eventHub.CheckIfColliderInMouth(other))
                {
                    Bite(other);
                    bitePrepared = false;
                    eventHub.AnnounceBiteAttack(BiteAttackStage.Finished);
                    return;
                }

                await Task.Yield();
                elapsedMs += Time.deltaTime * 1000;
            }

            Debug.Log("bite dash ended");
            eventHub.AnnounceBiteAttack(BiteAttackStage.Finished);
            bitePrepared = false;
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Bite cancelled!");
            bitePrepared = false;
        }
    }

    private void Bite(Collider other)
    {
        bitePrepared = false;
        Debug.Log("Succesful bite for " + biteDamage + " damage!");
        if (targetInterface.GetHealth() > 0)
            targetInterface.TakeDamage(biteDamage);
        else
        {
            Transform mouthTransform = eventHub.GetMouthTransform();
            if (mouthTransform != null)
            {
                targetInterface.OnSnatchAttachTo(mouthTransform);
                preyCaught = true;
                eventHub.AnnouncePreyCaught(targetInterface);
            }
        }
    }
}

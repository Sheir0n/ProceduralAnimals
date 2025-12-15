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

[CreateAssetMenu(fileName = "ChaseFoodController", menuName = "AI/Actions/ChaseFoodController")]
public class ChaseFoodController : ActionController, IUtilityAction
{
    private AnimalEventHub eventHub;

    [Header("Drain rate modifiers")]
    [SerializeField] private float energyDrainRateModifier = 1;
    [SerializeField] private float saturationDrainRateModifier = 1;

    [Header("Bite settings")]
    [SerializeField] private int biteCooldownMs = 500;
    [SerializeField] private int biteWindupMs = 100;
    [SerializeField] private int biteDashDuration = 500;
    [SerializeField] private int biteDamage = 1;

    TrackedWithScore bestPreyCandidate;

    private bool bitePrepared = false;

    IDamageable targetInterface;

    private int randomisedCooldownMs;
    private float biteTimerMs = 0;
    private bool preyCaught = false;

    private Collider biteTarget = null;
    public enum BiteAttackStage {Windup, Dash, Finished}
    private CancellationTokenSource biteCancelToken;
    AnimalMouthCollider animalMouth;

    public void OnInstantiate(Transform transform, AnimalEventHub eventHub, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate)
    {
        this.eventHub = eventHub;
        this.energyDrainRate = energyDrainRate;
        this.saturationDrainRate = saturationDrainRate;
        bestPreyCandidate = new TrackedWithScore(null, 0);
    }

    public AnimalAI.AIAction AIAction => AnimalAI.AIAction.ChaseFood;

    public string DebugName() => "Chase Food";

    public void Enter()
    {
        eventHub.OnAttemptBite += AttemptBite;
        bitePrepared = false;
        biteTimerMs = biteCooldownMs;
        randomisedCooldownMs = (int)(biteCooldownMs * UnityEngine.Random.Range(0.75f, 1.25f));
        preyCaught = false;

        animalMouth = eventHub.GetAnimalMouth();
    }

    public void Update()
    {
        biteTimerMs += Time.deltaTime * 1000;
    }

    public void AlwaysUpdate()
    {
        bestPreyCandidate = eventHub.RequestTrackedPrey();
    }

    public void Exit()
    {
        eventHub.OnAttemptBite -= AttemptBite;
        biteTarget = null;
        targetInterface = null;
        biteCancelToken?.Cancel();
        animalMouth = null;
    }

    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        //float targetScoreModifier = bestPreyCandidate.score;
        float targetScoreModifier = 1f;
        Transform targetTransform = bestPreyCandidate.tracked;

        //TODO do zmiany, na razie zapobiega blokadzie
        //ma sie odblokowac po zaniesieniu ofiary na spawn
        if (targetTransform == null || preyCaught)
            return -Mathf.Infinity;
        else
        {
            float normalisedSaturation = stats.saturation / stats.maxSaturation;
            return Mathf.Pow(1 - normalisedSaturation, (1.5f - stats.statAggressiveness) / 2) * targetScoreModifier * 2 / 3;
        }
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy - (0.75f + 0.25f * (1 - stats.statVigor)) * energyDrainRate * energyDrainRateModifier * Time.deltaTime, 0, stats.maxEnergy);

        stats.saturation = Mathf.Clamp(stats.saturation - saturationDrainRate *saturationDrainRateModifier* Time.deltaTime, 0, stats.maxSaturation);
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
                if(animalMouth.CheckIfOtherInMouth(other))
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
            if (animalMouth != null)
            {
                targetInterface.OnSnatchAttachTo(animalMouth.transform);
                preyCaught = true;
                eventHub.AnnouncePreyCaught(targetInterface);
            }
        }
    }
}

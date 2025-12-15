using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;


[CreateAssetMenu(fileName = "WanderController", menuName = "AI/Actions/WanderController")]
public class WanderController : ScriptableObject, IUtilityAction
{
    private float energyDrainRate = 0;
    private float saturationDrainRate = 0;

    [Header("Drain rate modifiers")]
    [SerializeField] private float energyDrainRateModifier = 1;
    [SerializeField] private float saturationDrainRateModifier = 1;

    public string DebugName() => "Wander";

    public void OnInstantiate(Transform transform, AnimalEventHub eventHub, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate)
    {
        this.energyDrainRate = energyDrainRate;
        this.saturationDrainRate = saturationDrainRate;
    }

    public void Enter() { }
    public void Update() { }
    public void AlwaysUpdate() { }
    public void Exit() { }
    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        float normalizedEnergy = stats.energy / stats.maxEnergy;
        float utilityScore = Mathf.Min(Mathf.Pow(normalizedEnergy, 2f) * 2 / 3, 0.5f);
        return utilityScore;
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy - (0.75f + 0.25f * (1 - stats.statVigor)) * energyDrainRate * Time.deltaTime, 0, stats.maxEnergy);

        stats.saturation = Mathf.Clamp(stats.saturation - saturationDrainRate * Time.deltaTime, 0, stats.maxSaturation);
    }
}

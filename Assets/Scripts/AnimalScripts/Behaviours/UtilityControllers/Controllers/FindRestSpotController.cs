using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


[CreateAssetMenu(fileName = "FindRestSpotController", menuName = "AI/Actions/FindRestSpotController")]
public class FindRestSpotController : ActionController, IUtilityAction
{
    private Transform transform;

    [Header("Drain rate modifiers")]
    [SerializeField] private float energyDrainRateModifier = 1;
    [SerializeField] private float saturationDrainRateModifier = 1;

    [Header("Rest spot settings")]
    [SerializeField] private const int maxRestSpots = 5;

    private bool enableScore = false;
    private List<Transform> restingSpots = new List<Transform>();

    public ActionID ActionTag => actionID;

    public void OnInstantiate(Transform transform, AnimalEventHub eventHub, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate)
    {
        this.transform = transform;
        this.energyDrainRate = energyDrainRate;
        this.saturationDrainRate = saturationDrainRate;

        eventHub.OnNewInterestFound += AddNewRestSpot;
        eventHub.OnNearestRestSpotRequest += GetNearestRestingSpot;
    }
    public void Enter() { }
    public void Update() { }
    public void AlwaysUpdate() { }
    public void Exit() { }

    public float GetUtilityScore(AnimalStats stats, IUtilityAction currAction)
    {
        if (!enableScore)
            return 0;

        float normalizedEnergy = stats.energy / stats.maxEnergy;
        float utilityScore = Mathf.Pow(1 - normalizedEnergy, 1f + stats.statVigor) * 1 / 3;
        return utilityScore;
    }

    public void CalculateStats(AnimalStats stats)
    {
        stats.energy = Mathf.Clamp(stats.energy - (0.5f + 0.5f * (1 - stats.statVigor)) * energyDrainRate *energyDrainRateModifier* Time.deltaTime, 0, stats.maxEnergy);
        stats.saturation = Mathf.Clamp(stats.saturation - saturationDrainRate *saturationDrainRateModifier* Time.deltaTime, 0, stats.maxSaturation);
    }

    private void AddNewRestSpot(List<Transform> restSpots)
    {
        foreach (Transform restSpot in restSpots)
        {
            if (restingSpots.Contains(restSpot) || !restSpot.CompareTag("Rock"))
                return;

            if (!enableScore)
                enableScore = true;

            restingSpots.Add(restSpot);
            if (restingSpots.Count > maxRestSpots)
            {
                Transform farthest = restingSpots
                    .OrderByDescending(r => Vector3.Distance(transform.position, r.position))
                    .First();

                restingSpots.Remove(farthest);
            }
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
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class LookTrackerTarget : TrackerTarget<LookTrackerData>
{
    public LookTrackerData.ObjectType objectType;

    public LookTrackerTarget(Transform target, LookTrackerData data)
        : base(target, data)
    {
        objectType = data.objectType;
    }
}

public class InterestTracker : DefaultTracker<LookTrackerData, LookTrackerTarget>
{
    public InterestTracker(List<LookTrackerData> datas, Transform transform, AnimalEventHub eventHub, IReadOnlyAnimalStats statsHook)
        : base(datas, transform, eventHub, statsHook)
    {
        eventHub.OnNewInterestSpotFound += AddTarget;
        eventHub.OnInterestLookTarget += GetLookTarget;
    }

    protected override LookTrackerTarget CreateTarget(Transform target, LookTrackerData data)
    {
        return new LookTrackerTarget(target, data);
    }

    protected override Transform GetMostImportantTracked()
    {
        Transform best = null;
        float highscore = 0;
        foreach (LookTrackerTarget targetData in trackerTargets)
        {
            Transform target = targetData.target;
            Vector3 position = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosition = new Vector3(target.position.x, 0, target.position.z);
            float distance = Mathf.Sqrt((transform.position - targetPosition).sqrMagnitude);

            float normalized = Mathf.Clamp01(distance / targetData.maxTrackDistance);
            float score = (1 - Mathf.Sqrt(normalized)) * targetData.importance;

            switch (targetData.objectType)
            {
                case LookTrackerData.ObjectType.Obstacle:
                    break;
                case LookTrackerData.ObjectType.RestSpot:
                    score *= (1 - (statsHook.Energy / statsHook.MaxEnergy)) * (0.5f + 0.5f * (1 - statsHook.StatVigor));
                    break;
                case LookTrackerData.ObjectType.Prey:
                    score *= 0.25f + 0.75f * (1 - (statsHook.Saturation / statsHook.MaxSaturation));
                    break;
                case LookTrackerData.ObjectType.Danger:
                    break;
                case LookTrackerData.ObjectType.OtherAnimal:
                    score *= 0.25f + 0.75f * statsHook.StatAggressiveness;
                    break;
            }

            if (score > highscore && score > 0.5f * (1f - statsHook.StatCuriosity))
            {
                highscore = score;
                best = target;
            }
        }

        return best;
    }

    public LookTarget GetLookTarget()
    {
        Transform best = GetMostImportantTracked();

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

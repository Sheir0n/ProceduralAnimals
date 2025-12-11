using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreyTracker : DefaultTracker<TrackerData, TrackerTarget<TrackerData>>
{
    public PreyTracker(List<TrackerData> datas, Transform transform, AnimalEventHub eventHub, IReadOnlyAnimalStats statsHook)
         : base(datas, transform, eventHub, statsHook)
    {
        eventHub.OnNewInterestFound += AddTarget;
        eventHub.OnTrackedPreyRequest += GetMostImportantTracked;
        
    }

    protected override TrackerTarget<TrackerData> CreateTarget(Transform target, TrackerData data)
    {
        return new TrackerTarget<TrackerData>(target, data);
    }

    protected override TrackedWithScore GetMostImportantTracked()
    {
        Transform best = null;
        float highscore = 0;
        foreach (TrackerTarget<TrackerData> targetData in trackerTargets)
        {
            Transform target = targetData.target;
            Vector3 position = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosition = new Vector3(target.position.x, 0, target.position.z);
            float distance = Mathf.Sqrt((transform.position - targetPosition).sqrMagnitude);

            float normalized = Mathf.Clamp01(distance / targetData.maxTrackDistance);
            float score = (1 - normalized) * targetData.importance;

            if (score > highscore)
            {
                highscore = score;
                best = target;
            }
        }
        return new TrackedWithScore(best, highscore);
    }
}

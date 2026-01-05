using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FearTracker : DefaultTracker<TrackerData, TrackerTarget<TrackerData>>
{
    public FearTracker(List<TrackerData> datas, Transform transform, AnimalEventHub eventHub, IReadOnlyAnimalStats statsHook)
         : base(datas, transform, eventHub, statsHook)
    {
        eventHub.OnNewInterestFound += AddTarget;
        eventHub.OnTrackedFearRequest += GetMostImportantTracked;
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
            if (target == null || target.transform == null)
                continue;
            Vector3 position = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosition = new Vector3(target.position.x, 0, target.position.z);
            float distance = Mathf.Sqrt((transform.position - targetPosition).sqrMagnitude);

            float normalized = Mathf.Clamp01(distance / targetData.maxTrackDistance);
            float score = (1 - 0.5f * normalized) * targetData.importance;

            if (score > highscore)
            {
                highscore = score;
                best = target;
            }
        }
        return new TrackedWithScore(best, highscore);
    }
}

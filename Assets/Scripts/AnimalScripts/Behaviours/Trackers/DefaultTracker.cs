using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;

public class TrackerTarget<TData> where TData : TrackerData
{
    public Transform target;
    public float memoryTimeMs;
    public float defaultMemoryTimeMs;
    public float importance;
    public float forgetMinRange;
    public float maxTrackDistance;

    public TrackerTarget(Transform target, TData data)
    {
        this.target = target;
        defaultMemoryTimeMs = data.memoryTimeInSec * 1000;
        importance = data.importance;
        forgetMinRange = data.forgetMinRange;
        maxTrackDistance = data.maxTrackDistance;
    }

    public void ResetMemoryTime()
    {
        memoryTimeMs = defaultMemoryTimeMs;
    }
}

public abstract class DefaultTracker<TData, TTarget>
    where TData : TrackerData
    where TTarget : TrackerTarget<TData>
{
    protected List<TData> trackerDatas = new List<TData>();
    protected List<TTarget> trackerTargets = new List<TTarget>();
    protected Transform transform;
    protected IReadOnlyAnimalStats statsHook;
    protected AnimalEventHub eventHub;

    public DefaultTracker(List<TData> datas, Transform transform, AnimalEventHub eventHub, IReadOnlyAnimalStats statsHook)
    {
        this.trackerDatas = datas;
        this.transform = transform;
        this.eventHub = eventHub;
        this.statsHook = statsHook;
    }

    public virtual void OnUpdate() 
    {
        UpdateHuntTargetMemory();
    }

    protected void AddTarget(Transform target)
    {
        if (target == null)
            return;

        for (int i = 0; i < trackerTargets.Count; i++)
            if (trackerTargets[i].target == target)
            {
                trackerTargets[i].ResetMemoryTime();
                return;
            }

        foreach (TData data in trackerDatas)
        {
            if (target.CompareTag(data.tag))
            {
                TTarget newTarget = CreateTarget(target, data);
                trackerTargets.Add(newTarget);
            }
        }
    }

    protected abstract TTarget CreateTarget(Transform target, TData data);

    protected abstract Transform GetMostImportantTracked();

    private void UpdateHuntTargetMemory()
    {
        for (int i = trackerTargets.Count - 1; i >= 0; i--)
        {
            Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosXZ = new Vector3(trackerTargets[i].target.position.x, 0, trackerTargets[i].target.position.z);


            TTarget target = trackerTargets[i];

            if (target.memoryTimeMs < 0)
            {
                trackerTargets.RemoveAt(i);
            }
            else if (Vector3.Distance(currentPosXZ, targetPosXZ) > target.maxTrackDistance)
                target.memoryTimeMs -= Time.deltaTime * 1000f;
        }
    }
}

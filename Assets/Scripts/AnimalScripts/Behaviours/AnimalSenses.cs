using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static AnimalAI;

public struct HeadCenterData
{
    public Vector3 pivot;
    public Vector3 direction;
    public HeadCenterData(Vector3 pivot, Vector3 direction)
    {
        this.pivot = pivot;
        this.direction = direction;
    }
}

public class AnimalSenses : MonoBehaviour
{
    protected AnimalEventHub eventHub { private set; get; }
    protected HeadCenterData coneCenterData { private set; get; }
    protected bool deathDisableSenses { private set; get; } = false;

    [SerializeField] protected VisionConeData visionConeData;

    [SerializeField] protected TrackerDatas allTrackedTagDatas;
    protected List<string> trackedInterestTags = new List<string>();
    protected List<string> trackedFoodTags = new List<string>();
    protected List<string> trackedFearTags = new List<string>();

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        eventHub.OnActionChanged += DisableSensesOnDeath;

        if (allTrackedTagDatas == null)
            Debug.LogWarning(" Tracked datas not set in AnimalSenses script, will not record vision cone events!", this);

        foreach (TrackerData  data in allTrackedTagDatas.lookTrackerTags)
        {
            trackedInterestTags.Add(data.tag);
        }
        foreach (TrackerData data in allTrackedTagDatas.foodTrackerTags)
        {
            trackedFoodTags.Add(data.tag);
        }
        foreach (TrackerData data in allTrackedTagDatas.fearTrackerTags)
        {
            trackedFearTags.Add(data.tag);
        }
    }

    protected virtual void Update()
    {
        coneCenterData = eventHub.RequestHeadData();
    }

    private void DisableSensesOnDeath(AIAction newAction)
    {
        if (newAction == AIAction.Death)
            deathDisableSenses = true;
    }
}

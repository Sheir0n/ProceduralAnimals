using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Xsl;
using Unity.Burst.CompilerServices;
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
    [SerializeField] protected bool showConeDebug = false;
    protected AnimalEventHub eventHub { private set; get; }
    protected HeadCenterData coneCenterData { private set; get; }
    protected bool deathDisableSenses { private set; get; } = false;

    [SerializeField] protected VisionConeData visionConeData;
    protected List<string> trackedInterestTags = new List<string>();
    protected List<string> trackedFoodTags = new List<string>();
    protected List<string> trackedFearTags = new List<string>();

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        eventHub.OnDeath += DisableSensesOnDeath;

        if (visionConeData.allTrackedTagDatas == null)
            Debug.LogWarning(" Tracked datas not set in AnimalSenses script, will not record vision cone events!", this);

        foreach (TrackerData  data in visionConeData.allTrackedTagDatas.lookTrackerTags)
        {
            trackedInterestTags.Add(data.tag);
        }
        foreach (TrackerData data in visionConeData.allTrackedTagDatas.foodTrackerTags)
        {
            trackedFoodTags.Add(data.tag);
        }
        foreach (TrackerData data in visionConeData.allTrackedTagDatas.fearTrackerTags)
        {
            trackedFearTags.Add(data.tag);
        }
    }

    protected void Update()
    {
        if (deathDisableSenses)
            return;

        coneCenterData = eventHub.RequestHeadData();
        CheckVisionCone();

    }

    private void DisableSensesOnDeath()
    {
            deathDisableSenses = true;
    }

    private void CheckVisionCone()
    {
        if (coneCenterData.direction == Vector3.zero) return;
        Vector3 pivot = coneCenterData.pivot;
        if (IsPivotObstructed(pivot)) return;

        Vector3 forward = coneCenterData.direction.normalized;
        float halfAngle = visionConeData.coneAngleRange * 0.5f;
        List<Transform> objectsFound = new List<Transform>();

        for (float angle = -halfAngle; angle <= halfAngle; angle += 2.5f)
        {
            Vector3 rayDir = Quaternion.Euler(0, angle, 0) * forward;
            if (Physics.Raycast(pivot, rayDir, out RaycastHit hit, visionConeData.coneSize))
            {
                var colTransform = hit.collider.transform;
                if(!objectsFound.Contains(colTransform))
                    objectsFound.Add(colTransform);

                if (showConeDebug)
                    ShowDebugHitRay(colTransform, pivot, hit);
            }
            else if (showConeDebug)
                Debug.DrawRay(pivot, rayDir * visionConeData.coneSize, Color.green);
        }

        eventHub.NewInterestsFound(objectsFound);
    }

    private void ShowDebugHitRay(Transform hitTransform, Vector3 pivot, RaycastHit hit)
    {
        if (trackedFoodTags.Contains(hitTransform.tag))
            Debug.DrawLine(pivot, hit.point, Color.white);
        else if (trackedFearTags.Contains(hitTransform.tag))
            Debug.DrawLine(pivot, hit.point, Color.red);
        else if (trackedInterestTags.Contains(hitTransform.tag))
            Debug.DrawLine(pivot, hit.point, Color.yellow);
        else
            Debug.DrawLine(pivot, hit.point, Color.green);
    }

    private bool IsPivotObstructed(Vector3 pivot)
    {
        Collider[] obstacles = FindObjectsOfType<Collider>();
        foreach (var col in obstacles)
            if (((1 << col.gameObject.layer) & LayerMask.GetMask("Obstacles")) != 0)
                if (col.bounds.Contains(pivot))
                    return true;
        return false;
    }
}

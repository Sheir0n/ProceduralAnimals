using System.Collections;
using System.Collections.Generic;
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

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        eventHub.OnActionChanged += DisableSensesOnDeath;
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

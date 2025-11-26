using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static AnimalAI;

public struct LerpedLookData
{
    public Vector3 pivot;
    public Vector3 direction;
    public LerpedLookData(Vector3 pivot, Vector3 direction)
    {
        this.pivot = pivot;
        this.direction = direction;
    }
}

public class AnimalSenses : MonoBehaviour
{
    protected AnimalEventHub eventHub { private set; get; }
    protected LerpedLookData coneCenterData { private set; get; }
    protected bool deathDisableSenses { private set; get; } = false;

    [SerializeField] protected VisionConeData visionConeData;

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
        eventHub.OnActionChanged += DisableSensesOnDeath;
    }

    protected virtual void Update()
    {
        coneCenterData = eventHub.RequestLookConeSetCenter();
    }

    private void DisableSensesOnDeath(AIAction newAction)
    {
        if (newAction == AIAction.Death)
            deathDisableSenses = true;
    }
}

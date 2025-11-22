using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    protected AnimalEventHub eventHub;

    protected LerpedLookData coneCenterData;
    [SerializeField] protected VisionConeData visionConeData;

    protected virtual void Awake()
    {
        eventHub = GetComponent<AnimalEventHub>();
    }


    protected virtual void Update()
    {
        coneCenterData = eventHub.RequestLookConeSetCenter();
    }
}

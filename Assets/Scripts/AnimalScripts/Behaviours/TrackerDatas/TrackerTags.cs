using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TrackerData
{
    [TagSelector]
    public string tag;

    [Range(0f, 1f)]
    public float importance = 0.5f;

    [Range(0f, 60f)]
    public float memoryTimeInSec = 5f;

    [Range(0f, 100f)]
    public float forgetMinRange = 0f;

    [Range(0f,100f)]
    public float maxTrackDistance = 10f;
}

[System.Serializable]
public class LookTrackerData : TrackerData
{
    public enum ObjectType { Obstacle, RestSpot, Prey, Danger, OtherAnimal}
    public ObjectType objectType;
}

[CreateAssetMenu(fileName = "TrackerTagData", menuName = "AI/Behavior/Tracker Data")]
public class TrackerTags : ScriptableObject
{
    public List<LookTrackerData> lookTrackerTags = new List<LookTrackerData>();
    public List<TrackerData> fearTrackerTags = new List<TrackerData>();
    public List<TrackerData> foodTrackerTags = new List<TrackerData>();
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CreatorData", menuName = "AI/Animal Creator Settings")]
public class ScriptableCreator : ScriptableObject
{
    [Header("Segment Datas")]
    [SerializeField] public List<SegmentData> spineSegmentData = new List<SegmentData>();
    [SerializeField] public List<AnimalLimbData> animalLimbData = new List<AnimalLimbData>();
    [SerializeField] public AnimalHeadData animalHeadData;

    [Header("Mouth Collider Data")]
    [SerializeField] public GameObject mouthColliderPrefab;

    [Header("True if attach to head segments, false if to body")]
    [SerializeField] public bool attachMouthToHeadSegment;
    [SerializeField] public int mouthParentId;
}

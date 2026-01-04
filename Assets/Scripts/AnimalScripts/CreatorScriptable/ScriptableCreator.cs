using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CreatorData", menuName = "AI/Animal Creator Settings")]
public class ScriptableCreator : ScriptableObject
{
    [Header("Ustawienia krêgos³upa")]
    [SerializeField] public List<SegmentData> spineSegmentData = new List<SegmentData>();
    [SerializeField] public Color spineColor = Color.white;

    [Header("Ustawienia koñczyn")]
    [SerializeField] public List<AnimalLimbData> animalLimbData = new List<AnimalLimbData>();
    [Header("Ustawienia g³owy")]
    [SerializeField] public AnimalHeadData animalHeadData;

    [Header("Prefabrykant otworu gêbowego")]
    [SerializeField] public GameObject mouthColliderPrefab;

    [Header("Pozycja otworu gêbowego (czy podpi¹æ do segmentu g³owy czy bezpoœrednio do cia³a)")]
    [SerializeField] public bool attachMouthToHeadSegment;
    [SerializeField] public int mouthParentId;
}

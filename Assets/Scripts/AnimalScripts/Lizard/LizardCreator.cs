using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LizardCreator : AnimalCreator
{
    [Header("Mouth Collider Data")]
    [SerializeField] private GameObject mouthColliderPrefab;
    [SerializeField] private int mouthColliderSegmentId;

    void Start()
    {
        GenerateBody();
        GenerateHead();
        GenerateLimbs();

        GameObject mouthCollider = Instantiate(mouthColliderPrefab);
        animalHead.AttachMouthCollider(mouthCollider, mouthColliderSegmentId);
        mouthCollider.GetComponent<AnimalMouthCollider>().OnInstantiate();
    }
}

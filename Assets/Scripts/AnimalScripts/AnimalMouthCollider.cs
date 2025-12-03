using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class AnimalMouthCollider : MonoBehaviour
{
    AnimalEventHub eventHub;
    protected List<string> detectionTags = new List<string>();
    private CapsuleCollider mouthCollider;

    public virtual void OnInstantiate()
    {
        eventHub = transform.parent.parent.GetComponent<AnimalEventHub>();
        mouthCollider = transform.GetComponent<CapsuleCollider>();
        eventHub.OnMouthHookRequest += GetThisHook;
    }
    private void Update()
    {
        CheckMouthCollider();
    }

    private void CheckMouthCollider()
    {
        if (mouthCollider == null) return;

        Vector3 point1 = mouthCollider.transform.position + mouthCollider.center + Vector3.up * ((mouthCollider.height / 2) - mouthCollider.radius);
        Vector3 point2 = mouthCollider.transform.position + mouthCollider.center - Vector3.up * ((mouthCollider.height / 2) - mouthCollider.radius);
        float radius = mouthCollider.radius;

        Collider[] hits = Physics.OverlapCapsule(point1, point2, radius);

        foreach (Collider hit in hits)
            if (detectionTags.Contains(hit.transform.tag) && eventHub != null)
                eventHub.AttemptBite(hit);
    }

    public bool CheckIfOtherInMouth(Collider other)
    {
        if (mouthCollider == null || other == null)
            return false;

        return mouthCollider.bounds.Intersects(other.bounds);
    }

    private AnimalMouthCollider GetThisHook() => this;
}

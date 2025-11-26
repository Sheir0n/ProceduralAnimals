using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LizardMouthCollider : AnimalMouthCollider
{
    public override void OnInstantiate()
    {
        base.OnInstantiate();
        detectionTags.Add("Beetle");
    }
}

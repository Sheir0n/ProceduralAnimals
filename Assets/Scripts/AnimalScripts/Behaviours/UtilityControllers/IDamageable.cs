using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    public void TakeDamage(int amount);
    public float GetHealth();
    public void OnSnatchAttachTo(Transform predatorTransform);
}

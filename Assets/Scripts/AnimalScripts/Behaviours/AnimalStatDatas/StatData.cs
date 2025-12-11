using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatData", menuName = "AI/Behavior/AnimalStats")]
public class StatData : ScriptableObject
{
    public float maxHealth;
    public float maxSaturation;
    public float maxEnergy;

    public float randomnessAmount;
}

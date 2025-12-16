using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class ActionController : ScriptableObject
{
    protected float energyDrainRate = 0;
    protected float saturationDrainRate = 0;

    [SerializeField] protected ActionID actionID = null;
}

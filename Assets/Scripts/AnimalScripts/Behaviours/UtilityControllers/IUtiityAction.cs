using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUtilityAction
{
    AnimalAI.AIAction AIAction { get; }
    string DebugName();
    void OnInstantiate(Transform transform, AnimalEventHub eventHub, AnimalAnimator animator, float energyDrainRate, float saturationDrainRate);
    void Enter();
    void Update();
    void AlwaysUpdate();
    void Exit();
    float GetUtilityScore(AnimalStats stats, IUtilityAction currAction);
    void CalculateStats(AnimalStats stats);
}

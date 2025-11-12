using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUtilityAction
{
    string DebugName();
    void Enter();
    void Update();
    void Exit();
    float GetUtilityScore(AnimalStats stats, IUtilityAction currAction);
    void CalculateStats(AnimalStats stats);
}

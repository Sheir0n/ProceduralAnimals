using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AnimalAI;

public class AnimalEventHub : MonoBehaviour
{
    public event Action<IReadOnlyAnimalStats> OnInitializeStats;
    public event Action<AIAction> OnActionChanged;

    public void SendInitializeRequest(IReadOnlyAnimalStats stats) => OnInitializeStats?.Invoke(stats);
    public void SendAIStateChange(AIAction newAction) => OnActionChanged?.Invoke(newAction);
}

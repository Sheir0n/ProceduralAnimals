using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AnimalAI;

public interface IAnimalObserver
{
    void OnActionChanged(AIAction actionEnum);
    void OnAnimalAIInitialize(IReadOnlyAnimalStats statsHook);
}

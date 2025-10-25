using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LizardCreator : AnimalCreator
{
    void Start()
    {
        GenerateBody();
        GenerateLimbs();
    }
}

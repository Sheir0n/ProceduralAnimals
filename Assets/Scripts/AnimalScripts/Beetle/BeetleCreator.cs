using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeetleCreator : AnimalCreator
{
    void Start()
    {
        GenerateBody();
        GenerateHead();
        GenerateLimbs();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.U2D;
using static UnityEngine.Rendering.DebugUI.Table;

public class SnakeCreator : AnimalCreator
{
    void Start()
    {
        GenerateBody();
    }
}

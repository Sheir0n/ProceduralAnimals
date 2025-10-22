using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerGeneral : MonoBehaviour
{
    [SerializeField] private int targetFrameRate = 144;
    void Awake()
    {
        Application.targetFrameRate = this.targetFrameRate;
    }

}

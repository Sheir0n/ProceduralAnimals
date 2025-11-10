using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public interface IAnimalBehavior
{
    void Enter();        
    void Update();       
    void Exit();
    Vector3? MoveTargetPosition { get; }
    Vector3? LookTargetPosition { get; }
    bool? LookAtTarget { get; }

}

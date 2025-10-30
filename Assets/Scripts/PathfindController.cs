using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.AI;

public class PathfindController : MonoBehaviour
{
    private NavMeshAgent agent;
    [SerializeField] bool enableMouseFollow = false;
    [SerializeField] bool enableMouseLook = false;
    public Vector3 moveTargetPos { get; private set; } = Vector3.zero;
    public Vector3 lookTargetPos { get; private set; } = Vector3.zero;
    public bool lookAtTarget { get; private set; } = false;

    void Start()
    {
        agent = transform.GetComponentInParent<NavMeshAgent>();
    }
    void Update()
    {
        if (Input.GetMouseButton(1) && enableMouseFollow)
        {
            Ray targetMovePos = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(targetMovePos, out var hitInfo))
            {
                moveTargetPos = hitInfo.point;
                agent.SetDestination(moveTargetPos);
            }
        }

        if (Input.GetMouseButton(0) && enableMouseFollow)
        {
            Ray targetLookPos = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(targetLookPos, out var hitInfo))
            {
                lookTargetPos = hitInfo.point;
                lookAtTarget = true;
            }
        }
        else
            lookAtTarget = false;
    }

    public bool IsMoving()
    {
        return agent.velocity.sqrMagnitude > 0.01f;
    }

    public float GetVelocity()
    {
        return agent.velocity.magnitude;
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathfindController : MonoBehaviour
{
    private NavMeshAgent agent;
    [SerializeField] bool enableMouseFollow = false;

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
                agent.SetDestination(hitInfo.point);
        }
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


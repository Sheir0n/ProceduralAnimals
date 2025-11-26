using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UI;
using UnityEngine;

public class LizardSenses : AnimalSenses
{

    [SerializeField] private bool showConeDebug = false;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        if (deathDisableSenses)
            return;

        base.Update();
        CheckVisionCone();
    }

    private void CheckVisionCone()
    {
        if (coneCenterData.direction == Vector3.zero) return;
        Vector3 pivot = coneCenterData.pivot;
        if (IsPivotObstructed(pivot)) return;

        Vector3 forward = coneCenterData.direction.normalized;
        float halfAngle = visionConeData.coneAngleRange * 0.5f;
        for (float angle = -halfAngle; angle <= halfAngle; angle += 2.5f)
        {
            Vector3 rayDir = Quaternion.Euler(0, angle, 0) * forward;

            if (Physics.Raycast(pivot, rayDir, out RaycastHit hit, visionConeData.coneSize))
            {
                var colTransform = hit.collider.transform;
                var parent = colTransform.parent;

                if (parent != null && parent.CompareTag("Rock"))
                {
                    eventHub.NewRestSpotFound(parent.transform);
                    eventHub.NewInterestSpotFound(parent.transform);

                    if (showConeDebug)
                        Debug.DrawLine(pivot, hit.point, Color.yellow);
                }
                else if (colTransform != null && colTransform.CompareTag("Lizard"))
                {
                    eventHub.NewInterestSpotFound(colTransform);

                    if (showConeDebug)
                        Debug.DrawLine(pivot, hit.point, Color.red);
                }
                else if (colTransform != null && colTransform.CompareTag("Beetle"))
                {
                    eventHub.NewHuntTargetFound(colTransform);
                    eventHub.NewInterestSpotFound(colTransform);

                    if (showConeDebug)
                        Debug.DrawLine(pivot, hit.point, Color.white);
                }
                else
                {
                    if (showConeDebug)
                        Debug.DrawLine(pivot, hit.point, Color.white);

                }
            }
            else
            {
                if (showConeDebug)
                    Debug.DrawRay(pivot, rayDir * visionConeData.coneSize, Color.green);
            }
        }
    }

    private bool IsPivotObstructed(Vector3 pivot)
    {
        Collider[] obstacles = FindObjectsOfType<Collider>();
        foreach (var col in obstacles)
            if (((1 << col.gameObject.layer) & LayerMask.GetMask("Obstacles")) != 0)
                if (col.bounds.Contains(pivot))
                    return true;
        return false;
    }
}

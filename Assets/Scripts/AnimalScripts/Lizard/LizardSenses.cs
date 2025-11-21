using System.Collections;
using System.Collections.Generic;
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
        base.Update();

        CheckVisionCone();
    }

    private void CheckVisionCone()
    {
        if (coneCenterData.direction == Vector3.zero) return;

        Vector3 pivot = coneCenterData.pivot;
        Vector3 forward = coneCenterData.direction.normalized;

        float halfAngle = visionConeData.coneAngleRange * 0.5f;
        for (float angle = -halfAngle; angle <= halfAngle; angle += 2.5f)
        {
            Vector3 rayDir = Quaternion.Euler(0, angle, 0) * forward;

            if (Physics.Raycast(pivot, rayDir, out RaycastHit hit, visionConeData.coneSize))
            {
                var parent = hit.collider.transform.parent;
                if (parent != null && parent.CompareTag("Rock"))
                {
                    if (showConeDebug)
                        Debug.DrawLine(pivot, hit.point, Color.yellow);
                }
                else
                {
                    if (showConeDebug)
                        Debug.DrawLine(pivot, hit.point, Color.red);

                }
            }
            else
            {
                if (showConeDebug)
                    Debug.DrawRay(pivot, rayDir * visionConeData.coneSize, Color.green);
            }
        }
    }
}

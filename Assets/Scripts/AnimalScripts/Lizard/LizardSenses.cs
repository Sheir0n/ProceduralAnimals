using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LizardSenses : AnimalSenses
{

    [SerializeField] private bool showConeDebug = false;

    private List<Vector3> restingSpots = new List<Vector3>();
    private const int maxSpotMemorySlots = 5;
    private bool foundFirstSpot = false;

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
        if (IsPivotObstructed(pivot)) return;

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

                    StoreRockUnique(parent.transform.position);
                    if(!foundFirstSpot)
                    {
                        foundFirstSpot = true;
                        eventHub.FoundFirstRestSpot();
                    }

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

    private bool IsPivotObstructed(Vector3 pivot)
    {
        Collider[] obstacles = FindObjectsOfType<Collider>();
        foreach (var col in obstacles)
            if (((1 << col.gameObject.layer) & LayerMask.GetMask("Obstacles")) != 0)
                if (col.bounds.Contains(pivot))
                    return true;
        return false;
    }

    private void StoreRockUnique(Vector3 rockPosition)
    {
        if (restingSpots.Contains(rockPosition))
            return;

        restingSpots.Add(rockPosition);
        if (restingSpots.Count > maxSpotMemorySlots)
        {
            Vector3 farthest = restingSpots
                .OrderByDescending(r => Vector3.Distance(transform.position, r))
                .First();

            restingSpots.Remove(farthest);
        }
    }

    private Vector3? GetNearestRestingSpot()
    {
        if (restingSpots == null || restingSpots.Count == 0)
            return null;

        Vector3 currentPos = transform.position;
        Vector3 nearest = restingSpots
            .OrderBy(spot => (spot - currentPos).sqrMagnitude)
            .First();

        return nearest;
    }
}

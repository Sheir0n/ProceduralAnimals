using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class AnimalJoint : MonoBehaviour
{
    public Quaternion segmentRotation { get; protected set; }
    public Vector3 segmentPosition { get; protected set; }
    public Vector3 segmentScale { get; protected set; }
    public float distanceConstraint { get; protected set; }
    public float angularConstraint { get; protected set; }
    public float prefferedAngle { get; protected set; }
    public int segmentId { get; protected set; } = 0;
    public Quaternion segmentLerpRotation { get; protected set; }
    public Vector3 segmentLerpPosition { get; protected set; }

    virtual public void AfterInstantiate(Vector3 _segPosition, Quaternion _segRotation, Vector3 _segScale, float _distanceConstraint, float _angularConstraint, float _prefferedAngle, int _id)
    {
        segmentRotation = _segRotation;
        segmentLerpRotation = _segRotation;
        segmentPosition = _segPosition;
        segmentLerpPosition = _segPosition;
        segmentScale = _segScale;
        distanceConstraint = _distanceConstraint;
        angularConstraint = _angularConstraint;
        prefferedAngle = _prefferedAngle;
        segmentId = _id;
    }

    virtual public void AfterInstantiate(float _distanceConstraint, float _angularConstraint, float _prefferedAngle, int _id)
    {
        segmentRotation = transform.rotation;
        segmentPosition = transform.position;
        segmentScale = transform.localScale;
        distanceConstraint = _distanceConstraint;
        angularConstraint = _angularConstraint;
        prefferedAngle = _prefferedAngle;
        segmentId = _id;
    }

    public void SetPosition(Vector3 _position) => segmentPosition = _position;
    public void SetRotation(Quaternion _rotation) => segmentRotation = _rotation;
    public void SetScale(Vector3 _scale) => segmentScale = _scale;

    public void UpdateSegmentTransform()
    {
        transform.rotation = Quaternion.Euler(90f, segmentRotation.eulerAngles.y, 0f);
        segmentLerpRotation = segmentRotation;
        transform.position = segmentPosition;
        segmentLerpPosition = segmentPosition;
        transform.localScale = segmentScale;
    }

    public void UpdateLerpPosition(Vector3 prevSegmentPosition)
    {
        Vector3 allowedDir = Quaternion.Euler(0f, segmentLerpRotation.eulerAngles.y, 0f) * Vector3.forward;
        segmentLerpPosition = prevSegmentPosition - allowedDir * distanceConstraint;
    }

    public void UpdateLerpRotation(float lerpSpeed)
    {
        Vector3 eulerLerp = new Vector3(
            90,
            Mathf.LerpAngle(segmentLerpRotation.eulerAngles.y, segmentRotation.eulerAngles.y, lerpSpeed * Time.deltaTime),
            0
        );

        segmentLerpRotation = Quaternion.Euler(eulerLerp);
    }

    public void UpdateSegmentLerpTransform()
    {
        transform.rotation = segmentLerpRotation;
        transform.position = segmentLerpPosition;

        //not currently lerped
        transform.localScale = segmentScale;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AnimalJoint : MonoBehaviour
{
    public Quaternion segmentRotation { get; protected set; }
    public Vector3 segmentPosition { get; protected set; }
    public Vector3 segmentScale { get; protected set; }
    public float distanceConstraint { get; protected set; }
    public float angularConstraint { get; protected set; }
    public float prefferedAngle { get; protected set; }
    public int segmentId { get; protected set; } = 0;

    virtual public void AfterInstantiate(Vector3 _segPosition, Quaternion _segRotation, Vector3 _segScale, float _distanceConstraint, float _angularConstraint, float _prefferedAngle, int _id)
    {
        segmentRotation = _segRotation;
        segmentPosition = _segPosition;
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
        transform.rotation = segmentRotation;
        transform.position = segmentPosition;
        transform.localScale = segmentScale;
    }
}

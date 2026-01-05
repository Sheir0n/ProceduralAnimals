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

    public float yaw { get; protected set; }
    public float lerpedYaw { get; protected set; }
    public float yOffset { get; protected set; } = 0;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private GameObject sprite;

    virtual public void AfterInstantiate(Vector3 segPosition, Quaternion segRotation, Vector3 segScale, float distanceConstraint, float angularConstraint, float prefferedAngle, float yOffset, GameObject sprite, int _id)
    {
        segmentRotation = segRotation;
        segmentLerpRotation = segRotation;
        segmentPosition = segPosition;
        segmentLerpPosition = segPosition;
        segmentScale = segScale;
        this.distanceConstraint = distanceConstraint;
        this.angularConstraint = angularConstraint;
        this.prefferedAngle = prefferedAngle;
        this.yOffset = yOffset;
        segmentId = _id;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        this.sprite = sprite;
    }

    virtual public void AfterInstantiate(float distanceConstraint, float angularConstraint, float prefferedAngle, float yOffset, GameObject sprite, int id)
    {
        segmentRotation = transform.rotation;
        segmentPosition = transform.position;
        segmentScale = transform.localScale;
        this.distanceConstraint = distanceConstraint;
        this.angularConstraint = angularConstraint;
        this.prefferedAngle = prefferedAngle;
        this.yOffset = yOffset;
        segmentId = id;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        this.sprite = sprite;
    }

    public void SetPosition(Vector3 _position)
    {
        segmentPosition = _position;
    }
    public void SetRotation(float newYaw)
    {
        yaw = newYaw;
    }

    public void SetScale(Vector3 _scale) => segmentScale = _scale;

    public void UpdateSegmentTransform()
    {
        transform.rotation = Quaternion.Euler(90f, segmentRotation.eulerAngles.y, 0f);
        segmentRotation = Quaternion.Euler(90f, yaw, 0f);
        segmentLerpRotation = segmentRotation;
        transform.position = segmentPosition + new Vector3(0, yOffset, 0);
        segmentLerpPosition = segmentPosition;
        transform.localScale = segmentScale;
    }

    public void UpdateLerpPosition(Vector3 prevSegmentPosition)
    {
        Vector3 allowedDir = Quaternion.Euler(0f, lerpedYaw, 0f) * Vector3.forward;
        segmentLerpPosition = prevSegmentPosition - allowedDir * distanceConstraint;
    }

    public void UpdateLerpRotation(float lerpSpeed)
    {
        float t = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);
        lerpedYaw = Mathf.LerpAngle(lerpedYaw, yaw, t);
        segmentLerpRotation = Quaternion.Euler(90f, lerpedYaw, 0f);
    }

    public void UpdateSegmentLerpTransform()
    {
        transform.rotation = segmentLerpRotation;
        transform.position = segmentLerpPosition + new Vector3(0, yOffset, 0);

        //not currently lerped
        transform.localScale = segmentScale;
    }

    public void SetColorFade(float amount)
    {
        if (spriteRenderer == null) return;
        amount = Mathf.Clamp01(amount);
        Color resultColor = Color.Lerp(originalColor, Color.gray, amount);
        spriteRenderer.color = resultColor;
    }
}

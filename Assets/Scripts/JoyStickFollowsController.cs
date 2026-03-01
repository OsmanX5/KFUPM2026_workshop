using System;
using System.Xml;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;

public class JoyStickFollowsController : MonoBehaviour
{
    [Header("Tracked Device")]
    [SerializeField] private TrackedPoseDriver controller;

    [Header("Rotation Limits (Degrees)")]
    [SerializeField] private Vector3 minRotation = new Vector3(-180f, -180f, -180f);
    [SerializeField] private Vector3 maxRotation = new Vector3(180f, 180f, 180f);

    [Header("Events")]
    [SerializeField] private UnityEvent<Vector3> onRotationChanged;

    private Vector3 previousNormalizedRotation;
    [SerializeField]
    bool applyMinMaxLimits = false;

    [SerializeField]
    bool reverseX = false;
    public UnityEvent<float> OnXRotationChangedMinus1To1 = new UnityEvent<float>();

    [SerializeField]
    bool reverseZ = false;
    public UnityEvent<float> OnZRotationChangedMinus1To1 = new UnityEvent<float>();
    [ShowInInspector]
    /// <summary>
    /// Gets the current rotation normalized to -1 to 1 range based on min/max limits.
    /// </summary>
    public Vector3 NormalizedRotation { get; private set; }
    [ShowInInspector]
    float XRotPrecent = 0, ZRotPrecent;
    [ShowInInspector]
    float xval, zval;
    private void Start()
    {
        previousNormalizedRotation = GetNormalizedRotation();
        NormalizedRotation = previousNormalizedRotation;
    }

    private void LateUpdate()
    {
        if (controller == null)
            return;

        ApplyRotation();
        NormalizedRotation = GetNormalizedRotation();

        if (NormalizedRotation != previousNormalizedRotation)
        {
            onRotationChanged?.Invoke(NormalizedRotation);
            previousNormalizedRotation = NormalizedRotation;
        }
        xval = NormalizeAngle(transform.localEulerAngles.x);
        xval = Mathf.Clamp(xval, minRotation.x, maxRotation.x);
        XRotPrecent = 1 - (2 * (maxRotation.x - xval) / (maxRotation.x - minRotation.x));
        zval = NormalizeAngle(transform.localEulerAngles.z);
        zval = Mathf.Clamp(zval, minRotation.z, maxRotation.z);
        ZRotPrecent = 1 - (2 * (maxRotation.z - zval) / (maxRotation.z - minRotation.z));


        OnXRotationChangedMinus1To1.Invoke(reverseX ? -XRotPrecent : XRotPrecent);
        OnZRotationChangedMinus1To1.Invoke(reverseZ ? -ZRotPrecent : ZRotPrecent);
    }

    /// <summary>
    /// Applies the tracked rotation to this object, clamped to min/max limits.
    /// </summary>
    private void ApplyRotation()
    {
        Vector3 trackedEuler = GetTrackedRotation();

        float x = NormalizeAngle(trackedEuler.x);
        float y = NormalizeAngle(trackedEuler.y);
        float z = NormalizeAngle(trackedEuler.z);

        // Clamp each axis
        x = Mathf.Clamp(x, minRotation.x, maxRotation.x);
        y = Mathf.Clamp(y, minRotation.y, maxRotation.y);
        z = Mathf.Clamp(z, minRotation.z, maxRotation.z);

        transform.localEulerAngles = new Vector3(x, y, z);
    }

    /// <summary>
    /// Gets the rotation from the tracked pose driver.
    /// </summary>
    private Vector3 GetTrackedRotation()
    {
        if (controller == null)
            return Vector3.zero;

        return controller.transform.localEulerAngles;
    }

    /// <summary>
    /// Converts the current rotation to a -1 to 1 range based on min/max limits.
    /// </summary>
    private Vector3 GetNormalizedRotation()
    {
        Vector3 currentEuler = GetTrackedRotation();

        float x = NormalizeAngle(currentEuler.x);
        float y = NormalizeAngle(currentEuler.y);
        float z = NormalizeAngle(currentEuler.z);

        return new Vector3(
            NormalizeToRange(x, minRotation.x, maxRotation.x),
            NormalizeToRange(y, minRotation.y, maxRotation.y),
            NormalizeToRange(z, minRotation.z, maxRotation.z)
        );
    }

    /// <summary>
    /// Converts an angle from 0-360 to -180 to 180 range.
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    /// <summary>
    /// Normalizes a value from min/max range to -1 to 1 range.
    /// </summary>
    private float NormalizeToRange(float value, float min, float max)
    {
        if (!applyMinMaxLimits)
        {
            return value;
        }
        if (Mathf.Approximately(max, min))
            return 0f;

        float midPoint = (min + max) / 2f;
        float halfRange = (max - min) / 2f;

        return Mathf.Clamp((value - midPoint) / halfRange, -1f, 1f);
    }
}

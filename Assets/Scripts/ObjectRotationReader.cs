using UnityEngine;
using UnityEngine.Events;

public class ObjectRotationReader : MonoBehaviour
{
    [Header("Rotation Limits (Degrees)")]
    [SerializeField] private Vector3 minRotation = new Vector3(-180f, -180f, -180f);
    [SerializeField] private Vector3 maxRotation = new Vector3(180f, 180f, 180f);

    [Header("Events")]
    [SerializeField] private UnityEvent<Vector3> onRotationChanged;

    private Vector3 previousNormalizedRotation;

    /// <summary>
    /// Gets the current rotation normalized to -1 to 1 range based on min/max limits.
    /// </summary>
    public Vector3 NormalizedRotation { get; private set; }

    private void Start()
    {
        previousNormalizedRotation = GetNormalizedRotation();
        NormalizedRotation = previousNormalizedRotation;
    }

    private void LateUpdate()
    {
        //ClampRotation();

        NormalizedRotation = GetNormalizedRotation();

        if (NormalizedRotation != previousNormalizedRotation)
        {
            onRotationChanged?.Invoke(NormalizedRotation);
            previousNormalizedRotation = NormalizedRotation;
        }
    }

    /// <summary>
    /// Clamps the object's rotation to stay within min/max limits.
    /// </summary>
    private void ClampRotation()
    {
        Vector3 currentEuler = transform.localEulerAngles;

        // Convert to signed angles (-180 to 180)
        float x = NormalizeAngle(currentEuler.x);
        float y = NormalizeAngle(currentEuler.y);
        float z = NormalizeAngle(currentEuler.z);

        // Clamp each axis
        x = Mathf.Clamp(x, minRotation.x, maxRotation.x);
        y = Mathf.Clamp(y, minRotation.y, maxRotation.y);
        z = Mathf.Clamp(z, minRotation.z, maxRotation.z);

        transform.localEulerAngles = new Vector3(x, y, z);
    }

    /// <summary>
    /// Converts the current rotation to a -1 to 1 range based on min/max limits.
    /// </summary>
    private Vector3 GetNormalizedRotation()
    {
        Vector3 currentEuler = transform.localEulerAngles;

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
        if (Mathf.Approximately(max, min))
            return 0f;

        float midPoint = (min + max) / 2f;
        float halfRange = (max - min) / 2f;

        return Mathf.Clamp((value - midPoint) / halfRange, -1f, 1f);
    }
}

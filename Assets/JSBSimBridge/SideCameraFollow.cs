using UnityEngine;

public class SideCameraFollow : MonoBehaviour
{
    [SerializeField]
    Transform targetTransform;
    [SerializeField]
    Vector3 offset = new Vector3(0f, 0f, -10f); 

    // Update is called once per frame
    void LateUpdate()
    {
        if (targetTransform == null) return;
        transform.position = targetTransform.position + offset;
        transform.LookAt(targetTransform);
    }
}

using UnityEngine;

public class CrashDetection : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Crash detected with object: " + other.gameObject.name);
    }
}

using UnityEngine;

public class MultiDisplayes : MonoBehaviour
{
    void Start()
    {
        // Activate Display 1 (index 1) if available
        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
        }

        // Activate Display 2 (index 2) if available
        if (Display.displays.Length > 2)
        {
            Display.displays[2].Activate();
        }

        Debug.Log($"Number of displays detected: {Display.displays.Length}");
    }
}

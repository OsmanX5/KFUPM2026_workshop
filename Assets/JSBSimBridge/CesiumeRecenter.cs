using System.Collections;
using CesiumForUnity;
using Sirenix.OdinInspector;
using UnityEngine;

public class CesiumeRecenter : MonoBehaviour
{

    [SerializeField]
    CesiumGlobeAnchor f15Anchor;
    [SerializeField]
    CesiumGeoreference cesiumGeoreference;
    [SerializeField]
    float recenterInterval = 5f;

    void Start()
    {
        StartCoroutine(UpdatingCenterRoutine());
    }

    IEnumerator UpdatingCenterRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(recenterInterval);
            RecenterTheF15AsWorldCenter();
        }
    }


    [Button("Recenter F15 as World Center")]
    void RecenterTheF15AsWorldCenter()
    {
        if (f15Anchor == null || cesiumGeoreference == null)
        {
            Debug.LogError("F15 Anchor or Cesium Georeference is not assigned.");
            return;
        }
        double f15Longitude = f15Anchor.longitudeLatitudeHeight.x;
        double f15Latitude = f15Anchor.longitudeLatitudeHeight.y;
        double f15Height = f15Anchor.longitudeLatitudeHeight.z;
        // Set the CesiumGeoreference's origin to the F15's position
        cesiumGeoreference.SetOriginLongitudeLatitudeHeight(f15Longitude, f15Latitude, f15Height);

        Debug.Log("Recentered Cesium Georeference to F15 position: " + f15Longitude + ", " + f15Latitude + ", " + f15Height);
    }
}

using UnityEngine;
using System.Collections;

public class BackgroundHandler : MonoBehaviour
{
    public GameObject CameraGO;
    private const float paralaxMultiplier = .7f;

    public void Start()
    {

    }

    public void Update()
    {

    }

    public void MoveCameraDown(float mainMovement)
    {
        CameraGO.transform.localPosition = new Vector3(
            CameraGO.transform.localPosition.x,
            CameraGO.transform.localPosition.y - (mainMovement * paralaxMultiplier),
            CameraGO.transform.localPosition.z);
    }

    public void MoveCameraUp(float mainMovement)
    {
        CameraGO.transform.localPosition = new Vector3(
            CameraGO.transform.localPosition.x,
            CameraGO.transform.localPosition.y + (mainMovement * paralaxMultiplier),
            CameraGO.transform.localPosition.z);
    }
}

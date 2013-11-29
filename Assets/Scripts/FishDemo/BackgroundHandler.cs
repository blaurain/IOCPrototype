using UnityEngine;
using System.Collections;

public enum WaveMotion
{
    UpLeft,
    DownLeft,
    UpRight,
    DownRight
}

public class BackgroundHandler : MonoBehaviour
{
    public GameObject CameraGO;
    public GameObject Fisherman;
    public GameObject LineAnchor;
    public RageSpline Line;
    public GameObject[] Waves;
    private WaveMotion CurrentMotion;
    private const float FishermanBobSpeed = .2f;
    private const float FishermanRotationSpeed = 1.2f;
    private readonly Vector3 TopPosition = new Vector3(-12.0f, 40.0f, -10);
    private const float TopZoom = 20f;
    private readonly Vector3 StartPosition = new Vector3(0, 1, -10);
    private const float StartZoom = 10f;
    private const float paralaxMultiplier = .5f;
    private const float MaxCameraY = -966f;
    private const float StartCameraY = 1f;
    private const float CameraPanSpeed = 2f;
    private const float CameraZoomSpeed = 20f;

    private const float WaveMoveSpeedX = .7f;
    private const float WaveMoveSpeedY = .2f;
    private const float WaveMoveTime = 2.7f;
    private const float WaveMovePercentageFalloff = .1f;
    private float WaveTimer;
    private float WaveMoveDirection;

    public void Start()
    {
        CameraGO.transform.localPosition = TopPosition;
        CameraGO.camera.orthographicSize = TopZoom;
        WaveTimer = 0;
        WaveMoveDirection = 1f;
        CurrentMotion = WaveMotion.UpRight;
    }

    public void UpdateWaves()
    {
        //move waves if at top

        Vector3 ToMoveEvens;
        Vector3 ToMoveOdds;
        switch (CurrentMotion)
        {
            case WaveMotion.UpRight:
                ToMoveEvens = new Vector3(
                    WaveMoveSpeedX * WaveMoveDirection * Time.deltaTime,
                    WaveMoveSpeedY * -WaveMoveDirection * Time.deltaTime, 0);

                ToMoveOdds = new Vector3(
                    WaveMoveSpeedX * -WaveMoveDirection * Time.deltaTime,
                    WaveMoveSpeedY * WaveMoveDirection * Time.deltaTime, 0);

                Fisherman.transform.Rotate(new Vector3(0, 0, 1), -FishermanRotationSpeed * Time.deltaTime);
                Fisherman.transform.localPosition = new Vector3(
                    Fisherman.transform.localPosition.x,
                    Fisherman.transform.localPosition.y + (FishermanBobSpeed * Time.deltaTime),
                    Fisherman.transform.localPosition.z);
                break;

            case WaveMotion.UpLeft:
                ToMoveEvens = new Vector3(
                    WaveMoveSpeedX * -WaveMoveDirection * Time.deltaTime,
                    WaveMoveSpeedY * -WaveMoveDirection * Time.deltaTime, 0);

                ToMoveOdds = new Vector3(
                    WaveMoveSpeedX * WaveMoveDirection * Time.deltaTime,
                    WaveMoveSpeedY * WaveMoveDirection * Time.deltaTime, 0);
                Fisherman.transform.Rotate(new Vector3(0, 0, 1), FishermanRotationSpeed * Time.deltaTime);
                Fisherman.transform.localPosition = new Vector3(
                    Fisherman.transform.localPosition.x,
                    Fisherman.transform.localPosition.y + (FishermanBobSpeed * Time.deltaTime),
                    Fisherman.transform.localPosition.z);
                break;

            case WaveMotion.DownRight:
                ToMoveEvens = new Vector3(
                    WaveMoveSpeedX * WaveMoveDirection * Time.deltaTime,
                    WaveMoveSpeedY * WaveMoveDirection * Time.deltaTime, 0);

                ToMoveOdds = new Vector3(
                    WaveMoveSpeedX * -WaveMoveDirection * Time.deltaTime,
                    WaveMoveSpeedY * -WaveMoveDirection * Time.deltaTime, 0);
                Fisherman.transform.Rotate(new Vector3(0, 0, 1), -FishermanRotationSpeed * Time.deltaTime);
                Fisherman.transform.localPosition = new Vector3(
                    Fisherman.transform.localPosition.x,
                    Fisherman.transform.localPosition.y - (FishermanBobSpeed * Time.deltaTime),
                    Fisherman.transform.localPosition.z);
                break;

            case WaveMotion.DownLeft:
                ToMoveEvens = new Vector3(
                    WaveMoveSpeedX * -WaveMoveDirection * Time.deltaTime,
                    WaveMoveSpeedY * WaveMoveDirection * Time.deltaTime, 0);

                ToMoveOdds = new Vector3(
                    WaveMoveSpeedX * WaveMoveDirection * Time.deltaTime,
                    WaveMoveSpeedY * -WaveMoveDirection * Time.deltaTime, 0);
                Fisherman.transform.Rotate(new Vector3(0, 0, 1), FishermanRotationSpeed * Time.deltaTime);
                Fisherman.transform.localPosition = new Vector3(
                    Fisherman.transform.localPosition.x,
                    Fisherman.transform.localPosition.y - (FishermanBobSpeed * Time.deltaTime),
                    Fisherman.transform.localPosition.z);
                break;

            default:
                ToMoveEvens = Vector3.zero;
                ToMoveOdds = Vector3.zero;
                Debug.Log("problem with waves");

                break;
        }

        float movePercentage = 1f;
        for (int i = Waves.Length - 1; i >= 0; i--)
        {
            if (i % 2 != 0)
            { //odd
                Waves[i].transform.localPosition = new Vector3(
                    Waves[i].transform.localPosition.x + (ToMoveOdds.x * movePercentage),
                    Waves[i].transform.localPosition.y + (ToMoveOdds.y * movePercentage),
                    Waves[i].transform.localPosition.z);
            }
            else
            { //even
                Waves[i].transform.localPosition = new Vector3(
                    Waves[i].transform.localPosition.x + (ToMoveEvens.x * movePercentage),
                    Waves[i].transform.localPosition.y + (ToMoveEvens.y * movePercentage),
                    Waves[i].transform.localPosition.z);
            }

            movePercentage -= WaveMovePercentageFalloff;
        }

        Line.SetPointWorldSpace(0, LineAnchor.transform.position);
        Line.RefreshMesh();

        WaveTimer += Time.deltaTime;
        if (WaveTimer >= WaveMoveTime)
        {
            switch (CurrentMotion)
            {
                case WaveMotion.UpRight:
                    CurrentMotion = WaveMotion.DownRight;
                    break;

                case WaveMotion.UpLeft:
                    CurrentMotion = WaveMotion.DownLeft;
                    break;

                case WaveMotion.DownRight:
                    CurrentMotion = WaveMotion.UpLeft;
                    break;

                case WaveMotion.DownLeft:
                    CurrentMotion = WaveMotion.UpRight;
                    break;
            }
            //WaveMoveDirection *= -1f;
            WaveTimer = 0f;
        }

    }

    public bool MoveToTop()
    {
        if (ZoomToTop())
        {
            if (Vector3.Distance(CameraGO.transform.localPosition, TopPosition) > .5f)
            {
                CameraGO.transform.localPosition = Vector3.Lerp(
                    CameraGO.transform.localPosition, TopPosition, CameraPanSpeed * Time.deltaTime);
                return false;
            }
            else
            {
                CameraGO.transform.localPosition = TopPosition;
                return true;
            }
        }
        else return false;
    }

    public bool MoveToStart()
    {
        if (Vector3.Distance(CameraGO.transform.localPosition, StartPosition) > .5f)
        {

            CameraGO.transform.localPosition = Vector3.Lerp(
                CameraGO.transform.localPosition, StartPosition, CameraPanSpeed * Time.deltaTime);
            return false;
        }
        else
        {
            CameraGO.transform.localPosition = StartPosition;
            return true;
        }
    }

    public bool ZoomToTop()
    {
        if (CameraGO.camera.orthographicSize < TopZoom)
        {
            CameraGO.camera.orthographicSize += CameraZoomSpeed * Time.deltaTime;
            if (CameraGO.camera.orthographicSize >= TopZoom)
            {
                CameraGO.camera.orthographicSize = TopZoom;
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }

    public bool ZoomToStart()
    {
        if (Vector3.Distance(CameraGO.transform.localPosition, StartPosition) < 8f)
        { //zoom

            if (CameraGO.camera.orthographicSize > StartZoom)
            {
                CameraGO.camera.orthographicSize -= CameraZoomSpeed * Time.deltaTime;
                if (CameraGO.camera.orthographicSize <= StartZoom)
                {
                    CameraGO.camera.orthographicSize = StartZoom;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    public void MoveCameraDown(float mainMovement)
    {
        CameraGO.transform.localPosition = new Vector3(
            CameraGO.transform.localPosition.x,
            CameraGO.transform.localPosition.y - (mainMovement * paralaxMultiplier),
            CameraGO.transform.localPosition.z);

        //rotate back up
        if (CameraGO.transform.localPosition.y < MaxCameraY)
            CameraGO.transform.localPosition = new Vector3(
            CameraGO.transform.localPosition.x,
            StartCameraY,
            CameraGO.transform.localPosition.z);
    }

    public void MoveCameraUp(float mainMovement)
    {
        //rotate back up
        if (CameraGO.transform.localPosition.y > StartCameraY)
            CameraGO.transform.localPosition = new Vector3(
            CameraGO.transform.localPosition.x,
            MaxCameraY,
            CameraGO.transform.localPosition.z);
        
        CameraGO.transform.localPosition = new Vector3(
            CameraGO.transform.localPosition.x,
            CameraGO.transform.localPosition.y + (mainMovement * paralaxMultiplier),
            CameraGO.transform.localPosition.z);
    }

    public void EnableWaves(bool enable)
    {
        //foreach (GameObject wave in Waves)
        //    wave.SetActive(enable);
    }
}

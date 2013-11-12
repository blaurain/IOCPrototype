using UnityEngine;
using System.Collections;

public struct WaterObjectInstance
{
    public float wObjectDepth;
    public WaterObjectType wObjectType;
    public Color wObjectColor;
}

public enum WObjState
{
    Swim,
    Float,
    CaughtCast,
    CaughtReel
}

public class WaterObject : MonoBehaviour
{
    public WaterObjectType ObjType; //set on prefab
    private bool isFish;
    private bool isCaught;
    private bool isHookedLeftSide;
    private const float FishMaxSpeed = 15f;
    private const float FishMinSpeed = 2f;
    private const float TurnaroundBuffer = 6f;
    private const float CaughtAngle = 80f; //degrees
    private Vector3 caughtLocation;
    [SerializeField] private float fishSpeed;
    private WObjState State;

    public Color ObjColor
    {
        get { return gameObject.GetComponent<tk2dSprite>().color; }
    }

    [SerializeField] private bool isFacingLeft;
    private bool Facingleft
    {
        get { return isFacingLeft; }
        set
        {
            isFacingLeft = value;
            if (value) gameObject.transform.localScale = new Vector3(1, 1, 1);
            else gameObject.transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    public void Start()
    {
        switch (ObjType)
        {
            case WaterObjectType.FishType1:
                fishSpeed = Random.Range(FishMinSpeed, FishMaxSpeed * .7f);
                isFish = true;
                State = WObjState.Swim;
                break;

            case WaterObjectType.FishType2:
                fishSpeed = Random.Range(FishMinSpeed, FishMaxSpeed);
                isFish = true;
                State = WObjState.Swim;
                break;

            case WaterObjectType.Boot:
            case WaterObjectType.ExplodingBarrel:
                isFish = false;
                State = WObjState.Float;
                break;
        }

        //choose random direction
        int rndDir = Random.Range(0, 2); //0 or 1
        if (rndDir == 0) Facingleft = true;
        else Facingleft = false;

        //choose random color
        if (isFish) gameObject.GetComponent<tk2dSprite>().color = GetRandomFishColor();


    }

    public void Update()
    {
        switch (State)
        {
            case WObjState.Swim:
                Swim();
                break;

            case WObjState.CaughtCast:
                if (isHookedLeftSide)
                {
                    Debug.Log(transform.parent.localEulerAngles.z);
                    if (gameObject.transform.parent.localEulerAngles.z < CaughtAngle || gameObject.transform.parent.localEulerAngles.z == 0)
                        gameObject.transform.parent.Rotate(0, 0, 50 * Time.deltaTime);

                }
                else
                {
                    Debug.Log("deg: " + gameObject.transform.parent.localEulerAngles.z + " rad: " + gameObject.transform.parent.localPosition.z);
                    if (((gameObject.transform.parent.localEulerAngles.z < 90) && (gameObject.transform.parent.localEulerAngles.z > (- CaughtAngle)))
                        || gameObject.transform.parent.localEulerAngles.z > (360-CaughtAngle))
                            gameObject.transform.parent.Rotate(0, 0, -50 * Time.deltaTime);
                        //|| (gameObject.transform.parent.localEulerAngles.z >= 270 && gameObject.transform.parent.localEulerAngles.z <= 360)
                        //|| gameObject.transform.parent.localEulerAngles.z == 0)
                        

                }
                break;
        }
    }

    public void Caught(Vector3 collisionPoint, bool isCast)
    {
        if (isCast) State = WObjState.CaughtCast;
        else State = WObjState.CaughtReel;

        caughtLocation = collisionPoint;

        if (gameObject.transform.parent.InverseTransformPoint(collisionPoint).x < 0)
            isHookedLeftSide = true;
        else 
            isHookedLeftSide = false;

        transform.parent.localRotation = new Quaternion(0, 0, 0, 1);
    }

    public void OnTriggerEnter(Collider c)
    {
        GameplayManager.DeadFishHere(this);
    }

    private void Swim()
    {
        if (Facingleft)
        {
            transform.parent.localPosition = new Vector3(
                transform.parent.localPosition.x - (fishSpeed * Time.deltaTime),
                transform.parent.localPosition.y,
                transform.parent.localPosition.z);

            if (transform.parent.localPosition.x < -(GameplayManager.ScreenBoundWorldX + TurnaroundBuffer))
            {
                Facingleft = !Facingleft;
                transform.parent.localPosition = new Vector3(
                    -GameplayManager.ScreenBoundWorldX - TurnaroundBuffer,
                    transform.parent.localPosition.y,
                    transform.parent.localPosition.z);
            }
        }
        else
        {
            transform.parent.localPosition = new Vector3(
                transform.parent.localPosition.x + (fishSpeed * Time.deltaTime),
                transform.parent.localPosition.y,
                transform.parent.localPosition.z);

            if (transform.parent.localPosition.x > (GameplayManager.ScreenBoundWorldX + TurnaroundBuffer))
            {
                Facingleft = !Facingleft;
                transform.parent.localPosition = new Vector3(
                    GameplayManager.ScreenBoundWorldX + TurnaroundBuffer,
                    transform.parent.localPosition.y,
                    transform.parent.localPosition.z);
            }
        }


    }

    private Color GetRandomFishColor()
    {
        int rndColor = Random.Range(0, 5);
        switch (rndColor)
        {
            case 0:
                return Color.blue;
            case 1:
                return Color.red;
            case 2:
                return Color.green;
            case 3:
                return Color.cyan;
            case 4:
                return Color.yellow;
            default:
                return Color.red;
        }
    }
}

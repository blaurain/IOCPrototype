using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct WaterObjectInstance
{
    public float Depth;
    public float XPos;
    public float Speed;
    public WaterObjectType Type;
    public Color Color;
    public bool FacingLeft;
    public bool PartOfSchool;
    public int SchoolSize;
}

public enum BillyColors
{
    DarkPink,
    BurntOrange,
    LimeGreen,
    FishYellow,
    LightBlue,
    DarkRed,
    DarkGrey
}    

public enum WObjState
{
    Swim,
    Float,
    CaughtCast,
    CaughtReel,
    SwitchCastToReel
}

public class WaterObject : MonoBehaviour
{
    public static int NumberOfFishTypes = 9;
    public WaterObjectType ObjType; //set on prefab
    private bool isFish;
    private bool isCaught;
    private bool isHookedLeftSide;
    private const float FishSlowBase = 10f;
    private const float FishMediumBase = 14f;
    private const float FishFastBase = 16f;
    private const float FishSuperFastBase = 20f;
    private const float FishMaxSpeed = 15f;
    private const float FishMinSpeed = 5f;
    private const float TurnaroundBuffer = 6f;
    private const float CaughtAngleDefault = .65f; //radians
    private const float AngleBuffer = .05f;
    private const float FishRotateSpeed = 150f;
    private const float HookedSlideSpeed = .17f;
    private const float HookedSlideBuffer = .01f;
    private const float HookMaxSpeed = 20f;
    private const float HookedMaxAngle = .15f; //rad
    private float CaughtAngle;
    private Vector3 caughtLocation;
    
    public WObjState State;

    private bool partOfSchool;
    private List<GameObject> school; //includes self
    public List<GameObject> School
    {
        get { return school; }
        set
        {
            partOfSchool = true;
            school = value;
        }
    }
    public int SchoolSize;
    public bool IsPartOfSchool
    {
        set { partOfSchool = value; }
        get { return partOfSchool; }
    }

    public Color ObjColor
    {
        get { return gameObject.GetComponent<tk2dSprite>().color; }
        set { gameObject.GetComponent<tk2dSprite>().color = value; }
    }

    public int ScoreValue
    {
        get
        {
            switch (ObjType)
            {
                case WaterObjectType.Angelfish:
                    return 100;
                case WaterObjectType.Boot:
                    return 100;
                case WaterObjectType.Eel:
                    return 100;
                case WaterObjectType.Jellyfish:
                    return 100;
                case WaterObjectType.Puffer:
                    return 100;
                case WaterObjectType.Shark:
                    return 100;
                case WaterObjectType.Swordfish:
                    return 100;
                case WaterObjectType.Tire:
                    return 100;
                case WaterObjectType.Trout:
                    return 100;
                default:
                    return 0;
            }
        }
    }

    public float fishSpeed { get; set; }

    [SerializeField]
    private bool isFacingLeft;
    public bool Facingleft
    {
        get { return isFacingLeft; }
        set
        {
            isFacingLeft = value;
            if (value) gameObject.transform.localScale = new Vector3(1, 1, 1);
            else gameObject.transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    public void Awake()
    {
        switch (ObjType)
        {
            case WaterObjectType.Angelfish:
            case WaterObjectType.Eel:
            case WaterObjectType.Jellyfish:
            case WaterObjectType.Trout:
            case WaterObjectType.Puffer:
            case WaterObjectType.Shark:
            case WaterObjectType.Swordfish:
                fishSpeed = GetRandomFishSpeed(ObjType);
                isFish = true;
                State = WObjState.Swim;
                break;

            case WaterObjectType.Boot:
            case WaterObjectType.Tire:
                isFish = false;
                State = WObjState.Float;
                break;

        }

        //choose random direction
        int rndDir = Random.Range(0, 2); //0 or 1
        if (rndDir == 0) Facingleft = true;
        else Facingleft = false;

        //choose random color
        if (isFish) ObjColor = GetRandomFishColor(ObjType);
    }

    public void Update()
    {
        float angDiff;
        switch (State)
        {
            case WObjState.Swim:
                Swim();
                break;

            case WObjState.CaughtCast:
                angDiff = Mathf.DeltaAngle(CaughtAngle, gameObject.transform.parent.localRotation.z);
                //Debug.Log("angle diff: " + angDiff + " local: " + gameObject.transform.parent.localRotation.z);

                if (angDiff > AngleBuffer)
                    gameObject.transform.parent.Rotate(0, 0, -FishRotateSpeed * Time.deltaTime);
                else if (angDiff < -AngleBuffer)
                    gameObject.transform.parent.Rotate(0, 0, FishRotateSpeed * Time.deltaTime);

                FakeSwingHandler();
                break;

            case WObjState.CaughtReel:
                angDiff = Mathf.DeltaAngle(CaughtAngle, gameObject.transform.parent.localRotation.z);
                //Debug.Log("angle diff: " + angDiff + " local: " + gameObject.transform.parent.localRotation.z);

                if (angDiff > AngleBuffer)
                    gameObject.transform.parent.Rotate(0, 0, -FishRotateSpeed * Time.deltaTime);
                else if (angDiff < -AngleBuffer)
                    gameObject.transform.parent.Rotate(0, 0, FishRotateSpeed * Time.deltaTime);

                //move to center slowly
                
                float distToCenter = Vector3.Distance(gameObject.transform.parent.localPosition,
                    new Vector3(0, 0, gameObject.transform.parent.localPosition.z));
                if (distToCenter > .1f)
                {
                    Vector3 direction = Vector3.zero;
                    if (gameObject.transform.parent.localPosition.x > HookedSlideBuffer)
                        direction.x = -1*Mathf.Min(Time.deltaTime * HookedSlideSpeed, gameObject.transform.parent.localPosition.x);
                    else if (gameObject.transform.parent.localPosition.x < -HookedSlideBuffer)
                        direction.x = Mathf.Min(Time.deltaTime * HookedSlideSpeed, -gameObject.transform.parent.localPosition.x);

                    if (gameObject.transform.parent.localPosition.y > HookedSlideBuffer)
                        direction.y = -1 * Mathf.Min(Time.deltaTime * HookedSlideSpeed, gameObject.transform.parent.localPosition.y);
                    else if (gameObject.transform.parent.localPosition.y < -HookedSlideBuffer)
                        direction.y = Mathf.Min(Time.deltaTime * HookedSlideSpeed, -gameObject.transform.parent.localPosition.y);

                    gameObject.transform.parent.localPosition = new Vector3(
                        gameObject.transform.parent.localPosition.x + direction.x,
                        gameObject.transform.parent.localPosition.y + direction.y,
                        gameObject.transform.parent.localPosition.z);
                }

                FakeSwingHandler();
                break;

            case WObjState.SwitchCastToReel:
                angDiff = Mathf.DeltaAngle(CaughtAngle, gameObject.transform.parent.localRotation.z);
                //Debug.Log("angle diff: " + angDiff + " local: " + gameObject.transform.parent.localRotation.z);

                if (angDiff > AngleBuffer)
                    gameObject.transform.parent.Rotate(0, 0, -FishRotateSpeed * Time.deltaTime);
                else if (angDiff < -AngleBuffer)
                    gameObject.transform.parent.Rotate(0, 0, FishRotateSpeed * Time.deltaTime);
                break;
        }
    }


    public void Reeling()
    {
        if (isHookedLeftSide) CaughtAngle = -Mathf.PI/4;
        else CaughtAngle = Mathf.PI/4;

        State = WObjState.CaughtReel;
    }

    public void Caught(Vector3 collisionPoint, bool isCast)
    {
        if (isCast) State = WObjState.CaughtCast;
        else State = WObjState.CaughtReel;

        caughtLocation = collisionPoint;

        //try grabbing from center of anchor instead of center of fish
        if (gameObject.transform.parent.InverseTransformPoint(collisionPoint).x < 0)
        {
            isHookedLeftSide = true;
            if (State == WObjState.CaughtCast) CaughtAngle = CaughtAngleDefault;
            else CaughtAngle = -Mathf.PI/4;
        }
        else
        {
            isHookedLeftSide = false;
            if (State == WObjState.CaughtCast) CaughtAngle = -CaughtAngleDefault;
            else CaughtAngle = Mathf.PI/4;
        }



        transform.parent.localRotation = new Quaternion(0, 0, 0, 1);
    }

    public void OnTriggerEnter(Collider c)
    {
        GameSystem.Instance.GameManager.DeadFishHere(this);
    }

    private void FakeSwingHandler()
    {
        //check anchor velocity and react to it with fake changes in target angle
        float hookVelocity = gameObject.transform.parent.parent.gameObject.rigidbody.velocity.x;
        //Debug.Log("hook velocity: " + hookVelocity);

        float velMultiplier;
        if (hookVelocity == 0) velMultiplier = 0;
        else velMultiplier = (hookVelocity / HookMaxSpeed) * HookedMaxAngle;
        if (State == WObjState.CaughtReel)
        {
            if (isHookedLeftSide) CaughtAngle = (-(Mathf.PI / 4) - velMultiplier);
            else CaughtAngle = ((Mathf.PI / 4) - velMultiplier);
        }
        else if (State == WObjState.CaughtCast)
        {
            if (isHookedLeftSide) CaughtAngle = (CaughtAngleDefault + velMultiplier);
            else CaughtAngle = (-CaughtAngleDefault + velMultiplier);
        }
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

    public static float GetRandomFishSpeed(WaterObjectType t)
    {
        switch (t)
        {
            case WaterObjectType.Angelfish:
                return Random.Range(FishMediumBase / 2, FishMediumBase);

            case WaterObjectType.Eel:
                return Random.Range(FishMediumBase / 2, FishMediumBase);

            case WaterObjectType.Jellyfish:
                return Random.Range(FishMediumBase / 2, FishMediumBase);

            case WaterObjectType.Puffer:
                return Random.Range(FishMediumBase / 2, FishMediumBase);

            case WaterObjectType.Shark:
                return Random.Range(FishMediumBase / 2, FishMediumBase);

            case WaterObjectType.Swordfish:
                return Random.Range(FishMediumBase / 2, FishMediumBase);
            
            case WaterObjectType.Trout:
                return Random.Range(FishMediumBase / 2, FishMediumBase);

            case WaterObjectType.Tire:
            case WaterObjectType.Boot:
            default:
                return 0;
        }
    }

    

    public static Color GetBillyColor(BillyColors c)
    {
        switch (c)
        {
            
            case BillyColors.DarkPink:
                return ToColor(163, 89, 130); //dark pink
            case BillyColors.DarkRed:
                return ToColor(156, 26, 34); //dark red
            case BillyColors.LightBlue:
                return ToColor(0, 95, 227); //light blue
            case BillyColors.FishYellow:
                return ToColor(197, 191, 120); //fish yellow
            case BillyColors.LimeGreen:
                return ToColor(105, 195, 105); //lime green\
            case BillyColors.DarkGrey:
                return ToColor(98, 98, 98);
            default:
            case BillyColors.BurntOrange:
                return ToColor(167, 79, 0); //burnt orange
        }
    }

    public static Color GetRandomFishColor(WaterObjectType t)
    {
        switch (t)
        {
            case WaterObjectType.Angelfish:
                return ChooseRandomlyBetween(BillyColors.DarkRed, BillyColors.DarkPink, 
                    BillyColors.BurntOrange, BillyColors.LightBlue, BillyColors.LimeGreen,
                    BillyColors.FishYellow);

            case WaterObjectType.Eel:
                return ChooseRandomlyBetween(BillyColors.DarkRed, BillyColors.DarkPink,
                    BillyColors.BurntOrange, BillyColors.LightBlue, BillyColors.LimeGreen,
                    BillyColors.FishYellow);

            case WaterObjectType.Jellyfish:
                return ChooseRandomlyBetween(BillyColors.BurntOrange, BillyColors.LightBlue, 
                    BillyColors.LimeGreen, BillyColors.FishYellow);

            case WaterObjectType.Puffer:
                return ChooseRandomlyBetween(BillyColors.DarkRed, BillyColors.DarkPink,
                    BillyColors.BurntOrange, BillyColors.LightBlue, BillyColors.LimeGreen,
                    BillyColors.FishYellow);

            case WaterObjectType.Shark:
                return ChooseRandomlyBetween(BillyColors.DarkRed, BillyColors.DarkPink);

            case WaterObjectType.Swordfish:
                return ChooseRandomlyBetween(BillyColors.DarkRed, BillyColors.DarkPink,
                    BillyColors.BurntOrange, BillyColors.LightBlue, BillyColors.LimeGreen,
                    BillyColors.FishYellow);

            case WaterObjectType.Trout:
                return ChooseRandomlyBetween(BillyColors.DarkRed, BillyColors.DarkPink,
                    BillyColors.BurntOrange, BillyColors.LightBlue, BillyColors.LimeGreen,
                    BillyColors.FishYellow);

            case WaterObjectType.Tire:
                return ChooseRandomlyBetween(BillyColors.DarkGrey);

            case WaterObjectType.Boot:
                return ChooseRandomlyBetween(BillyColors.BurntOrange);

            default:
                return ChooseRandomlyBetween(BillyColors.DarkGrey);
        }
    }

    public static Color ChooseRandomlyBetween(params BillyColors[] colors)
    {
        int rndColor = Random.Range(0, colors.Length);
        return GetBillyColor(colors[rndColor]);
    }

    public static Color ToColor(float r, float g, float b)
    {
        return new Color(r / 255f, g / 255f, b / 255f);
    }
}

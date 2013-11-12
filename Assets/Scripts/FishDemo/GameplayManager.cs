using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GameplayState
{
    AtTop,
    Casting,
    Reeling
}

public class GameplayManager : MonoBehaviour
{

    public GameObject HookObject;
    public GameObject ActiveView;
    public BackgroundHandler Background;
    public LevelGenerator LevelGen; //last public member

    public static float ScreenBoundWorldX = 30;
    private const float MouseMoveLimit = 5;
    private const float KeyMovementSpeed = 50;

    private float CurrentDepth;
    private GameplayState GameState;
    private float CurrentScrollSpeed = 10f;
    private static Stack<WaterObjectInstance> FishStackz = new Stack<WaterObjectInstance>();

    private const float GenerateDistance = 30;
    private const float GenTimerBaseMax = 2f;
    private const float GenTimerBaseMin = .7f;
    private float GenTimer;
    private float GenNextSpawnTime;
    private float OddsType1;
    private float OddsType2;
    private float OddsBoot;
    private float OddsBarrel;
    private float NextDepthGoal;
    private const float DepthGoalSectionLength = 100f;

    private float Depth
    {
        get { return -ActiveView.transform.localPosition.y; }
    }

    public void Start()
    {
        StartGame();
    }

    public void Update()
    {
        switch (GameState)
        {
            case GameplayState.AtTop:

                break;

            case GameplayState.Casting:
                float amtToMove = CurrentScrollSpeed * Time.deltaTime;
                ActiveView.transform.localPosition = new Vector3(
                    0, ActiveView.transform.localPosition.y - amtToMove, 0);
                Background.MoveCameraDown(amtToMove);

                UpdateCast();
                break;

            case GameplayState.Reeling:

                break;
        }
        HandleKeyboardInput();
    }

    public static void DeadFishHere(WaterObject deadFish)
    {
        WaterObjectInstance df = new WaterObjectInstance();
        df.wObjectColor = deadFish.ObjColor;
        df.wObjectDepth = deadFish.transform.localPosition.y;
        df.wObjectType = deadFish.ObjType;
        FishStackz.Push(df); //push a dead fish
        deadFish.gameObject.SetActive(false);
    }

    public void HookHit(WaterObject thingItHit, Collision c)
    {
        Vector3 contactWorld = new Vector3(c.contacts[0].point.x, c.contacts[0].point.y, 0);
        thingItHit.gameObject.collider.enabled = false;
        if (GameState == GameplayState.Casting)
            thingItHit.Caught(contactWorld, true);
        else
            thingItHit.Caught(contactWorld, false);

        float distY = thingItHit.gameObject.transform.parent.position.y - contactWorld.y;
        float distX = thingItHit.gameObject.transform.parent.position.x - contactWorld.x;


        thingItHit.gameObject.transform.parent.position = new Vector3(
            thingItHit.gameObject.transform.parent.position.x - distX,
            thingItHit.gameObject.transform.parent.position.y - distY,
            0);
        thingItHit.gameObject.transform.localPosition = new Vector3(
            thingItHit.gameObject.transform.localPosition.x + distX,
            thingItHit.gameObject.transform.localPosition.y + distY,
            0);


        //ConfigurableJoint joint = thingItHit.gameObject.AddComponent<ConfigurableJoint>();
        //joint.autoConfigureConnectedAnchor = false;
        //SoftJointLimit limit = new SoftJointLimit();
        //limit.limit = .1f;
        //joint.linearLimit = limit;
        //joint.anchor = thingItHit.gameObject.transform.InverseTransformPoint(contactWorld);
        //joint.connectedAnchor = thingItHit.gameObject.transform.InverseTransformPoint(contactWorld);
        //joint.axis = new Vector3(0, 0, 1);
        //joint.connectedBody = HookObject.rigidbody;
        //thingItHit.gameObject.rigidbody.useGravity = false;

        // thingItHit.gameObject.transform.parent = HookObject.transform;
        //thingItHit.gameObject.transform.position = new Vector3(
        //    HookObject.transform.position.x, 
        //    thingItHit.transform.position.y, 
        //    thingItHit.transform.position.z);
        thingItHit.gameObject.transform.parent.parent = HookObject.transform;

    }

    public void StartGame()
    {
        CurrentDepth = 0;
        ActiveView.transform.localPosition = new Vector3(0, 0, 0);
        GameState = GameplayState.Casting;
        GenTimer = 0;
        GenNextSpawnTime = Random.Range(GenTimerBaseMin, GenTimerBaseMax);
        NextDepthGoal = DepthGoalSectionLength;
        SetOddsByDepth();
    }

    private void UpdateCast()
    {
        GenTimer += Time.deltaTime;
        if (GenTimer > GenNextSpawnTime)
        {
            GameObject wObj = LevelGen.GetOrCreate(GetRandomType());
            tk2dSprite wSprite = wObj.GetComponent<tk2dSprite>();

            float rndX = Random.Range(-ScreenBoundWorldX, ScreenBoundWorldX);
            wObj.transform.localPosition = new Vector3(rndX, ActiveView.transform.localPosition.y - GenerateDistance, 0);



            GenTimer = 0;
            GenNextSpawnTime = Random.Range(GenTimerBaseMin, GenTimerBaseMax);
        }

        if (Depth > NextDepthGoal)
        {
            SetOddsByDepth();
            NextDepthGoal += DepthGoalSectionLength;
        }
    }

    private void SetOddsByDepth()
    {
        if (Depth < DepthGoalSectionLength * 2)
            SetRandomOdds(.8f, .2f, 0f, 0f);
        else if (Depth < DepthGoalSectionLength * 3)
            SetRandomOdds(.7f, .2f, .1f, 0f);
        else if (Depth < DepthGoalSectionLength * 4)
            SetRandomOdds(.65f, .25f, .1f, 0f);
        else if (Depth < DepthGoalSectionLength * 5)
            SetRandomOdds(.6f, .25f, .15f, 0f);
        else if (Depth < DepthGoalSectionLength * 6)
            SetRandomOdds(.5f, .35f, .15f, 0f);
        else if (Depth < DepthGoalSectionLength * 7)
            SetRandomOdds(.4f, .4f, .2f, 0f);
        else if (Depth < DepthGoalSectionLength * 8)
            SetRandomOdds(.35f, .45f, .2f, 0f);

    }

    private void SetRandomOdds(float type1, float type2, float boot, float barrel)
    { //must add up to 1
        OddsType1 = type1;
        OddsType2 = type2;
        OddsBoot = boot;
        OddsBarrel = barrel;
        //if (float.Equals((type1 + type2 + boot + barrel),1.0f)) Debug.Log("**Bad Odds = "+ (type1 + type2 + boot + barrel) + " **");
    }

    private WaterObjectType GetRandomType()
    {
        float rnd = Random.Range(0f, 1.0f);

        if (rnd < OddsType1)
            return WaterObjectType.FishType1;
        else if (rnd < OddsType1 + OddsType2)
            return WaterObjectType.FishType2;
        else if (rnd < OddsType1 + OddsType2 + OddsBoot)
            return WaterObjectType.Boot;
        else if (rnd < OddsType1 + OddsType2 + OddsBoot + OddsBarrel)
            return WaterObjectType.ExplodingBarrel;

        Debug.Log("problem with random");
        return WaterObjectType.FishType1;
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
        { //swing left
            HookObject.rigidbody.AddForce(-60 * Time.deltaTime, 0, 0);
        }
        else if (Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.LeftArrow))
        { //swing right
            HookObject.rigidbody.AddForce(60 * Time.deltaTime, 0, 0);
        }
    }

    private void HandleMouseInput()
    {
        float mouseX = (float)Input.GetAxis("Mouse X");
        Debug.Log(mouseX);

        float mouseXAdjusted = Mathf.Clamp(mouseX, -MouseMoveLimit, MouseMoveLimit);
        HookObject.transform.localPosition = new Vector3(
            Mathf.Clamp(HookObject.transform.localPosition.x + mouseXAdjusted, -ScreenBoundWorldX, ScreenBoundWorldX),
            HookObject.transform.localPosition.y,
            HookObject.transform.localPosition.z);

    }

}

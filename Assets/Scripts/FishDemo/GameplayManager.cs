using UnityEngine;
using System.Collections;

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
    
    private const float ScreenBoundWorldX = 30;
    private const float MouseMoveLimit = 5;
    private const float KeyMovementSpeed = 50;
    
    private float CurrentDepth;
    private GameplayState GameState;
    private float CurrentScrollSpeed = 7f;
    
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

    public void UpdateCast()
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

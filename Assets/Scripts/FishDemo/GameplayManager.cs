using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GameplayState
{
    AtTop,
    BackToTop,
    Casting,
    Reeling,
    Switching
}

public class GameplayManager : MonoBehaviour
{
    public GameObject HookObject;
    public GameObject ActiveView;
    public BackgroundHandler Background;
    public tk2dTextMesh UIDepth;
    public tk2dTextMesh UIScore;
    public tk2dTextMesh UIHighScore;
    public tk2dTextMesh UIDepthRecord;
    public GameObject UIInstructions;
    public tk2dUIItem PlayButton;
    public LevelGenerator LevelGen; //last public member

    public static float ScreenBoundWorldX = 30;
    private const float MouseMoveLimit = 5;
    private const float KeyMovementSpeed = 50;
    private const float HookUpperY = 10;
    private const float HookLowerY = -10;

    private int NumFishCaught;
    private float CurrentDepth;
    private int FarthestDepth;
    private GameplayState GameState;
    private float CurrentScrollSpeed;
    private const float ScrollSpeedStart = 14f;
    private const float ReelSpeedStart = 20f;
    private const float ScrollSpeedDecrease = 1f;
    private int PlayerScore;
    private Stack<WaterObjectInstance> FishStackz = new Stack<WaterObjectInstance>();
    private List<WaterObject> FishCaught = new List<WaterObject>();
    private WaterObjectInstance NextFish;

    private const float GenerateDistance = 30;
    private const float GenTimerBaseMax = 2f;
    private const float GenTimerLowestMax = .5f;
    private const float GenTimerBaseMin = .2f;
    private const float GenTimerIncrement = .2f;
    private const float SchoolGenBaseMax = 6f;
    private const float SchoolGenBaseMin = 1f;
    private const float GoodFishDist = 3f;
    private const float DepthGoalSectionLength = 100f;
    private float SchoolGenTimer;
    private bool FishLeftToGen;
    private float GenTimer;
    private float GenTimerAdjustment;
    private float GenNextSpawnTime;
    private float SchoolGenNextSpawnTime;
    private float OddsType1;
    private float OddsType2;
    private float OddsBoot;
    private float NextDepthGoal;
    private int CurrentDepthDisplay;

    #region PlayerPrefs
    private const string HighScoreKey = "HighScore";
    private bool gotHighScore = false;
    private int highScore;
    private int HighScore
    {
        get
        {
            if (gotHighScore) return highScore;
            else
            {
                if (PlayerPrefs.HasKey(HighScoreKey))
                {
                    
                    gotHighScore = true;
                    highScore = PlayerPrefs.GetInt(HighScoreKey);
                    return highScore;
                }
                else return 0;
            }
        }

        set
        {
            highScore = value;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
        }
    }

    private const string DepthRecordKey = "DepthRecord";
    private bool gotDepthRecord = false;
    private int depthRecord;
    private int DepthRecord
    {
        get
        {
            if (gotDepthRecord) return depthRecord;
            else
            {
                if (PlayerPrefs.HasKey(DepthRecordKey))
                {
                    
                    gotDepthRecord = true;
                    depthRecord = PlayerPrefs.GetInt(DepthRecordKey);
                    return depthRecord;
                }
                else return 0;
            }
        }

        set
        {
            depthRecord = value;
            PlayerPrefs.SetInt(DepthRecordKey, depthRecord);
        }
    }
    #endregion

    private float Depth
    {
        get { return -ActiveView.transform.localPosition.y; }
    }

    public void Start()
    {
        //StartGame();
        GameState = GameplayState.AtTop;
    }

    public void Update()
    {
        switch (GameState)
        {
            case GameplayState.AtTop:

                break;

            case GameplayState.BackToTop:
                UpdateBackToTop();
                break;

            case GameplayState.Casting:
                float amtToMoveDown = CurrentScrollSpeed * Time.deltaTime;
                ActiveView.transform.localPosition = new Vector3(
                    0, ActiveView.transform.localPosition.y - amtToMoveDown, 0);
                Background.MoveCameraDown(amtToMoveDown);

                UpdateCast();
                break;

            case GameplayState.Reeling:
                float amtToMoveUp = CurrentScrollSpeed * Time.deltaTime;
                ActiveView.transform.localPosition = new Vector3(
                    0, ActiveView.transform.localPosition.y + amtToMoveUp, 0);
                Background.MoveCameraUp(amtToMoveUp);

                UpdateReel();

                if (ActiveView.transform.localPosition.y >= 0)
                { //at top
                    ActiveView.transform.localPosition = new Vector3(0, 0, 0);
                    SwitchToTop();
                }
                break;

            case GameplayState.Switching:
                UpdateSwitch();
                break;
        }
        HandleKeyboardInput();
        SetDepthUI();
    }
    
    public void StartGame()
    {
        CurrentDepth = 0;
        ActiveView.transform.localPosition = new Vector3(0, 0, 0);
        GameState = GameplayState.Casting;
        GenTimer = 0;
        NumFishCaught = 0;
        PlayerScore = 0;
        CurrentDepthDisplay = 0;
        GenTimerAdjustment = 0;
        FarthestDepth = 0;
        FishCaught.Clear();
        CurrentScrollSpeed = ScrollSpeedStart;
        HookObject.transform.localPosition = new Vector3(0, HookUpperY, 0);
        GenNextSpawnTime = Random.Range(GenTimerBaseMin, GenTimerBaseMax);
        NextDepthGoal = DepthGoalSectionLength;
        SetOddsByDepth();
        SetScoreUI();
        SetDepthUI();
    }

    public void PlayButtonHit()
    {
        StartGame();
        PlayButton.gameObject.SetActive(false);
        UIHighScore.gameObject.transform.parent.gameObject.SetActive(false);
        UIDepthRecord.gameObject.transform.parent.gameObject.SetActive(false);
        UIInstructions.gameObject.SetActive(false);
    }

    public void DeadFishHere(WaterObject deadFish)
    {
        if (GameState == GameplayState.Casting)
        {
            //Debug.Log("type: " + deadFish.ObjType.ToString() + " color: " + deadFish.ObjColor.ToString());
            AddFishToStack(deadFish, false);
            
            if (deadFish.IsPartOfSchool)
            {
                List<GameObject> schoolList = deadFish.School;
                //Debug.Log("school adding to stack. size: " + deadFish.SchoolSize);
                foreach (GameObject go in schoolList)
                {
                    WaterObject wo = go.GetComponentInChildren<WaterObject>();
                    if((wo.State == WObjState.Swim) && (wo != deadFish))
                        AddFishToStack(wo, true);
                }
            }
            deadFish.transform.parent.gameObject.SetActive(false); //destroy original fish
        }
        else
        {
            deadFish.transform.parent.gameObject.SetActive(false);
        }
    }

    private void AddFishToStack(WaterObject deadFish, bool deactivate)
    {
        WaterObjectInstance df = new WaterObjectInstance();
        df.Color = deadFish.ObjColor;
        df.Depth = -deadFish.transform.parent.localPosition.y;
        df.Type = deadFish.ObjType;
        df.Speed = deadFish.fishSpeed;
        df.FacingLeft = deadFish.Facingleft;
        df.XPos = deadFish.transform.parent.localPosition.x;
        df.PartOfSchool = deadFish.IsPartOfSchool;
        if (df.PartOfSchool) df.SchoolSize = deadFish.SchoolSize;
        else deadFish.SchoolSize = 0;
        FishStackz.Push(df); //push a dead fish
        if (deactivate) deadFish.transform.parent.gameObject.SetActive(false);
    }

    public void HookHit(WaterObject thingItHit, Collision c)
    {
        Vector3 contactWorld = new Vector3(c.contacts[0].point.x, c.contacts[0].point.y, 0);
        thingItHit.gameObject.collider.enabled = false;

        if (GameState == GameplayState.Casting || GameState == GameplayState.Switching)
            thingItHit.Caught(contactWorld, true);
        else
            thingItHit.Caught(contactWorld, false);

        float distY = thingItHit.gameObject.transform.parent.position.y - contactWorld.y;
        float distX = thingItHit.gameObject.transform.parent.position.x - contactWorld.x;

        thingItHit.gameObject.transform.parent.position = new Vector3(
            thingItHit.gameObject.transform.parent.position.x - distX,
            thingItHit.gameObject.transform.parent.position.y - distY,
            .1f);
        thingItHit.gameObject.transform.localPosition = new Vector3(
            thingItHit.gameObject.transform.localPosition.x + distX,
            thingItHit.gameObject.transform.localPosition.y + distY,
            0);
        thingItHit.gameObject.transform.parent.parent = HookObject.transform;

        if (thingItHit.IsPartOfSchool)
        { //remove from school
            foreach (GameObject go in thingItHit.School)
                go.GetComponentInChildren<WaterObject>().SchoolSize--;
        }

        AddScore(thingItHit.ScoreValue);
        NumFishCaught++;
        FishCaught.Add(thingItHit);
        if(GameState == GameplayState.Casting) CurrentScrollSpeed -= ScrollSpeedDecrease;
        if (NumFishCaught >= 3 && GameState == GameplayState.Casting)
        {
            GameState = GameplayState.Switching;
            FarthestDepth = (int)Depth;
        }
    }



    private void UpdateCast()
    {
        GenTimer += Time.deltaTime;
        if (GenTimer > GenNextSpawnTime)
        { //spawn one fish
            GameObject wObj = LevelGen.GetOrCreate(GetRandomType());

            float rndX = Random.Range(-ScreenBoundWorldX, ScreenBoundWorldX);
            wObj.transform.localPosition = new Vector3(rndX, -(Depth + GenerateDistance), 0);

            GenTimer = 0;
            GenNextSpawnTime = Random.Range(GenTimerBaseMin, GenTimerBaseMax - GenTimerAdjustment);
        }

        SchoolGenTimer += Time.deltaTime;
        if (SchoolGenTimer > SchoolGenNextSpawnTime)
        { //spawn a school of fish
            int NumOfSchoolTypes = 2;
            switch(Random.Range(0, NumOfSchoolTypes))
            {
                case 0: //swimming V (random direction and v direction, random speed and type too)
                    GenerateSchool(SchoolType.SwimmingV);
                    break;

                case 1: //Slice N Dice (three fish, middle spawn other direction at other side, fast)
                    GenerateSchool(SchoolType.SliceNDice);
                    break;
            }

            SchoolGenTimer = 0;
            SchoolGenNextSpawnTime = Random.Range(SchoolGenBaseMin, SchoolGenBaseMax - GenTimerAdjustment);
        }

        if (Depth > NextDepthGoal)
        {
            SetOddsByDepth();
            
            NextDepthGoal += DepthGoalSectionLength;
            
            if((GenTimerBaseMax - GenTimerAdjustment) > GenTimerLowestMax)
                GenTimerAdjustment += GenTimerIncrement;
        }
    }

    private void UpdateReel()
    {
        if (FishLeftToGen && ((Depth - GenerateDistance) <= NextFish.Depth))
        {
            GameObject wObj = LevelGen.GetOrCreate(NextFish.Type);
            WaterObject wObjScript = wObj.GetComponentInChildren<WaterObject>();
            
            wObj.transform.localPosition = new Vector3(NextFish.XPos, -NextFish.Depth, 0);
            wObjScript.ObjColor = NextFish.Color;
            wObjScript.fishSpeed = NextFish.Speed;
            wObjScript.Facingleft = NextFish.FacingLeft;
            //Debug.Log("gen fish- type: " + NextFish.wObjectType.ToString() + " color: " + NextFish.wObjectColor.ToString());
            //need a way to regen entire school at a time, save school instead of individual fish
            //seperate school stack?

            if (NextFish.PartOfSchool)
            {
                Debug.Log("regenning school, size: " + NextFish.SchoolSize);
                for (int i = 0; i < NextFish.SchoolSize - 1; i++)
                {
                    if (FishStackz.Count != 0) NextFish = FishStackz.Pop();
                    else
                    {
                        FishLeftToGen = false;
                        return;
                    }

                    GameObject wObjS = LevelGen.GetOrCreate(NextFish.Type);
                    WaterObject wObjScriptS = wObjS.GetComponentInChildren<WaterObject>();

                    wObjS.transform.localPosition = new Vector3(NextFish.XPos, -NextFish.Depth, 0);
                    wObjScriptS.ObjColor = NextFish.Color;
                    wObjScriptS.fishSpeed = NextFish.Speed;
                    wObjScriptS.Facingleft = NextFish.FacingLeft;
                }
            }

            if (FishStackz.Count != 0)
            {
                NextFish = FishStackz.Pop();
            }
            else FishLeftToGen = false;
        }

    }

    private void UpdateSwitch()
    {
        HookObject.transform.localPosition = new Vector3(
            HookObject.transform.localPosition.x, 
            HookObject.transform.localPosition.y - (CurrentScrollSpeed * Time.deltaTime), 
            0);

        if (HookObject.transform.localPosition.y < HookLowerY)
        {
            HookObject.transform.localPosition = new Vector3(HookObject.transform.localPosition.x, HookLowerY, 0);
            GameState = GameplayState.Reeling;
            CurrentScrollSpeed = ReelSpeedStart;

            foreach (WaterObject w in FishCaught)
                w.Reeling();

            if (FishStackz.Count != 0)
            {
                NextFish = FishStackz.Pop();
                FishLeftToGen = true;
            }
            else FishLeftToGen = false;
        }
    }

    private void UpdateBackToTop()
    {
        HookObject.transform.localPosition = new Vector3(
            HookObject.transform.localPosition.x,
            HookObject.transform.localPosition.y + (CurrentScrollSpeed * Time.deltaTime),
            0);

        if (HookObject.transform.localPosition.y > HookUpperY)
        {
            GameState = GameplayState.AtTop;
            PlayButton.gameObject.SetActive(true);
            UIHighScore.gameObject.transform.parent.gameObject.SetActive(true);
            UIDepthRecord.gameObject.transform.parent.gameObject.SetActive(true);
            UIInstructions.gameObject.SetActive(true);

            bool setNewRecord = false;
            if (HighScore < PlayerScore)
            { //new high score
                Debug.Log("new high score!");
                HighScore = PlayerScore;
                setNewRecord = true;
            }

            if (DepthRecord < FarthestDepth)
            { //new depth record
                Debug.Log("new depth record!");
                DepthRecord = FarthestDepth;
                setNewRecord = true;
            }

            if (setNewRecord) PlayerPrefs.Save();

            Debug.Log("high score: " + HighScore + " depth record: " + DepthRecord);
            UIHighScore.text = HighScore.ToString();
            UIDepthRecord.text = DepthRecord.ToString() + "m";

            LevelGen.ClearAll();

            foreach (WaterObject w in FishCaught)
            {
                Destroy(w.gameObject.transform.parent.gameObject);
            }

            FishCaught.Clear();
        }
    }

    private void SwitchToTop()
    {
        GameState = GameplayState.BackToTop;
        Debug.Log("score: " + PlayerScore);
    }

    private void SetScoreUI()
    {
        UIScore.text = ((int)PlayerScore).ToString();
    }

    private void SetDepthUI()
    {
        if (Depth != CurrentDepthDisplay)
        {
            CurrentDepthDisplay = (int)Depth;
            UIDepth.text = CurrentDepthDisplay.ToString() + "m";
        }
    }

    private void GenerateSchool(SchoolType t)
    {
        switch (t)
        {
            case SchoolType.SwimmingV: //swimming V (random direction and v direction, random speed and type too)
                { //brackets for scope
                    WaterObjectType randomType = GetRandomSchoolType();
                    float randomSpeed = WaterObject.GetRandomFishSpeed(randomType);
                    float randomX = Random.Range(0f, ScreenBoundWorldX);
                    int randomDirection = Random.Range(0, 2);
                    if (randomDirection == 0) randomDirection = -1;
                    int randomSpreadDirection = Random.Range(0, 2);
                    if (randomSpreadDirection == 0) randomSpreadDirection = -1;
                    List<GameObject> swimmingV = LevelGen.GetOrCreate(randomType, 5);
                    foreach (GameObject go in swimmingV)
                    {
                        WaterObject wo = go.GetComponentInChildren<WaterObject>();
                        if (randomDirection == 1) wo.Facingleft = true;
                        else wo.Facingleft = false;
                        wo.fishSpeed = randomSpeed;
                        wo.IsPartOfSchool = true;
                        wo.SchoolSize = 5;
                        wo.School = swimmingV;
                    }
                    swimmingV[0].transform.localPosition = new Vector3(randomDirection * randomX, -(Depth + GenerateDistance - GoodFishDist * 2), 0);
                    swimmingV[1].transform.localPosition = new Vector3(randomDirection * randomX + (2f * randomSpreadDirection), -(Depth + GenerateDistance - GoodFishDist), 0);
                    swimmingV[2].transform.localPosition = new Vector3(randomDirection * randomX + (4f * randomSpreadDirection), -(Depth + GenerateDistance), 0);
                    swimmingV[3].transform.localPosition = new Vector3(randomDirection * randomX + (2f * randomSpreadDirection), -(Depth + GenerateDistance + GoodFishDist), 0);
                    swimmingV[4].transform.localPosition = new Vector3(randomDirection * randomX, -(Depth + GenerateDistance + GoodFishDist * 2), 0);
                }
                break;

            case SchoolType.SliceNDice: //Slice N Dice (three fish, middle spawn other direction at other side, fast)
                {
                    WaterObjectType randomType = GetRandomSchoolType();
                    float randomSpeed = WaterObject.GetRandomFishSpeed(randomType) * 2;
                    int randomDirection = Random.Range(0, 2);
                    if (randomDirection == 0) randomDirection = -1;
                    float randomYBuffer = Random.Range(0f, 4.0f);
                    float randomX = Random.Range(0f, ScreenBoundWorldX);
                    List<GameObject> SliceNDice = LevelGen.GetOrCreate(randomType, 3);
                    foreach (GameObject go in SliceNDice)
                    {
                        WaterObject wo = go.GetComponentInChildren<WaterObject>();
                        wo.fishSpeed = randomSpeed;
                        wo.IsPartOfSchool = true;
                        wo.SchoolSize = 3;
                        wo.School = SliceNDice;
                    }

                    SliceNDice[0].transform.localPosition = new Vector3(randomDirection * randomX * randomDirection, -(Depth + GenerateDistance - GoodFishDist + randomYBuffer), 0);
                    SliceNDice[0].GetComponentInChildren<WaterObject>().Facingleft = (randomDirection == 1);
                    SliceNDice[0].GetComponentInChildren<WaterObject>().fishSpeed = randomSpeed;

                    SliceNDice[1].transform.localPosition = new Vector3(randomDirection * randomX * -randomDirection, -(Depth + GenerateDistance + randomYBuffer), 0);
                    SliceNDice[1].GetComponentInChildren<WaterObject>().Facingleft = (randomDirection != 1);
                    SliceNDice[1].GetComponentInChildren<WaterObject>().fishSpeed = randomSpeed;

                    SliceNDice[2].transform.localPosition = new Vector3(randomDirection * randomX * randomDirection, -(Depth + GenerateDistance + GoodFishDist + randomYBuffer), 0);
                    SliceNDice[2].GetComponentInChildren<WaterObject>().Facingleft = (randomDirection == 1);
                    SliceNDice[2].GetComponentInChildren<WaterObject>().fishSpeed = randomSpeed;
                }
                break;

        }
    }

    private void SetOddsByDepth()
    {
        if (Depth < DepthGoalSectionLength * 2)
            SetRandomOdds(.8f, .2f, 0f);
        else if (Depth < DepthGoalSectionLength * 3)
            SetRandomOdds(.7f, .2f, .1f);
        else if (Depth < DepthGoalSectionLength * 4)
            SetRandomOdds(.65f, .25f, .1f);
        else if (Depth < DepthGoalSectionLength * 5)
            SetRandomOdds(.6f, .25f, .15f);
        else if (Depth < DepthGoalSectionLength * 6)
            SetRandomOdds(.5f, .35f, .15f);
        else if (Depth < DepthGoalSectionLength * 7)
            SetRandomOdds(.4f, .4f, .2f);
        else if (Depth < DepthGoalSectionLength * 8)
            SetRandomOdds(.35f, .45f, .2f);
    }

    private void SetRandomOdds(float type1, float type2, float boot)
    { //must add up to 1
        OddsType1 = type1;
        OddsType2 = type2;
        OddsBoot = boot;
        //if (float.Equals((type1 + type2 + boot + barrel),1.0f)) Debug.Log("**Bad Odds = "+ (type1 + type2 + boot + barrel) + " **");
    }

    private void AddScore(int score)
    {
        PlayerScore += score;
        SetScoreUI();
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

        Debug.Log("problem with random");
        return WaterObjectType.FishType1;
    }

    private WaterObjectType GetRandomSchoolType()
    {
        float rnd = Random.Range(0f, OddsType1 + OddsType2);

        if (rnd < OddsType1)
            return WaterObjectType.FishType1;
        else if (rnd < OddsType1 + OddsType2)
            return WaterObjectType.FishType2;

        Debug.Log("problem with random");
        return WaterObjectType.FishType1;
    }

    private void HandleKeyboardInput()
    {
        if ((Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
            || (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D)))
        { //swing left
            HookObject.rigidbody.AddForce(-60 * Time.deltaTime, 0, 0);
        }
        else if ((Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.LeftArrow))
            || (Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A)))
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

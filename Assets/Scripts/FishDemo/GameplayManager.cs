using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GameplayState
{
    AtTop,
    FinishReel,
    MoveToTop,
    MoveToStart,
    Casting,
    Reeling,
    Switching
}

public class GameplayManager : MonoBehaviour
{
    public GameObject HookObject;
    public GameObject ActiveView;
    public BackgroundHandler Background;
    public GUIManager UI;
    public AudioClip CatchSoundEffect;
    public LevelGenerator LevelGen; //last public member

    public static float ScreenBoundWorldX = 30;
    private const float MouseMoveLimit = 5;
    private const float KeyMovementSpeed = 50;

    private readonly Vector3 HookOffScreenPosition = new Vector3(0, 38, 0);
    private readonly Vector3 HookPreOffScreenPosition = new Vector3(0, 25, 0);
    private readonly Vector3 HookUpperPosition = new Vector3(0, 10, 0);
    private readonly Vector3 HookLowerPosition = new Vector3(0, -10, 0);

    private bool Boosting;
    private float BoostTimer;
    private float RechargeTimer;
    private const float BoostTimeLimit = .4f;
    private const float BoostSpeed = 2.6f;
    private const float RechargeTime = 3.0f;
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
    private int[] FishCaughtCount;

    private const float GenerateDistance = 40;
    private const float GenTimerBaseMax = 1.4f;
    private const float GenTimerLowestMax = .5f;
    private const float GenTimerSchoolLowestMax = 4f;
    private const float GenTimerBaseMin = .3f;
    private const float GenTimerIncrement = .15f;
    private const float GenTimerSchoolIncrement = .05f;
    private const float SchoolGenBaseMax = 8f;
    private const float SchoolGenBaseMin = 2f;
    private const float GoodFishDist = 4f;
    private const float DepthGoalSectionLength = 100f;
    private float SchoolGenTimer;
    private bool FishLeftToGen;
    private float GenTimer;
    private float GenTimerAdjustment;
    private float GenTimerSchoolAdjustment;
    private float GenNextSpawnTime;
    private float SchoolGenNextSpawnTime;
    private float[] GenOdds;
    private float OddsType1;
    private float OddsType2;
    private float OddsBoot;
    private float NextDepthGoal;
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

    private float rechargePercentage;
    private float RechargePercentage
    {
        get { return rechargePercentage; }
        set
        {
            //update ui here  
            UI.SetRechargeFill(rechargePercentage);
            rechargePercentage = value;
        }
    }

    private float Depth
    {
        get { return -ActiveView.transform.localPosition.y; }
    }

    public void Awake()
    {
        //StartGame();
        GameState = GameplayState.AtTop;
        GenOdds = new float[WaterObject.NumberOfFishTypes];
        FishCaughtCount = new int[WaterObject.NumberOfFishTypes];
    }

    public void Update()
    {
        switch (GameState)
        {
            case GameplayState.AtTop:
                Background.UpdateWaves();

                break;

            case GameplayState.FinishReel:
                UpdateFinishReel();
                break;

            case GameplayState.MoveToTop:
                Background.UpdateWaves();

                if (RaiseAnchorOffScreen())
                {
                    if (Background.MoveToTop()) //only need one call, calls zoom inside of move
                        SetUpTop();
                }


                break;

            case GameplayState.MoveToStart:
                Background.UpdateWaves();

                bool panToStartDone = Background.MoveToStart();
                bool zoomToStartDone = Background.ZoomToStart();

                if (panToStartDone && zoomToStartDone)
                {
                    if (LowerAnchorToStart())
                        StartCast();
                }

                break;

            case GameplayState.Casting:
                float amtToMoveDown;

                if (Boosting)
                {
                    amtToMoveDown = CurrentScrollSpeed * BoostSpeed * Time.deltaTime;
                    BoostTimer -= Time.deltaTime;
                    RechargePercentage = BoostTimer / BoostTimeLimit;

                    if (BoostTimer <= 0f)
                    {
                        RechargeTimer = 0;
                        Boosting = false;
                    }
                }
                else
                {
                    amtToMoveDown = CurrentScrollSpeed * Time.deltaTime;
                    if (RechargeTimer < RechargeTime)
                    {
                        RechargeTimer += Time.deltaTime;
                        if (RechargeTimer > RechargeTime) RechargeTimer = RechargeTime;
                        RechargePercentage = RechargeTimer / RechargeTime;
                    }
                }

                ActiveView.transform.localPosition = new Vector3(
                    0, ActiveView.transform.localPosition.y - amtToMoveDown, 0);
                Background.MoveCameraDown(amtToMoveDown);

                UpdateCast();
                break;

            case GameplayState.Reeling:
                float amtToMoveUp = CurrentScrollSpeed * Time.deltaTime;
                ActiveView.transform.localPosition = new Vector3(
                    0, ActiveView.transform.localPosition.y + amtToMoveUp, 0);

                UpdateReel();

                if (ActiveView.transform.localPosition.y >= 0)
                { //at top
                    ActiveView.transform.localPosition = new Vector3(0, 0, 0);
                    ReelDone();
                }
                else
                {
                    Background.MoveCameraUp(amtToMoveUp);
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
        GameState = GameplayState.MoveToStart;
        GenTimer = 0;
        NumFishCaught = 0;
        PlayerScore = 0;
        GenTimerAdjustment = 0;
        FarthestDepth = 0;
        Boosting = false;
        BoostTimer = 0;
        RechargePercentage = 0;
        RechargeTimer = 0;

        UI.ResetPulsing();

        RechargePercentage = 0;
        FishCaught.Clear();
        CurrentScrollSpeed = ScrollSpeedStart;
        HookObject.transform.localPosition = new Vector3(0, HookPreOffScreenPosition.y, 0);
        GenNextSpawnTime = Random.Range(GenTimerBaseMin, GenTimerBaseMax);
        NextDepthGoal = DepthGoalSectionLength;
        SetOddsByDepth();
        SetScoreUI();
        SetDepthUI();
    }

    public void StartCast()
    {
        GameState = GameplayState.Casting;
        Background.EnableWaves(false);
        HookObject.transform.localPosition = new Vector3(0, HookUpperPosition.y, 0);
        UI.StartCasting();
    }

    public void SetUpTop()
    {
        GameState = GameplayState.AtTop;
        UI.SetUpTopMenu(true);

        CountFish();

        //take care of fish
        LevelGen.ClearAll();
        foreach (WaterObject w in FishCaught)
        {
            Destroy(w.gameObject.transform.parent.gameObject);
        }

        FishCaught.Clear();
    }

    private void CountFish()
    {
        foreach (WaterObject w in FishCaught)
        {
            FishCaughtCount[(int)w.ObjType]++;
        }
        UI.SetFishCount(FishCaughtCount[(int)WaterObjectType.Angelfish],
            FishCaughtCount[(int)WaterObjectType.Trout],
            FishCaughtCount[(int)WaterObjectType.Puffer],
            FishCaughtCount[(int)WaterObjectType.Eel],
            FishCaughtCount[(int)WaterObjectType.Jellyfish],
            FishCaughtCount[(int)WaterObjectType.Shark],
            FishCaughtCount[(int)WaterObjectType.Swordfish],
            FishCaughtCount[(int)WaterObjectType.Boot],
            FishCaughtCount[(int)WaterObjectType.Tire]);
    }

    private void ReelDone()
    {
        GameState = GameplayState.FinishReel;
        Background.EnableWaves(true);
    }

    private bool LowerAnchorToStart()
    {
        HookObject.transform.localPosition = new Vector3(
            HookUpperPosition.x,
            HookObject.transform.localPosition.y - (CurrentScrollSpeed * Time.deltaTime),
            HookUpperPosition.z);

        if (HookObject.transform.localPosition.y <= HookUpperPosition.y)
        {
            HookObject.transform.localPosition = HookUpperPosition;
            return true;
        }
        else return false;
    }

    private bool RaiseAnchorOffScreen()
    {
        if (HookObject.transform.localPosition.y < HookOffScreenPosition.y)
        {
            HookObject.transform.localPosition = new Vector3(
                HookObject.transform.localPosition.x,
                HookObject.transform.localPosition.y + (CurrentScrollSpeed * Time.deltaTime),
                HookOffScreenPosition.z);

            if (HookObject.transform.localPosition.y >= HookOffScreenPosition.y)
            { //at very top now
                HookObject.transform.localPosition = HookOffScreenPosition;
                return true;
            }
            else
            { //check if were far enough to move on
                if (HookObject.transform.localPosition.y >= HookPreOffScreenPosition.y)
                    return true;
                else
                    return false;
            }
         }
        else
        {
            return true;
        }
    }

    public void PlayButtonHit()
    {
        StartGame();
        //change UI
        UI.SetUpTopMenu(false);
        HookObject.transform.localPosition = new Vector3(0, HookPreOffScreenPosition.y, 0);
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
                    if ((wo.State == WObjState.Swim) && (wo != deadFish))
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

        if (GameState == GameplayState.Casting)
        {
            GameState = GameplayState.Switching;
            FarthestDepth = (int)Depth;
            UI.HideBoostFill();
        }

        audio.PlayOneShot(CatchSoundEffect);
    }

    private void UpdateCast()
    {
        GenTimer += Time.deltaTime;
        if (GenTimer > GenNextSpawnTime)
        { //spawn one fish
            GameObject wObj = LevelGen.GetOrCreate(GetRandomType(false));

            float rndX = Random.Range(-ScreenBoundWorldX, ScreenBoundWorldX);
            wObj.transform.localPosition = new Vector3(rndX, -(Depth + GenerateDistance), 0);

            GenTimer = 0;
            GenNextSpawnTime = Random.Range(GenTimerBaseMin, GenTimerBaseMax - GenTimerAdjustment);
        }

        SchoolGenTimer += Time.deltaTime;
        if (SchoolGenTimer > SchoolGenNextSpawnTime)
        { //spawn a school of fish
            int NumOfSchoolTypes = 5;
            switch (Random.Range(0, NumOfSchoolTypes))
            {
                case 0: //swimming V (random direction and v direction, random speed and type too)
                    GenerateSchool(SchoolType.SwimmingV);
                    break;

                case 1: //Slice N Dice (three fish, middle spawn other direction at other side, fast)
                    GenerateSchool(SchoolType.SliceNDice);
                    break;

                case 2: //half of the flying v
                    GenerateSchool(SchoolType.Slant);
                    break;

                case 3: //three in a line and two staggered slicing through them
                    GenerateSchool(SchoolType.Mixer);
                    break;

                case 4: //4 fish in a diamond shape
                    GenerateSchool(SchoolType.Diamond);
                    break;
            }

            SchoolGenTimer = 0;
            SchoolGenNextSpawnTime = Random.Range(SchoolGenBaseMin, SchoolGenBaseMax - GenTimerAdjustment);
        }

        if (Depth > NextDepthGoal)
        {
            SetOddsByDepth();

            NextDepthGoal += DepthGoalSectionLength;

            if ((GenTimerBaseMax - GenTimerAdjustment) > GenTimerLowestMax)
                GenTimerAdjustment += GenTimerIncrement;

            if ((SchoolGenBaseMax - GenTimerSchoolAdjustment) > GenTimerSchoolLowestMax)
                GenTimerSchoolAdjustment += GenTimerSchoolIncrement;
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

            if (NextFish.PartOfSchool)
            {
                //Debug.Log("regenning school, size: " + NextFish.SchoolSize);
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

        if (HookObject.transform.localPosition.y < HookLowerPosition.y)
        {
            HookObject.transform.localPosition = new Vector3(HookObject.transform.localPosition.x, HookLowerPosition.y, 0);
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

    private void UpdateFinishReel()
    {
        HookObject.transform.localPosition = new Vector3(
            HookObject.transform.localPosition.x,
            HookObject.transform.localPosition.y + (CurrentScrollSpeed * Time.deltaTime),
            0);

        if (HookObject.transform.localPosition.y > HookUpperPosition.y)
        {
            GameState = GameplayState.MoveToTop;

            bool setNewRecord = false;
            if (HighScore < PlayerScore)
            { //new high score
                Debug.Log("new high score!");
                HighScore = PlayerScore;
                setNewRecord = true;
                UI.HighScorePulse = true;
            }

            if (DepthRecord < FarthestDepth)
            { //new depth record
                Debug.Log("new depth record!");
                DepthRecord = FarthestDepth;
                setNewRecord = true;
                UI.DepthRecordPulse = true;
            }

            if (setNewRecord) PlayerPrefs.Save();

            Debug.Log("high score: " + HighScore + " depth record: " + DepthRecord);
            UI.SetRecords(HighScore.ToString(), DepthRecord.ToString());
        }
    }

    private void SetScoreUI()
    {
        UI.SetScore(PlayerScore);
    }

    private void SetDepthUI()
    {
        UI.SetDepth((int)Depth);
    }

    private void GenerateSchool(SchoolType t)
    {
        //debug
        //t = SchoolType.Mixer;
        switch (t)
        {
            case SchoolType.SwimmingV: //swimming V (random direction and v direction, random speed and type too)
                { //brackets for scope
                    WaterObjectType randomType = GetRandomType(true);
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
                    swimmingV[1].transform.localPosition = new Vector3(randomDirection * randomX + (GoodFishDist * randomSpreadDirection), -(Depth + GenerateDistance - GoodFishDist), 0);
                    swimmingV[2].transform.localPosition = new Vector3(randomDirection * randomX + (GoodFishDist*2 * randomSpreadDirection), -(Depth + GenerateDistance), 0);
                    swimmingV[3].transform.localPosition = new Vector3(randomDirection * randomX + (GoodFishDist * randomSpreadDirection), -(Depth + GenerateDistance + GoodFishDist), 0);
                    swimmingV[4].transform.localPosition = new Vector3(randomDirection * randomX, -(Depth + GenerateDistance + GoodFishDist * 2), 0);
                }
                break;

            case SchoolType.SliceNDice: //Slice N Dice (three fish, middle spawn other direction at other side, fast)
                {
                    WaterObjectType randomType = GetRandomType(true);
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

                    SliceNDice[0].transform.localPosition = new Vector3(randomX * randomDirection, -(Depth + GenerateDistance - GoodFishDist + randomYBuffer), 0);
                    SliceNDice[0].GetComponentInChildren<WaterObject>().Facingleft = (randomDirection == 1);
                    SliceNDice[0].GetComponentInChildren<WaterObject>().fishSpeed = randomSpeed;

                    SliceNDice[1].transform.localPosition = new Vector3(randomX * -randomDirection, -(Depth + GenerateDistance + randomYBuffer), 0);
                    SliceNDice[1].GetComponentInChildren<WaterObject>().Facingleft = (randomDirection != 1);
                    SliceNDice[1].GetComponentInChildren<WaterObject>().fishSpeed = randomSpeed;

                    SliceNDice[2].transform.localPosition = new Vector3(randomX * randomDirection, -(Depth + GenerateDistance + GoodFishDist + randomYBuffer), 0);
                    SliceNDice[2].GetComponentInChildren<WaterObject>().Facingleft = (randomDirection == 1);
                    SliceNDice[2].GetComponentInChildren<WaterObject>().fishSpeed = randomSpeed;
                }
                break;


            case SchoolType.Diamond:
                {
                    WaterObjectType randomType = GetRandomType(true);
                    float randomSpeed = WaterObject.GetRandomFishSpeed(randomType);
                    float randomX = Random.Range(0f, ScreenBoundWorldX);
                    int randomDirection = Random.Range(0, 2);
                    if (randomDirection == 0) randomDirection = -1;
                    Color randomColor = WaterObject.GetRandomFishColor(randomType);

                    List<GameObject> Diamond = LevelGen.GetOrCreate(randomType, 4);
                    foreach (GameObject go in Diamond)
                    {
                        WaterObject wo = go.GetComponentInChildren<WaterObject>();
                        if (randomDirection == 1) wo.Facingleft = true;
                        else wo.Facingleft = false;
                        wo.fishSpeed = randomSpeed;
                        wo.IsPartOfSchool = true;
                        wo.SchoolSize = 4;
                        wo.ObjColor = randomColor;
                        wo.School = Diamond;
                    }
                    Diamond[0].transform.localPosition = new Vector3(randomDirection * randomX - (GoodFishDist*1.5f), -(Depth + GenerateDistance), 0);
                    Diamond[1].transform.localPosition = new Vector3(randomDirection * randomX, -(Depth + GenerateDistance - (GoodFishDist*1.3f)), 0);
                    Diamond[2].transform.localPosition = new Vector3(randomDirection * randomX, -(Depth + GenerateDistance + (GoodFishDist*1.3f)), 0);
                    Diamond[3].transform.localPosition = new Vector3(randomDirection * randomX + (GoodFishDist*1.5f), -(Depth + GenerateDistance), 0);
                    
                }
                break;

            case SchoolType.Slant:
                {
                    WaterObjectType randomType = GetRandomType(true);
                    float randomSpeed = WaterObject.GetRandomFishSpeed(randomType);
                    float randomX = Random.Range(0f, ScreenBoundWorldX);
                    int randomDirection = Random.Range(0, 2);
                    if (randomDirection == 0) randomDirection = -1;
                    int randomSpreadDirection = Random.Range(0, 2);
                    if (randomSpreadDirection == 0) randomSpreadDirection = -1;
                    List<GameObject> SlantFish = LevelGen.GetOrCreate(randomType, 3);
                    foreach (GameObject go in SlantFish)
                    {
                        WaterObject wo = go.GetComponentInChildren<WaterObject>();
                        if (randomDirection == 1) wo.Facingleft = true;
                        else wo.Facingleft = false;
                        wo.fishSpeed = randomSpeed;
                        wo.IsPartOfSchool = true;
                        wo.SchoolSize = 3;
                        wo.School = SlantFish;
                    }
                    SlantFish[0].transform.localPosition = new Vector3(randomDirection * randomX, -(Depth + GenerateDistance - GoodFishDist * 2), 0);
                    SlantFish[1].transform.localPosition = new Vector3(randomDirection * randomX + (GoodFishDist * randomSpreadDirection), -(Depth + GenerateDistance - GoodFishDist), 0);
                    SlantFish[2].transform.localPosition = new Vector3(randomDirection * randomX + (GoodFishDist*2 * randomSpreadDirection), -(Depth + GenerateDistance), 0);
                }
                break;

            case SchoolType.Mixer:
                {
                    WaterObjectType randomType = GetRandomType(true);
                    float randomSpeed = WaterObject.GetRandomFishSpeed(randomType) * 1.5f;
                    Color randomColor = WaterObject.GetRandomFishColor(randomType);
                    int randomDirection = Random.Range(0, 2);
                    if (randomDirection == 0) randomDirection = -1;
                    float randomYBuffer = Random.Range(0f, 4.0f);
                    float randomX = Random.Range(-ScreenBoundWorldX/2.0f, ScreenBoundWorldX/2.0f);
                    List<GameObject> Mixer = LevelGen.GetOrCreate(randomType, 5);
                    foreach (GameObject go in Mixer)
                    {
                        WaterObject wo = go.GetComponentInChildren<WaterObject>();
                        wo.fishSpeed = randomSpeed;
                        wo.IsPartOfSchool = true;
                        wo.SchoolSize = 5;
                        wo.ObjColor = randomColor;
                        wo.School = Mixer;
                    }

                    Mixer[0].transform.localPosition = new Vector3(randomX , -(Depth + GenerateDistance - (GoodFishDist*2) + randomYBuffer), 0);
                    Mixer[0].GetComponentInChildren<WaterObject>().Facingleft = (randomDirection == 1);
                    Mixer[0].GetComponentInChildren<WaterObject>().fishSpeed = randomSpeed;

                    Mixer[1].transform.localPosition = new Vector3(randomX - GoodFishDist, -(Depth + GenerateDistance - GoodFishDist + randomYBuffer), 0);
                    Mixer[1].GetComponentInChildren<WaterObject>().Facingleft = (randomDirection != 1);
                    Mixer[1].GetComponentInChildren<WaterObject>().fishSpeed = randomSpeed;

                    Mixer[2].transform.localPosition = new Vector3(randomX , -(Depth + GenerateDistance + randomYBuffer), 0);
                    Mixer[2].GetComponentInChildren<WaterObject>().Facingleft = (randomDirection == 1);
                    Mixer[2].GetComponentInChildren<WaterObject>().fishSpeed = randomSpeed;

                    Mixer[3].transform.localPosition = new Vector3(randomX + GoodFishDist, -(Depth + GenerateDistance + GoodFishDist + randomYBuffer), 0);
                    Mixer[3].GetComponentInChildren<WaterObject>().Facingleft = (randomDirection != 1);
                    Mixer[3].GetComponentInChildren<WaterObject>().fishSpeed = randomSpeed;

                    Mixer[4].transform.localPosition = new Vector3(randomX, -(Depth + GenerateDistance + (GoodFishDist*2) + randomYBuffer), 0);
                    Mixer[4].GetComponentInChildren<WaterObject>().Facingleft = (randomDirection == 1);
                    Mixer[4].GetComponentInChildren<WaterObject>().fishSpeed = randomSpeed;
                }
                break;
        }
    }

    private void SetOddsByDepth()
    {
        if (Depth < DepthGoalSectionLength * 2)
            SetRandomOdds(.75f, .25f, .0f, .0f, 0f, 0f, 0f, 0f, 0f);
        else if (Depth < DepthGoalSectionLength * 3)
            SetRandomOdds(.50f, .30f, .20f, .0f, 0f, 0f, 0f, 0f, 0f);
        else if (Depth < DepthGoalSectionLength * 4)
            SetRandomOdds(.3f, .3f, .3f, .0f, 0f, 0f, 0f, .1f, 0f);
        else if (Depth < DepthGoalSectionLength * 5)
            SetRandomOdds(.1f, .25f, .25f, .25f, 0f, 0f, 0f, .15f, 0f);
        else if (Depth < DepthGoalSectionLength * 6)
            SetRandomOdds(.1f, .1f, .2f, .4f, 0f, 0f, 0f, .2f, 0f);
        else if (Depth < DepthGoalSectionLength * 7)
            SetRandomOdds(.1f, .2f, .2f, .1f, .3f, 0f, 0f, .1f, 0f);
        else if (Depth < DepthGoalSectionLength * 8)
            SetRandomOdds(.1f, .2f, .1f, .1f, .2f, .2f, 0f, .1f, 0f);
        else if (Depth < DepthGoalSectionLength * 9)
            SetRandomOdds(0f, .2f, .1f, .1f, .2f, .3f, 0f, .1f, 0f);
        else if (Depth < DepthGoalSectionLength * 10)
            SetRandomOdds(.05f, .1f, .05f, .1f, .1f, .2f, .1f, .1f, .2f);
        else if (Depth < DepthGoalSectionLength * 11)
            SetRandomOdds(.05f, .05f, .1f, .1f, .1f, .2f, .2f, .05f, .15f);
        else if (Depth < DepthGoalSectionLength * 12)
            SetRandomOdds(.05f, .05f, .1f, .1f, .2f, .2f, .2f, .05f, .05f);
        else if (Depth < DepthGoalSectionLength * 13)
            SetRandomOdds(.1f, .1f, .1f, .1f, .1f, .2f, .1f, .05f, .15f);
        else if (Depth < DepthGoalSectionLength * 14)
            SetRandomOdds(.05f, .05f, .05f, .05f, .05f, .1f, .15f, .2f, .3f);
        else if (Depth < DepthGoalSectionLength * 15)
            SetRandomOdds(.05f, .05f, .05f, .05f, .2f, .2f, .2f, .1f, .1f);
        //go till at least 15
    }

    private void SetRandomOdds(float AngelFish, float Trout, float Puffer, float Eel, float Jellyfish,
        float Shark, float Swordfish, float Boot, float Tire)
    { //must add up to 1
        GenOdds[(int)WaterObjectType.Angelfish] = AngelFish;
        GenOdds[(int)WaterObjectType.Eel] = Eel;
        GenOdds[(int)WaterObjectType.Jellyfish] = Jellyfish;
        GenOdds[(int)WaterObjectType.Puffer] = Puffer;
        GenOdds[(int)WaterObjectType.Shark] = Shark;
        GenOdds[(int)WaterObjectType.Swordfish] = Swordfish;
        GenOdds[(int)WaterObjectType.Trout] = Trout;
        GenOdds[(int)WaterObjectType.Tire] = Tire;
        GenOdds[(int)WaterObjectType.Boot] = Boot;
        //if (float.Equals((type1 + type2 + boot + barrel),1.0f)) Debug.Log("**Bad Odds = "+ (type1 + type2 + boot + barrel) + " **");
    }

    private void AddScore(int score)
    {
        PlayerScore += score;
        SetScoreUI();
    }

    public int GetFarthestDepth() { return FarthestDepth; }

    private WaterObjectType GetRandomType(bool SchoolOnly)
    {
        float rnd;
        if (!SchoolOnly) rnd = Random.Range(0f, 1.0f);
        else rnd = Random.RandomRange(0,
            (1.0f - GenOdds[(int)WaterObjectType.Boot] - GenOdds[(int)WaterObjectType.Tire] 
            - GenOdds[(int)WaterObjectType.Jellyfish] - GenOdds[(int)WaterObjectType.Shark]
            - GenOdds[(int)WaterObjectType.Swordfish]));

        if (rnd < GenOdds[(int)WaterObjectType.Angelfish])
            return WaterObjectType.Angelfish;
        else if (rnd <
            (GenOdds[(int)WaterObjectType.Angelfish] +
            GenOdds[(int)WaterObjectType.Trout]))
            return WaterObjectType.Trout;
        else if (rnd < (GenOdds[(int)WaterObjectType.Angelfish] +
            GenOdds[(int)WaterObjectType.Trout] +
            GenOdds[(int)WaterObjectType.Eel]))
            return WaterObjectType.Eel;
        else if (rnd < (GenOdds[(int)WaterObjectType.Angelfish] +
            GenOdds[(int)WaterObjectType.Trout] +
            GenOdds[(int)WaterObjectType.Eel] +
            GenOdds[(int)WaterObjectType.Puffer]))
            return WaterObjectType.Puffer;
        else if (rnd < (GenOdds[(int)WaterObjectType.Angelfish] +
            GenOdds[(int)WaterObjectType.Trout] +
            GenOdds[(int)WaterObjectType.Eel] +
            GenOdds[(int)WaterObjectType.Puffer] +
            GenOdds[(int)WaterObjectType.Swordfish]))
            return WaterObjectType.Swordfish;
        else if (rnd < (GenOdds[(int)WaterObjectType.Angelfish] +
            GenOdds[(int)WaterObjectType.Trout] +
            GenOdds[(int)WaterObjectType.Eel] +
            GenOdds[(int)WaterObjectType.Swordfish] +
            GenOdds[(int)WaterObjectType.Puffer] +
            GenOdds[(int)WaterObjectType.Shark]))
            return WaterObjectType.Shark;
        else if (rnd < (GenOdds[(int)WaterObjectType.Angelfish] +
            GenOdds[(int)WaterObjectType.Trout] +
            GenOdds[(int)WaterObjectType.Eel] +
            GenOdds[(int)WaterObjectType.Swordfish] +
            GenOdds[(int)WaterObjectType.Puffer] +
            GenOdds[(int)WaterObjectType.Shark] +
            GenOdds[(int)WaterObjectType.Jellyfish]))
            return WaterObjectType.Jellyfish;
        else if (rnd < (GenOdds[(int)WaterObjectType.Angelfish] +
            GenOdds[(int)WaterObjectType.Trout] +
            GenOdds[(int)WaterObjectType.Eel] +
            GenOdds[(int)WaterObjectType.Swordfish] +
            GenOdds[(int)WaterObjectType.Puffer] +
            GenOdds[(int)WaterObjectType.Shark] +
            GenOdds[(int)WaterObjectType.Jellyfish] +
            GenOdds[(int)WaterObjectType.Tire]))
            return WaterObjectType.Tire;
        else if (rnd < (GenOdds[(int)WaterObjectType.Angelfish] +
            GenOdds[(int)WaterObjectType.Trout] +
            GenOdds[(int)WaterObjectType.Eel] +
            GenOdds[(int)WaterObjectType.Swordfish] +
            GenOdds[(int)WaterObjectType.Puffer] +
            GenOdds[(int)WaterObjectType.Shark] +
            GenOdds[(int)WaterObjectType.Jellyfish] +
            GenOdds[(int)WaterObjectType.Tire] +
            GenOdds[(int)WaterObjectType.Boot]))
            return WaterObjectType.Boot;

        Debug.Log("problem with random");
        return WaterObjectType.Angelfish;
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

        if ((Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.Space))
            && RechargePercentage >= .3f)
        {
            Boosting = true;
            BoostTimer = BoostTimeLimit * RechargePercentage;
            //RechargeTimer = 0;
        }

        if (Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.Space))
        {
            if (Boosting)
            {
                Boosting = false;
                RechargeTimer = RechargePercentage * RechargeTime;
            }
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && GameState == GameplayState.AtTop)
        {
            PlayButtonHit();
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

    public GameplayState GetGamestate()
    {
        return GameState;
    }
}

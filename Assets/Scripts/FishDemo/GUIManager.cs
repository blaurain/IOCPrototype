using UnityEngine;
using System.Collections;

public class GUIManager : MonoBehaviour
{
    public tk2dTextMesh UIDepth;
    public tk2dTextMesh UIScore;
    public tk2dTextMesh UIHighScore;
    public tk2dTextMesh UIDepthRecord;
    public tk2dTextMesh UIDepthLabel;
    public tk2dTextMesh UIScoreLabel;
    public GameObject UIFishBox;
    public tk2dUIToggleControl UISoundOff;
    public tk2dUIToggleControl UISoundOn;
    public GameObject UIBoostFill;
    public GameObject UIInstructions;
    public GameObject UISettingsParent;
    public tk2dUIItem PlayButton;
    public tk2dUIItem SettingsButton;
    public GameObject SettingsImage;
    public GameObject BackImage;

    public bool IsShowingSettings;
    public bool HighScorePulse;
    public bool DepthRecordPulse;
    private int CurrentDepthDisplay;
    private const float PulseTime = .5f;
    private const float PulseSpeed = .2f;
    private float PulseTimer;
    private float PulseDirection = 1f;

    private const float Volume = 50f;
    private const string SoundPrefString = "SoundOn";
    private bool soundCheck;
    private bool isSoundOn;
    public bool IsSoundOn
    {
        get
        {
            if (!soundCheck)
            {
                if (PlayerPrefs.HasKey(SoundPrefString))
                {
                    int res = PlayerPrefs.GetInt(SoundPrefString);
                    if (res == 1)
                        isSoundOn = true;
                    else
                        isSoundOn = false;
                }
                else
                {
                    isSoundOn = true;
                }
                soundCheck = true;
                return isSoundOn;
            }
            else
            {
                return isSoundOn;
            }

        }

        set
        {
            if (value)
            {
                AudioListener.volume = Volume;
                PlayerPrefs.SetInt(SoundPrefString, 1);
            }
            else
            {
                AudioListener.volume = 0;
                PlayerPrefs.SetInt(SoundPrefString, 0);
            }

            isSoundOn = value;
        }
    }

    public void Awake()
    {
        IsShowingSettings = false;
        soundCheck = false;
        if (IsSoundOn) AudioListener.volume = Volume;
        else AudioListener.volume = 0;
    }

    public void Update()
    {
        if(GameSystem.Instance.GameManager.GetGamestate() == GameplayState.AtTop && 
        (HighScorePulse || DepthRecordPulse))
        {
            PulseTimer += Time.deltaTime;
            if (PulseTimer >= PulseTime)
            {
                PulseDirection *= -1;
                PulseTimer = 0;
            }

            if (HighScorePulse)
            {
                UIHighScore.transform.parent.localScale = new Vector3(
                    UIHighScore.transform.parent.localScale.x + (PulseSpeed * Time.deltaTime * PulseDirection),
                    UIHighScore.transform.parent.localScale.y + (PulseSpeed * Time.deltaTime * PulseDirection),
                    UIHighScore.transform.parent.localScale.z);

            }
            if (DepthRecordPulse)
            {
                UIDepthRecord.transform.parent.localScale = new Vector3(
                    UIDepthRecord.transform.parent.localScale.x + (PulseSpeed * Time.deltaTime * PulseDirection),
                    UIDepthRecord.transform.parent.localScale.y + (PulseSpeed * Time.deltaTime * PulseDirection),
                    UIDepthRecord.transform.parent.localScale.z);
            }
        }
    }

    public void SetRechargeFill(float percentage)
    {
        UIBoostFill.transform.localScale = new Vector3(percentage, 1f, 1f);
    }

    public void ResetPulsing()
    {
        UIHighScore.transform.parent.localScale = new Vector3(1, 1, 1);
        UIDepthRecord.transform.parent.localScale = new Vector3(1, 1, 1);
        HighScorePulse = false;
        DepthRecordPulse = false;
        PulseDirection = 1f;
        CurrentDepthDisplay = 0;
    }

    public void StartCasting()
    {
        UIBoostFill.transform.parent.gameObject.SetActive(true);
        UIScoreLabel.transform.gameObject.SetActive(true);
        UIDepthLabel.transform.gameObject.SetActive(true);
    }

    public void SetUpTopMenu(bool ToTop)
    {
        PlayButton.gameObject.SetActive(ToTop);
        UIHighScore.gameObject.transform.parent.gameObject.SetActive(ToTop);
        UIDepthRecord.gameObject.transform.parent.gameObject.SetActive(ToTop);
        UIInstructions.gameObject.SetActive(ToTop);
        SettingsButton.gameObject.SetActive(ToTop);
    }

    public void HideBoostFill()
    {
        UIBoostFill.transform.parent.gameObject.SetActive(false);
    }

    public void SetRecords(string HighScore, string DepthRecord)
    {
        UIHighScore.text = HighScore;
        UIDepthRecord.text = DepthRecord + "m";
    }

    public void SetScore(int score)
    {
        UIScore.text = ((int)score).ToString();
    }

    public void SetDepth(int depth)
    {
        if (depth != CurrentDepthDisplay)
        {
            CurrentDepthDisplay = depth;
            UIDepth.text = CurrentDepthDisplay.ToString() + "m";
        }
    }

    public void ResetButtonHit()
    {
        Debug.Log("clearing saved data");
        PlayerPrefs.DeleteAll();
    }

    public void SettingsButtonHit()
    {
        if (IsShowingSettings)
        {
            UIInstructions.SetActive(true);
            UISettingsParent.SetActive(false);
            SettingsImage.SetActive(true);
            BackImage.SetActive(false);
            PlayButton.gameObject.SetActive(true);
        }
        else
        {
            UIInstructions.SetActive(false);
            UISettingsParent.SetActive(true);
            SettingsImage.SetActive(false);
            BackImage.SetActive(true);
            PlayButton.gameObject.SetActive(false);

            if (IsSoundOn)
            {
                UISoundOn.IsOn = true;
                UISoundOff.IsOn = false;
            }
            else
            {
                UISoundOn.IsOn = false;
                UISoundOff.IsOn = true;
            }
        }
        IsShowingSettings = !IsShowingSettings;
    }

    public void SettingsSoundOnHit()
    {
        if (!IsSoundOn)
        {
            IsSoundOn = true;
            UISoundOn.IsOn = true;
            UISoundOff.IsOn = false;
        }
    }

    public void SettingsSoundOffHit()
    {
        if (IsSoundOn)
        {
            IsSoundOn = false;
            UISoundOff.IsOn = true;
            UISoundOn.IsOn = false;
        }
    }
}

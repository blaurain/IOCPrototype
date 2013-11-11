using UnityEngine;
using System.Collections;

public class WaterObject : MonoBehaviour
{
    public WaterObjectType ObjType; //set on prefab
    private bool isFish;

    private bool isFacingLeft;
    private bool Facingleft
    {
        get { return isFacingLeft; }
        set
        {
            isFacingLeft = value;
            if (value) gameObject.GetComponent<tk2dSprite>().FlipX = true;
            else gameObject.GetComponent<tk2dSprite>().FlipX = false;
        }
    }

    public void Start()
    {
        switch (ObjType)
        {
            case WaterObjectType.FishType1:
            case WaterObjectType.FishType2:
                isFish = true;
                break;

            case WaterObjectType.Boot:
            case WaterObjectType.ExplodingBarrel:
                isFish = false;
                break;
        }

        //choose random direction
        int rndDir = Random.Range(0, 2); //0 or 1
        if (rndDir == 0) Facingleft = true;
        else Facingleft = false;

        //choose random color
        gameObject.GetComponent<tk2dSprite>().color = GetRandomFishColor();
    }

    public void Update()
    {
        if (isFish)
        { //swim
            Swim();
        }
    }

    private void Swim()
    {

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

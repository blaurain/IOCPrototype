using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class LevelGenerator
{
    public GameObject PrefabAngelfish;
    public GameObject PrefabEel;
    public GameObject PrefabJellyfish;
    public GameObject PrefabPuffer;
    public GameObject PrefabShark;
    public GameObject PrefabSwordfish;
    public GameObject PrefabTrout;
    public GameObject PrefabTire;
    public GameObject PrefabBoot;

    private List<GameObject>[] ObjList;

    public GameObject MasterFolder;

    public LevelGenerator()
    {
        ObjList = new List<GameObject>[WaterObject.NumberOfFishTypes];
        for (int i = 0; i < WaterObject.NumberOfFishTypes; i++)
        {
            ObjList[i] = new List<GameObject>();
        }
    }

    public List<GameObject> GetOrCreate(WaterObjectType t, int numToCreate)
    {
        List<GameObject> goList = new List<GameObject>();
        for (int i = 0; i < numToCreate; i++)
            goList.Add(GetOrCreate(t));

        return goList; //add peer list to objs
    }

    public GameObject GetOrCreate(WaterObjectType t)
    {
        GameObject PrefabToGet;
        switch (t)
        {
            case WaterObjectType.Eel:
                PrefabToGet = PrefabEel;
                break;

            case WaterObjectType.Jellyfish:
                PrefabToGet = PrefabJellyfish;
                break;

            case WaterObjectType.Puffer:
                PrefabToGet = PrefabPuffer;
                break;

            case WaterObjectType.Shark:
                PrefabToGet = PrefabShark;
                break;

            case WaterObjectType.Swordfish:
                PrefabToGet = PrefabSwordfish;
                break;

            case WaterObjectType.Trout:
                PrefabToGet = PrefabTrout;
                break;

            case WaterObjectType.Tire:
                PrefabToGet = PrefabAngelfish;
                break;

            case WaterObjectType.Boot:
                PrefabToGet = PrefabAngelfish;
                break;

            default:
            case WaterObjectType.Angelfish:
                PrefabToGet = PrefabAngelfish;
                break;
        }

        foreach (GameObject go in ObjList[(int)t])
        {
            if (!go.activeInHierarchy)
            {
                SetSingleFishActive(go);
                return go;
            }
        }
        //didn't find it, create new one
        GameObject newGO = (GameObject)GameObject.Instantiate(PrefabToGet);
        newGO.transform.parent = MasterFolder.transform;
        ObjList[(int)t].Add(newGO);
        return newGO;
    }

    public void ClearAll()
    {
        List<GameObject> allChildren = new List<GameObject>();
        foreach (Transform child in MasterFolder.transform) allChildren.Add(child.gameObject);
        allChildren.ForEach(child => GameObject.Destroy(child));

        foreach (List<GameObject> list in ObjList)
            list.Clear();
    }

    private void SetSingleFishActive(GameObject go)
    {
        go.SetActive(true);
        go.GetComponentInChildren<WaterObject>().IsPartOfSchool = false;
    }
}

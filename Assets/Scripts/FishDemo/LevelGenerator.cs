using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class LevelGenerator
{
    public GameObject PrefabFishType1;
    public GameObject PrefabFishType2;
    public GameObject PrefabBoot;
    
    private List<GameObject> ObjListFishType1;
    private List<GameObject> ObjListFishType2;
    private List<GameObject> ObjListBoot;
    private List<GameObject> ObjListBarrel;

    public GameObject Fish1Folder;
    public GameObject Fish2Folder;
    public GameObject BootFolder;
    public GameObject BarrelFolder;

    public LevelGenerator()
    {
        ObjListFishType1 = new List<GameObject>();
        ObjListFishType2 = new List<GameObject>();
        ObjListBoot = new List<GameObject>();
        ObjListBarrel = new List<GameObject>();
    }

    public GameObject GetOrCreate(WaterObjectType t)
    {
        switch (t)
        {
            case WaterObjectType.FishType1:
                foreach (GameObject go in ObjListFishType1)
                {
                    if (!go.activeInHierarchy)
                    {
                        go.SetActive(true);
                        return go;
                    }
                }
                //didn't find it, create new one
                GameObject newGO1 = (GameObject)GameObject.Instantiate(PrefabFishType1);
                newGO1.transform.parent = Fish1Folder.transform;
                ObjListFishType1.Add(newGO1);
                return newGO1;

            case WaterObjectType.FishType2:
                foreach (GameObject go in ObjListFishType2)
                {
                    if (!go.activeInHierarchy)
                    {
                        go.SetActive(true);
                        return go;
                    }
                }
                //didn't find it, create new one
                GameObject newGO2 = (GameObject)GameObject.Instantiate(PrefabFishType2);
                newGO2.transform.parent = Fish2Folder.transform;
                ObjListFishType2.Add(newGO2);
                return newGO2;

            default: //default to boot if nothing else, will never happen in real game
            case WaterObjectType.Boot:
                foreach (GameObject go in ObjListBoot)
                {
                    if (!go.activeInHierarchy)
                    {
                        go.SetActive(true);
                        return go;
                    }
                }
                //didn't find it, create new one
                GameObject newGO3 = (GameObject)GameObject.Instantiate(PrefabBoot);
                newGO3.transform.parent = BootFolder.transform;
                ObjListBoot.Add(newGO3);
                return newGO3;
        }
    }
}

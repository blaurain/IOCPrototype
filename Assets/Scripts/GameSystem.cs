using UnityEngine;
using System.Collections;

public class GameSystem : MonoBehaviour
{
    #region Singleton
    private static GameSystem instance;
    public static GameSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = (GameSystem)GameObject.FindObjectOfType(typeof(GameSystem));
            }
            return instance;
        }
    }
    #endregion

    public GameplayManager GameManager;

    //public void Awake()
    //{

    //}

    //public void Update()
    //{

    //}
}

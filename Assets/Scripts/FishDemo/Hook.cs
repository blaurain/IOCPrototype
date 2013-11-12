using UnityEngine;
using System.Collections;

public class Hook : MonoBehaviour
{
    public GameplayManager gameplayManager;

    public void OnCollisionEnter(Collision c)
    {
        //Debug.Log("hook hit something");
        if (c.collider.gameObject.CompareTag("WaterObject"))
        {
            gameplayManager.HookHit(c.collider.gameObject.GetComponent<WaterObject>(), c);
        }
    }
}

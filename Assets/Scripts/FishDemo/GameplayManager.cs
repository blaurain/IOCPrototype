using UnityEngine;
using System.Collections;

public class GameplayManager : MonoBehaviour
{
    public GameObject HookObject;
    private GameObject ObjectFolder;
    private const float ScreenBoundWorldX = 30;
    private const float MouseMoveLimit = 5;
    private const float KeyMovementSpeed = 50;

    public void Start()
    {
        //create an empty game object, treat it as a folder
        ObjectFolder = new GameObject("_ObjectsCreated");
        ObjectFolder.transform.parent = this.transform;
    }

    public void Update()
    {
        //HandleMouseInput();
        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
        { //swing left
            //HookObject.transform.localPosition = new Vector3(
            //Mathf.Clamp(HookObject.transform.localPosition.x - KeyMovementSpeed*Time.deltaTime, -ScreenBoundWorldX, ScreenBoundWorldX),
            //HookObject.transform.localPosition.y,
            //HookObject.transform.localPosition.z);
            HookObject.rigidbody.AddForce(-60 * Time.deltaTime, 0, 0);
        }
        else if (Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.LeftArrow))
        { //swing right
            //HookObject.transform.localPosition = new Vector3(
            //Mathf.Clamp(HookObject.transform.localPosition.x + KeyMovementSpeed * Time.deltaTime, -ScreenBoundWorldX, ScreenBoundWorldX),
            //HookObject.transform.localPosition.y,
            //HookObject.transform.localPosition.z);
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

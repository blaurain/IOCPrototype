using UnityEngine;
using System.Collections;
using com.shephertz.app42.paas.sdk.csharp.user;
using com.shephertz.app42.paas.sdk.csharp.pushNotification;
using com.shephertz.app42.paas.sdk.csharp;
using SimpleJSON;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Collections.Generic;
using System;

public class LoginUI : MonoBehaviour
{
    public tk2dTextMesh userName;
    public GameObject errorText;


    ServiceAPI sp = null;
    UserService userService = null; // Initializing User Service.
    User createUserObj = null;

    public void Start()
    {
        //set up server stuff
        ServicePointManager.ServerCertificateValidationCallback = Validator;
        sp = new ServiceAPI(ServerConstants.apiKey, ServerConstants.secretKey);
        userService = sp.BuildUserService();
        
    }

    public void Update()
    {

    }

    public void LoginClicked()
    {
        if (userName.text.Length == 0 || userName.text.Length > 12 || userName.text.CompareTo("enter name") == 0)
        {
            errorText.SetActive(true);
            return;
        }

        Debug.Log("login clicked");
        //userService.GetUser(userName.text, loginCallback);
        try
        {
            //User tempUser = userService.GetUser(userName.text);
            
            User tempUser = userService.Authenticate(userName.text, "password");
            Debug.Log("got user");


        }
        catch (Exception e)
        {
            Debug.Log("didn't got user");
            try
            {
                userService.CreateUser(userName.text, "password", "fake@email.com");
                Debug.Log("created user");
            }
            catch (Exception e2)
            {
                Debug.Log("didn't create user");
                errorText.SetActive(true);
            }
        }
    }

    public static bool Validator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

}
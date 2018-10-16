using System;
using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Client;
using UnityEngine;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{

    public static LoginManager Instance;

    public GameObject LoginWindow;
    public InputField NameInput;
    public Button SubmitLoginButton;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        LoginWindow.SetActive(false);
        SubmitLoginButton.onClick.AddListener(OnSubmitLogin);
        GlobalManager.Instance.Client.MessageReceived += OnMessage;
    }

    void OnDestroy()
    {
        GlobalManager.Instance.Client.MessageReceived -= OnMessage;
    }

    public void StartLoginProcess()
    {
        LoginWindow.SetActive(true);
    }

    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        using (Message m = e.GetMessage())
        {
            switch ((Tags) m.Tag)
            {
                case Tags.LoginRequestDenied:
                    OnLoginDecline();
                    break;
                case Tags.LoginRequestAccepted:
                    OnLoginAccept(m.Deserialize<LoginInfoData>());
                    break;
            }
        }

    }


    public void OnSubmitLogin()
    {
        if (NameInput.text != "")
        {
            LoginWindow.SetActive(false);

            using (Message m = Message.Create((ushort)Tags.LoginRequest, new LoginRequestData(NameInput.text)))
            {
                GlobalManager.Instance.Client.SendMessage(m, SendMode.Reliable);
            }
        }
    }

    public void OnLoginDecline()
    {
        LoginWindow.SetActive(true);
    }

    public void OnLoginAccept(LoginInfoData data)
    {
        GlobalManager.Instance.PlayerId = data.Id;
        GlobalManager.Instance.LoadLobbyScene(data.Data);
    }


}


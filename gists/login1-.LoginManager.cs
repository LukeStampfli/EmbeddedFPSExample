using System;
using DarkRift;
using DarkRift.Client;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameObject loginWindow;
    [SerializeField]
    private InputField nameInput;
    [SerializeField]
    private Button submitLoginButton;

    void Start()
    {
        ConnectionManager.Instance.OnConnected += StartLoginProcess;
        submitLoginButton.onClick.AddListener(OnSubmitLogin);
        loginWindow.SetActive(false);
    }

    void OnDestroy()
    {
        ConnectionManager.Instance.OnConnected -= StartLoginProcess;
    }

    public void StartLoginProcess()
    {
        loginWindow.SetActive(true);
    }

    public void OnSubmitLogin()
    {
        if (!String.IsNullOrEmpty(nameInput.text))
        {
            loginWindow.SetActive(false);

            using (Message message = Message.Create((ushort)Tags.LoginRequest, new LoginRequestData(nameInput.text)))
            {
                ConnectionManager.Instance.Client.SendMessage(message, SendMode.Reliable);
            }
        }
    }
}


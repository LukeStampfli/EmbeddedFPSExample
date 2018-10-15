using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UnityClient))]
public class GlobalManager : MonoBehaviour
{

    public string IpAdress;
    public int Port;

    public static GlobalManager Instance;
    public UnityClient Client { get; private set; }

    [Header("Global Variables")]
    public ushort PlayerId;
    public LobbyInfoData LastRecievedLobbyInfoData;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Client = GetComponent<UnityClient>();
        DontDestroyOnLoad(this);
    }


    void Start()
    {
       Client.ConnectInBackground(IPAddress.Parse(IpAdress),Port, IPVersion.IPv4, ConnectCallback); 
    }

    private void ConnectCallback(Exception exception)
    {
        Debug.Log(exception.Message);
        if (Client.Connected)
        {
            LoginManager.Instance.StartLoginProcess();
        }
        else
        {
            Start();
        }
    }

    public void LoadLobbyScene(LobbyInfoData data)
    {
        LastRecievedLobbyInfoData = data;
        SceneManager.LoadScene("Lobby");
    }

}

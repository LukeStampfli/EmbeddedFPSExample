using System;
using System.Net;
using DarkRift;
using DarkRift.Client.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UnityClient))]
public class GlobalManager : MonoBehaviour
{
    public static GlobalManager Instance;

    [Header("Variables")]
    public string IpAdress;
    public int Port;

    [Header("References")]
    public UnityClient Client;

    [Header("Public Fields")]
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
        DontDestroyOnLoad(this);
    }


    void Start()
    {
       Client.ConnectInBackground(IPAddress.Parse(IpAdress),Port, IPVersion.IPv4, ConnectCallback); 
    }

    private void ConnectCallback(Exception exception)
    {
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

using System;
using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using DarkRift.Server.Unity;
using UnityEngine;

public class ServerManager : MonoBehaviour
{

    public static ServerManager Instance;
    public XmlUnityServer XmlServer { get; private set; }
    public DarkRiftServer Server;

    public Dictionary<ushort, Player> Players = new Dictionary<ushort, Player>();
    public Dictionary<string, Player> PlayersByName = new Dictionary<string, Player>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        XmlServer = GetComponent<XmlUnityServer>();
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        Server = XmlServer.Server;
        Server.ClientManager.ClientConnected += OnClientConnect;
        Server.ClientManager.ClientDisconnected += OnClientDisconnect;
    }

    void OnDestroy()
    {
        Server.ClientManager.ClientConnected -= OnClientConnect;
        Server.ClientManager.ClientDisconnected -= OnClientDisconnect;
    }

    private void OnClientDisconnect(object sender, ClientDisconnectedEventArgs e)
    {
        IClient client = e.Client;
        Player p;
        if (Players.TryGetValue(client.ID, out p))
        {
            p.OnClientDisconnect(sender, e);
        }
        e.Client.MessageReceived -= OnMessage;
    }

    private void OnClientConnect(object sender, ClientConnectedEventArgs e)
    {
        e.Client.MessageReceived += OnMessage;
    }

    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        IClient client = (IClient) sender;
        using (Message m = e.GetMessage())
        {
            switch ((Tags) m.Tag)
            {
                case Tags.LoginRequest:
                    OnclientLogin(client, m.Deserialize<LoginRequestData>());
                    break;
                case Tags.LobbyJoinRoomRequest:
                    RoomManager.Instance.TryJoinRoom(client, m.Deserialize<JoinRoomRequest>());
                    break;
            }
        }
    }

    private void OnclientLogin(IClient client, LoginRequestData data)
    {
        if (PlayersByName.ContainsKey(data.Name))
        {
            using (Message m = Message.CreateEmpty((ushort)Tags.LoginRequestDenied))
            {
                client.SendMessage(m, SendMode.Reliable);
            }
            return;
        }

        //from now on the player will handle his messages
        client.MessageReceived -= OnMessage;
        new Player(client, data);
    }
}

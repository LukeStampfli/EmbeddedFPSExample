using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using DarkRift.Server.Unity;
using UnityEngine;

[RequireComponent(typeof(XmlUnityServer))]
public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance;

    private XmlUnityServer xmlServer;
    private DarkRiftServer server;

    public Dictionary<ushort, ClientConnection> Players = new Dictionary<ushort, ClientConnection>();
    public Dictionary<string, ClientConnection> PlayersByName = new Dictionary<string, ClientConnection>();

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
        xmlServer = GetComponent<XmlUnityServer>();
        server = xmlServer.Server;
        server.ClientManager.ClientConnected += OnClientConnected;
        server.ClientManager.ClientDisconnected += OnClientDisconnected;
    }

    void OnDestroy()
    {
        server.ClientManager.ClientConnected -= OnClientConnected;
        server.ClientManager.ClientDisconnected -= OnClientDisconnected;
    }

    private void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e)
    {
        e.Client.MessageReceived -= OnMessage;
    }

    private void OnClientConnected(object sender, ClientConnectedEventArgs e)
    {
        e.Client.MessageReceived += OnMessage;
    }

    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        IClient client = (IClient)sender;
        using (Message message = e.GetMessage())
        {
            switch ((Tags)message.Tag)
            {
                case Tags.LoginRequest:
                    OnclientLogin(client, message.Deserialize<LoginRequestData>());
                    break;
            }
        }
    }

    private void OnclientLogin(IClient client, LoginRequestData data)
    {
        if (PlayersByName.ContainsKey(data.Name))
        {
            using (Message message = Message.CreateEmpty((ushort)Tags.LoginRequestDenied))
            {
                client.SendMessage(message, SendMode.Reliable);
            }
            return;
        }

        // In the future the ClientConnection will handle its messages
        client.MessageReceived -= OnMessage;

        new ClientConnection(client, data);
    }
}

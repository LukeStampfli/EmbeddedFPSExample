using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Client;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;
    public GameObject PlayerPrefab;
    public uint CurrentServerTick;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GlobalManager.Instance.Client.MessageReceived += OnMessage;
        using (Message m = Message.CreateEmpty((ushort)Tags.GameJoinRequest))
        {
            GlobalManager.Instance.Client.SendMessage(m, SendMode.Reliable);
        }
    }

    void FixedUpdate()
    {
        CurrentServerTick++;
    }
    void OnDestroy()
    {
        GlobalManager.Instance.Client.MessageReceived -= OnMessage;
    }


    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        using (Message m = e.GetMessage())
        {
            switch ((Tags)m.Tag)
            {
                case Tags.GameStartDataResponse:
                    OnGameJoinAccept(m.Deserialize<GameStartData>());
                    break;
            }
        }

    }

    public void OnGameJoinAccept(GameStartData data)
    {
        CurrentServerTick = data.OnJoinServerTick;
        foreach (PlayerSpawnData ppd in data.Players)
        {
            GameObject go = Instantiate(PlayerPrefab);
            ClientPlayer player = go.GetComponent<ClientPlayer>();
            player.Initialize(ppd.Id, ppd.Name);
        }
    }
}

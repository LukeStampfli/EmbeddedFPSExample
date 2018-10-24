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
    public uint LastRecievedServerTick;

    public Dictionary<ushort, ClientPlayer> Players = new Dictionary<ushort, ClientPlayer>();

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
                case Tags.GameUpdate:
                    OnGameUpdate(m.Deserialize<GameUpdateData>());
                    break;
            }
        }

    }

    public void OnGameUpdate(GameUpdateData updateData)
    {
        LastRecievedServerTick = updateData.Frame;
        foreach (PlayerSpawnData data in updateData.SpawnData)
        {
            if (data.Id != GlobalManager.Instance.PlayerId)
            {
                SpawnPlayer(data);
            }
        }

        foreach (PlayerUpdateData data in updateData.UpdateData)
        {
            ClientPlayer p;
            if (Players.TryGetValue(data.Id, out p))
            {
                p.RecieveUpdate(data);
            }
        }

        foreach (PLayerHealthUpdateData data in updateData.HealthData)
        {
            ClientPlayer p;
            if (Players.TryGetValue(data.PlayerId, out p))
            {
                p.SetHealth(data.Value);
            }
        }
    }

    public void OnGameJoinAccept(GameStartData data)
    {
        LastRecievedServerTick = data.OnJoinServerTick;
        CurrentServerTick = data.OnJoinServerTick;
        foreach (PlayerSpawnData ppd in data.Players)
        {
            SpawnPlayer(ppd);
        }
    }

    public void SpawnPlayer(PlayerSpawnData ppd)
    {
        GameObject go = Instantiate(PlayerPrefab);
        ClientPlayer player = go.GetComponent<ClientPlayer>();
        player.Initialize(ppd.Id, ppd.Name);
        Players.Add(player.Id, player);
    }
}

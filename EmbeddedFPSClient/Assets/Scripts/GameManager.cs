using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Client;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public GameObject PlayerPrefab;

    [Header("Public Fields")]
    public uint ClientTick;
    public uint LastRecievedServerTick;

    private Dictionary<ushort, ClientPlayer> players = new Dictionary<ushort, ClientPlayer>();

    private Buffer<GameUpdateData> gameUpdateBuffer = new Buffer<GameUpdateData>(1, 1);

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ConnectionManager.Instance.Client.MessageReceived += OnMessage;
        using (Message m = Message.CreateEmpty((ushort)Tags.GameJoinRequest))
        {
            ConnectionManager.Instance.Client.SendMessage(m, SendMode.Reliable);
        }
    }

    void OnDestroy()
    {
        ConnectionManager.Instance.Client.MessageReceived -= OnMessage;
    }


    void OnMessage(object sender, MessageReceivedEventArgs e)
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

    void OnGameUpdate(GameUpdateData updateData)
    {
        gameUpdateBuffer.Add(updateData);
    }

    void FixedUpdate()
    {
        ClientTick++;
        GameUpdateData[] datas = gameUpdateBuffer.Get();
        foreach (GameUpdateData data in datas)
        {
            PerformGameUpdate(data);
        }
    }

    void PerformGameUpdate(GameUpdateData updateData)
    {
        LastRecievedServerTick = updateData.Frame;
        foreach (PlayerSpawnData data in updateData.SpawnData)
        {
            if (data.Id != ConnectionManager.Instance.PlayerId)
            {
                SpawnPlayer(data);
            }
        }

        foreach (PlayerDespawnData data in updateData.DespawnData)
        {
            if (players.ContainsKey(data.Id))
            {
                Destroy(players[data.Id].gameObject);
                players.Remove(data.Id);
            }
        }

        foreach (PlayerUpdateData data in updateData.UpdateData)
        {
            ClientPlayer p;
            if (players.TryGetValue(data.Id, out p))
            {
                p.OnServerDataUpdate(data);
            }
        }

        foreach (PLayerHealthUpdateData data in updateData.HealthData)
        {
            ClientPlayer p;
            if (players.TryGetValue(data.PlayerId, out p))
            {
                p.SetHealth(data.Value);
            }
        }
    }


    void OnGameJoinAccept(GameStartData data)
    {
        LastRecievedServerTick = data.OnJoinServerTick;
        ClientTick = data.OnJoinServerTick;
        foreach (PlayerSpawnData ppd in data.Players)
        {
            SpawnPlayer(ppd);
        }
    }

    void SpawnPlayer(PlayerSpawnData ppd)
    {
        GameObject go = Instantiate(PlayerPrefab);
        ClientPlayer player = go.GetComponent<ClientPlayer>();
        player.Initialize(ppd.Id, ppd.Name);
        players.Add(player.Id, player);
    }
}

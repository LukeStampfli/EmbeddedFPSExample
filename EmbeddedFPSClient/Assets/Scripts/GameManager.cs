using System.Collections.Generic;
using DarkRift;
using DarkRift.Client;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private Dictionary<ushort, ClientPlayer> players = new Dictionary<ushort, ClientPlayer>();

    private Buffer<GameUpdateData> gameUpdateDataBuffer = new Buffer<GameUpdateData>(1, 1);

    [Header("Prefabs")]
    public GameObject PlayerPrefab;

    public uint ClientTick { get; private set; }
    public uint LastReceivedServerTick { get; private set; }

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

    void OnDestroy()
    {
        Instance = null;
        ConnectionManager.Instance.Client.MessageReceived -= OnMessage;
    }

    void Start()
    {
        ConnectionManager.Instance.Client.MessageReceived += OnMessage;
        using (Message message = Message.CreateEmpty((ushort)Tags.GameJoinRequest))
        {
            ConnectionManager.Instance.Client.SendMessage(message, SendMode.Reliable);
        }
    }

    void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            switch ((Tags)message.Tag)
            {
                case Tags.GameStartDataResponse:
                    OnGameJoinAccept(message.Deserialize<GameStartData>());
                    break;
                case Tags.GameUpdate:
                    OnGameUpdate(message.Deserialize<GameUpdateData>());
                    break;
            }
        }
    }

    void OnGameJoinAccept(GameStartData gameStartData)
    {
        LastReceivedServerTick = gameStartData.OnJoinServerTick;
        ClientTick = gameStartData.OnJoinServerTick;
        foreach (PlayerSpawnData playerSpawnData in gameStartData.Players)
        {
            SpawnPlayer(playerSpawnData);
        }
    }

    void OnGameUpdate(GameUpdateData gameUpdateData)
    {
        gameUpdateDataBuffer.Add(gameUpdateData);
    }

    void SpawnPlayer(PlayerSpawnData playerSpawnData)
    {
        GameObject go = Instantiate(PlayerPrefab);
        ClientPlayer player = go.GetComponent<ClientPlayer>();
        player.Initialize(playerSpawnData.Id, playerSpawnData.Name);
        players.Add(playerSpawnData.Id, player);
    }

    void FixedUpdate()
    {
        ClientTick++;
        GameUpdateData[] receivedGameUpdateData = gameUpdateDataBuffer.Get();
        foreach (GameUpdateData data in receivedGameUpdateData)
        {
            UpdateClientGameState(data);
        }
    }

    void UpdateClientGameState(GameUpdateData gameUpdateData)
    {
        LastReceivedServerTick = gameUpdateData.Frame;
        foreach (PlayerSpawnData data in gameUpdateData.SpawnDataData)
        {
            if (data.Id != ConnectionManager.Instance.PlayerId)
            {
                SpawnPlayer(data);
            }
        }

        foreach (PlayerDespawnData data in gameUpdateData.DespawnDataData)
        {
            if (players.ContainsKey(data.Id))
            {
                Destroy(players[data.Id].gameObject);
                players.Remove(data.Id);
            }
        }

        foreach (PlayerStateData data in gameUpdateData.UpdateData)
        {
            ClientPlayer p;
            if (players.TryGetValue(data.Id, out p))
            {
                p.OnServerDataUpdate(data);
            }
        }

        foreach (PlayerHealthUpdateData data in gameUpdateData.HealthData)
        {
            ClientPlayer p;
            if (players.TryGetValue(data.PlayerId, out p))
            {
                p.SetHealth(data.Value);
            }
        }
    }
}

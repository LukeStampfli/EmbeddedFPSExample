using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

public class Room : MonoBehaviour
{
    public string Name;
    public List<PlayerClient> Players = new List<PlayerClient>();
    public List<ServerPlayer> ServerPlayers = new List<ServerPlayer>();
    public byte MaxSlots;
    public GameObject PlayerPrefab;

    public PlayerUpdateData[] updateDatas = new PlayerUpdateData[0];
    private List<PlayerSpawnData> spawnDatas = new List<PlayerSpawnData>(4);
    private List<PlayerDespawnData> despawnDatas = new List<PlayerDespawnData>(4);

    public uint ServerTick;

    void FixedUpdate()
    {
        ServerTick++;
        //perform updates for all players in the room
        foreach (ServerPlayer player in ServerPlayers)
        {
            player.PerformUpdate();
        }

        //send update message to all players
        using (Message m = Message.Create((ushort) Tags.GameUpdate, new GameUpdateData(updateDatas, spawnDatas.ToArray(), despawnDatas.ToArray())))
        {
            foreach (ServerPlayer p in ServerPlayers)
            {
                p.Client.SendMessage(m, SendMode.Reliable);
            }
        }

        //clear values
        spawnDatas.Clear();
        despawnDatas.Clear();
    }


    public void Initialize(string name, byte maxslots)
    {
        Name = name;
        MaxSlots = maxslots;
    }

    public void AddPlayerToRoom(PlayerClient playerClient)
    {
        Players.Add(playerClient);
        playerClient.Room = this;
        using (Message message = Message.CreateEmpty((ushort)Tags.LobbyJoinRoomAccepted))
        {
            playerClient.Client.SendMessage(message, SendMode.Reliable);
        }
    }


    public void RemovePlayerFromRoom(PlayerClient playerClient)
    {
        Players.Remove(playerClient);
        playerClient.Room = null;
    }

    public void JoinPlayerToGame(PlayerClient client)
    {
        GameObject go = Instantiate(PlayerPrefab, transform);
        ServerPlayer player = go.GetComponent<ServerPlayer>();
        player.Initialize(Vector3.zero, client);

        spawnDatas.Add(player.GetPlayerSpawnData());
    }

    public void Close()
    {
        foreach(PlayerClient p in Players)
        {
            RemovePlayerFromRoom(p);
        }
        Destroy(gameObject);
    }
}

using System.Collections.Generic;
using DarkRift;
using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Public Fields")]
    public string Name;
    public List<ServerPlayer> ServerPlayers = new List<ServerPlayer>();
    public List<ClientConnection> ClientConnections = new List<ClientConnection>();
    public byte MaxSlots;
    public uint ServerTick;

    [Header("Prefabs")]
    public GameObject PlayerPrefab;

    public PlayerUpdateData[] UpdateDatas = new PlayerUpdateData[0];
    public List<PLayerHealthUpdateData> HealthUpdates = new List<PLayerHealthUpdateData>();
    private List<PlayerSpawnData> spawnDatas = new List<PlayerSpawnData>(4);
    private List<PlayerDespawnData> despawnDatas = new List<PlayerDespawnData>(4);

    void FixedUpdate()
    {
        ServerTick++;
        //perform updates for all players in the room
        foreach (ServerPlayer player in ServerPlayers)
        {
            player.PerformShootupdate();
        }

        int i = 0;
        foreach (ServerPlayer player in ServerPlayers)
        {
            player.PerformUpdate(i);
            i++;
        }

        //send update message to all players

        PlayerSpawnData[] tpsd = spawnDatas.ToArray();
        PlayerDespawnData[] tpdd = despawnDatas.ToArray();
        foreach (ServerPlayer p in ServerPlayers)
        {
            using (Message m = Message.Create((ushort)Tags.GameUpdate, new GameUpdateData(ServerTick, UpdateDatas, tpsd, tpdd, HealthUpdates.ToArray())))
            {
                p.Client.SendMessage(m, SendMode.Reliable);
            }
        }
     
        //clear values
        spawnDatas.Clear();
        despawnDatas.Clear();
        HealthUpdates.Clear();
    }


    public void Initialize(string name, byte maxslots)
    {
        Name = name;
        MaxSlots = maxslots;
    }

    public void AddPlayerToRoom(ClientConnection clientConnection)
    {
        ClientConnections.Add(clientConnection);
        clientConnection.Room = this;
        using (Message message = Message.CreateEmpty((ushort)Tags.LobbyJoinRoomAccepted))
        {
            clientConnection.Client.SendMessage(message, SendMode.Reliable);
        }
    }


    public void RemovePlayerFromRoom(ClientConnection clientConnection)
    {
        Destroy(clientConnection.Player.gameObject);
        despawnDatas.Add(new PlayerDespawnData(clientConnection.Client.ID));
        ClientConnections.Remove(clientConnection);
        ServerPlayers.Remove(clientConnection.Player);
        clientConnection.Room = null;
    }

    public void JoinPlayerToGame(ClientConnection clientConnection)
    {
        GameObject go = Instantiate(PlayerPrefab, transform);
        ServerPlayer player = go.GetComponent<ServerPlayer>();
        player.Initialize(Vector3.zero, clientConnection);

        spawnDatas.Add(player.GetPlayerSpawnData());
    }

    public void Close()
    {
        foreach(ClientConnection p in ClientConnections)
        {
            RemovePlayerFromRoom(p);
        }
        Destroy(gameObject);
    }

    public void PerformShootRayCast(uint frame, ServerPlayer shooter)
    {
        int dif = (int) (ServerTick - 1 - frame);

        //get the position of the ray
        Vector3 firepoint;
        Vector3 direction;

        if (shooter.UpdateDataHistory.Count > dif)
        {
            firepoint = shooter.UpdateDataHistory[dif].Position;
            direction = shooter.UpdateDataHistory[dif].LookDirection * Vector3.forward;
        }
        else
        {
            firepoint = shooter.CurrentUpdateData.Position;
            direction = shooter.CurrentUpdateData.LookDirection * Vector3.forward;
        }

        firepoint += direction * 5f;
        firepoint += transform.parent.localPosition;

        //set all players back in time
        foreach (ServerPlayer player in ServerPlayers)
        {
            if (player.UpdateDataHistory.Count > dif)
            {
                player.Logic.CharacterController.enabled = false;
                player.transform.localPosition = player.UpdateDataHistory[dif].Position;
            }
        }



        RaycastHit hit;

        if (Physics.Raycast(firepoint, direction,out hit, 200f))
        {
            if (hit.transform.CompareTag("Unit"))
            {
                hit.transform.GetComponent<ServerPlayer>().TakeDamage(5);
            }
        }


        //set all players back
        foreach (ServerPlayer player in ServerPlayers)
        {
            player.transform.localPosition = player.CurrentUpdateData.Position;
            player.Logic.CharacterController.enabled = true;
        }
    }
}

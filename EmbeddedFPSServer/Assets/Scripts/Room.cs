using System.Collections.Generic;
using DarkRift;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Room : MonoBehaviour
{
    private Scene scene;
    private PhysicsScene physicsScene;

    private List<ServerPlayer> serverPlayers = new List<ServerPlayer>();

    private List<PlayerStateData> playerStateData = new List<PlayerStateData>(4);
    private List<PlayerSpawnData> playerSpawnData = new List<PlayerSpawnData>(4);
    private List<PlayerDespawnData> playerDespawnData = new List<PlayerDespawnData>(4);


    [Header("Public Fields")]
    public string Name;
    public List<ClientConnection> ClientConnections = new List<ClientConnection>();
    public byte MaxSlots;
    public uint ServerTick;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject playerPrefab;

    public List<PlayerHealthUpdateData> HealthUpdates = new List<PlayerHealthUpdateData>();

    void FixedUpdate()
    {
        ServerTick++;

        // Perform updates for all players in the room.
        foreach (ServerPlayer player in serverPlayers)
        {
            player.PerformuPreUpdate();
        }

        for (var i = 0; i < serverPlayers.Count; i++)
        {
            ServerPlayer player = serverPlayers[i];
            playerStateData[i] = player.PlayerUpdate();
        }

        // Send update message to all players.
        PlayerStateData[] playerStateDataArray = playerStateData.ToArray();
        PlayerSpawnData[] playerSpawnDataArray = playerSpawnData.ToArray();
        PlayerDespawnData[] playerDespawnDataArray = playerDespawnData.ToArray();
        foreach (ServerPlayer p in serverPlayers)
        {
            using (Message m = Message.Create((ushort)Tags.GameUpdate, new GameUpdateData(p.InputTick, playerStateDataArray, playerSpawnDataArray, playerDespawnDataArray, HealthUpdates.ToArray())))
            {
                p.Client.SendMessage(m, SendMode.Reliable);
            }
        }
        
        playerSpawnData.Clear();
        playerDespawnData.Clear();
        HealthUpdates.Clear();
    }


    public void Initialize(string name, byte maxslots)
    {
        Name = name;
        MaxSlots = maxslots;

        CreateSceneParameters csp = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        scene = SceneManager.CreateScene("Room_" + name, csp);
        physicsScene = scene.GetPhysicsScene();

        SceneManager.MoveGameObjectToScene(gameObject, scene);
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
        playerDespawnData.Add(new PlayerDespawnData(clientConnection.Client.ID));
        ClientConnections.Remove(clientConnection);
        serverPlayers.Remove(clientConnection.Player);
        clientConnection.Room = null;
    }

    public void JoinPlayerToGame(ClientConnection clientConnection)
    {
        GameObject go = Instantiate(playerPrefab, transform);
        ServerPlayer player = go.GetComponent<ServerPlayer>();
        serverPlayers.Add(player);
        playerStateData.Add(default);
        player.Initialize(Vector3.zero, clientConnection);

        playerSpawnData.Add(player.GetPlayerSpawnData());
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
            firepoint = shooter.CurrentPlayerStateData.Position;
            direction = shooter.CurrentPlayerStateData.LookDirection * Vector3.forward;
        }

        firepoint += direction * 3f;

        //set all players back in time
        foreach (ServerPlayer player in serverPlayers)
        {
            if (player.UpdateDataHistory.Count > dif)
            {
                player.PlayerLogic.CharacterController.enabled = false;
                player.transform.localPosition = player.UpdateDataHistory[dif].Position;
            }
        }



        RaycastHit hit;

        if (physicsScene.Raycast(firepoint, direction,out hit, 200f))
        {
            if (hit.transform.CompareTag("Unit"))
            {
                hit.transform.GetComponent<ServerPlayer>().TakeDamage(5);
            }
        }


        //set all players back
        foreach (ServerPlayer player in serverPlayers)
        {
            player.transform.localPosition = player.CurrentPlayerStateData.Position;
            player.PlayerLogic.CharacterController.enabled = true;
        }
    }

    public PlayerSpawnData[] GetSpawnDataForAllPlayers()
    {
        PlayerSpawnData[] playerSpawnData = new PlayerSpawnData[serverPlayers.Count];
        for (int i = 0; i < serverPlayers.Count; i++)
        {
            ServerPlayer p = serverPlayers[i];
            playerSpawnData[i] = p.GetPlayerSpawnData();
        }

        return playerSpawnData;
    }
}

using System.Collections.Generic;
using DarkRift;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Room : MonoBehaviour
{
    private Scene scene;
    private PhysicsScene physicsScene;

    [Header("Public Fields")]
    public string Name;
    public List<ServerPlayer> ServerPlayers = new List<ServerPlayer>();
    public List<ClientConnection> ClientConnections = new List<ClientConnection>();
    public byte MaxSlots;
    public uint ServerTick;

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
        ClientConnections.Remove(clientConnection);
        clientConnection.Room = null;
    }

    public void Close()
    {
        foreach (ClientConnection p in ClientConnections)
        {
            RemovePlayerFromRoom(p);
        }
        Destroy(gameObject);
    }
}

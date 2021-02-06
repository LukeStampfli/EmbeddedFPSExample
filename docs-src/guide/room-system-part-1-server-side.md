# Room System Part 1 (Server-Side)

We will create a very basic room system for our FPS game. It will work like this:
- Players can join and leave game rooms.
- Players can create a room with a name and an amount slots.( not in the tutorial but you can add it :smiley:)

::: warning
We are creating a room system which runs on a single game server. This system does not scale well because all your players are on a single server. Usually games distribute players to different game server with another system first.
:::

Since 2018.3 Unity can handle multiple physics scenes which makes it much easier to run multiple rooms on a single server. Before 2018.3 this option didn't exist. One option to do it was to just move each room far away from the others by spawning each room at an icrementing offset but that had some performance disbenefits.

Our Room system will spawn a map and objects for each room and then create a separate physics scene for that room.

Let's start by creating a "Room" script in the Scripts folder and filling it with:

```csharp
using System.Collections.Generic;
using DarkRift;
using UnityEngine;

public class Room : MonoBehaviour
{
    private Scene scene;
    private PhysicsScene physicsScene;

    [Header("Public Fields")]
    public string Name;
    public List<ClientConnection> ClientConnections = new List<ClientConnection>();
    public byte MaxSlots;

    public void Initialize(string name, byte maxslots)
    {
        Name = name;
        MaxSlots = maxslots;

        CreateSceneParameters csp = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        scene = SceneManager.CreateScene("Room_" + name, csp);
        physicsScene = scene.GetPhysicsScene();

        SceneManager.MoveGameObjectToScene(gameObject, scene);
    }
}
```

::: danger 
Generally speaking using public fields in c# is bad practice. We do it here and at other places because it is very convenient. The Unity inspector does not serialize properties so we would
have to implement properties with backingfields with the [SerializeField] attribute. 
:::

At the moment this is just a class to hold ClientConnections. In addition we create a new scene in the Inititialize() function and move the gameobject to which the Room is attached to into that new scene. Finally we cache the physicsScene for later use.

We will also need a Manager to manage all rooms. So create a "RoomManager" script in the scripts folder.
Our RoomManager will look like this:
```csharp
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    Dictionary<string, Room> rooms = new Dictionary<string, Room>();

    public static RoomManager Instance;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject roomPrefab;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);
        CreateRoom("Main",25);
        CreateRoom("Main 2", 15);
    }

    public void CreateRoom(string roomName, byte maxSlots)
    {
        GameObject go = Instantiate(roomPrefab);
        Room room = go.GetComponent<Room>();
        room.Initialize(roomName, maxSlots);
        rooms.Add(roomName, room);
    }

    public void RemoveRoom(string roomName)
    {
        rooms.Remove(roomName); 
    }
}
```
In this script we have a reference to a room prefab which we instantiate to spawn a new room and a dictionary to find rooms by name.

The CreateRoom() function creates a room. It instantiates a copy of the prefab. It initializes the room component that we created earlier with a name and a maxSlot value and adds the room to the dictionary.

The RemoveRoom() function removes a room from the list but does not delete it yet we will do that later.

Now create a RoomManager gameobject in the Main scene of the server and ad the RoomManager component to it.

As a next step we create the Room Prefab:
- Create a new GameObject call it "Room".
- Add a Room component to the GameObject.
- Add a plane as a child to the room.
- scale the plane up to (5,5,5).
- Drag the Room into the prefabs folder.
- Delete the Room from the Scene.
- Drag the Room in the RoomPrefab field of the RoomManager.

Now the basics for our room system are done. As our next step we will implement a way for a client to display all available rooms on the
server. We start at definieng a way to serialize the data of a room.
Open the Networking Data script **again on the client because it's in the shared folder**

Create a RoomData object:
```csharp
public struct RoomData : IDarkRiftSerializable
{
    public string Name;
    public byte Slots;
    public byte MaxSlots;

    public RoomData(string name, byte slots, byte maxSlots)
    {
        Name = name;
        Slots = slots;
        MaxSlots = maxSlots;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Name = e.Reader.ReadString();
        Slots = e.Reader.ReadByte();
        MaxSlots = e.Reader.ReadByte();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Name);
        e.Writer.Write(Slots);
        e.Writer.Write(MaxSlots);
    }
}
```

Players will receive one of these for each room. It contains its name and the number of slots available.

Now change the lobby info to the include RoomDatas:

```csharp
public struct LobbyInfoData : IDarkRiftSerializable
{
    public RoomData[] Rooms;

    public LobbyInfoData(RoomData[] rooms)
    {
        Rooms = rooms;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Rooms = e.Reader.ReadSerializables<RoomData>();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Rooms);
    }
}
```

Now we just need a way to fetch the Roomdata[] array. We will do this in the RoomManager class.
Add the following function to the RoomManager:

```csharp
    public RoomData[] GetRoomDataList()
    {
        RoomData[] data = new RoomData[rooms.Count];
        int i = 0;
        foreach (KeyValuePair<string, Room> kvp in rooms)
        {
            Room r = kvp.Value;
            data[i] = new RoomData(r.Name, (byte) r.ClientConnections.Count, r.MaxSlots);
            i++;
        }
        return data;
    }
```

::: tip 
This list could be cached and updated whenver a room get's added/removed from the room manager to improve performance.
:::

Finally we have to edit a line in ClientConnection because we changed the constructor of LobbyInfoData.
from:
```csharp
    using (Message m = Message.Create((ushort)Tags.LoginRequestAccepted, new LoginInfoData(client.ID, new LobbyInfoData())))
        {
            client.SendMessage(m, SendMode.Reliable);
        }
```
to:
```csharp
    using (Message m = Message.Create((ushort)Tags.LoginRequestAccepted, new LoginInfoData(client.ID, new LobbyInfoData(RoomManager.Instance.GetRoomDataList()))))
        {
            client.SendMessage(m, SendMode.Reliable);
        }
```

Your scripts should look like this:
- [Room](https://github.com/LukeStampfli/EmbeddedFPSExample/tree/master/gists/room1-Room.cs)
- [RoomManager](https://github.com/LukeStampfli/EmbeddedFPSExample/tree/master/gists/room1-RoomManager.cs)
- [NetworkingData](https://github.com/LukeStampfli/EmbeddedFPSExample/tree/master/gists/room1-NetworkingData.cs)
- [ClientConnection](https://github.com/LukeStampfli/EmbeddedFPSExample/tree/master/gists/room1-ClientConnection.cs)

To finish the room system we still have to add function to join rooms but we do that later first we will implement the client side system to display the rooms.
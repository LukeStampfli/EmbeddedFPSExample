# Multiplayer Gameplay (Synchronization and Buffers)

Finally we can start to develop the actual multiplayer part of our game.
Before we start spawning players on our server we first have to lay out a simple system on how the player and the server will be exchanging information. We will use this basic scheme:

- Once the client loaded the Game scene it will send a GameJoinRequest to the server(An empty message)
- The Server will respond with information about the current room (All spawned players and their states)
- From that point of the server will send information every frame to the client. The following information types exist:
    - PlayerSpawnData (Whenever a new player joined we send that information to all connected clients)
    - PlayerStateData (We have already implemented this)
    - PlayerDespawnData (Opposite of PlayerSpawnData, tells the client to despawn a player)
- The client also sends inputs every frame to the server (We have already done that)


So let's add these to our NetworkingData script:
```csharp
public struct PlayerSpawnData : IDarkRiftSerializable
{
    public ushort Id;
    public string Name;
    public Vector3 Position;

    public PlayerSpawnData(ushort id, string name, Vector3 position)
    {
        Id = id;
        Name = name;
        Position = position;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Id = e.Reader.ReadUInt16();
        Name = e.Reader.ReadString();
        Position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Id);
        e.Writer.Write(Name);

        e.Writer.Write(Position.x);
        e.Writer.Write(Position.y);
        e.Writer.Write(Position.z);
    }
}

public struct PlayerDespawnData : IDarkRiftSerializable
{
    public ushort Id;

    public PlayerDespawnData(ushort id)
    {
        Id = id;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Id = e.Reader.ReadUInt16();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Id);
    }
}
```

```csharp
public struct GameUpdateData : IDarkRiftSerializable
{
    public uint Frame;
    public PlayerSpawnData[] SpawnData;
    public PlayerDespawnData[] DespawnData;
    public PlayerStateData[] UpdateData;

    public GameUpdateData(uint frame, PlayerStateData[] updateData, PlayerSpawnData[] spawnData, PlayerDespawnData[] despawnData)
    {
        Frame = frame;
        UpdateData = updateData;
        DespawnDataData = despawnData;
        SpawnDataData = spawnData;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Frame = e.Reader.ReadUInt32();
        SpawnData = e.Reader.ReadSerializables<PlayerSpawnData>();
        DespawnData = e.Reader.ReadSerializables<PlayerDespawnData>();
        UpdateData = e.Reader.ReadSerializables<PlayerStateData>();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Frame);
        e.Writer.Write(SpawnData);
        e.Writer.Write(DespawnData);
        e.Writer.Write(UpdateData);
    }
}
```
And also add a struct containing the game start information:
```csharp
public struct GameStartData : IDarkRiftSerializable
{
    public uint OnJoinServerTick;
    public PlayerSpawnData[] Players;

    public GameStartData(PlayerSpawnData[] players, uint serverTick)
    {
        Players = players;
        OnJoinServerTick = serverTick;
    }

    public void Deserialize(DeserializeEvent e)
    {
        OnJoinServerTick = e.Reader.ReadUInt32();
        Players = e.Reader.ReadSerializables<PlayerSpawnData>();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(OnJoinServerTick);
        e.Writer.Write(Players);
    }
}
```
Also add some Tags so that we can send messages containing those types of data:
```csharp
    GameJoinRequest = 200,
    GameStartDataResponse = 201,
    GameUpdate = 202,
```
Now let's create a script that talks to server once the Game scene is loaded.
- Create a GameManager script in the Scripts folder
- Create an empty gameobject name it "GameManager".
- Add the GameManager script to that gameobject.
- Drag the Player gameobject into the Prefabs folder, then delete it in the Scene

Open The GameManager script and add some variables:
```csharp
using System.Collections.Generic;
using DarkRift;
using DarkRift.Client;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private Dictionary<ushort, ClientPlayer> players = new Dictionary<ushort, ClientPlayer>();

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
    }
```
ClientTick is the frame number that the client is actually simulating with FixedUpdate, which is also it's input tick, it will be important later. LastRecievedServerTick is the tick of the last message received from the server. The Dictionary will be used to keep track of players.

Next we want to send a message to the server in Start() and we want to receive messages and process them:
```csharp
    void OnDestroy()
    {
        Instance = null;
        GlobalManager.Instance.Client.MessageReceived -= OnMessage;
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
    }

    void SpawnPlayer(PlayerSpawnData playerSpawnData)
    {
    }
```

Next **delete the Start() function from the clientPlayer** and add:

```csharp
public void Initialize(ushort id, string playerName)
{
    this.id = id;
    this.playerName = playerName;
    NameText.text = this.playerName;
    SetHealth(100);
    if (ConnectionManager.Instance.PlayerId == id)
    {
        isOwn = true;
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0,0,0);
        Camera.main.transform.localRotation = Quaternion.identity;
        interpolation.CurrentData = new PlayerStateData(this.id,0, Vector3.zero, Quaternion.identity);
    }
}
```
We will initialize spawned players with this function. Our own player will get a camera Attached and we mark him as our own player.

Also add code to update the players based on data from the server:
```csharp
public void OnServerDataUpdate(PlayerStateData playerStateData)
{
    if (isOwn)
    {
        
    }
    else
    {
        Interpolation.SetFramePosition(playerStateData);
    }
}
```
(We use the part in isOwn later for reconciliation)

In the Fixedupdate function put everything in a if(IsOwn){} bracket it should look like this:
```csharp
void FixedUpdate()
{
    if (isOwn)
    {
        bool[] inputs = new bool[6];
        inputs[0] = Input.GetKey(KeyCode.W);
        inputs[1] = Input.GetKey(KeyCode.A);
        inputs[2] = Input.GetKey(KeyCode.S);
        inputs[3] = Input.GetKey(KeyCode.D);
        inputs[4] = Input.GetKey(KeyCode.Space);
        inputs[5] = Input.GetMouseButton(0);

        yaw += Input.GetAxis("Mouse X") * sensitivityX;
        pitch += Input.GetAxis("Mouse Y") * sensitivityY;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        PlayerInputData inputData = new PlayerInputData(inputs, rotation, GameManager.Instance.LastReceivedServerTick - 1);

        transform.position = interpolation.CurrentData.Position;
        PlayerStateData nextStateData = playerLogic.GetNextFrameData(inputData, interpolation.CurrentData);
        interpolation.SetFramePosition(nextStateData);

        using (Message message = Message.Create((ushort) Tags.GamePlayerInput, inputData))
        {
            ConnectionManager.Instance.Client.SendMessage(message, SendMode.Reliable);
        }

        reconciliationHistory.Enqueue(new ReconciliationInfo(GameManager.Instance.ClientTick, nextStateData, inputData));
    }
}
```

Now we can fill in the SpawnPlayer function in the GameManager:
```csharp
void SpawnPlayer(PlayerSpawnData playerSpawnData)
{
    GameObject go = Instantiate(PlayerPrefab);
    ClientPlayer player = go.GetComponent<ClientPlayer>();
    player.Initialize(playerSpawnData.Id, playerSpawnData.Name);
    players.Add(playerSpawnData.Id, player);
}
```

Now there is still the OnGameUpdate function left. this function is called whenever we receive a GameUpdateData from the server. We could just execute all events in that data like spawning players or moving them as soon as we receive a message but if we do that we run into a few problems which we havo to solve first...
### Buffers for Game Networking

Let's assume we have a server and a client both running at 50 frames per second so they run a Tick every 20 ms. The server will send a message every Tick and the client will receive a message and perform some actions. On a time line this would look like this:

(messages have a constant delay of 15 ms)
- 0 ms: Server sends message 0 to client, client received no messages so waits
- 15 ms: Client receives message 0 and will perform actions in the next frame
- 20 ms: Server sends 1, client performs 0
- 35 ms: client receives 1
- 40 ms server sends 2, client performs 1
......

works fine so far but in reality sending messages will not have a constant delay so let's look at another example.

(messages have a delay of 15-30 ms)
- 0 ms: server sends 0(with delay 17 ms), client performs nothing
- 17 ms: client receives 0
- 20 ms: server sends 1(with delay 28 ms), client performs 0
- 40 ms: server sends 2(with delay 16 ms), client performs nothing(has received nothing)
- 48 ms: client receives 1
- 56 ms: client receives 2
- 60 ms: server sends 3(with delay 25 ms), client performs ??? maybe just 1 and store 2 or both or just 2 and discard 1 or  what.....

This leads to jitter (Players bouncing forward and backwards or jumping small distances) or even worse it could lead to spells being not casted if you discard messages. So how can we handle that problem. How it's done differs from game to game, we will use a very basic approach with the idea of fulfilling the following requirements:
- When we send a message from the server we will receive it no matter what
- Messages arrive always in a sorted order
- No jittering -> We (almost)always want to perform 1 message per frame
- No discarding -> We want to perform every message

::: warning
One could argue that these are bad requirements for an FPS. For instance we could do movement completely fine with unordered and unreliable(not all messages arrive) messages, because leaving out a movement update would barely make a difference and interpolation would smooth it out. I will add a section "Networking Discussion" at the end of this tutorial where I go further into details like this.
:::

Luckily the TCP protcol delivers reliable and sorted messages(on the cost of performance and additional delay in the case of bad connections) so we only have to solve the jittering and discarding problem.

Since this is a problem that each game has to face there exists a solution to it already. It's called a buffer or dejitter buffer. A buffer stores the elements it receives for a short time in a list and allows us to pull an element out each frame. Create a new Buffer script in Scripts/shared and fill it with the following code:
```csharp
using System.Collections.Generic;
using System.Linq;

public class Buffer<T>
{
    private Queue<T> elements;
    private int bufferSize;
    private int counter;
    private int correctionTollerance;

    public Buffer(int bufferSize, int correctionTollerance)
    {
        this.bufferSize = bufferSize;
        this.correctionTollerance = correctionTollerance;
        elements = new Queue<T>();
    }

    public int Count => elements.Count;

    public void Add(T element)
    {
        elements.Enqueue(element);
    }

    public T[] Get()
    {
        int size = elements.Count - 1;

        if (size == bufferSize)
        {
            counter = 0;
        }

        if (size > bufferSize)
        {
            if (counter < 0)
            {
                counter = 0;
            }
            counter++;
            if (counter > correctionTollerance)
            {
                int amount = elements.Count - bufferSize;
                T[] temp = new T[amount];
                for (int i = 0; i < amount; i++)
                {
                    temp[i] = elements.Dequeue();
                }

                return temp;
            }
        }

        if (size < bufferSize)
        {
            if (counter > 0)
            {
                counter = 0;
            }
            counter--;
            if (-counter > correctionTollerance)
            {
                return new T[0];
            }
        }

        if (elements.Any())
        {
            return new T[] { elements.Dequeue() };
        }
        return new T[0];
    }
}
```

::: warning
the Get() generates garbage which you should avoid as much as possible in generic classes like this. You could reuse an ArraySegment which you pass into this function to get rid of that allocation.
:::

So how does it work. First the constructor takes 2 parameters. bufferSize is the ideal size that the buffer should have, the buffer will try to always keep that many elements in it. If we increase this value we add delay to the execution of messages but reduce the chance of getting jitter. By how much exactly? increasing bufferSize by one will add a delay of FixedDeltaTime (in our case 25 ms because we have 40 FixedUpdates per second) but will also allow the ping of the player to bounce between twice that amount.

For Instance if we take a buffer with size, 3 a player would have 75 ms more delay but he could still play fine without jitter even if his ping spikes between 30 ms and 130 ms (130-30 < 75*20). Now we have to decide on a bufferSize for our game. We will go for 1 (add a delay of 25 ms but allow the ping to vary up to 50 ms.)

Let's add a buffer to our GameManager:
```
    private Buffer<GameUpdateData> gameUpdateDataBuffer = new Buffer<GameUpdateData>(1, 1);
```

and change the OnGameUpdate function to:
```csharp
void OnGameUpdate(GameUpdateData gameUpdateData)
{
    gameUpdateDataBuffer.Add(gameUpdateData);
}
```

Now in FixedUpdate we pull objects from the buffer and process them:
```csharp
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
}
```

The client is now ready for multiplayer gameplay.
So let's swap to the server project:
- Create a new GameObject and name it "Player"
- Add a CharacterController to it and set the height to 0
- Add the PlayerLogic script as a component to it
- In the PlayerLogic component set WalkSpeed = 8, GravityConstant = 2, JumpStrength = 11 (same values as on the client)
- Create a ServerPlayer script in the Scripts folder.
- Drag the Player GameObject into the prefabs folder and delete it from the scene.

First of all the Room script should store and interact with ServerPlayers, to do that we have to store all players which are currently inside the room. Open the Room script and add the following to the Public Fields variables:
```csharp
public uint ServerTick;
```

We will also need a reference to the player prefab to spawn it:
```csharp
[Header("Prefabs")]
[SerializeField]
private GameObject playerPrefab;
```

Finally add Lists to store the room state:
```csharp
private List<ServerPlayer> serverPlayers = new List<ServerPlayer>();

private List<PlayerStateData> playerStateData = new List<PlayerStateData>(4);
private List<PlayerSpawnData> playerSpawnData = new List<PlayerSpawnData>(4);
private List<PlayerDespawnData> playerDespawnData = new List<PlayerDespawnData>(4);
```

Add a function to get the spawn data for all players currently in the room. New players will need this data so that they can spawn in all other existing players.
```csharp
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
```

Now let's create a function to react on a GameJoinRequest from a client:
```csharp
public void JoinPlayerToGame(ClientConnection clientConnection)
{
    GameObject go = Instantiate(PlayerPrefab, transform);
    ServerPlayer player = go.GetComponent<ServerPlayer>();
    serverPlayers.Add(player);
    playerStateData.Add(default);
    player.Initialize(Vector3.zero, clientConnection);

    spawnDatas.Add(player.GetPlayerSpawnData());
}
```

This will create a player and initialize it. Also note that we instatiate it as a child of the room which will put it automatically into the scene of this room which means it will be in the physics system which belongs to this room. We still have to write that initialize function but first we will also add a ServerPlayer field to our ClientConnection. Open the ClientConnection script and add to the Public Fields:
```csharp
    public ServerPlayer Player { get; set; }
```


Then open the ServerPlayer script and add the following:
```csharp
using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

[RequireComponent(typeof(PlayerLogic))]
public class ServerPlayer : MonoBehaviour
{
    private ClientConnection clientConnection;
    private Room room;

    private PlayerStateData currentPlayerStateData;

    private Buffer<PlayerInputData> inputBuffer = new Buffer<PlayerInputData>(1, 2);

    public PlayerLogic PlayerLogic { get; private set; }
    public uint InputTick { get; private set; }
    public IClient Client { get; private set; }
    public PlayerStateData CurrentPlayerStateData => currentPlayerStateData;

    void Awake()
    {
        PlayerLogic = GetComponent<PlayerLogic>();
    }

    public void Initialize(Vector3 position, ClientConnection clientConnection)
    {
        this.clientConnection = clientConnection;
        room = clientConnection.Room;
        Client = clientConnection.Client;
        this.clientConnection.Player = this;
        
        currentPlayerStateData = new PlayerStateData(Client.ID,0, position, Quaternion.identity);
        InputTick = room.ServerTick;

        var playerSpawnData = room.GetSpawnDataForAllPlayers();
        using (Message m = Message.Create((ushort)Tags.GameStartDataResponse, new GameStartData(playerSpawnData, room.ServerTick)))
        {
            Client.SendMessage(m, SendMode.Reliable);
        }
```
The Initialize function we created, firstly assigns a lot of references for later use and then generates a GameStartData out of the information from our Room and sends it back as GameStartDataResponse. After that point the client will be an official player on the server and can move and be seen by other players.

So let's add a bit of logic to the ServerPlayer:
```csharp
    public void RecieveInput(PlayerInputData input)
    {
        inputBuffer.Add(input);
    }

    public PlayerStateData PlayerUpdate()
    {
        PlayerInputData[] inputs = inputBuffer.Get();
        if (inputs.Length > 0)
        {
            PlayerInputData input = inputs.First();
            InputTick++;

            for (int i = 1; i < inputs.Length; i++)
            {
                InputTick++;
                for (int j = 0; j < input.Keyinputs.Length; j++)
                {
                    input.Keyinputs[j] = input.Keyinputs[j] || inputs[i].Keyinputs[j];
                }
                input.LookDirection = inputs[i].LookDirection;
            }

            currentPlayerStateData = PlayerLogic.GetNextFrameData(input, currentPlayerStateData);
        }

        transform.localPosition = currentPlayerStateData.Position;
        transform.localRotation = currentPlayerStateData.LookDirection;
        return currentPlayerStateData;
    }

    public PlayerSpawnData GetPlayerSpawnData()
    {
        return new PlayerSpawnData(Client.ID, ClientConnection.Name, transform.localPosition);
    }
```

ReceiveInput will just add inputs from the client to the input buffer of the server player(We need a buffer on both sides we don't want the server to miss inputs too). GetPlayerSpawnData will be used to generate information for new players.

PlayerUpdate Will read and input from the buffer and perform it and store a PlayerStateData in the room which will be used to generate GameUpdateDatas later.

No we can write the main game routine in the Room script:
```csharp
    void FixedUpdate()
    {
        ServerTick++;

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
            using (Message m = Message.Create((ushort)Tags.GameUpdate, new GameUpdateData(p.InputTick, playerStateDataArray, playerSpawnDataArray, playerDespawnDataArray)))
            {
                p.Client.SendMessage(m, SendMode.Reliable);
            }
        }
        
        playerSpawnData.Clear();
        playerDespawnData.Clear();
    }
```

It is very simple, it just performs an update on each player and then sends each player the new room state and resets all data in the room at the end.

The last thing that is left to do is actually calling functions when we receive anything from the client, so open the ClientConnection script and add to the Switch these 2 cases:
```csharp
case Tags.GameJoinRequest:
    Room.JoinPlayerToGame(this);
    break;
case Tags.GamePlayerInput:
    Player.RecieveInput(message.Deserialize<PlayerInputData>());
    break;
```

Finally assign all the references of the player prefab in the editor. Now our multiplayer game is playable, you can test it out by building and starting 2 clients and running around. You will see that the players get synchronized to each other and that they can collide with each other.

There is a small thing that we have to fix. The RemovePlayerFromRoom function in our room doesn't remove players from the game yet when we reconnect. So open the Room script and replace the function with this:
```csharp
public void RemovePlayerFromRoom(ClientConnection clientConnection)
{
    Destroy(clientConnection.Player.gameObject);
    playerDespawnData.Add(new PlayerDespawnData(clientConnection.Client.ID));
    ClientConnections.Remove(clientConnection);
    serverPlayers.Remove(clientConnection.Player);
    clientConnection.Room = null;
}
```

This is almost everything that is needed to create an fps style multiplayer game. There are still some things left to do though. Sometimes we have to correct the local player if he predicted something wrong, that process is called reconciliation and we will implement it in the next section.

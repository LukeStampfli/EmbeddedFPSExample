# Health, Shooting, Lag Compensation

Before we start to implement health and shooting we will add a simple health bar to our player to display it.
Open the ClientPlayer script and add the following fields:
```csharp
    [Header("HealthBar")]
    [SerializeField]
    private Text nameText;
    [SerializeField]
    private Image healthBarFill;
    [SerializeField]
    private GameObject healthBarObject;
```
and add the follwing field to track the player's health:
```csharp
    private int health;
```
Then add these functions:
```csharp
    public void SetHealth(int value)
    {
        health = value;
        healthBarFill.fillAmount = value / 100f;
    }

    void LateUpdate()
    {
        Vector3 point = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 1, 0));
        if (point.z > 2)
        {
            healthBarObject.transform.position = point;
        }
        else
        {
            healthBarObject.transform.position = new Vector3(10000,0,0);
        }
    }
```
The LateUpdate moves the HealthBar on top of the player or hides it if the player is behind the camera.

Next, in the Initialize function after Name = name; add:
```csharp
 nameText.text = Name;
 SetHealth(100);
```

Open the Game scene and drag the player prefab into it.
- Right click on the player(in the Hierarchy) and choose UI - canvas. This creates a new gameobject with a UI canvas attached as a child of the player.
- Set the canvas scaler to scale with screen size and set the reference resolution to 1920x1080 and name the gameobject "HealthBar"
- Right click the HealthBar and choose Create Empty. This creates an empty gameobject. Rename it to "Root".
- Set the Rect Transform Pos Y property of the Root to 50.
- Right click the Root and choose UI - image to create a new child with an image attached. Name it "Border". Set the Source Image of the Image component to UISprite, Image Type to simple and in the Rect Transform set the height to 20.
- Right click the Border and choose UI - image again. name it "Fill". set it's source image to UISprite and set the Image Type to filled and Fill Method to Horizontal and Fill Origin to Left. Finally also set it's height to 20 in the Rect Transform.
- Right click the Root and choose UI/Text to add a child object with a text attached to it. Name it "TextObject".
- In the Rect Transform of the TextObject set Pos Y to 22 and in the Text component set the Font Size to 18 and Font Style to bold.
- Go to the Player and set the NameText variable to the TextObject, Health Bar Fill to the Fill and HealthBarObject to the Root.

Now we have a working health bar that hovers over our player and displays the current health state. It should look like this now:\
![](https://i.imgur.com/zdpVfez.png)
Make sure to apply the changes to the Player prefab and then delete it from the scene.


Before we start with the actual shooting let's create something to have some visual feedback when shooting. For simplicity we will just use a colored ray.

- Create a new GameObject in the Game scene name it "Shot"
- Add a line renderer to the object
- Set the material to default-line
- And the positions to 2 positions the first being (0, 0, 0) and the second (0, 0, 100)
- Set the color to red and the alpha to something like 120.
- Set the width to 0.2\
![](https://i.imgur.com/YQsYGC9.png)

Drag it in the prefab folders then delete it from the Scene.

Open the ClientPlayer and add:
```csharp
[Header("Prefabs")]
[SerializeField]
private GameObject shotPrefab;
```

and in FixedUpdate before:
```csharp
yaw += Input.GetAxis("Mouse X") * sensitivityX;
```
add:
```csharp
if (inputs[5])
{
    GameObject go = Instantiate(shotPrefab);
    go.transform.position = interpolation.CurrentData.Position;
    go.transform.rotation = transform.rotation;
    Destroy(go, 1f);
}
```
::: warning
Please use pooling for something like this instead of this lazy implementation.
:::

Finally we will need a way to change player health over the network so open Networking Data and add:
```csharp
public struct PlayerHealthUpdateData : IDarkRiftSerializable
{
    public ushort PlayerId;
    public byte Value;

    public PlayerHealthUpdateData(ushort id, byte val)
    {
        PlayerId = id;
        Value = val;
    }

    public void Deserialize(DeserializeEvent e)
    {
        PlayerId = e.Reader.ReadUInt16();
        Value = e.Reader.ReadByte();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(PlayerId);
        e.Writer.Write(Value);
    }
}
```

And change the GameUpdateData a bit. Add the following below the other arrays:
```csharp
    public PlayerHealthUpdateData[] HealthData;
```

Also pass a HealthUpdateDataArray into the constructor. It should look like this now:
```csharp
public GameUpdateData(uint frame, PlayerUpdateData[] updateData, PlayerSpawnData[] spawns, PlayerDespawnData[] despawns, PlayerHealthUpdateData[] healthData)
    {
        Frame = frame;
        UpdateData = updateData;
        DespawnData = despawns;
        SpawnData = spawns;
        HealthData = healthData;
    }
```
In the serialize function at the end, add this:
```csharp
e.Writer.Write(HealthData);
```
Same for the deserialize method
```csharp
HealthData = e.Reader.ReadSerializables<PlayerHealthUpdateData>();
```

Finally open the GameManager and in the UpdateClientGameState function add the following below the last foreach loop:
```csharp
foreach (PlayerHealthUpdateData data in gameUpdateData.HealthData)
{
    ClientPlayer p;
    if (players.TryGetValue(data.PlayerId, out p))
    {
        p.SetHealth(data.Value);
    }
}
```

Switch to the server project and open the Room script and add to the other lists which store the room state:
```csharp
private List<PlayerHealthUpdateData> healthUpdateData = new List<PlayerHealthUpdateData>(4);
```
And in the FixedUpdate change the lines that send the message to:
```csharp
// Send update message to all players.
PlayerStateData[] playerStateDataArray = playerStateData.ToArray();
PlayerSpawnData[] playerSpawnDataArray = playerSpawnData.ToArray();
PlayerDespawnData[] playerDespawnDataArray = playerDespawnData.ToArray();
PlayerHealthUpdateData[] healthUpdateDataArray = healthUpdateData.ToArray();
foreach (ServerPlayer p in serverPlayers)
{
    using (Message m = Message.Create((ushort)Tags.GameUpdate, new GameUpdateData(p.InputTick, playerStateDataArray, playerSpawnDataArray, playerDespawnDataArray, healthUpdateDataArray)))
    {
        p.Client.SendMessage(m, SendMode.Reliable);
    }
}
```
and at the end of FixedUpdate call:
```csharp
healthUpdateData.Clear();
```

Let's also add a new function to update the health of a player:
```csharp
public void UpdatePlayerHealth(ServerPlayer player, byte health)
{
    healthUpdateData.Add(new PlayerHealthUpdateData(player.Client.ID, health));
}
```

Now open the ServerPlayer and add to the Public Fields:
```csharp
private int health;
```
and initialize add after InputTick = room.ServerTick;
```csharp
Health = 100;
```
Also add a function to let a player take damage and respawn him if he dies:
```csharp
public void TakeDamage(int value)
{
    health -= value;
    if (health <= 0)
    {
        health = 100;
        currentPlayerStateData.Position = new Vector3(0,1,0) + transform.parent.transform.localPosition;
        currentPlayerStateData.Gravity = 0;
        transform.localPosition = currentPlayerStateData.Position;
    }
    room.UpdatePlayerHealth(this, (byte)health);
}
```

Now lets do lag compensation :grinning:

As a reminder. Lag compensation is a technique which is mostly used in FPS games. Because shooting has to be very precise we need to
compensate for the predicted position of the client player compared to the world state.

To do lag compensation we will need a historyBuffer on the server too so open the ServerPlayer and add:
```csharp
public List<PlayerStateData> PlayerStateDataHistory { get; } = new List<PlayerStateData>();
```

and at the end of the PlayerUpdate function add:
```csharp
PlayerStateDataHistory.Add(currentPlayerStateData);
if (PlayerStateDataHistory.Count > 10)
{
    PlayerStateDataHistory.RemoveAt(0);
}
```

We want the shots to be performed in a separate loop before the main game update because then all players are in the same frame. But this means we have to store inputs somehow so in the ServerPlayer add:
```csharp
private PlayerInputData[] inputs;
```
And remove the following line from PlayerUpdate:
```csharp
PlayerInputData[] inputs = inputBuffer.Get();
```
Now add a new function for inputs and shooting:
```csharp
public void PlayerPreUpdate()
{
    inputs = inputBuffer.Get();
    for (int i = 0; i < inputs.Length; i++)
    {
        if (inputs[i].Keyinputs[5])
        {
            room.PerformShootRayCast(inputs[i].Time, this);
            return;
        }
    }
}
```

No lets implement the actual shooting logic as a PerformShootRayCast function in the Room:
```csharp
public void PerformShootRayCast(uint frame, ServerPlayer shooter)
{
    int dif = (int) (ServerTick - 1 - frame);

    // Get the position of the ray
    Vector3 startPosition;
    Vector3 direction;

    if (shooter.PlayerStateDataHistory.Count > dif)
    {
        startPosition = shooter.PlayerStateDataHistory[dif].Position;
        direction = shooter.PlayerStateDataHistory[dif].LookDirection * Vector3.forward;
    }
    else
    {
        startPosition = shooter.CurrentPlayerStateData.Position;
        direction = shooter.CurrentPlayerStateData.LookDirection * Vector3.forward;
    }

    startPosition += direction * 3f;

    //set all players back in time
    foreach (ServerPlayer player in serverPlayers)
    {
        if (player.PlayerStateDataHistory.Count > dif)
        {
            player.PlayerLogic.CharacterController.enabled = false;
            player.transform.localPosition = player.PlayerStateDataHistory[dif].Position;
        }
    }

    RaycastHit hit;
    if (physicsScene.Raycast(startPosition, direction,out hit, 200f))
    {
        if (hit.transform.CompareTag("Unit"))
        {
            hit.transform.GetComponent<ServerPlayer>().TakeDamage(5);
        }
    }

    // Set all players back.
    foreach (ServerPlayer player in serverPlayers)
    {
        player.transform.localPosition = player.CurrentPlayerStateData.Position;
        player.PlayerLogic.CharacterController.enabled = true;
    }
}
```
This looks complicated at first glance but it isn't. First we calculate for how many frames we have to lag compensate( the -1 because we ticked up already in this tick). Then we set all players back to that point of time by using the history buffers, next we create a ray and check if he hit a "Unit" and if we do we deal damage to that player. Finally we set all players back. Note that we use physicsScene.Raycast(). We do so because our room has its own physicsScene and using just Physics.Raycast() would cast that ray on the default scene.

In the Room class we have to change the Fixedupdate so that it performs the pre update forst for every player. So in FixedUpdate replace
```csharp
ServerTick++;

for (var i = 0; i < serverPlayers.Count; i++)
{
    ServerPlayer player = serverPlayers[i];
    playerStateData[i] = player.PlayerUpdate();
}
```
with
```csharp
ServerTick++;

foreach (ServerPlayer player in serverPlayers)
{
    player.PlayerPreUpdate();
}

for (var i = 0; i < serverPlayers.Count; i++)
{
    ServerPlayer player = serverPlayers[i];
    playerStateData[i] = player.PlayerUpdate();
}
```

The only thing left to do now is to create the "Unit" tag and add it to the player prefab and also to add a capsule collider to the player prefab.

Now we can run around on a map with collisions and shoot other players and even join multiple rooms, this should be the basic functions of an FPS. There is still much room to extend to for instance:
- Let players create rooms
- Ammo
- Abilties and cooldowns
- A death zone if players fall out of the map
- Well... a better map

But I'll leave that to you!

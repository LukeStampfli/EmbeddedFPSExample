# Health, Shooting, Lag Compensation

Before we start to implement health and shooting we will add a simple health bar to our player to display it.
Open the PlayerClient script and add the following variables to the References:
```csharp
    public Text NameText;
    public Image HealthBarFill;
    public GameObject HealthBarObject;
```
and add the follwing to the Public Fields:
```csharp
    public int Health;
```
Then add these functions:
```csharp
    public void SetHealth(int value)
    {
        Health = value;
        HealthBarFill.fillAmount = value / 100f;
    }

    void LateUpdate()
    {
        Vector3 point = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 1, 0));
        if (point.z > 2)
        {
            HealthBarObject.transform.position = point;
        }
        else
        {
            HealthBarObject.transform.position = new Vector3(10000,0,0);
        }
    }
```

And in the Initialize function after Name = name; add:
```csharp
 NameText.text = Name;
 SetHealth(100);
```

Open the Game scene and drag the player prefab into it.
- Right click on the player and add a UI canvas.
- Set the canvas to scale with screen size and set the reference resolution to 1920x1080 and name it "HealthBar"
- Right click the HealthBar and add an empty gameobject
- Set the Position Y property of it to 50
- Right click the gameobject and add an image(as a new gameobject), set source image to UISprite, image type to simple and the height in the transform to 20
- Right click the image and add another image(as a new gameobject), name it "Fill". set it's source image to UISprite and it's height to 20 and set the Image Type to filled and Fill Method to Horizontal and Fill Origin to Left.
- Right click the empty gameobject (child of HealthBar) and add a UI/Text (as a new gameobject).
- Set the y position of the text to 22 and the font size to 18 and bold.
- Go to the Player and set NameText to the text object, Health Bar Fill to the Fill object and HealthBarObject to the empty gameobject.

Now we have a working health bar that hovers over our player and displays the current health state.

Before we start with the actual shooting let's create something to display our shots.

- Create a new GameObject in the Game scene name it "Shot"
- Add a line renderer to the object
- Set the material to default-line
- And the positions to 2 positions the first being (0,0,0) and the second (0,0,100)
- Set the color to red and the alpha to something like 120
- Set the width to 0.2\
![](https://i.imgur.com/YQsYGC9.png)

Drag it in the prefab folders then delete it from the Scene.

Open the ClientPlayer and add:
```csharp
    [Header("Prefabs")]
    public GameObject ShotPrefab;
```

and in FixedUpdate before:
```csharp
    yaw += Input.GetAxis("Mouse X") * SensitivityX;
```
add:
```csharp
            if (inputs[5])
            {
                GameObject go = Instantiate(ShotPrefab);
                go.transform.position = Interpolation.CurrentData.Position;
                go.transform.rotation = transform.rotation;
                Destroy(go,1f);
            }
```

Finally we will need a way to change player health over the network so open Networking Data and add:
```csharp
public struct PLayerHealthUpdateData : IDarkRiftSerializable
{
    public ushort PlayerId;
    public byte Value;

    public PLayerHealthUpdateData(ushort id, byte val)
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
    public PLayerHealthUpdateData[] HealthData;
```

Also pass an HealthUpdateDataArray into the constructor. It should look like this now:
```csharp
public GameUpdateData(uint frame, PlayerUpdateData[] updateData, PlayerSpawnData[] spawns, PlayerDespawnData[] despawns, PLayerHealthUpdateData[] healthDatas)
    {
        Frame = frame;
        UpdateData = updateData;
        DespawnData = despawns;
        SpawnData = spawns;
        HealthData = healthDatas;
    }
```

In the serialize function add at the end:
```csharp
    e.Writer.Write(HealthData);
```
Same for the deserialize method
```csharp
        HealthData = e.Reader.ReadSerializables<PLayerHealthUpdateData>();
```

Finally open the GameManager and in the PerformGameUpdate function add the following below the last foreach loop:
```csharp
   foreach (PLayerHealthUpdateData data in updateData.HealthData)
        {
            ClientPlayer p;
            if (players.TryGetValue(data.PlayerId, out p))
            {
                p.SetHealth(data.Value);
            }
        }
```

Switch to the server project and open the Room script and add:
```csharp
    public List<PLayerHealthUpdateData> HealthUpdates = new List<PLayerHealthUpdateData>();
```
And in the FixedUpdate change the lines that send the message to:
```csharp
using (Message m = Message.Create((ushort)Tags.GameUpdate, new GameUpdateData(p.InputTick, UpdateDatas, tpsd, tpdd, HealthUpdates.ToArray())))
            {
                p.Client.SendMessage(m, SendMode.Reliable);
            }
```
and at the end of FixedUpdate call:
```csharp
        HealthUpdates.Clear();
```

Now open the ServerPlayer and add to the Public Fields:
```csharp
    public int Health;
```
and initialize add after InputTick = Room.ServerTick:
```csharp
        Health = 100;
```

also add a function to let a player take damage and respawn him if he dies:
```csharp
  public void TakeDamage(int value)
    {
        Health -= value;
        if (Health <= 0)
        {
            Health = 100;
            CurrentUpdateData.Position = new Vector3(0,1,0)+ transform.parent.transform.localPosition;
            CurrentUpdateData.Gravity = 0;
            transform.localPosition = CurrentUpdateData.Position;
        }
        Room.HealthUpdates.Add(new PLayerHealthUpdateData(Client.ID, (byte) Health));
    }
```

Now lets do lag compensation :grinning:

To do lag compensation we will need a historyBuffer on the server too so open the ServerPlayer and add:
```csharp
    public List<PlayerUpdateData> UpdateDataHistory = new List<PlayerUpdateData>();
```

and at the end of the PerformUpdate function add:
```csharp
        UpdateDataHistory.Add(CurrentUpdateData);
        if (UpdateDataHistory.Count > 10)
        {
            UpdateDataHistory.RemoveAt(0);
        }
```

We want the shots to be performed in a separate loop before the main game update because then all players are in the same frame. But this means we have to store inputs somehow so in the ServerPlayer add:
```csharp
    private PlayerInputData[] inputs;
```
And remove the following line from PerformUpdate:
```csharp
        PlayerInputData[] inputs = inputBuffer.Get();
```
Now add a new function for inputs and shooting:
```csharp
  public void PerformuPreUpdate()
    {
        PlayerInputData[] inputs = inputBuffer.Get();
        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i].Keyinputs[5])
            {
                Room.PerformShootRayCast(inputs[i].Time, this);
                return;
            }
        }
    }
```

We will go implement the missing PerformShootRayCast in the Room:
```csharp
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
```

This looks complicated at the first glance but it isn't. First we calculate for how many frames we have to lag compensate( the -1 because we ticked up already in this tick). Then we set all players back to that point of time by using the history buffers, next we create a ray and check if he hit a "Unit" and if we do we deal damage to that player. Finally we set all players back.

The only thing left to do now is to create the "Unit" tag and add it to the player prefab and also to add a capsule collider to the player prefab.

Now we can run around on a map with collisions and shoot other players and even join multiple rooms, this should be the basic functions of an FPS. There is still much room to extend to for instance:
- Let players create rooms
- Ammo
- Abilties and cooldowns
- A death zone if players fall out of the map
- Well... a better map

But I'll leave that to you!
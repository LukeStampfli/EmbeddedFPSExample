# Player-Movement, Interpolation and Client Side Prediction

Player movement in multiplayer games is a bit more commplicated then in singleplayer games. To make it feel snappy a lot of games need to move the player immediately
on the client side. On the other hand if our game is competitive we want to make sure that players can't cheat by sending illegal movement to the server.
We achieve this in the following way:
- Movement is done in a fixed time step. That way the distance a player can move is the same on any machine regardless of their framerate.
- Client and server both run the same movemenment code. This gives us accurate prediction
- If Server and client end up with different results we always take the servers result.

Let's start by implementing a function which moves the player based on input. That function should run on the client and the server, so that the client can predict his movement.

Let's first define what our player input will be and create a struct to represent it. We will also make it a IDarkriftSerializable so that we can send it to the server later. For our FPS we will need to monitor the following key inputs: W, A, S, D, Space, LeftClick we will store them in a bool[] (true means key is down false means key is up). But in additon to that we need the look direction of the player because his walk and shoot directions depend on it. (We will store the exact look direction and not just the delta).

So let's create a struct in the Networking Data script:
```csharp
public struct PlayerInputData : IDarkRiftSerializable
{
    public bool[] Keyinputs; // 0 = w, 1 = a, 2 = s, 3 = d, 4 = space, 5 = leftClick
    public Quaternion LookDirection;
    public uint Time;
 
    public PlayerInputData(bool[] keyInputs, Quaternion lookdirection, uint time)
    {
        Keyinputs = keyInputs;
        LookDirection = lookdirection;
        Time = time;
    }
 
    public void Deserialize(DeserializeEvent e)
    {
        Keyinputs = e.Reader.ReadBooleans();
        LookDirection = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
 
        if (Keyinputs[5])
        {
            Time = e.Reader.ReadUInt32();
        }
    }
 
    public void Serialize(SerializeEvent e)
    {
 
        e.Writer.Write(Keyinputs);
        e.Writer.Write(LookDirection.x);
        e.Writer.Write(LookDirection.y);
        e.Writer.Write(LookDirection.z);
        e.Writer.Write(LookDirection.w);
 
        if (Keyinputs[5])
        {
            e.Writer.Write(Time);
        }
    }
}
```

We will use the Time field in the input later for lag compensation so you can igore it for now.

:::warning
There are far better ways to write booleans or quaternions which use less bandwidth, which i'm not going to explain here. You can take a look at a [script of mine](https://github.com/LukeStampfli/DarkriftSerializationExtensions/blob/master/SerializationExtensions.cs) to see examples on how to write bools or quaternions.
:::

We will also need a struct to represent a player state (his position and rotation) we will also use this struct to sync player data in general so it will also contain the id of the player:
```csharp
public struct PlayerStateData : IDarkRiftSerializable
{

    public PlayerStateData(ushort id, float gravity, Vector3 position, Quaternion lookDirection)
    {
        Id = id;
        Position = position;
        LookDirection = lookDirection;
        Gravity = gravity;
    }

    public ushort Id;
    public Vector3 Position;
    public float Gravity;
    public Quaternion LookDirection;

    public void Deserialize(DeserializeEvent e)
    {
        Position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        LookDirection = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        Id = e.Reader.ReadUInt16();
        Gravity = e.Reader.ReadSingle();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Position.x);
        e.Writer.Write(Position.y);
        e.Writer.Write(Position.z);
 
        e.Writer.Write(LookDirection.x);
        e.Writer.Write(LookDirection.y);
        e.Writer.Write(LookDirection.z);
        e.Writer.Write(LookDirection.w);
        e.Writer.Write(Id);
        e.Writer.Write(Gravity);
    }
}
```


Now we are ready to create our player.

- Create a ClientPlayer script in the Scripts folder
- Create a PlayerLogic script in the **Scripts/Shared** folder
- Create a new "Game" scene and open it.
- Create a new plane and scale it up to (5,5,5)
- Create a new empty gameobject name it "Player".
- Add a CharacterController to the player and set its height to 0
- Add The ClientPlayer and the PlayerLogic script to the Player gameobject.
- Create a Sphere as a child of the Player(this will be our visual representation)

Now let's start by writing the PlayerLogic which is a shared script between the client and the server. We will use the player logic script to calculate the next position of the player based on an input.

first of all add some variables
```csharp
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLogic : MonoBehaviour
{
    private Vector3 gravity;

    [Header("Settings")]
    [SerializeField]
    private float walkSpeed;
    [SerializeField]
    private float gravityConstant;
    [SerializeField]
    private float jumpStrength;

    public CharacterController CharacterController { get; private set; }

    void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
    }
}
```
Now we want a function to get the next PlayerStateData depending on a InputData. Something like this:

```csharp
    public PlayerStateData GetNextFrameData(PlayerInputData input, PlayerStateData currentStateData)
    {
    }
```

inside that function let's first get our inputs:
```csharp
        bool w = input.Keyinputs[0];
        bool a = input.Keyinputs[1];
        bool s = input.Keyinputs[2];
        bool d = input.Keyinputs[3];
        bool space = input.Keyinputs[4];
```
Calculating the next rotation is very easy we just take the one the player gives us. (working with rotation delta values is ugly and it doesn't prevent players from cheating so there is no reason to do it)
```csharp
        Vector3 rotation =  input.LookDirection.eulerAngles;
```
we also define a gravity vector:
```csharp
        gravity = new Vector3(0, currentStateData.Gravity, 0);
```
The next step is movement. We want our player to move depending on his y rotation and the WASD inputs. A possible implementation is this:
```csharp
        Vector3 movement = Vector3.zero;

        if (w)
        {
            movement += Vector3.forward;
        }
        if (a)
        {
            movement += Vector3.left;
        }
        if (s)
        {
            movement += Vector3.back;
        }
        if (d)
        {
            movement += Vector3.right;
        }

        movement = Quaternion.Euler(0, rotation.y, 0) * movement; // Move towards the look direction.
        movement.Normalize();
        movement = movement * walkSpeed;

        movement = movement * Time.fixedDeltaTime;
        movement = movement + gravity * Time.fixedDeltaTime;
```
Now we have to move our character controller to detect any collisions. But before that we need a little fix because the character controller isn't very reliable. So just add:
```csharp
        CharacterController.Move(new Vector3(0, -0.001f, 0));
```
Well just Unity things, anyways...

Add logic to handle jumping:
```csharp
        if (CharacterController.isGrounded)
        {
            if (space)
            {
                gravity = new Vector3(0, jumpStrength, 0);
            }
        }
        else
        {
            gravity -= new Vector3(0, gravityConstant, 0);
        }
```

Finally we can move the CharacterController and return the result as a new PlayerStateData:
```csharp
        CharacterController.Move(movement);

        return new PlayerStateData(currentStateData.Id, gravity.y, transform.localPosition, input.LookDirection);
```

Now we can calculate the next position of any player depending on our input. We should test if it works locally in the ClientPlayer script.

So open the ClientPlayer script and add the following:
```csharp
[RequireComponent(typeof(PlayerLogic))]
public class ClientPlayer : MonoBehaviour
{
    private PlayerLogic playerLogic;

    // Store look direction.
    private float yaw;
    private float pitch;

    private ushort id;
    private string playerName;
    private bool isOwn;

    private int health;

    private PlayerStateData playerStateData;

    [Header("Settings")]
    [SerializeField]
    private float sensitivityX;
    [SerializeField]
    private float sensitivityY;

    void Awake()
    {
        playerLogic = GetComponent<PlayerLogic>();
    }
}


```

The id is the id of the player on the server and isOwn is be true for our own player but not for enemies.
The sensitivity, yaw and pitch variables will be used to rotate the camera based on the mouse movements.

First let's attach the camera to our player. (we do that by script because we later want to attach the camera to our own player).
```csharp
void Start(){
    Camera.main.transform.SetParent(transform);
    Camera.main.transform.localPosition = new Vector3(0,0,0);
    Camera.main.transform.localRotation = Quaternion.identity;
    
    playerStateData = new PlayerStateData(Id, 0, Vector3.zero, Quaternion.identity);
}
```
Now we can create a simple logic to read inputs and perform movement in FixedUpdate:
```csharp
    void FixedUpdate()
    {
            bool[]inputs = new bool[6];
            inputs[0] = Input.GetKey(KeyCode.W);
            inputs[1] = Input.GetKey(KeyCode.A);
            inputs[2] = Input.GetKey(KeyCode.S);
            inputs[3] = Input.GetKey(KeyCode.D);
            inputs[4] = Input.GetKey(KeyCode.Space);
            inputs[5] = Input.GetMouseButton(0);

            yaw += Input.GetAxis("Mouse X") * sensitivityX;
            pitch += Input.GetAxis("Mouse Y") * sensitivityY;

            Quaternion rotation = Quaternion.Euler(pitch, yaw,0);

            PlayerInputData inputData = new PlayerInputData(inputs, rot, 0/*here we later synchronize the last recieved tick number from the server*/);

            PlayerStateData nextStateData = Logic.GetNextFrameData(inputData, data);
            transform.rotation = data.LookDirection;
    }
```

This is already enough to move our player. We don't have to set the position after calculating it since the character controller does that for us already.

Go into Unity assign the reference to the PlayerLogic and set walkSpeed = 8, GravityConstant = 2, JumpStrength = 11 and in the ClientPlayer script set SensivityX to 5 and SensivityY to -5. 

 You can press play now and run around with your character. But you may realize that there is something wrong. The movement might feel jittered. The reason for that is that we just sample inputs at our fixed rate (50 times per second as default), this means on certain frames we might get multiple movement updates and on other frames none. We want our movement to be smooth. This is done with interpolation which we will implement next.

Create a new PlayerInterpolation script in the Scripts folder and open it.

Basic interpolation works like this; We store 2 values and use our current time to lerp between the two values. In our case we want to always interpolate on the last two PlayerStateData from our player. (Note that we add a little bit of input delay to our movement by doing this compared to moving instantly in Update. There are other options without a delay (extrapolating) but they come with their own set of challenges. Simple interpolation works very well for most types of multiplayer games. 

Let's start by defining some fields and properties in the PlayerInterpolation script:
```csharp
    private float lastInputTime;

    public PlayerStateData CurrentData { get; set; }
    public PlayerStateData PreviousData { get; private set; }
```

CurrentData will be the last position we moved to and PreviousData the position in the frame before that. We will also keep track of a time value where we store the fixed time of when we updated CurrentData the last time. This value is only relevant if don't get updates for a while in that case we start to predict(extrapolate).

Now we can also add a function to set those values or to just input the next value:
```csharp
    public void SetFramePosition(PlayerStateData data)
    {
        RefreshToPosition(data, CurrentData);
    }

    public void RefreshToPosition(PlayerStateData data, PlayerStateData prevData)
    {
        PreviousData = prevData;
        CurrentData = data;
        lastInputTime = Time.fixedTime;
    }
```
Now we just have to interpolate in Update:
```csharp
    public void Update()
    {
        float timeSinceLastInput = Time.time - lastInputTime;
        float t = timeSinceLastInput / Time.fixedDeltaTime;
        transform.position = Vector3.LerpUnclamped(PreviousData.Position, CurrentData.Position, t);
        transform.rotation = Quaternion.SlerpUnclamped(PreviousData.LookDirection,CurrentData.LookDirection, t);
    }
```

Interpolation is very simple in its raw form. we set the as the time since our last input and divide it by fixedDeltaTime. Then we lerp between the last 2 values. We use LerpUnclamped because we don't want players to stop moving when they don't get an input for a while. LerpUnclamped will extrapolate their position if we don't receive updates for a while.

Add the PlayerInterpolation to the Player and open the ClientPlayer script and add a field to track the PlayerInterpolation and 
```csharp
    public PlayerInterpolation interpolation;
```
and remove the following: (We store our PlayerStateData now in the Interpolation class)
```csharp
    private PlayerStateData playerStateData;
```

In awake get a reference to our Interpolation by adding
```csharp
        interpolation = GetComponent<PlayerInterpolation>();
```

Finally add a require component Attribute to the ClientPlayer class like this:
```csharp
[RequireComponent(typeof(PlayerLogic))]
[RequireComponent(typeof(PlayerInterpolation))]
public class ClientPlayer : MonoBehaviour
{
......
```
Next we have to replace our logic which used the playerStateData with the values from our interpolation script. Start in the Start function by replacing:
```csharp
    playerStateData = new PlayerStateData(Id, 0, Vector3.zero, Quaternion.identity);
```
with
```csharp
    Interpolation.CurrentData = new PlayerStateData(Id, 0, Vector3.zero, Quaternion.identity);
```

Next search for the following lines in FixedUpdate:
```csharp
    PlayerStateData nextStateData = Logic.GetNextFrameData(inputData, playerStateData);
    transform.rotation = playerStateData.LookDirection;
```
and replace them with:
```csharp
    transform.position = Interpolation.CurrentData.Position;
    PlayerStateData nextStateData = playerLogic.GetNextFrameData(inputData, interpolation.CurrentData);
    interpolation.SetFramePosition(nextStateData);
```

The first line we added will set the player back to the last calculated position. We must do that because the PlayerInterpolation also moves the player. And at the end with simply inform the interpolation about the new value, that's all  we have to do. If we run now again we can experience 100% smooth movements.

Finally let's send that information to the server. So after the replaced lines add:
```csharp
    using (Message message = Message.Create((ushort)Tags.GamePlayerInput, inputData))
    {
        ConnectionManager.Instance.Client.SendMessage(message, SendMode.Reliable);
    }
```
and add the Tag to the Tags of the Networking Data script:
```csharp
    GamePlayerInput = 203,
```

Now we are ready for the server side. In the next section we will spawn players on the server and send information about them to clients, which makes our game finally a real multiplayer game.

Your scripts should look now like this:
- [NetworkingData](https://github.com/LukeStampfli/EmbeddedFPSExample/tree/master/gists/movement1-NetworkingData.cs)
- [PlayerLogic](https://github.com/LukeStampfli/EmbeddedFPSExample/tree/master/gists/movement1-PlayerLogic.cs)
- [ClientPlayer](https://github.com/LukeStampfli/EmbeddedFPSExample/tree/master/gists/movement1-ClientPlayer.cs)
- [PlayerInterpolation](https://github.com/LukeStampfli/EmbeddedFPSExample/tree/master/gists/movement1-PlayerInterpolation.cs)
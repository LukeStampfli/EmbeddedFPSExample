# Player-Movement, Interpolation and Client Side Prediction

We will have to create a function to move the player based on an input. That function should run on the client and the server, so that the client can predict his movement.

Lets first define what our player input will be and create a struct to represent it. We will also make it a IDarkriftSerializable so that we can send it to the server later. For our FPS we will need to monitor the following key inputs: W,A,S,D,Space,LeftClick we will store them in a bool[] (true means key is down false means key is up). But we additionally need the look direction of the player because his walk and shoot directions depend on it. (We will store the exact look direction and not just the delta).

So lets create a struct in the Networking Data script:
```csharp
public struct PlayerInputData : IDarkRiftSerializable
{
    public bool[] Keyinputs; //0 = w, 1 = a, 2 = s, 3 = d, 4 = space, 5 = leftClick
    public Quaternion LookDirection;

    public PlayerInputData(bool[] keyInputs, Quaternion lookDirection)
    {
        Keyinputs = keyInputs;
        LookDirection = lookDirection;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Keyinputs = e.Reader.ReadBooleans();
        LookDirection = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Keyinputs);
        e.Writer.Write(LookDirection.x);
        e.Writer.Write(LookDirection.y);
        e.Writer.Write(LookDirection.z);
        e.Writer.Write(LookDirection.w);
    }
}
:::warning
There are far better ways to write booleans or quaternions which i'm not going to explain here. You can take a look at a [script of mine](https://github.com/LestaAllmaron/DarkriftSerializationExtensions/blob/master/DarkriftSerializationExtensions/DarkriftSerializationExtensions/SerializationExtensions.cs) to see examples on how to write bools or quaternions.
:::

We will also need a struct to represent a player state (his position and rotation) we will also use this struct to sync player data in general so it will also contain the id of the player:
```csharp
public struct PlayerUpdateData : IDarkRiftSerializable
{

    public PlayerUpdateData(ushort id, float gravity, Vector3 position, Quaternion lookDirection)
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
        LookDirection = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(),
            e.Reader.ReadSingle());
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
- Create a PlayerLogic script in the **Scripts/shared** folder
- Open the Game Scene
- Create a new plane and scale it up to (5,5,5)
- Create a new empty gameobject name it "Player".
- Add a CharacterController to the player and set it's height to 0
- Add The ClientPlayer and the PlayerLogic script to the Player gameobject.
- Create a Sphere as a child of the Player(this will be our visual representation)

Now lets start by writing the PlayerLogic which is a shared script between the client and the server. We will use the player logic script to calculate the next position of the player based on an input.

first of all add some variables
```csharp
    [Header("References")]
    public CharacterController CharacterController;

    [Header("Shared Variables")]
    public float WalkSpeed;
    public float GravityConstant;
    public float JumpStrength;

    private Vector3 gravity;
```
These should look familiar to you if you did single player character movement a some point.
Now we want a function to get the next PlayerUpdateData depending on a InputData. Something like this:

```csharp
    public PlayerUpdateData GetNextFrameData(PlayerInputData input, PlayerUpdateData currentUpdateData)
    {
    }
```

inside that function let's first get our key inputs:
```csharp
        bool w = input.Keyinputs[0];
        bool a = input.Keyinputs[1];
        bool s = input.Keyinputs[2];
        bool d = input.Keyinputs[3];
        bool space = input.Keyinputs[4];
        bool left = input.Keyinputs[5];
```
Calculating the next rotation is very easy we just take the one the player gives us. (working with rotation deltas is ugly and it doesn't prevent players from cheating so there is no reason to do it)
```csharp
        Vector3 rotation =  input.LookDirection.eulerAngles;
```
we also define a gravity vector:
```csharp
        gravity = new Vector3(0,currentUpdateData.Gravity,0);
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

        movement = Quaternion.Euler(0, rotation.y, 0) * movement;
        movement.Normalize();
        movement = movement * WalkSpeed;

        movement = movement * Time.fixedDeltaTime;
        movement = movement + gravity * Time.fixedDeltaTime;
```
Now we have to move our character controller to detect any collisions. But before that we need a little fix because the character controller isn't very reliable. So just add:
```csharp
        CharacterController.Move(new Vector3(0,-0.001f,0));
        if (CharacterController.isGrounded)
        {
            if (space)
            {
                gravity = new Vector3(0, JumpStrength, 0);
            }
        }
        else
        {
            gravity -= new Vector3(0, GravityConstant, 0);
        }
```
Well just Unity things anyways...

Finally we can move the CharacterController and return the result as a new PlayerUpdateData:
```csharp
        CharacterController.Move(movement);

        return new PlayerUpdateData(currentUpdateData.Id,gravity.y, transform.localPosition, input.LookDirection);
```

Now we can calculate the next position of any player depending on our input. We should test if it works locally in the ClientPlayer script.

So open the ClientPlayer script and add the following:
```csharp
    [Header("Variables")]
    public float SensitivityX;
    public float SensitivityY;

    [Header("References")]
    public PlayerLogic Logic;

    private float yaw;
    private float pitch;
    
    private PlayerUpdateData data
```
The sensitivity, yaw and pitch will be used to rotate the camera based on the mouse movements.
First lets attach the camera to our player. (we do that by script because we later want to attach the camera to our own player).
```csharp
void Start(){
    Camera.main.transform.SetParent(transform);
    Camera.main.transform.localPosition = new Vector3(0,0,0);
    Camera.main.transform.localRotation = Quaternion.identity;
    
    data = new PlayerUpdateData(Id,0, Vector3.zero, Quaternion.identity);
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

            yaw += Input.GetAxis("Mouse X") * SensitivityX;
            pitch += Input.GetAxis("Mouse Y") * SensitivityY;

            Quaternion rot = Quaternion.Euler(pitch, yaw,0);

            PlayerInputData inputData = new PlayerInputData(inputs,rot, 0/*here we later write the last recieved tick*/);

            PlayerUpdateData updateData = Logic.GetNextFrameData(inputData,data);
            transform.rotation = data.LookDirection;
    }
```

This is already enough to move our player, we don't have to set the position after calculating it since we also move the character controller when we calculate the next frame. Go into Unity asign the reference to the PlayerLogic and set WalkSpeed = 8, GravityConstant = 2, JumpStrength = 11 and in the ClientPlayer script set SensivityX to 5 and SensivityY to -5. 

 You can press play now and run around with your character. But you may realize that there is something wrong. The movement might feel jittered. The reason for that is that we just sample inputs at our fixed rate (50 times per second as default), this means on certain frames we might get multiple movement updates and on other frames 0, we wan't our movement to be smooth, so we have to smooth it out. This is done with interpolation which we will implement next.

Create a new PlayerInterpolation script in the Scripts folder and open it.

Basic interpolation works like this, we store 2 values and use a time depended variable t to lerp between the two values. In our case we want to always interpolate on the last two PlayerUpdateDatas from our player. (Note that we add a little bit of input delay to our movement by doing this, to be precise Time.fixedDeltaTime delay. There are other options without a delay (extrapolating) but they usually have a lot of downsides). 

Lets start by defining some variables again:
```csharp
    [Header("Public Fields")]
    public PlayerUpdateData CurrentData;
    public PlayerUpdateData PreviousData;
    private float lastInputTime;
```

CurrentData will be the last position and PreviousData the position before that. We will also keep track of a time value, this value is only relevant if don't get updates for a while in that case we start to predict(extrapolate). Now we can also add a function to set those values or to just input the next value:
```csharp
    public void SetFramePosition(PlayerUpdateData data)
    {
        RefreshToPosition(data, CurrentData);
    }

    public void RefreshToPosition(PlayerUpdateData data, PlayerUpdateData prevData)
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

Well interpolation is very simple in it's raw form. we set the as the time since our last input and divide it by fixedDeltaTime. Then we lerp between the last 2 values. We use LerpUnclamped because we don't want players to stop moving when they don't get an input for a while(we will use this interpolation scripts for enemies too).

Add the PlayerInterpolation to the Player and open the ClientPlayer script and add the following Variables to it:
```csharp
    [Header("Public Fields")]
    public ushort Id;
    public string Name;
    public bool IsOwn;
```

Id will be the id of the player on the server and IsOwn will be true for our own player but not for enemies.

Add a new variable to the References(I mean below the References header)
```csharp
    public PlayerInterpolation Interpolation;
```
and remove the following (We store our PlayerUpdateData now in the Interpolation)
```csharp
    private PlayerUpdateData data;
```
also change the line in the Start function
```csharp
    data = new PlayerUpdateData(Id,0, Vector3.zero, Quaternion.identity);
```
to:
```csharp
    Interpolation.CurrentData = new PlayerUpdateData(Id,0, Vector3.zero, Quaternion.identity);
```

then search for the following lines in FixedUpdate:
```csharp
            PlayerUpdateData updateData = Logic.GetNextFrameData(inputData,data);
            transform.rotation = data.LookDirection;
```
and replace them with:
```csharp
            transform.position = Interpolation.CurrentData.Position;
            PlayerUpdateData updateData = Logic.GetNextFrameData(inputData,Interpolation.CurrentData);
            Interpolation.SetFramePosition(updateData);
```

The first line we added will set the player back to the last calculated position. We must do that because the PlayerInterpolation also moves the player. And at the end with simply inform the interpolation about the new value, that's all  we have to do. If we run now again we can experience 100% smooth movements.

Finally let's send that information to the server, so after the replaced lines add:
```csharp
            using (Message m = Message.Create((ushort)Tags.GamePlayerInput, inputData))
            {
                GlobalManager.Instance.Client.SendMessage(m, SendMode.Reliable);
            }
```

and add the Tag to the Tags of the Networking Data script:
```csharp
    GamePlayerInput = 203,
```

Now we are ready for the server side. In the next section we will spawn players on the server and send information about them to clients, which makes our game finally a real multiplayer game.

Your scripts should look now like this:
- [Networking Data](https://pastebin.com/HBBTmXmA)
- [PlayerLogic](https://pastebin.com/uV3HUbAj)
- [ClientPlayer](https://pastebin.com/WWtDqziZ)
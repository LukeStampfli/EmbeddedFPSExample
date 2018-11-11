# Client Basics and Login System part 1
 
We will start by implementing functionality to connect to a server and send a simple login request. Let's create a script that connects our client to a server.

- In the client project create a "GlobalManager" script in the Scripts folder.
- In the main scene create an empty Gameobject and name it "GlobalScripts"
- Add a UnityClient component to that gameobject (you need to search for just "client" when adding the component)
- Set the values of the UnityClient like this:\
![](https://i.imgur.com/gxPD6tI.png)\
**Important:** Set Auto Connect to false we will connect in the GlobalManager!

Now we will implement the GlobalManager. First of all add the GlobalManager as a component to the GlobalScripts gameobject. Then open the GlobalManager in your favorite IDE or editor. (Mine is Rider)\
We want the GlobalManager to be globally accessible and present in all scenes so we add the following code:
```csharp
using System;
using System.Net;
using DarkRift;
using DarkRift.Client.Unity;
using UnityEngine;

[RequireComponent(typeof(UnityClient))]
public class GlobalManager : MonoBehaviour
{
    public static GlobalManager Instance;

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

}
```

This allows us to access GlobalManager from any script by calling GlobalManager.Instance, it will also keep the gameobject, where GlobalManager is attached to, loaded when we swap it. We also make sure that we don't create 2 GlobalManagers somehow.

The next step is creating a connection to a server. There are two ways of connecting to a Darkrift server. ConnectInBackGround() and Connect().
- Connect() will connect to the server and freeze everything until it's connected or failed the connect. You can run code immediately after to check if the connection worked (it runs synchronously).
- ConnectInBackGround() connects asynchronously to the server so your code will continue to run and players won't experience a screen freeze. It will invoke a void function of your choice with an exeception parameter as soon as it connected or when the connection failed.

We will use ConnectInBackGround because we don't want our clients to freeze for a few seconds after starting up. (In a real application you would also want to display an animated connection screen for the users)

We will add some variables and a way to ConnectInBackGround to our GlobalManager.
Add the following to the GlobalManager script:
```csharp
   [Header("Variables")]
   public string IpAdress;
   public int Port;

   [Header("References")]
   public UnityClient Client;
```
And also the following connection code:
```csharp
    void Start()
    {
       Client.ConnectInBackground(IPAddress.Parse(IpAdress),Port, IPVersion.IPv4, ConnectCallback); 
    }

    private void ConnectCallback(Exception exception)
    {
        if (Client.Connected)
        {
            //here we will add login code later
        }
        else
        {
            Start();
        }
    }
```

This code tries to connect to a server when we start unity and as soon as we are connected it will call the ConnectCallback function. The function checks if we are connected or if the connection failed and if the connection failed it just tries to connect again by calling Start(); 
This is everything needed to connect to a Darkrift server.

Now lets create a simple login interface.
We will only use a username and no password or account management in our login system to keep it simple.
- Create a "LoginManager" script in the Scripts folder
- Create a "LoginManager" GameObject in the Main scene.
- Add the LoginManager as a component to the LoginManager Gameobject.
- Create a Canvas and add a Window(UI Image),scale it up to the screen size and then add a Button and a InputField to the window, your scene should look now like this:\
![](https://i.imgur.com/zgiTrVo.png)

Now open the LoginManager and again create a static reference to itself and some references to the login elements
```csharp
    public static LoginManager Instance;

    [Header("References")]
    public GameObject LoginWindow;
    public InputField NameInput;
    public Button SubmitLoginButton;

    void Awake()
    {
        Instance = this;
    }
```

It would be nice if the login window only appeared once we connected to the server so lets hide it:
```csharp
    void Start()
    {
        LoginWindow.SetActive(false);
    }
```

And create a function to activate it: 
```csharp
    public void StartLoginProcess()
    {
        LoginWindow.SetActive(true);
    }
```

Unlike in the GlobalManager we don't have to check if Instance != null and delete the script if another already exists because there will always only be one LoginManager because it gets destroyed when we load another scene.

In the GlobalManager we can start now the login process once the client is connected:
```csharp
    if (Client.Connected)
    {
        LoginManager.Instance.StartLoginProcess();
    }
    else
    {
        Start();
    }
```

Now we can code our first message that we send to the server, but first we have to create a new script.
- Create a Networking Data script in the Scripts/shared folder.
- Open it and remove everything that unity created.

Darkrift uses tags to identify messages. A tag is a ushort value(0 to 65,535). We could try to remember all tags but people tend to be better at remembering words so we will use a c# feature to convert our tags into a human readable format.

Add the following to the Networking Data script
```csharp
public enum Tags
{
    LoginRequest = 0,
    LoginRequestAccepted = 1,
    LoginRequestDenied = 2,
}
```

This allows us to use Tags.LoginRequest  as a tag which is easy to remember.
We created 3 tags. LoginRequest is the tag for a message that a client sends to the server when he wants to login.
LoginRequestAccepted and LoginRequestDenied are answers from the server.

But we have to send additional data to the server to login. In our case we just send a username but in most cases you additionally also send a password.

::: tip
Never send passwords without encryption, you have to use an encryption library to send messages with important information.
:::

When we send data to a server we have to serialize it (create a bunch of bytes out of our things that we want to send).
Darkrift does that part for us. We can use a writer to serialize data like this. (Don't write this anywhere its just an example)
```csharp
DarkriftWriter writer = DarkriftWriter.Create();
writer.write(5); //writes an int value of 5 to the writer
writer.write((ushort)3) // writes a ushort value.
 ```
You can also write strings and floats and arrays to a writer. A writer is then used to create a message. A message wants a tag and a writer to send, in its constructor.
```csharp
Message message = Message.Create(tag, writer);
```
You can the send a message by accessing a UnityClient and calling
```csharp
 UnityClient.SendMessage(message, sendmode);
```
There are 2 send modes in Darkrift SendMode.Reliable and SendMode.Unreliable. We will just use reliable for now and will come back to this point later.

On the server when we receive a message we can read values out of the message by creating a DarkriftReader:
```csharp
DarkriftReader reader = Message.GetReader();
reader.readInt32(); //reads an int
reader.readuint32(); //reads an ushort
reader.ReadString(); //reads a string
reader.ReadSingle(); //reads a float
```

Readers, Writers and Messages should be disposed after using them so call Reader.Dispose() after using them. you can also use using blocks and achieve the same effect for example sending a message correctly looks like this:
```csharp
using(DarkriftWriter writer = DarkriftWriter.Create()){
    writer.Write("this is a test message.");
    using(Message message = message.Create(tag, writer)){
         UnityClient.SendMessage(message, SendMode.Reliable);
    }
}
```

It is very important that you always read and write the same amount of data. But doing this the way above leaves room for a lot of mistakes and you will end up with a lot of read and write.

It would be nice to have a way to abstract that process right? Darkrift has a neat system to do that, IDarkriftSerializables. So instead of using writers and reader we will create a IDarkriftSerializable in our Networking Data class:
```csharp
public struct LoginRequestData : IDarkRiftSerializable
{
    public string Name;

    public LoginRequestData(string name)
    {
        Name = name;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Name = e.Reader.ReadString();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Name);
    }
}
```

The LoginRequestData contains data in the form of a string Name and a function Deserialize and Serialize.
These two functions are called when Darkrift sends a message. e.Writer.Write(Name) will "add" the name string to the message when we send it and e.Reader.ReadString(); will read it on the server side and recreate the LoginRequestData. As you can see reading and writing still works in a similar way. But debugging this will be much easier because you can check easily if the serialization of LoginData is correct or wrong. to now send a IDarkriftSerializable you can just put it into a message instead of a writer.

So lets add a function to the LoginManager to send a message:
```csharp
    public void OnSubmitLogin()
    {
        if (NameInput.text != "")
        {
            LoginWindow.SetActive(false);

            using (Message m = Message.Create((ushort)Tags.LoginRequest, new LoginRequestData(NameInput.text)))
            {
                GlobalManager.Instance.Client.SendMessage(m, SendMode.Reliable);
            }
        }
    }
```
This function will deactivate the login window and then send a message to the server with the tag LoginRequest (= 0) and data in the form of a LoginRequestData which contains the name entered in the input field. The (ushort) in front of Tags.LoginRequest is used to tell C# to convert LoginRequest back in an actual number.\

Finally lets subscribe our login button to that function. To do that add the following in the Start() function:

```csharp
SubmitLoginButton.onClick.AddListener(OnSubmitLogin);
```

The scripts should now look like this:
- [GlobalManager](https://pastebin.com/3ikzDGmU)
- [LoginManager](https://pastebin.com/CzNWwC0T)
- [Networking Data](https://pastebin.com/AUwDCCZc)

Now lets jump to the to the server side...
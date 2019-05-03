# Client Login System part 2 and Lobby
After creating responses to the LoginRequest on the server we can let the client handle them.
To do that we first have to subscribe to server messages in the LoginManager script.
Add the following in the Start() function:
```csharp
    GlobalManager.Instance.Client.MessageReceived += OnMessage;
```
and the function:
```csharp
    void OnDestroy()
    {
        GlobalManager.Instance.Client.MessageReceived -= OnMessage;
    }
```
(We always have to unsubscribe from events)

Also add the OnMessage function and functions to handle the responses:
```csharp
    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        using (Message m = e.GetMessage())
        {
            switch ((Tags) m.Tag)
            {
                case Tags.LoginRequestDenied:
                    OnLoginDecline();
                    break;
                case Tags.LoginRequestAccepted:
                    OnLoginAccept(m.Deserialize<LoginInfoData>());
                    break;
            }
        }
    }

    public void OnLoginDecline()
    {
        LoginWindow.SetActive(true);
    }

    public void OnLoginAccept(LoginInfoData data)
    {
    }
```
The OnMessage function works like the one we did on the server.

OnLoginDecline will just show the login window again. (You could a line of text saying login failed or something like that)

OnLoginAccept will store the data and load a LobbyScene to do that we first have to create fields in the GlobalManager to store data. (We will store all data that has to be preserved on scene changes in the GlobalManager).

So Add to the GlobalManager:
```csharp
    [Header("Public Fields")]
    public ushort PlayerId;
    public LobbyInfoData LastRecievedLobbyInfoData;
```

and then modify the OnLoginAccept() function in the LoginManager to:
```csharp
    public void OnLoginAccept(LoginInfoData data)
    {
        GlobalManager.Instance.PlayerId = data.Id;
        GlobalManager.Instance.LastRecievedLobbyInfoData = data.Data;
        SceneManager.LoadScene("Lobby");
    }
```

Now let's create the client side lobby.

- Create a new Scene "Lobby" in the Scenes folder and open it.
- Create a LobbyManager GameObject.
- Create a "LobbyManager" script in the Scripts folder and add it to the LobbyManager GameObject.

We want to display a list of all rooms in graphical matter, we will use UI elements for that:
- Create a Canvas > set it to "Scale with Screen Size" and Reference Resolution to 1920x1080
- Create a Scroll View as a child of the Canvas.
- Add a Vertical Layout Group to the Content object of the Scroll view

Now we need to create RoomObjects. First create a RoomObject script in the Scripts folder and open it and fill it with the following code:
```csharp
using UnityEngine;
using UnityEngine.UI;

public class RoomListObject : MonoBehaviour
{

    [Header("References")]
    public Text NameText;
    public Text SlotText;
    public Button JoinButton;

    public void Set(RoomData data)
    {
        NameText.text = data.Name;
        SlotText.text = data.Slots + "/" + data.MaxSlots;
    }
}
```
Should be self explanatory. The Set() function is used to apply a RoomData to it.
We will also have to create a prefab for the script:
- Create a UI Image as a child of the Content of Scroll View and name it RoomListPrefab.
- Add 2 texts and a button to it, 1 text will be the name the other the slots and the button will be used to join the room.
- Add the RoomListObject script to it and refrecne the texts and the button.

It should look now kind of like [this](https://imgur.com/gallery/6ElYM5g):\
![](https://i.imgur.com/2xuzOVj.png)

Now drag the RoomListPrefab into the prefabs folder and then delete it from the scene.

Open the LobbyManager script and set it to:
```csharp
using DarkRift;
using DarkRift.Client;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{

    public static LobbyManager Instance;

    [Header("References")]
    public Transform RoomListContainerTransform;

    [Header("Prefabs")]
    public GameObject RoomListPrefab;


    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GlobalManager.Instance.Client.MessageReceived += OnMessage;
        RefreshRooms(GlobalManager.Instance.LastRecievedLobbyInfoData);
    }


    void OnDestroy()
    {
        GlobalManager.Instance.Client.MessageReceived -= OnMessage;
    }


    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        using (Message m = e.GetMessage())
        {
            switch ((Tags)m.Tag)
            {
              
            }
        }
    }

   
    public void RefreshRooms(LobbyInfoData data)
    {
        RoomListObject[] roomObjects = RoomListContainerTransform.GetComponentsInChildren<RoomListObject>();
        for (int i = 0; i < data.Rooms.Length; i++)
        {
            RoomData d = data.Rooms[i];
            if (i < roomObjects.Length)
            {
                roomObjects[i].Set(d);
            }
            else
            {
                GameObject go = Instantiate(RoomListPrefab, RoomListContainerTransform);
                go.GetComponent<RoomListObject>().Set(d);
            }
        }
    }

}

```

This might look overwhelming at first glance but its very simple.
The script has a reference to a RoomListContainerTransform which is the transform of the Content and a reference to the RoomListPrefab. (you should go drag them into their respective fields).
It also subscribes like the LoginManager to the server to recieve messages.

The last thing it does is the RefreshRooms() function. I'm not going to explain that function but it does refresh the display of all the rooms, it shouldn't be too complicated to understand.

At this point you can try to run the project. Run the server first and then the client and check it you can log in and if it displays 2 rooms "Main" and "Main 2" in the lobby.

If everyting works fine you can continue. If not, try to look if any errors occurred somewhere. If you are totally stuck you can always contact me on Discord (@Allmaron#6641) or ask in the [Offical Darkrift Discord](https://discordapp.com/invite/cz2FQ6k)

The next step is to allow the player to join a Room. We will need to create new request messages for that.
First add the following new Tags in the NetworkingData.Tags enum:
```csharp
    LobbyJoinRoomRequest = 100,
    LobbyJoinRoomDenied = 101,
    LobbyJoinRoomAccepted = 102,
```

We also need a new data object for the request so also add:
```csharp
public struct JoinRoomRequest : IDarkRiftSerializable
{
    public string RoomName;

    public JoinRoomRequest(string name)
    {
        RoomName = name;
    }

    public void Deserialize(DeserializeEvent e)
    {
        RoomName = e.Reader.ReadString();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(RoomName);
    }
}
```

And then open the LobbyManager and add these functions:
```csharp
    public void SendJoinRoomRequest(string roomName)
    {
        using (Message m = Message.Create((ushort)Tags.LobbyJoinRoomRequest, new JoinRoomRequest(roomName)))
        {
            GlobalManager.Instance.Client.SendMessage(m, SendMode.Reliable);
        }
    }

    public void OnRoomJoinDenied(LobbyInfoData data)
    {
        RefreshRooms(data);
    }

    public void OnRoomJoinAcepted()
    {
        SceneManager.LoadScene("Game");
    }
```
And change the OnMessage function to:
```csharp
    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        using (Message m = e.GetMessage())
        {
            switch ((Tags)m.Tag)
            {
                case Tags.LobbyJoinRoomDenied:
                    OnRoomJoinDenied(m.Deserialize<LobbyInfoData>());
                    break;
                case Tags.LobbyJoinRoomAccepted:
                    OnRoomJoinAcepted();
                    break;
            }
        }
    }
```

Ok so what does this do?
We have a function SendJoinRoomRequest to ask the server to join a room with a given Name.
The server can respond with an Accepted or a Denied resposne. The Denied request will contain new RoomDatas so that the rooms and available slots get refreshed and the accepted will just load the "Game" scene because it means we connected successfully.

Now we just need a way to call SendJoinRoomRequest(). We will subscribe the function to the buttons in the list of rooms.
To do that open the RoomListObject script and at the end of the Set function the following lines:
```csharp
JoinButton.onClick.RemoveAllListeners();
JoinButton.onClick.AddListener(delegate { LobbyManager.Instance.SendJoinRoomRequest(data.Name); });
```

Your scripts should look like this:
- [LoginManager](https://pastebin.com/ue7T0sFL)
- [GlobalManager](https://pastebin.com/wMBubKxN)
- [RoomListObject](https://pastebin.com/2Fvczdq1)
- [LobbyManager](https://pastebin.com/MZpWAuqX)
- [Networking Data](https://pastebin.com/HYXKwysi)

The lobby and login system is now fully completed on the client. Now, we will swap now to the server to finish the system there too.
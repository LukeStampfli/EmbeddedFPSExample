# Room System Part 2 (Server-Side)

In this section will add the functionality to join rooms.
We start by modifying the ClientConnection script a bit:

Next to the other properties we add:
```csharp
    public Room Room { get; set; }
```

In addition we will let the ClientConnection handle messages:
```csharp
    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        IClient client = (IClient)sender;
        using (Message message = e.GetMessage())
        {
            switch ((Tags)message.Tag)
            {
            }
        }
    }
```

We have to subscribe the function, in the Constructor add:
```csharp
       Client.MessageReceived += OnMessage;
```

And create a function to call if the player disconnects:
```csharp
   public void OnClientDisconnect(object sender, ClientDisconnectedEventArgs e)
    {
        ServerManager.Instance.Players.Remove(Client.ID);
        ServerManager.Instance.PlayersByName.Remove(Name);
        e.Client.MessageReceived -= OnMessage;
    }
```

Open the ServerManager and replace the OnClientDisconnect() function with this new version which calls the function in the ClientConnection:
```csharp
    private void OnClientDisconnect(object sender, ClientDisconnectedEventArgs e)
    {
        IClient client = e.Client;
        ClientConnection p;
        if (Players.TryGetValue(client.ID, out p))
        {
            p.OnClientDisconnect(sender, e);
        }
        e.Client.MessageReceived -= OnMessage;
    }
```

Now we add a way to add/remove a client to our Room script and we will also add a Close function to close the room:
```csharp
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
        foreach(ClientConnection p in ClientConnections)
        {
            RemovePlayerFromRoom(p);
        }
        Destroy(gameObject);
    }
```

But we don't know which room we should add the player to, so we have to ask the RoomManager first:
```csharp
    public void TryJoinRoom(IClient client, JoinRoomRequest data)
    {
        bool canJoin = ServerManager.Instance.Players.TryGetValue(client.ID, out var clientConnection);

        if (!rooms.TryGetValue(data.RoomName, out var room))
        {
            canJoin = false;
        }
        else if (room.ClientConnections.Count >= room.MaxSlots)
        {
            canJoin = false;
        }

        if (canJoin)
        {
            room.AddPlayerToRoom(clientConnection);
        }
        else
        {
            using (Message m = Message.Create((ushort)Tags.LobbyJoinRoomDenied, new LobbyInfoData(GetRoomDataList())))
            {
                client.SendMessage(m, SendMode.Reliable);
            }
        }
    }
```
We do a few checks here to test if the player is logged in, the room exists and if the room has space. And if everything is alright, we add the player to the room. Else we send a JoinRoomDenied message back with a refreshed list of RoomDatas.

We also change the RemoveRoom function because we can Close rooms now:
```csharp
    public void RemoveRoom(string name)
    {
        Room r = rooms[name];
        r.Close();
        rooms.Remove(name);
    }
```

Lets call TryJoinRoom from the ClientConnection script if we get a LobbyJoinRoomRequest:
```csharp
    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        IClient client = (IClient)sender;
        using (Message message = e.GetMessage())
        {
            switch ((Tags)m.Tag)
            {
                case Tags.LobbyJoinRoomRequest:
                    RoomManager.Instance.TryJoinRoom(client, message.Deserialize<JoinRoomRequest>());
                    break;
            }
        }
    }
```

And in the OnClientDisconnect function we will remove the player from the room, so add the following lines at the beginning:
```csharp
    if (Room != null)
    {
        Room.RemovePlayerFromRoom(this);
    }
```

Your scripts should look like this:

- [ClientConnection](https://github.com/LukeStampfli/EmbeddedFPSExample/tree/master/gists/room2-ClientConnection.cs)
- [Room](https://github.com/LukeStampfli/EmbeddedFPSExample/tree/master/gists/room2-Room.cs)
- [RoomManager](https://github.com/LukeStampfli/EmbeddedFPSExample/tree/master/gists/room2-RoomManager.cs)

Now we have a working room system. So we can finally work at the actual gameplay :smiley: 

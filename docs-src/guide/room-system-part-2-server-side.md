# Room System part 2 (Server-side)

In this section will add the functionality to join rooms.
We start by chaning the ClientConnection script a bit:

To the other public fields, we add:
```csharp
    public Room Room;
```

In addition we will let the ClientConnection handle messages:
```csharp
    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        IClient client = (IClient)sender;
        using (Message m = e.GetMessage())
        {
            switch ((Tags)m.Tag)
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

Now we add a way to add/reomve a client to our rooms to our Room script and we will also add a Close function to close the room:
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

But we don't know to which room we should add the player so we have to ask the RoomManager first:
```csharp
    public void TryJoinRoom(IClient client, JoinRoomRequest data)
    {
        ClientConnection p;
        Room r;
        if (!ServerManager.Instance.Players.TryGetValue(client.ID, out p))
        {
            using (Message m = Message.Create((ushort)Tags.LobbyJoinRoomDenied, new LobbyInfoData(GetRoomDataList())))
            {
                client.SendMessage(m, SendMode.Reliable);
            }
            return;
        }

        if (!rooms.TryGetValue(data.RoomName, out r))
        {
            using (Message m = Message.Create((ushort)Tags.LobbyJoinRoomDenied, new LobbyInfoData(GetRoomDataList())))
            {
                client.SendMessage(m, SendMode.Reliable);
            }
            return;
        }

        if (r.ClientConnections.Count >= r.MaxSlots)
        {
            using (Message m = Message.Create((ushort)Tags.LobbyJoinRoomDenied, new LobbyInfoData(GetRoomDataList())))
            {
                client.SendMessage(m, SendMode.Reliable);
            }
        }

        r.AddPlayerToRoom(p);
    }
```

We all change the RemoveRoom function because we can Close rooms now to:
```csharp
    public void RemoveRoom(string name)
    {
        Room r = rooms[name];
        r.Close();
        rooms.Remove(name);
        
    }
```

We do a few checks here to test if the player is logged in, the room exists and if the room has space and if everything is alright we add the player to the room else we send a JoinRoomDenied message back with a refreshed list of RoomDatas.

Lets call TryJoinRoom from the ClientConnection script if we get a LobbyJoinRoomRequest:
```csharp
    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        IClient client = (IClient)sender;
        using (Message m = e.GetMessage())
        {
            switch ((Tags)m.Tag)
            {
                case Tags.LobbyJoinRoomRequest:
                    RoomManager.Instance.TryJoinRoom(client, m.Deserialize<JoinRoomRequest>());
                    break;
            }
        }
    }
```

and in the OnClientDisconnect function we will remove the player from the room, so add the following lines at the beginning:
```csharp
        if (Room != null)
        {
            Room.RemovePlayerFromRoom(this);
        }
```

Your scripts should look like this:

- [ClientConnection](https://pastebin.com/VixNs1q9)
- [Room](https://pastebin.com/MHPGRAbj)
- [RoomManager](https://pastebin.com/j9eXBM5h)

Now we have a working room system. so we can finally work at the actual gameplay :smiley: 

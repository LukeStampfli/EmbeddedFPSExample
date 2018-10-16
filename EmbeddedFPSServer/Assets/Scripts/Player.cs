using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

public class Player
{
    public string Name;
    public IClient Client;
    public Room Room;

    public Player(IClient client , LoginRequestData data)
    {
        Client = client;
        Name = data.Name;

        ServerManager.Instance.Players.Add(client.ID, this);
        ServerManager.Instance.PlayersByName.Add(Name, this);

        Client.MessageReceived += OnMessage;
        

        using (Message m = Message.Create((ushort)Tags.LoginRequestAccepted, new LoginInfoData(client.ID, new LobbyInfoData(RoomManager.Instance.GetRoomDataList()))))
        {
            client.SendMessage(m, SendMode.Reliable);
        }
    }

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


    public void OnClientDisconnect(object sender, ClientDisconnectedEventArgs e)
    {
        if (Room != null)
        {
            Room.RemovePlayerFromRoom(this);
        }

        ServerManager.Instance.Players.Remove(Client.ID);
        ServerManager.Instance.PlayersByName.Remove(Name);
        e.Client.MessageReceived -= OnMessage;
    }
}

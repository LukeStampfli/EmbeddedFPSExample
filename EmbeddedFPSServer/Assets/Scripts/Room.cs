using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

public class Room : MonoBehaviour
{
    public string Name;
    public List<Player> Players = new List<Player>();
    public byte MaxSlots;

    public void Initialize(string name, byte maxslots)
    {
        Name = name;
        MaxSlots = maxslots;
    }

    public void AddPlayerToRoom(Player player)
    {
        Players.Add(player);
        player.Room = this;
        using (Message message = Message.CreateEmpty((ushort)Tags.LobbyJoinRoomAccepted))
        {
            player.Client.SendMessage(message, SendMode.Reliable);
        }
    }


    public void RemovePlayerFromRoom(Player player)
    {
        Players.Remove(player);
        player.Room = null;
    }

    public void Close()
    {
        foreach(Player p in Players)
        {
            RemovePlayerFromRoom(p);
        }
        Destroy(gameObject);
    }
}

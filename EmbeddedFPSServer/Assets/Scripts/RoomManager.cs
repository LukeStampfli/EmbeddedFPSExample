using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

public class RoomManager : MonoBehaviour
{

    public static RoomManager Instance;
    public Dictionary<string, Room> Rooms = new Dictionary<string, Room>();
    private List<Room> roomList = new List<Room>();
    public GameObject RoomPrefab;

    void Awake()
    {
        Instance = this;
        CreateRoom("Main",25);
    }

    public RoomData[] GetRoomDataList()
    {
        RoomData[] datas = new RoomData[roomList.Count];
        for (int i = 0; i < roomList.Count; i++)
        {
            Room r = roomList[i];
            datas[i] = new RoomData(r.Name, (byte) r.Players.Count, r.MaxSlots);
        }

        return datas;
    }

    public void TryJoinRoom(IClient client, JoinRoomRequest data)
    {
        Player p;
        Room r;
        if (!ServerManager.Instance.Players.TryGetValue(client.ID, out p))
        {
            using (Message m = Message.Create((ushort)Tags.LobbyJoinRoomDenied, new LobbyInfoData(GetRoomDataList())))
            {
                client.SendMessage(m, SendMode.Reliable);
            }
            return;
        }

        if (!Rooms.TryGetValue(data.RoomName, out r))
        {
            using (Message m = Message.Create((ushort)Tags.LobbyJoinRoomDenied, new LobbyInfoData(GetRoomDataList())))
            {
                client.SendMessage(m, SendMode.Reliable);
            }
            return;
        }

        if (r.Players.Count >= r.MaxSlots)
        {
            using (Message m = Message.Create((ushort)Tags.LobbyJoinRoomDenied, new LobbyInfoData(GetRoomDataList())))
            {
                client.SendMessage(m, SendMode.Reliable);
            }
        }

        r.AddPlayerToRoom(p);
    }

    public void CreateRoom(string name, byte maxslots)
    {
        GameObject go = Instantiate(RoomPrefab, transform);
        Room r = go.GetComponent<Room>();
        r.Initialize(name, maxslots);
        Rooms.Add(name, r);
        roomList.Add(r);
    }

    public void RemoveRoom(string name)
    {
        Room r = Rooms[name];
        r.Close();
        roomList.Remove(r);
        Rooms.Remove(name);
    }

}

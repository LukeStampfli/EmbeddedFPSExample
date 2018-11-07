using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

public class RoomManager : MonoBehaviour
{

    public static RoomManager Instance;

    [Header("Prefabs")]
    public GameObject RoomPrefab;

    private List<Room> roomList = new List<Room>();
    Dictionary<string, Room> rooms = new Dictionary<string, Room>();
    private float offset;

    void Awake()
    {
        Instance = this;
        CreateRoom("Main",25);
        CreateRoom("Main 2", 15);
    }

    public RoomData[] GetRoomDataList()
    {
        RoomData[] datas = new RoomData[roomList.Count];
        for (int i = 0; i < roomList.Count; i++)
        {
            Room r = roomList[i];
            datas[i] = new RoomData(r.Name, (byte) r.ClientConnections.Count, r.MaxSlots);
        }

        return datas;
    }

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

    public void CreateRoom(string name, byte maxslots)
    {
        GameObject go = Instantiate(RoomPrefab, transform);
        go.transform.position = new Vector3(offset,0,0);
        offset += 300;
        Room r = go.GetComponent<Room>();
        r.Initialize(name, maxslots);
        rooms.Add(name, r);
        roomList.Add(r);
    }

    public void RemoveRoom(string name)
    {
        Room r = rooms[name];
        r.Close();
        roomList.Remove(r);
        rooms.Remove(name);
        
    }

}

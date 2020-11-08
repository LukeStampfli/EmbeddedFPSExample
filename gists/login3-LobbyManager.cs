using DarkRift;
using DarkRift.Client;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform roomListContainerTransform;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject roomListPrefab;

    void Start()
    {
        ConnectionManager.Instance.Client.MessageReceived += OnMessage;
        RefreshRooms(ConnectionManager.Instance.LobbyInfoData);
    }

    void OnDestroy()
    {
        ConnectionManager.Instance.Client.MessageReceived -= OnMessage;
    }

    private void OnMessage(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            switch ((Tags)message.Tag)
            {
                case Tags.LobbyJoinRoomDenied:
                    OnRoomJoinDenied(message.Deserialize<LobbyInfoData>());
                    break;
                case Tags.LobbyJoinRoomAccepted:
                    OnRoomJoinAcepted();
                    break;
            }
        }
    }

    public void SendJoinRoomRequest(string roomName)
    {
        using (Message message = Message.Create((ushort)Tags.LobbyJoinRoomRequest, new JoinRoomRequest(roomName)))
        {
            ConnectionManager.Instance.Client.SendMessage(message, SendMode.Reliable);
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

    public void RefreshRooms(LobbyInfoData data)
    {
        RoomListObject[] roomObjects = roomListContainerTransform.GetComponentsInChildren<RoomListObject>();

        if (roomObjects.Length > data.Rooms.Length)
        {
            for (int i = data.Rooms.Length; i < roomObjects.Length; i++)
            {
                Destroy(roomObjects[i].gameObject);
            }
        }

        for (int i = 0; i < data.Rooms.Length; i++)
        {
            RoomData d = data.Rooms[i];
            if (i < roomObjects.Length)
            {
                roomObjects[i].Set(this, d);
            }
            else
            {
                GameObject go = Instantiate(roomListPrefab, roomListContainerTransform);
                go.GetComponent<RoomListObject>().Set(this, d);
            }
        }
    }
}

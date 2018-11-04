using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

public class ServerPlayer : MonoBehaviour
{
    [Header("Public Fields")]
    public ClientConnection ClientConnection;
    public Room Room;
    public int Health;

    [Header("References")]
    public PlayerLogic Logic;

    public IClient Client;
    public PlayerUpdateData CurrentUpdateData;



    private int indexInRoomList;
    private int bufferWaitTime = 3;
    private Queue<PlayerInputData>inputBuffer = new Queue<PlayerInputData>();
    public List<PlayerUpdateData> UpdateDataHistory = new List<PlayerUpdateData>();

    public void Initialize(Vector3 position, ClientConnection clientConnection)
    {
        ClientConnection = clientConnection;
        Room = clientConnection.Room;
        Client = clientConnection.Client;
        ClientConnection.Player = this;
        Room.ServerPlayers.Add(this);
        indexInRoomList = Room.ServerPlayers.Count - 1;
        Room.UpdateDatas = new PlayerUpdateData[Room.ServerPlayers.Count];
        CurrentUpdateData = new PlayerUpdateData(Client.ID,0, Vector3.zero, Quaternion.identity);
        Health = 100;

        PlayerSpawnData[] datas = new PlayerSpawnData[Room.ServerPlayers.Count];
        for (int i = 0; i < Room.ServerPlayers.Count; i++)
        {
            ServerPlayer p = Room.ServerPlayers[i];
            datas[i] = p.GetPlayerSpawnData();
        }
        using (Message m = Message.Create((ushort)Tags.GameStartDataResponse, new GameStartData(datas,Room.ServerTick)))
        {
            Client.SendMessage(m, SendMode.Reliable);
        }
    }

    public void Recieveinput(PlayerInputData input)
    {
        inputBuffer.Enqueue(input);
    }

    public void TakeDamage(int value)
    {
        Health -= value;
        if (Health <= 0)
        {
            Health = 100;
            CurrentUpdateData.Position = new Vector3(0,1,0)+ transform.parent.transform.localPosition;
            CurrentUpdateData.Gravity = 0;
            transform.localPosition = CurrentUpdateData.Position;
        }
        Room.HealthUpdates.Add(new PLayerHealthUpdateData(Client.ID, (byte) Health));
    }


    public void PerformShootupdate()
    {
        if (inputBuffer.Any())
        {
            PlayerInputData next = inputBuffer.Peek();
            if (next.Keyinputs[5])
            {
                Room.PerformShootRayCast(next.Time, this);
            }
        }
    }

    public void PerformUpdate()
    {
        if (bufferWaitTime > 0)
        {
            bufferWaitTime--;
        }
        else
        {
            if (inputBuffer.Any())
            {
                while (inputBuffer.Count > 3 && bufferWaitTime < 0)
                {
                    bufferWaitTime++;
                    inputBuffer.Dequeue();
                }


                PlayerUpdateData data = Logic.GetNextFrameData(inputBuffer.Dequeue(), CurrentUpdateData);
                CurrentUpdateData = data;

            }
            else
            {
                bufferWaitTime--;
            }

        }

        UpdateDataHistory.Add(CurrentUpdateData);
        if (UpdateDataHistory.Count > 10)
        {
            UpdateDataHistory.RemoveAt(0);
        }

        transform.localPosition = CurrentUpdateData.Position;
        transform.localRotation = CurrentUpdateData.LookDirection;
        Room.UpdateDatas[indexInRoomList] = CurrentUpdateData;
    }

    public PlayerSpawnData GetPlayerSpawnData()
    {
        return new PlayerSpawnData(Client.ID, ClientConnection.Name, transform.localPosition);
    }

}

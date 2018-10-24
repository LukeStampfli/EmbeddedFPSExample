using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

public class ServerPlayer : MonoBehaviour
{

    public IClient Client;
    public PlayerClient PlayerClient;
    public PlayerLogic Logic;
    public PlayerUpdateData CurrentUpdateData;
    public Room Room;
    public int Health;

    private int indexInRoomList;
    private uint bufferWaitTime = 3;
    private Queue<PlayerInputData>inputBuffer = new Queue<PlayerInputData>();

    public void Initialize(Vector3 position, PlayerClient playerClient)
    {
        PlayerClient = playerClient;
        Room = playerClient.Room;
        Client = playerClient.Client;
        PlayerClient.Player = this;
        Room.ServerPlayers.Add(this);
        indexInRoomList = Room.ServerPlayers.Count - 1;
        Room.updateDatas = new PlayerUpdateData[Room.ServerPlayers.Count];
        CurrentUpdateData = new PlayerUpdateData(Client.ID,0, Vector3.zero, new Vector3(0,0,0));
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
            CurrentUpdateData.Position = new Vector3(0,1,0);
            CurrentUpdateData.Gravity = 0;
            transform.localPosition = CurrentUpdateData.Position;
        }
        Room.healthUpdates.Add(new PLayerHealthUpdateData(Client.ID, (byte) Health));
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
                PlayerUpdateData data = Logic.GetNextFrameData(inputBuffer.Dequeue(), CurrentUpdateData);
                CurrentUpdateData = data;
            }

        }

        transform.localPosition = CurrentUpdateData.Position;
        transform.localRotation = Quaternion.Euler(CurrentUpdateData.LookDirection);
        Room.updateDatas[indexInRoomList] = CurrentUpdateData;
    }

    public PlayerSpawnData GetPlayerSpawnData()
    {
        return new PlayerSpawnData(Client.ID, PlayerClient.Name, transform.localPosition);
    }

}

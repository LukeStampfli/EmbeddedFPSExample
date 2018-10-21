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
    private int indexInRoomList;
    private uint NextInputTick;
    private uint WaitticksUntilPerformInput;
    private List<PlayerInputData>inputBuffer = new List<PlayerInputData>();

    public void Initialize(Vector3 position, PlayerClient playerClient)
    {
        PlayerClient = playerClient;
        Room = playerClient.Room;
        Client = playerClient.Client;
        PlayerClient.Player = this;
        Room.ServerPlayers.Add(this);
        NextInputTick = Room.ServerTick+1;
        WaitticksUntilPerformInput = 3;
        indexInRoomList = Room.ServerPlayers.Count - 1;
        Room.updateDatas = new PlayerUpdateData[Room.ServerPlayers.Count];
        CurrentUpdateData = new PlayerUpdateData(Vector3.zero, Quaternion.identity);

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
        inputBuffer.Add(input);
    }

    public void PerformUpdate()
    {
        if (WaitticksUntilPerformInput > 0)
        {
            WaitticksUntilPerformInput--;
        }
        else
        {
            if (inputBuffer.Any())
            {
                Debug.Log("performing input");
                PlayerUpdateData data = Logic.GetNextFrameData(inputBuffer.First(), CurrentUpdateData);
                inputBuffer.RemoveAt(0);
                CurrentUpdateData = data;
            }

        }

        transform.localPosition = CurrentUpdateData.Position;
        Room.updateDatas[indexInRoomList] = CurrentUpdateData;
    }

    public PlayerSpawnData GetPlayerSpawnData()
    {
        return new PlayerSpawnData(Client.ID, PlayerClient.Name, transform.localPosition, transform.rotation.eulerAngles.y);
    }

}

using System;
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
    public uint InputTick;
    public int Health;

    [Header("References")]
    public PlayerLogic Logic;

    public IClient Client;
    public PlayerUpdateData CurrentUpdateData;

    public List<PlayerUpdateData> UpdateDataHistory = new List<PlayerUpdateData>();

    private Buffer<PlayerInputData> inputBuffer = new Buffer<PlayerInputData>(1,2);

    private PlayerInputData[] inputs;

    public void Initialize(Vector3 position, ClientConnection clientConnection)
    {
        ClientConnection = clientConnection;
        Room = clientConnection.Room;
        Client = clientConnection.Client;
        ClientConnection.Player = this;
        Room.ServerPlayers.Add(this);

        Room.UpdateDatas = new PlayerUpdateData[Room.ServerPlayers.Count];
        CurrentUpdateData = new PlayerUpdateData(Client.ID,0, Vector3.zero, Quaternion.identity);
        InputTick = Room.ServerTick;
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

    public void RecieveInput(PlayerInputData input)
    {
        inputBuffer.Add(input);
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


    public void PerformuPreUpdate()
    {
        inputs = inputBuffer.Get();
        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i].Keyinputs[5])
            {
                Room.PerformShootRayCast(inputs[i].Time, this);
                return;
            }
        }
    }

    public void PerformUpdate(int index)
    {

        if (inputs.Length > 0)
        {
            PlayerInputData input = inputs.First();
            InputTick++;

            for (int i = 1; i < inputs.Length; i++)
            {
                InputTick++;
                for (int j = 0; j < input.Keyinputs.Length; j++)
                {
                    input.Keyinputs[j] = input.Keyinputs[j] || inputs[i].Keyinputs[j];
                }
                input.LookDirection = inputs[i].LookDirection;
            }

            CurrentUpdateData = Logic.GetNextFrameData(input, CurrentUpdateData);
        }
     
       
    
        UpdateDataHistory.Add(CurrentUpdateData);
        if (UpdateDataHistory.Count > 10)
        {
            UpdateDataHistory.RemoveAt(0);
        }

        transform.localPosition = CurrentUpdateData.Position;
        transform.localRotation = CurrentUpdateData.LookDirection;
        Room.UpdateDatas[index] = CurrentUpdateData;
    }

    public PlayerSpawnData GetPlayerSpawnData()
    {
        return new PlayerSpawnData(Client.ID, ClientConnection.Name, transform.localPosition);
    }

}

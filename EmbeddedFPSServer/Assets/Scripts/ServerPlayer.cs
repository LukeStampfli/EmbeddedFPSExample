using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

[RequireComponent(typeof(PlayerLogic))]
public class ServerPlayer : MonoBehaviour
{
    private ClientConnection clientConnection;
    private Room room;

    private PlayerStateData currentPlayerStateData;

    private Buffer<PlayerInputData> inputBuffer = new Buffer<PlayerInputData>(1, 2);

    private int health;

    public PlayerLogic PlayerLogic { get; private set; }
    public uint InputTick { get; private set; }
    public IClient Client { get; private set; }
    public PlayerStateData CurrentPlayerStateData => currentPlayerStateData;
    public List<PlayerStateData> PlayerStateDataHistory { get; } = new List<PlayerStateData>();

    private PlayerInputData[] inputs;

    void Awake()
    {
        PlayerLogic = GetComponent<PlayerLogic>();
    }

    public void Initialize(Vector3 position, ClientConnection clientConnection)
    {
        this.clientConnection = clientConnection;
        room = clientConnection.Room;
        Client = clientConnection.Client;
        this.clientConnection.Player = this;
        
        currentPlayerStateData = new PlayerStateData(Client.ID,0, position, Quaternion.identity);
        InputTick = room.ServerTick;
        health = 100;

        var playerSpawnData = room.GetSpawnDataForAllPlayers();
        using (Message m = Message.Create((ushort)Tags.GameStartDataResponse, new GameStartData(playerSpawnData, room.ServerTick)))
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
        health -= value;
        if (health <= 0)
        {
            health = 100;
            currentPlayerStateData.Position = new Vector3(0,1,0) + transform.parent.transform.localPosition;
            currentPlayerStateData.Gravity = 0;
            transform.localPosition = currentPlayerStateData.Position;
        }
        room.UpdatePlayerHealth(this, (byte)health);
    }

    public void PlayerPreUpdate()
    {
        inputs = inputBuffer.Get();
        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i].Keyinputs[5])
            {
                room.PerformShootRayCast(inputs[i].Time, this);
                return;
            }
        }
    }

    public PlayerStateData PlayerUpdate()
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

            currentPlayerStateData = PlayerLogic.GetNextFrameData(input, currentPlayerStateData);
        }
        
        PlayerStateDataHistory.Add(currentPlayerStateData);
        if (PlayerStateDataHistory.Count > 10)
        {
            PlayerStateDataHistory.RemoveAt(0);
        }

        transform.localPosition = currentPlayerStateData.Position;
        transform.localRotation = currentPlayerStateData.LookDirection;
        return currentPlayerStateData;
    }

    public PlayerSpawnData GetPlayerSpawnData()
    {
        return new PlayerSpawnData(Client.ID, clientConnection.Name, transform.localPosition);
    }

}

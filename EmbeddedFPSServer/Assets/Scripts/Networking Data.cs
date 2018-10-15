using DarkRift;
using UnityEngine;
using DarkriftSerializationExtensions;

public enum Tags
{
    LoginRequest = 0,
    LoginRequestAccepted = 1,
    LoginRequestDenied = 2,

    LobbyJoinRoomRequest = 100,
    LobbyJoinRoomDenied = 101,
    LobbyJoinRoomAccepted = 102,

    GameJoinRequest = 200,
    GameStartDataResponse = 201,
    GameUpdate = 202,
    GamePlayerInput = 203,
}


public struct LoginRequestData : IDarkRiftSerializable
{
    public string Name;

    public LoginRequestData(string name)
    {
        Name = name;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Name = e.Reader.ReadString();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Name);
    }
}

public struct LoginInfoData : IDarkRiftSerializable
{
    public ushort Id;
    public LobbyInfoData Data;

    public void Deserialize(DeserializeEvent e)
    {
        Id = e.Reader.ReadUInt16();
        Data = e.Reader.ReadSerializable<LobbyInfoData>();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Id);
        e.Writer.Write(Data);
    }
}

public struct LobbyInfoData : IDarkRiftSerializable
{
    public RoomData[] Rooms;

    public LobbyInfoData(RoomData[] rooms)
    {
        Rooms = rooms;
    }

    public void Deserialize(DeserializeEvent e)
    {
       Rooms = e.Reader.ReadSerializables<RoomData>();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Rooms);
    }
}

public struct RoomData : IDarkRiftSerializable
{
    public string Name;
    public byte Slots;
    public byte MaxSlots;

    public RoomData(string name, byte slots, byte maxSlots)
    {
        Name = name;
        Slots = slots;
        MaxSlots = maxSlots;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Name = e.Reader.ReadString();
        Slots = e.Reader.ReadByte();
        MaxSlots = e.Reader.ReadByte();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Name);
        e.Writer.Write(Slots);
        e.Writer.Write(MaxSlots);
    }
}

public struct JoinRoomRequest : IDarkRiftSerializable
{
    public string RoomName;

    public JoinRoomRequest(string name)
    {
        RoomName = name;
    }

    public void Deserialize(DeserializeEvent e)
    {
        RoomName = e.Reader.ReadString();
    }

    public void Serialize(SerializeEvent e)
    {
       e.Writer.Write(RoomName);
    }
}

public struct GameStartData : IDarkRiftSerializable
{
    public uint OnJoinServerTick;
    public PlayerSpawnData[] Players;

    public GameStartData(PlayerSpawnData[] players, uint servertick)
    {
        Players = players;
        OnJoinServerTick = servertick;
    }

    public void Deserialize(DeserializeEvent e)
    {
        OnJoinServerTick = e.Reader.ReadUInt32();
        Players = e.Reader.ReadSerializables<PlayerSpawnData>();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(OnJoinServerTick);
        e.Writer.Write(Players);
    }
}

public struct PlayerSpawnData: IDarkRiftSerializable
{
    public ushort Id;
    public string Name;

    public Vector3 Position;
    public float Rotation;

    public PlayerSpawnData(ushort id, string name, Vector3 position, float rotation)
    {
        Id = id;
        Name = name;
        Position = position;
        Rotation = rotation;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Id = e.Reader.ReadUInt16();
        Name = e.Reader.ReadString();
        Position = e.Reader.ReadVector3();
        Rotation = e.Reader.ReadSingle();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Id);
        e.Writer.Write(Name);
        e.Writer.WriteVector3(Position);
        e.Writer.Write(Rotation);
    }
}

public struct PlayerDespawnData : IDarkRiftSerializable
{
    public ushort Id;

    public PlayerDespawnData(ushort id)
    {
        Id = id;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Id = e.Reader.ReadUInt16();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Id);
    }
}

public struct PlayerUpdateData:IDarkRiftSerializable
{

    public Vector3 Position;
    public float Rotation;
    public Quaternion LookDirection;

    public void Deserialize(DeserializeEvent e)
    {
        Position = e.Reader.ReadVector3();
        Rotation = e.Reader.ReadSingle();
        LookDirection = e.Reader.ReadQuaternionCompressed();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.WriteVector3(Position);
        e.Writer.Write(Rotation);
        e.Writer.WriteQuaternionCompressed(LookDirection);
    }
}

public struct GameUpdateData : IDarkRiftSerializable
{
    public PlayerSpawnData[] SpawnData;
    public PlayerDespawnData[] DespawnData;
    public PlayerUpdateData[] UpdateData;

    public GameUpdateData(PlayerUpdateData[] updateData, PlayerSpawnData[] spawns, PlayerDespawnData[] despawns)
    {
        UpdateData = updateData;
        DespawnData = despawns;
        SpawnData = spawns;
    }
    public void Deserialize(DeserializeEvent e)
    {
        SpawnData = e.Reader.ReadSerializables<PlayerSpawnData>();
        DespawnData = e.Reader.ReadSerializables<PlayerDespawnData>();
        UpdateData = e.Reader.ReadSerializables<PlayerUpdateData>();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(SpawnData);
        e.Writer.Write(DespawnData);
        e.Writer.Write(UpdateData);
    }
}

public struct PlayerInputData : IDarkRiftSerializable
{
    public bool[] Keyinputs;
    public Quaternion LookDirectionDelta;

    public PlayerInputData(bool[] keyInputs, Quaternion lookDirectionDelta)
    {
        Keyinputs = keyInputs;
        LookDirectionDelta = lookDirectionDelta;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Keyinputs = e.Reader.ReadBooleans();
        LookDirectionDelta = e.Reader.ReadQuaternionCompressed();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Keyinputs);
        e.Writer.WriteQuaternionCompressed(LookDirectionDelta);
    }
}
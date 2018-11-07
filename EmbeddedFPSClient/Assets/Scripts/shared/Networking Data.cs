using DarkRift;
using UnityEngine;

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

    public LoginInfoData(ushort id, LobbyInfoData data)
    {
        Id = id;
        Data = data;
    }

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

public struct PlayerSpawnData : IDarkRiftSerializable
{
    public ushort Id;
    public string Name;

    public Vector3 Position;

    public PlayerSpawnData(ushort id, string name, Vector3 position)
    {
        Id = id;
        Name = name;
        Position = position;
    }

    public void Deserialize(DeserializeEvent e)
    {
        Id = e.Reader.ReadUInt16();
        Name = e.Reader.ReadString();
<<<<<<< HEAD
        Position = new Vector3(e.Reader.ReadSingle(),e.Reader.ReadSingle(),e.Reader.ReadSingle());
        Rotation = e.Reader.ReadSingle();
=======
        Position = e.Reader.ReadVector3();
>>>>>>> 1ed7e4d4b265e0c62eebc144d34ebedc9d1da0ff
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Id);
        e.Writer.Write(Name);
<<<<<<< HEAD

        e.Writer.Write(Position.x);
        e.Writer.Write(Position.y);
        e.Writer.Write(Position.z);

        e.Writer.Write(Rotation);
=======
        e.Writer.WriteVector3(Position);
>>>>>>> 1ed7e4d4b265e0c62eebc144d34ebedc9d1da0ff
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

public struct PlayerUpdateData : IDarkRiftSerializable
{

    public PlayerUpdateData(ushort id, float gravity, Vector3 position, Quaternion lookDirection)
    {
        Id = id;
        Position = position;
        LookDirection = lookDirection;
        Gravity = gravity;
    }

    public ushort Id;
    public Vector3 Position;
    public float Gravity;
    public Quaternion LookDirection;

    public void Deserialize(DeserializeEvent e)
    {
<<<<<<< HEAD
        Position = new Vector3(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
        LookDirection = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
=======
        Id = e.Reader.ReadUInt16();
        Gravity = e.Reader.ReadSingle();
        Position = e.Reader.ReadVector3();
        LookDirection = e.Reader.ReadQuaternion();
>>>>>>> 1ed7e4d4b265e0c62eebc144d34ebedc9d1da0ff
    }

    public void Serialize(SerializeEvent e)
    {
<<<<<<< HEAD
        e.Writer.Write(Position.x);
        e.Writer.Write(Position.y);
        e.Writer.Write(Position.z);
 
        e.Writer.Write(LookDirection.x);
        e.Writer.Write(LookDirection.y);
        e.Writer.Write(LookDirection.z);
        e.Writer.Write(LookDirection.w);
=======
        e.Writer.Write(Id);
        e.Writer.Write(Gravity);
        e.Writer.WriteVector3(Position);
        e.Writer.WriteQuaternion(LookDirection);
>>>>>>> 1ed7e4d4b265e0c62eebc144d34ebedc9d1da0ff
    }
}


public struct GameUpdateData : IDarkRiftSerializable
{
    public uint Frame;
    public PlayerSpawnData[] SpawnData;
    public PlayerDespawnData[] DespawnData;
    public PlayerUpdateData[] UpdateData;
    public PLayerHealthUpdateData[] HealthData;

    public GameUpdateData(uint frame, PlayerUpdateData[] updateData, PlayerSpawnData[] spawns, PlayerDespawnData[] despawns, PLayerHealthUpdateData[] healthDatas)
    {
        Frame = frame;
        UpdateData = updateData;
        DespawnData = despawns;
        SpawnData = spawns;
        HealthData = healthDatas;
    }
    public void Deserialize(DeserializeEvent e)
    {
        Frame = e.Reader.ReadUInt32();
        SpawnData = e.Reader.ReadSerializables<PlayerSpawnData>();
        DespawnData = e.Reader.ReadSerializables<PlayerDespawnData>();
        UpdateData = e.Reader.ReadSerializables<PlayerUpdateData>();
        HealthData = e.Reader.ReadSerializables<PLayerHealthUpdateData>();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(Frame);
        e.Writer.Write(SpawnData);
        e.Writer.Write(DespawnData);
        e.Writer.Write(UpdateData);
        e.Writer.Write(HealthData);
    }
}

public struct PLayerHealthUpdateData : IDarkRiftSerializable
{
    public ushort PlayerId;
    public byte Value;

    public PLayerHealthUpdateData(ushort id, byte val)
    {
        PlayerId = id;
        Value = val;
    }


    public void Deserialize(DeserializeEvent e)
    {
        PlayerId = e.Reader.ReadUInt16();
        Value = e.Reader.ReadByte();
    }

    public void Serialize(SerializeEvent e)
    {
        e.Writer.Write(PlayerId);
        e.Writer.Write(Value);
    }
}

public struct PlayerInputData : IDarkRiftSerializable
{
<<<<<<< HEAD
    public bool[] Keyinputs; //0 = w, 1 = a, 2 = s, 3 = d, 4 = space, 5 = leftKlick
    public Quaternion LookDirection;

    public PlayerInputData(bool[] keyInputs, Quaternion lookDirection)
    {
        Keyinputs = keyInputs;
        LookDirection = lookDirection;
=======
    public bool[] Keyinputs;
    public Quaternion LookdDirection;

    public uint Time;

    public PlayerInputData(bool[] keyInputs, Quaternion lookdirection, uint time)
    {
        Keyinputs = keyInputs;
        LookdDirection = lookdirection;
        Time = time;
>>>>>>> 1ed7e4d4b265e0c62eebc144d34ebedc9d1da0ff
    }

    public void Deserialize(DeserializeEvent e)
    {
<<<<<<< HEAD
        Keyinputs = e.Reader.ReadBooleans();
        LookDirection = new Quaternion(e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle(), e.Reader.ReadSingle());
=======
        Keyinputs = new bool[6];
        for (int i = 0; i < Keyinputs.Length; i++)
        {
            Keyinputs[i] = e.Reader.ReadBoolean();
        }

        LookdDirection = e.Reader.ReadQuaternion();
        if (Keyinputs[5])
        {
            Time = e.Reader.ReadUInt32();
        }

>>>>>>> 1ed7e4d4b265e0c62eebc144d34ebedc9d1da0ff
    }

    public void Serialize(SerializeEvent e)
    {
<<<<<<< HEAD
        e.Writer.Write(Keyinputs);
        e.Writer.Write(LookDirection.x);
        e.Writer.Write(LookDirection.y);
        e.Writer.Write(LookDirection.z);
        e.Writer.Write(LookDirection.w);
=======
        for (int i = 0; i < 6; i++)
        {
            e.Writer.Write(Keyinputs[i]);
        }
        e.Writer.WriteQuaternion(LookdDirection);
        if (Keyinputs[5])
        {
            e.Writer.Write(Time);
        }
>>>>>>> 1ed7e4d4b265e0c62eebc144d34ebedc9d1da0ff
    }
}
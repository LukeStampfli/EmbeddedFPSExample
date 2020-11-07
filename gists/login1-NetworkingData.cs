using DarkRift;
using UnityEngine;

public enum Tags
{
    LoginRequest = 0,
    LoginRequestAccepted = 1,
    LoginRequestDenied = 2,
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
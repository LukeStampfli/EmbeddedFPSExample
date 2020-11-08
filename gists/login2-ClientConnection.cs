using DarkRift;
using DarkRift.Server;

public class ClientConnection
{
    public string Name { get; }
    public IClient Client { get; }

    public ClientConnection(IClient client, LoginRequestData data)
    {
        Client = client;
        Name = data.Name;

        ServerManager.Instance.Players.Add(client.ID, this);
        ServerManager.Instance.PlayersByName.Add(Name, this);

        using (Message m = Message.Create((ushort)Tags.LoginRequestAccepted, new LoginInfoData(client.ID, new LobbyInfoData())))
        {
            client.SendMessage(m, SendMode.Reliable);
        }
    }
}
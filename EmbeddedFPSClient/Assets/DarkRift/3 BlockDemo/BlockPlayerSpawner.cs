using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using UnityEngine;

/// <summary>
///     Spawns players in the game.
/// </summary>
internal class BlockPlayerSpawner : MonoBehaviour
{
    /// <summary>
    ///     The client to communicate with the server via.
    /// </summary>
    [SerializeField]
    [Tooltip("The client to communicate with the server via.")]
    UnityClient client;

    /// <summary>
    ///     The block world in the scene.
    /// </summary>
    [SerializeField]
    [Tooltip("The block world in the scene.")]
    BlockWorld blockWorld;

    /// <summary>
    ///     The player object to spawn for our player.
    /// </summary>
    [SerializeField]
    [Tooltip("The player object to spawn.")]
    GameObject playerPrefab;

    /// <summary>
    ///     The player object to spawn for others' players.
    /// </summary>
    [SerializeField]
    [Tooltip("The network player object to spawn.")]
    GameObject networkPlayerPrefab;

    /// <summary>
    ///     The character manager for network players.
    /// </summary>
    [SerializeField]
    [Tooltip("The network player manager.")]
    BlockCharacterManager characterManager;

    void Awake()
    {
        if (client == null)
        {
            Debug.LogError("No client assigned to BlockPlayerSpawner component!");
            return;
        }

        client.MessageReceived += Client_MessageReceived;
        client.Disconnected += Client_Disconnected;
    }

    /// <summary>
    ///     Invoked when a message is received from the server.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void Client_MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            //Spawn or despawn the player as necessary.
            if (message.Tag == BlockTags.SpawnPlayer)
            {
                using (DarkRiftReader reader = message.GetReader())
                    SpawnPlayer(reader);
            }
            else if (message.Tag == BlockTags.DespawnSplayer)
            {
                using (DarkRiftReader reader = message.GetReader())
                    DespawnPlayer(reader);
            }
        }
    }

    /// <summary>
    ///     Called when we disconnect from the server.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void Client_Disconnected(object sender, DisconnectedEventArgs e)
    {
        //If we disconnect then we need to destroy everything!
        characterManager.RemoveAllCharacters();
        blockWorld.RemoveAllBlocks();
    }

    /// <summary>
    ///     Spawns a new player from the data received from the server.
    /// </summary>
    /// <param name="reader">The reader from the server.</param>
    void SpawnPlayer(DarkRiftReader reader)
    {
        //Extract the positions
        Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        Vector3 rotation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        //Extract their ID
        ushort id = reader.ReadUInt16();

        //If it's a player for us then spawn us our prefab and set it up
        if (id == client.ID)
        {
            GameObject o = Instantiate(
                playerPrefab,
                position,
                Quaternion.Euler(rotation)
            ) as GameObject;

            BlockCharacter character = o.GetComponent<BlockCharacter>();
            character.PlayerID = id;
            character.Setup(client, blockWorld);
        }
        //If it's for another player then spawn a network player and and to the manager. 
        else
        {
            GameObject o = Instantiate(
                networkPlayerPrefab,
                position,
                Quaternion.Euler(rotation)
            ) as GameObject;

            BlockNetworkCharacter character = o.GetComponent<BlockNetworkCharacter>();
            characterManager.AddCharacter(id, character);
        }
    }

    /// <summary>
    ///     Despawns and destroys a player from the data received from the server.
    /// </summary>
    /// <param name="reader">The reader from the server.</param>
    void DespawnPlayer(DarkRiftReader reader)
    {
        characterManager.RemoveCharacter(reader.ReadUInt16());
    }
}


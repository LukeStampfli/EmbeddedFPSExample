using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
///     Handles the synchronization of other player's characters.
/// </summary>
internal class BlockCharacterManager : MonoBehaviour
{
    /// <summary>
    ///     The unit client we communicate via.
    /// </summary>
    [SerializeField]
    [Tooltip("The client to communicate with the server via.")]
    UnityClient client;

    /// <summary>
    ///     The characters we are managing.
    /// </summary>
    Dictionary<ushort, BlockNetworkCharacter> characters = new Dictionary<ushort, BlockNetworkCharacter>();

    void Awake()
    {
        if (client == null)
        {
            Debug.LogError("No client assigned to BlockPlayerSpawner component!");
            return;
        }

        client.MessageReceived += Client_MessageReceived;
    }

    /// <summary>
    ///     Called when a message is received from the server.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void Client_MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            //Check the tag
            if (message.Tag == BlockTags.Movement)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    //Read message
                    Vector3 newPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    Vector3 newRotation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    ushort id = reader.ReadUInt16();

                    //Update characters to move to new positions
                    characters[id].NewPosition = newPosition;
                    characters[id].NewRotation = newRotation;
                }
            }
        }
    }

    /// <summary>
    ///     Adds a character to the list of those we're managing.
    /// </summary>
    /// <param name="id">The ID of the owning player.</param>
    /// <param name="character">The character to synchronize.</param>
    public void AddCharacter(ushort id, BlockNetworkCharacter character)
    {
        characters.Add(id, character);
    }

    /// <summary>
    ///     Removes a character from the list of those we're managing.
    /// </summary>
    /// <param name="id">The ID of the owning player.</param>
    public void RemoveCharacter(ushort id)
    {
        Destroy(characters[id].gameObject);
        characters.Remove(id);
    }

    /// <summary>
    ///     Removes all characters that are being managded.
    /// </summary>
    internal void RemoveAllCharacters()
    {
        foreach (BlockNetworkCharacter character in characters.Values)
            Destroy(character.gameObject);

        characters.Clear();
    }
}


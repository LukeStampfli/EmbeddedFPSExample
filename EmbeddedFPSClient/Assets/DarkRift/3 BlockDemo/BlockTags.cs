using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
///     The tags used for messages between the server and client.
/// </summary>
static class BlockTags
{
    public static readonly ushort SpawnPlayer       = 0;
    public static readonly ushort DespawnSplayer    = 1;
    public static readonly ushort Movement          = 2;
    public static readonly ushort PlaceBlock        = 3;
    public static readonly ushort DestroyBlock      = 4;
}

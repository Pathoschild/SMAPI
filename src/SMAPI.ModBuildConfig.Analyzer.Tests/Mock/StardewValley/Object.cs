// ReSharper disable CheckNamespace, InconsistentNaming -- matches Stardew Valley's code
using Netcode;

namespace StardewValley;

/// <summary>A simplified version of Stardew Valley's <c>StardewValley.Object</c> class for unit testing.</summary>
public class Object : Item
{
    /// <summary>A net int field with an equivalent non-net property.</summary>
    public NetInt type = new() { Value = 42 };
}

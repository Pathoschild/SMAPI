// ReSharper disable CheckNamespace, InconsistentNaming -- matches Stardew Valley's code
// ReSharper disable UnusedMember.Global -- used dynamically for unit tests
using System.Collections.Generic;

namespace StardewValley;

/// <summary>A simplified version of Stardew Valley's <c>StardewValley.Farmer</c> class for unit testing.</summary>
internal class Farmer
{
    /// <summary>A sample field which should be replaced with a different property.</summary>
    public readonly IDictionary<string, int[]> friendships = new Dictionary<string, int[]>();
}

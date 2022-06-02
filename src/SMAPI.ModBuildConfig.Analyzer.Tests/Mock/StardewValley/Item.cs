// ReSharper disable CheckNamespace, InconsistentNaming -- matches Stardew Valley's code
// ReSharper disable UnusedMember.Global -- used dynamically for unit tests
using Netcode;

namespace StardewValley;

/// <summary>A simplified version of Stardew Valley's <c>StardewValley.Item</c> class for unit testing.</summary>
public class Item
{
    /// <summary>A net int field with an equivalent non-net <c>Category</c> property.</summary>
    public readonly NetInt category = new() { Value = 42 };

    /// <summary>A generic net int field with no equivalent non-net property.</summary>
    public readonly NetInt netIntField = new() { Value = 42 };

    /// <summary>A generic net ref field with no equivalent non-net property.</summary>
    public readonly NetRef<object> netRefField = new();

    /// <summary>A generic net int property with no equivalent non-net property.</summary>
    public NetInt netIntProperty = new() { Value = 42 };

    /// <summary>A generic net ref property with no equivalent non-net property.</summary>
    public NetRef<object> netRefProperty { get; } = new();

    /// <summary>A sample net list.</summary>
    public readonly NetList<int> netList = new();

    /// <summary>A sample net object list.</summary>
    public readonly NetObjectList<int> netObjectList = new();

    /// <summary>A sample net collection.</summary>
    public readonly NetCollection<int> netCollection = new();
}

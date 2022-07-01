namespace StardewModdingAPI.Events;

/// <summary>The event priorities for method handlers.</summary>
public enum EventPriority
{
    /// <summary>Low priority.</summary>
    Low = -1000,

    /// <summary>The default priority.</summary>
    Normal = 0,

    /// <summary>High priority.</summary>
    High = 1000
}

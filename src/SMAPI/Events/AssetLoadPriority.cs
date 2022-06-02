namespace StardewModdingAPI.Events;

/// <summary>The priority for an asset load when multiple apply for the same asset.</summary>
/// <remarks>If multiple non-<see cref="Exclusive"/> loads have the same priority, the one registered first will be selected. You can also specify arbitrary intermediate values, like <c>AssetLoadPriority.Low + 5</c>.</remarks>
public enum AssetLoadPriority
{
    /// <summary>This load is optional and can safely be skipped if there are higher-priority loads.</summary>
    Low = -1000,

    /// <summary>The load is optional and can safely be skipped if there are higher-priority loads, but it should still be preferred over any <see cref="Low"/>-priority loads.</summary>
    Medium = 0,

    /// <summary>The load is optional and can safely be skipped if there are higher-priority loads, but it should still be preferred over any <see cref="Low"/>- or <see cref="Medium"/>-priority loads.</summary>
    High = 1000,

    /// <summary>The load is not optional. If more than one loader has <see cref="Exclusive"/> priority, SMAPI will log an error and ignore all of them.</summary>
    Exclusive = int.MaxValue
}

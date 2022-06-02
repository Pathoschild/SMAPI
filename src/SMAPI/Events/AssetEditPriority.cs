namespace StardewModdingAPI.Events;

/// <summary>The priority for an asset edit when multiple apply for the same asset.</summary>
/// <remarks>You can also specify arbitrary intermediate values, like <c>AssetLoadPriority.Low + 5</c>.</remarks>
public enum AssetEditPriority
{
    /// <summary>This edit should be applied before (i.e. 'under') <see cref="Default"/> edits.</summary>
    Early = -1000,

    /// <summary>The default priority.</summary>
    Default = 0,

    /// <summary>This edit should be applied after (i.e. 'on top of') <see cref="Default"/> edits.</summary>
    Late = 1000
}

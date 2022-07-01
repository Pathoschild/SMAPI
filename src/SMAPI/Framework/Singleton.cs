namespace StardewModdingAPI.Framework;

/// <summary>Provides singleton instances of a given type.</summary>
/// <typeparam name="T">The instance type.</typeparam>
internal static class Singleton<T> where T : new()
{
    /// <summary>The singleton instance.</summary>
    public static T Instance { get; } = new();
}

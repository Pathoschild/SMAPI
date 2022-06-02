using System.Collections.Generic;

namespace StardewModdingAPI;

/// <summary>Provides an API for fetching metadata about loaded mods.</summary>
public interface IModRegistry : IModLinked
{
    /// <summary>Get metadata for all loaded mods.</summary>
    IEnumerable<IModInfo> GetAll();

    /// <summary>Get metadata for a loaded mod.</summary>
    /// <param name="uniqueID">The mod's unique ID.</param>
    /// <returns>Returns the matching mod's metadata, or <c>null</c> if not found.</returns>
    IModInfo? Get(string uniqueID);

    /// <summary>Get whether a mod has been loaded.</summary>
    /// <param name="uniqueID">The mod's unique ID.</param>
    bool IsLoaded(string uniqueID);

    /// <summary>Get the API provided by a mod, or <c>null</c> if it has none. This signature requires using the <see cref="IModHelper.Reflection"/> API to access the API's properties and methods.</summary>
    /// <param name="uniqueID">The mod's unique ID.</param>
    object? GetApi(string uniqueID);

    /// <summary>Get the API provided by a mod, mapped to a given interface which specifies the expected properties and methods. If the mod has no API or it's not compatible with the given interface, get <c>null</c>.</summary>
    /// <typeparam name="TInterface">The interface which matches the properties and methods you intend to access.</typeparam>
    /// <param name="uniqueID">The mod's unique ID.</param>
    TInterface? GetApi<TInterface>(string uniqueID)
        where TInterface : class;
}

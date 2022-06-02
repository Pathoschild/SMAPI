using System.Collections.Generic;

namespace StardewModdingAPI;

/// <summary>Encapsulates access and changes to dictionary content being read from a data file.</summary>
public interface IAssetDataForDictionary<TKey, TValue> : IAssetData<IDictionary<TKey, TValue>> { }

using System;

namespace StardewModdingAPI;

/// <summary>Provides an API for reading and storing local mod data.</summary>
public interface IDataHelper
{
    /*********
    ** Public methods
    *********/
    /****
    ** JSON file
    ****/
    /// <summary>Read data from a JSON file in the mod's folder.</summary>
    /// <typeparam name="TModel">The model type. This should be a plain class that has public properties for the data you want. The properties can be complex types.</typeparam>
    /// <param name="path">The file path relative to the mod folder.</param>
    /// <returns>Returns the deserialized model, or <c>null</c> if the file doesn't exist or is empty.</returns>
    /// <exception cref="InvalidOperationException">The <paramref name="path"/> is not relative or contains directory climbing (../).</exception>
    TModel? ReadJsonFile<TModel>(string path)
        where TModel : class;

    /// <summary>Save data to a JSON file in the mod's folder.</summary>
    /// <typeparam name="TModel">The model type. This should be a plain class that has public properties for the data you want. The properties can be complex types.</typeparam>
    /// <param name="path">The file path relative to the mod folder.</param>
    /// <param name="data">The arbitrary data to save, or <c>null</c> to delete the file.</param>
    /// <exception cref="InvalidOperationException">The <paramref name="path"/> is not relative or contains directory climbing (../).</exception>
    void WriteJsonFile<TModel>(string path, TModel? data)
        where TModel : class;

    /****
    ** Save file
    ****/
    /// <summary>Read arbitrary data stored in the current save slot. This is only possible if a save has been loaded.</summary>
    /// <typeparam name="TModel">The model type. This should be a plain class that has public properties for the data you want. The properties can be complex types.</typeparam>
    /// <param name="key">The unique key identifying the data.</param>
    /// <returns>Returns the parsed data, or <c>null</c> if the entry doesn't exist or is empty.</returns>
    /// <exception cref="InvalidOperationException">The player hasn't loaded a save file yet or isn't the main player.</exception>
    TModel? ReadSaveData<TModel>(string key)
        where TModel : class;

    /// <summary>Save arbitrary data to the current save slot. This is only possible if a save has been loaded, and the data will be lost if the player exits without saving the current day.</summary>
    /// <typeparam name="TModel">The model type. This should be a plain class that has public properties for the data you want. The properties can be complex types.</typeparam>
    /// <param name="key">The unique key identifying the data.</param>
    /// <param name="data">The arbitrary data to save, or <c>null</c> to remove the entry.</param>
    /// <exception cref="InvalidOperationException">The player hasn't loaded a save file yet or isn't the main player.</exception>
    void WriteSaveData<TModel>(string key, TModel? data)
        where TModel : class;


    /****
    ** Global app data
    ****/
    /// <summary>Read arbitrary data stored on the local computer, synchronised by GOG/Steam if applicable.</summary>
    /// <typeparam name="TModel">The model type. This should be a plain class that has public properties for the data you want. The properties can be complex types.</typeparam>
    /// <param name="key">The unique key identifying the data.</param>
    /// <returns>Returns the parsed data, or <c>null</c> if the entry doesn't exist or is empty.</returns>
    TModel? ReadGlobalData<TModel>(string key)
        where TModel : class;

    /// <summary>Save arbitrary data to the local computer, synchronised by GOG/Steam if applicable.</summary>
    /// <typeparam name="TModel">The model type. This should be a plain class that has public properties for the data you want. The properties can be complex types.</typeparam>
    /// <param name="key">The unique key identifying the data.</param>
    /// <param name="data">The arbitrary data to save, or <c>null</c> to delete the file.</param>
    void WriteGlobalData<TModel>(string key, TModel? data)
        where TModel : class;
}

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley;

namespace StardewModdingAPI.Framework.ModHelpers;

/// <summary>Provides an API for reading and storing local mod data.</summary>
internal class DataHelper : BaseHelper, IDataHelper
{
    /*********
    ** Fields
    *********/
    /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
    private readonly JsonHelper JsonHelper;

    /// <summary>The absolute path to the mod folder.</summary>
    private readonly string ModFolderPath;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod using this instance.</param>
    /// <param name="modFolderPath">The absolute path to the mod folder.</param>
    /// <param name="jsonHelper">The absolute path to the mod folder.</param>
    public DataHelper(IModMetadata mod, string modFolderPath, JsonHelper jsonHelper)
        : base(mod)
    {
        this.ModFolderPath = modFolderPath;
        this.JsonHelper = jsonHelper;
    }

    /****
    ** JSON file
    ****/
    /// <inheritdoc />
    public TModel? ReadJsonFile<TModel>(string path)
        where TModel : class
    {
        if (!PathUtilities.IsSafeRelativePath(path))
            throw new InvalidOperationException($"You must call {nameof(IModHelper.Data)}.{nameof(this.ReadJsonFile)} with a relative path.");

        path = Path.Combine(this.ModFolderPath, PathUtilities.NormalizePath(path));
        return this.JsonHelper.ReadJsonFileIfExists(path, out TModel? data)
            ? data
            : null;
    }

    /// <inheritdoc />
    public void WriteJsonFile<TModel>(string path, TModel? data)
        where TModel : class
    {
        if (!PathUtilities.IsSafeRelativePath(path))
            throw new InvalidOperationException($"You must call {nameof(IMod.Helper)}.{nameof(IModHelper.Data)}.{nameof(this.WriteJsonFile)} with a relative path (without directory climbing).");

        path = Path.Combine(this.ModFolderPath, PathUtilities.NormalizePath(path));

        if (data != null)
            this.JsonHelper.WriteJsonFile(path, data);
        else
            File.Delete(path);
    }

    /****
    ** Save file
    ****/
    /// <inheritdoc />
    public TModel? ReadSaveData<TModel>(string key)
        where TModel : class
    {
        if (Context.LoadStage == LoadStage.None)
            throw new InvalidOperationException($"Can't use {nameof(IMod.Helper)}.{nameof(IModHelper.Data)}.{nameof(this.ReadSaveData)} when a save file isn't loaded.");
        if (!Context.IsOnHostComputer)
            throw new InvalidOperationException($"Can't use {nameof(IMod.Helper)}.{nameof(IModHelper.Data)}.{nameof(this.ReadSaveData)} when connected to a remote host. (Save files are stored on the main player's computer.)");


        string internalKey = this.GetSaveFileKey(key);
        foreach (IDictionary<string, string> dataField in this.GetDataFields(Context.LoadStage))
        {
            if (dataField.TryGetValue(internalKey, out string? value))
                return this.JsonHelper.Deserialize<TModel>(value);
        }
        return null;
    }

    /// <inheritdoc />
    public void WriteSaveData<TModel>(string key, TModel? model)
        where TModel : class
    {
        if (Context.LoadStage == LoadStage.None)
            throw new InvalidOperationException($"Can't use {nameof(IMod.Helper)}.{nameof(IModHelper.Data)}.{nameof(this.WriteSaveData)} when a save file isn't loaded.");
        if (!Context.IsOnHostComputer)
            throw new InvalidOperationException($"Can't use {nameof(IMod.Helper)}.{nameof(IModHelper.Data)}.{nameof(this.WriteSaveData)} when connected to a remote host. (Save files are stored on the main player's computer.)");

        string internalKey = this.GetSaveFileKey(key);
        string? data = model != null
            ? this.JsonHelper.Serialize(model, Formatting.None)
            : null;

        foreach (IDictionary<string, string> dataField in this.GetDataFields(Context.LoadStage))
        {
            if (data != null)
                dataField[internalKey] = data;
            else
                dataField.Remove(internalKey);
        }
    }

    /****
    ** Global app data
    ****/
    /// <inheritdoc />
    public TModel? ReadGlobalData<TModel>(string key)
        where TModel : class
    {
        string path = this.GetGlobalDataPath(key);
        return this.JsonHelper.ReadJsonFileIfExists(path, out TModel? data)
            ? data
            : null;
    }

    /// <inheritdoc />
    public void WriteGlobalData<TModel>(string key, TModel? data)
        where TModel : class
    {
        string path = this.GetGlobalDataPath(key);
        if (data != null)
            this.JsonHelper.WriteJsonFile(path, data);
        else
            File.Delete(path);
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Get the unique key for a save file data entry.</summary>
    /// <param name="key">The unique key identifying the data.</param>
    private string GetSaveFileKey(string key)
    {
        this.AssertSlug(key, nameof(key));
        return $"smapi/mod-data/{this.ModID}/{key}".ToLower();
    }

    /// <summary>Get the data fields to read/write for save data.</summary>
    /// <param name="stage">The current load stage.</param>
    private IEnumerable<IDictionary<string, string>> GetDataFields(LoadStage stage)
    {
        if (stage == LoadStage.None)
            yield break;

        yield return Game1.CustomData;
        if (SaveGame.loaded != null)
            yield return SaveGame.loaded.CustomData;
    }

    /// <summary>Get the absolute path for a global data file.</summary>
    /// <param name="key">The unique key identifying the data.</param>
    private string GetGlobalDataPath(string key)
    {
        this.AssertSlug(key, nameof(key));
        return Path.Combine(Constants.DataPath, ".smapi", "mod-data", this.ModID.ToLower(), $"{key}.json".ToLower());
    }

    /// <summary>Assert that a key contains only characters that are safe in all contexts.</summary>
    /// <param name="key">The key to check.</param>
    /// <param name="paramName">The argument name for any assertion error.</param>
    private void AssertSlug(string key, string paramName)
    {
        if (!PathUtilities.IsSlug(key))
            throw new ArgumentException("The data key is invalid (keys must only contain letters, numbers, underscores, periods, or hyphens).", paramName);
    }
}

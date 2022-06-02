namespace StardewModdingAPI.Web.Framework.LogParsing.Models;

/// <summary>The different sections of a log.</summary>
public enum LogSection
{
    /// <summary>The list of mods the user has.</summary>
    ModsList,

    /// <summary>The list of content packs the user has.</summary>
    ContentPackList,

    /// <summary>The list of mod updates SMAPI has found.</summary>
    ModUpdateList
}

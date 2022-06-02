using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;
using StardewModdingAPI.Toolkit.Framework.GameScanning;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Framework.ModScanning;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Toolkit.Serialization;

namespace StardewModdingAPI.Toolkit;

/// <summary>A convenience wrapper for the various tools.</summary>
public class ModToolkit
{
    /*********
    ** Fields
    *********/
    /// <summary>The default HTTP user agent for the toolkit.</summary>
    private readonly string UserAgent;

    /// <summary>Maps vendor keys (like <c>Nexus</c>) to their mod URL template (where <c>{0}</c> is the mod ID). This doesn't affect update checks, which defer to the remote web API.</summary>
    private readonly Dictionary<ModSiteKey, string> VendorModUrls = new()
    {
        [ModSiteKey.Chucklefish] = "https://community.playstarbound.com/resources/{0}",
        [ModSiteKey.GitHub] = "https://github.com/{0}/releases",
        [ModSiteKey.Nexus] = "https://www.nexusmods.com/stardewvalley/mods/{0}"
    };


    /*********
    ** Accessors
    *********/
    /// <summary>Encapsulates SMAPI's JSON parsing.</summary>
    public JsonHelper JsonHelper { get; } = new();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public ModToolkit()
    {
        ISemanticVersion version = new SemanticVersion(this.GetType().Assembly.GetName().Version!);
        this.UserAgent = $"SMAPI Mod Handler Toolkit/{version}";
    }

    /// <summary>Find valid Stardew Valley install folders.</summary>
    /// <remarks>This checks default game locations, and on Windows checks the Windows registry for GOG/Steam install data. A folder is considered 'valid' if it contains the Stardew Valley executable for the current OS.</remarks>
    public IEnumerable<DirectoryInfo> GetGameFolders()
    {
        return new GameScanner().Scan();
    }

    /// <summary>Extract mod metadata from the wiki compatibility list.</summary>
    public async Task<WikiModList> GetWikiCompatibilityListAsync()
    {
        using WikiClient client = new(this.UserAgent);
        return await client.FetchModsAsync();
    }

    /// <summary>Get SMAPI's internal mod database.</summary>
    /// <param name="metadataPath">The file path for the SMAPI metadata file.</param>
    public ModDatabase GetModDatabase(string metadataPath)
    {
        MetadataModel metadata = JsonConvert.DeserializeObject<MetadataModel>(File.ReadAllText(metadataPath));
        ModDataRecord[] records = metadata.ModData.Select(pair => new ModDataRecord(pair.Key, pair.Value)).ToArray();
        return new ModDatabase(records, this.GetUpdateUrl);
    }

    /// <summary>Extract information about all mods in the given folder.</summary>
    /// <param name="rootPath">The root folder containing mods.</param>
    /// <param name="useCaseInsensitiveFilePaths">Whether to match file paths case-insensitively, even on Linux.</param>
    public IEnumerable<ModFolder> GetModFolders(string rootPath, bool useCaseInsensitiveFilePaths)
    {
        return new ModScanner(this.JsonHelper).GetModFolders(rootPath, useCaseInsensitiveFilePaths);
    }

    /// <summary>Extract information about all mods in the given folder.</summary>
    /// <param name="rootPath">The root folder containing mods. Only the <paramref name="modPath"/> will be searched, but this field allows it to be treated as a potential mod folder of its own.</param>
    /// <param name="modPath">The mod path to search.</param>
    /// <param name="useCaseInsensitiveFilePaths">Whether to match file paths case-insensitively, even on Linux.</param>
    public IEnumerable<ModFolder> GetModFolders(string rootPath, string modPath, bool useCaseInsensitiveFilePaths)
    {
        return new ModScanner(this.JsonHelper).GetModFolders(rootPath, modPath, useCaseInsensitiveFilePaths);
    }

    /// <summary>Get an update URL for an update key (if valid).</summary>
    /// <param name="updateKey">The update key.</param>
    public string? GetUpdateUrl(string updateKey)
    {
        UpdateKey parsed = UpdateKey.Parse(updateKey);
        if (!parsed.LooksValid)
            return null;

        if (this.VendorModUrls.TryGetValue(parsed.Site, out string? urlTemplate))
            return string.Format(urlTemplate, parsed.ID);

        return null;
    }
}

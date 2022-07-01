using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Toolkit.Framework.Clients.WebApi;

/// <summary>Provides methods for interacting with the SMAPI web API.</summary>
public class WebApiClient : IDisposable
{
    /*********
    ** Fields
    *********/
    /// <summary>The API version number.</summary>
    private readonly ISemanticVersion Version;

    /// <summary>The underlying HTTP client.</summary>
    private readonly IClient Client;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="baseUrl">The base URL for the web API.</param>
    /// <param name="version">The web API version.</param>
    public WebApiClient(string baseUrl, ISemanticVersion version)
    {
        this.Version = version;
        this.Client = new FluentClient(baseUrl)
            .SetUserAgent($"SMAPI/{version}");

        this.Client.Formatters.JsonFormatter.SerializerSettings = JsonHelper.CreateDefaultSettings();
    }

    /// <summary>Get metadata about a set of mods from the web API.</summary>
    /// <param name="mods">The mod keys for which to fetch the latest version.</param>
    /// <param name="apiVersion">The SMAPI version installed by the player. If this is null, the API won't provide a recommended update.</param>
    /// <param name="gameVersion">The Stardew Valley version installed by the player.</param>
    /// <param name="platform">The OS on which the player plays.</param>
    /// <param name="includeExtendedMetadata">Whether to include extended metadata for each mod.</param>
    public async Task<IDictionary<string, ModEntryModel>> GetModInfoAsync(ModSearchEntryModel[] mods, ISemanticVersion apiVersion, ISemanticVersion gameVersion, Platform platform, bool includeExtendedMetadata = false)
    {
        ModEntryModel[] result = await this.Client
            .PostAsync(
                $"v{this.Version}/mods",
                new ModSearchModel(mods, apiVersion, gameVersion, platform, includeExtendedMetadata)
            )
            .As<ModEntryModel[]>();

        return result.ToDictionary(p => p.ID);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.Client.Dispose();
    }
}

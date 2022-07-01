using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Web.Framework.Clients.Nexus;

/// <summary>A client for the Nexus website which does nothing, used for local development.</summary>
internal class DisabledNexusClient : INexusClient
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public ModSiteKey SiteKey => ModSiteKey.Nexus;


    /*********
    ** Public methods
    *********/
    /// <summary>Get update check info about a mod.</summary>
    /// <param name="id">The mod ID.</param>
    public Task<IModPage?> GetModData(string id)
    {
        return Task.FromResult<IModPage?>(
            new GenericModPage(ModSiteKey.Nexus, id).SetError(RemoteModStatus.TemporaryError, "The Nexus client is currently disabled due to the configuration.")
        );
    }

    /// <inheritdoc />
    public void Dispose() { }
}

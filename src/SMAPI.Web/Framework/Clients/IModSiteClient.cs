using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Web.Framework.Clients;

/// <summary>A client for fetching update check info from a mod site.</summary>
internal interface IModSiteClient
{
    /*********
    ** Accessors
    *********/
    /// <summary>The unique key for the mod site.</summary>
    public ModSiteKey SiteKey { get; }


    /*********
    ** Methods
    *********/
    /// <summary>Get update check info about a mod.</summary>
    /// <param name="id">The mod ID.</param>
    Task<IModPage?> GetModData(string id);
}

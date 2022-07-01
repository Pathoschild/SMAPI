namespace StardewModdingAPI.Web.Framework.Clients.CurseForge.ResponseModels;

/// <summary>A list of links for a mod.</summary>
/// <param name="WebsiteUrl">The URL for the CurseForge mod page.</param>
/// <param name="SourceUrl">The URL for the mod's source code, if any.</param>
public record ModLinksModel(string WebsiteUrl, string? SourceUrl);

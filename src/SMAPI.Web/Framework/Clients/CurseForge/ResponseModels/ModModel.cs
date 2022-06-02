namespace StardewModdingAPI.Web.Framework.Clients.CurseForge.ResponseModels;

/// <summary>A mod from the CurseForge API.</summary>
/// <param name="Id">The mod's unique ID on CurseForge.</param>
/// <param name="Name">The mod name.</param>
/// <param name="LatestFiles">The available file downloads.</param>
/// <param name="Links">The URLs for this mod.</param>
public record ModModel(int Id, string Name, ModFileModel[] LatestFiles, ModLinksModel Links);

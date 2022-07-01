using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.CurseForge.ResponseModels;

/// <summary>A response from the CurseForge API.</summary>
/// <param name="Data">The data returned by the API.</param>
public record ResponseModel<TData>(TData Data);

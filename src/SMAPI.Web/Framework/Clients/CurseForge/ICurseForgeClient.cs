using System;

namespace StardewModdingAPI.Web.Framework.Clients.CurseForge;

/// <summary>An HTTP client for fetching mod metadata from the CurseForge API.</summary>
internal interface ICurseForgeClient : IModSiteClient, IDisposable { }

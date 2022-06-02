using System;

namespace StardewModdingAPI.Web.Framework.Clients.Nexus;

/// <summary>An HTTP client for fetching mod metadata from Nexus Mods.</summary>
internal interface INexusClient : IModSiteClient, IDisposable { }

using System;

namespace StardewModdingAPI.Web.Framework.Clients.ModDrop;

/// <summary>An HTTP client for fetching mod metadata from the ModDrop API.</summary>
internal interface IModDropClient : IDisposable, IModSiteClient { }

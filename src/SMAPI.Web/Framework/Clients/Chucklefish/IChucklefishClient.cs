using System;

namespace StardewModdingAPI.Web.Framework.Clients.Chucklefish;

/// <summary>An HTTP client for fetching mod metadata from the Chucklefish mod site.</summary>
internal interface IChucklefishClient : IModSiteClient, IDisposable { }

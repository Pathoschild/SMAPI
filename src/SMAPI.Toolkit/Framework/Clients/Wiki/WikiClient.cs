using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

/// <summary>An HTTP client for fetching mod metadata from the wiki.</summary>
public class WikiClient : IDisposable
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying HTTP client.</summary>
    private readonly IClient Client;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="userAgent">The user agent for the wiki API.</param>
    /// <param name="baseUrl">The base URL for the wiki API.</param>
    public WikiClient(string userAgent, string baseUrl = "https://stardewvalleywiki.com/mediawiki/api.php")
    {
        this.Client = new FluentClient(baseUrl).SetUserAgent(userAgent);
    }

    /// <summary>Fetch mods from the compatibility list.</summary>
    public async Task<WikiModList> FetchModsAsync()
    {
        // fetch HTML
        ResponseModel response = await this.Client
            .GetAsync("")
            .WithArguments(new
            {
                action = "parse",
                page = "Modding:Mod_compatibility",
                format = "json"
            })
            .As<ResponseModel>();
        string html = response.Parse.Text["*"];

        // parse HTML
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // fetch game versions
        string? stableVersion = doc.DocumentNode.SelectSingleNode("//div[@class='game-stable-version']")?.InnerText;
        string? betaVersion = doc.DocumentNode.SelectSingleNode("//div[@class='game-beta-version']")?.InnerText;
        if (betaVersion == stableVersion)
            betaVersion = null;

        // parse mod data overrides
        Dictionary<string, WikiDataOverrideEntry> overrides = new Dictionary<string, WikiDataOverrideEntry>(StringComparer.OrdinalIgnoreCase);
        {
            HtmlNodeCollection modNodes = doc.DocumentNode.SelectNodes("//table[@id='mod-overrides-list']//tr[@class='mod']");
            if (modNodes == null)
                throw new InvalidOperationException("Can't parse wiki compatibility list, no mod data overrides section found.");

            foreach (WikiDataOverrideEntry entry in this.ParseOverrideEntries(modNodes))
            {
                if (entry.Ids.Any() != true || !entry.HasChanges)
                    continue;

                foreach (string id in entry.Ids)
                    overrides[id] = entry;
            }
        }

        // parse mod entries
        WikiModEntry[] mods;
        {
            HtmlNodeCollection modNodes = doc.DocumentNode.SelectNodes("//table[@id='mod-list']//tr[@class='mod']");
            if (modNodes == null)
                throw new InvalidOperationException("Can't parse wiki compatibility list, no mods found.");
            mods = this.ParseModEntries(modNodes, overrides).ToArray();
        }

        // build model
        return new WikiModList(
            stableVersion: stableVersion,
            betaVersion: betaVersion,
            mods: mods
        );
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        this.Client.Dispose();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Parse valid mod compatibility entries.</summary>
    /// <param name="nodes">The HTML compatibility entries.</param>
    /// <param name="overridesById">The mod data overrides to apply, if any.</param>
    private IEnumerable<WikiModEntry> ParseModEntries(IEnumerable<HtmlNode> nodes, IDictionary<string, WikiDataOverrideEntry> overridesById)
    {
        foreach (HtmlNode node in nodes)
        {
            // extract fields
            string[] names = this.GetAttributeAsCsv(node, "data-name");
            string[] authors = this.GetAttributeAsCsv(node, "data-author");
            string[] ids = this.GetAttributeAsCsv(node, "data-id");
            string[] warnings = this.GetAttributeAsCsv(node, "data-warnings");
            int? nexusID = this.GetAttributeAsNullableInt(node, "data-nexus-id");
            int? chucklefishID = this.GetAttributeAsNullableInt(node, "data-cf-id");
            int? curseForgeID = this.GetAttributeAsNullableInt(node, "data-curseforge-id");
            string? curseForgeKey = this.GetAttribute(node, "data-curseforge-key");
            int? modDropID = this.GetAttributeAsNullableInt(node, "data-moddrop-id");
            string? githubRepo = this.GetAttribute(node, "data-github");
            string? customSourceUrl = this.GetAttribute(node, "data-custom-source");
            string? customUrl = this.GetAttribute(node, "data-url");
            string? anchor = this.GetAttribute(node, "id");
            string? contentPackFor = this.GetAttribute(node, "data-content-pack-for");
            string? devNote = this.GetAttribute(node, "data-dev-note");
            string? pullRequestUrl = this.GetAttribute(node, "data-pr");

            // parse stable compatibility
            WikiCompatibilityInfo compatibility = new(
                status: this.GetAttributeAsEnum<WikiCompatibilityStatus>(node, "data-status") ?? WikiCompatibilityStatus.Ok,
                brokeIn: this.GetAttribute(node, "data-broke-in"),
                unofficialVersion: this.GetAttributeAsSemanticVersion(node, "data-unofficial-version"),
                unofficialUrl: this.GetAttribute(node, "data-unofficial-url"),
                summary: this.GetInnerHtml(node, "mod-summary")?.Trim()
            );

            // parse beta compatibility
            WikiCompatibilityInfo? betaCompatibility = null;
            {
                WikiCompatibilityStatus? betaStatus = this.GetAttributeAsEnum<WikiCompatibilityStatus>(node, "data-beta-status");
                if (betaStatus.HasValue)
                {
                    betaCompatibility = new WikiCompatibilityInfo(
                        status: betaStatus.Value,
                        brokeIn: this.GetAttribute(node, "data-beta-broke-in"),
                        unofficialVersion: this.GetAttributeAsSemanticVersion(node, "data-beta-unofficial-version"),
                        unofficialUrl: this.GetAttribute(node, "data-beta-unofficial-url"),
                        summary: this.GetInnerHtml(node, "mod-beta-summary")
                    );
                }
            }

            // find data overrides
            WikiDataOverrideEntry? overrides = ids
                .Select(id => overridesById.TryGetValue(id, out overrides) ? overrides : null)
                .FirstOrDefault(p => p != null);

            // yield model
            yield return new WikiModEntry(
                id: ids,
                name: names,
                author: authors,
                nexusId: nexusID,
                chucklefishId: chucklefishID,
                curseForgeId: curseForgeID,
                curseForgeKey: curseForgeKey,
                modDropId: modDropID,
                githubRepo: githubRepo,
                customSourceUrl: customSourceUrl,
                customUrl: customUrl,
                contentPackFor: contentPackFor,
                compatibility: compatibility,
                betaCompatibility: betaCompatibility,
                warnings: warnings,
                pullRequestUrl: pullRequestUrl,
                devNote: devNote,
                overrides: overrides,
                anchor: anchor
            );
        }
    }

    /// <summary>Parse valid mod data override entries.</summary>
    /// <param name="nodes">The HTML mod data override entries.</param>
    private IEnumerable<WikiDataOverrideEntry> ParseOverrideEntries(IEnumerable<HtmlNode> nodes)
    {
        foreach (HtmlNode node in nodes)
        {
            yield return new WikiDataOverrideEntry
            {
                Ids = this.GetAttributeAsCsv(node, "data-id"),
                ChangeLocalVersions = this.GetAttributeAsChangeDescriptor(node, "data-local-version",
                    raw => SemanticVersion.TryParse(raw, out ISemanticVersion? version) ? version.ToString() : raw
                ),
                ChangeRemoteVersions = this.GetAttributeAsChangeDescriptor(node, "data-remote-version",
                    raw => SemanticVersion.TryParse(raw, out ISemanticVersion? version) ? version.ToString() : raw
                ),

                ChangeUpdateKeys = this.GetAttributeAsChangeDescriptor(node, "data-update-keys",
                    raw => UpdateKey.TryParse(raw, out UpdateKey key) ? key.ToString() : raw
                )
            };
        }
    }

    /// <summary>Get an attribute value.</summary>
    /// <param name="element">The element whose attributes to read.</param>
    /// <param name="name">The attribute name.</param>
    private string? GetAttribute(HtmlNode element, string name)
    {
        string value = element.GetAttributeValue(name, null);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return WebUtility.HtmlDecode(value);
    }

    /// <summary>Get an attribute value and parse it as a change descriptor.</summary>
    /// <param name="element">The element whose attributes to read.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="formatValue">Format an raw entry value when applying changes.</param>
    private ChangeDescriptor? GetAttributeAsChangeDescriptor(HtmlNode element, string name, Func<string, string> formatValue)
    {
        string? raw = this.GetAttribute(element, name);
        return raw != null
            ? ChangeDescriptor.Parse(raw, out _, formatValue)
            : null;
    }

    /// <summary>Get an attribute value and parse it as a comma-delimited list of strings.</summary>
    /// <param name="element">The element whose attributes to read.</param>
    /// <param name="name">The attribute name.</param>
    private string[] GetAttributeAsCsv(HtmlNode element, string name)
    {
        string? raw = this.GetAttribute(element, name);
        return !string.IsNullOrWhiteSpace(raw)
            ? raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray()
            : Array.Empty<string>();
    }

    /// <summary>Get an attribute value and parse it as an enum value.</summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="element">The element whose attributes to read.</param>
    /// <param name="name">The attribute name.</param>
    private TEnum? GetAttributeAsEnum<TEnum>(HtmlNode element, string name) where TEnum : struct
    {
        string? raw = this.GetAttribute(element, name);
        if (raw == null)
            return null;
        if (!Enum.TryParse(raw, true, out TEnum value) && Enum.IsDefined(typeof(TEnum), value))
            throw new InvalidOperationException($"Unknown {typeof(TEnum).Name} value '{raw}' when parsing compatibility list.");
        return value;
    }

    /// <summary>Get an attribute value and parse it as a semantic version.</summary>
    /// <param name="element">The element whose attributes to read.</param>
    /// <param name="name">The attribute name.</param>
    private ISemanticVersion? GetAttributeAsSemanticVersion(HtmlNode element, string name)
    {
        string? raw = this.GetAttribute(element, name);
        return SemanticVersion.TryParse(raw, out ISemanticVersion? version)
            ? version
            : null;
    }

    /// <summary>Get an attribute value and parse it as a nullable int.</summary>
    /// <param name="element">The element whose attributes to read.</param>
    /// <param name="name">The attribute name.</param>
    private int? GetAttributeAsNullableInt(HtmlNode element, string name)
    {
        string? raw = this.GetAttribute(element, name);
        if (raw != null && int.TryParse(raw, out int value))
            return value;
        return null;
    }

    /// <summary>Get the text of an element with the given class name.</summary>
    /// <param name="container">The metadata container.</param>
    /// <param name="className">The field name.</param>
    private string? GetInnerHtml(HtmlNode container, string className)
    {
        return container.Descendants().FirstOrDefault(p => p.HasClass(className))?.InnerHtml;
    }

    /// <summary>The response model for the MediaWiki parse API.</summary>
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    private class ResponseModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The parse API results.</summary>
        public ResponseParseModel Parse { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="parse">The parse API results.</param>
        public ResponseModel(ResponseParseModel parse)
        {
            this.Parse = parse;
        }
    }

    /// <summary>The inner response model for the MediaWiki parse API.</summary>
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    private class ResponseParseModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The parsed text.</summary>
        public IDictionary<string, string> Text { get; } = new Dictionary<string, string>();
    }
}
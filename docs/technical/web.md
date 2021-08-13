&larr; [README](../README.md)

**SMAPI.Web** contains the code for the `smapi.io` website, including the mod compatibility list
and update check API.

## Contents
* [Log parser](#log-parser)
* [JSON validator](#json-validator)
* [Web API](#web-api)
* [Short URLs](#short-urls)
* [For SMAPI developers](#for-smapi-developers)
  * [Local development](#local-development)
  * [Production environment](#production-environment)

## Log parser
The log parser at https://smapi.io/log provides a web UI for uploading, parsing, and sharing SMAPI
logs.

The logs are saved in a compressed form to Amazon Blob storage for 30 days.

## JSON validator
### Overview
The JSON validator at https://smapi.io/json provides a web UI for uploading and sharing JSON files,
and validating them as plain JSON or against a predefined format like `manifest.json` or Content
Patcher's `content.json`.

The logs are saved in a compressed form to Amazon Blob storage for 30 days.

### Schema file format
Schema files are defined in `wwwroot/schemas` using the [JSON Schema](https://json-schema.org/)
format. The JSON validator UI recognises a superset of the standard fields to change output:

<dl>
<dt>Documentation URL</dt>
<dd>

The root schema may have a `@documentationURL` field, which is a web URL for the user
documentation:
```js
"@documentationUrl": "https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest"
```

If present, this is shown in the JSON validator UI.

</dd>
<dt>Error messages</dt>
<dd>

Any part of the schema can define an `@errorMessages` field, which overrides matching schema
errors. You can override by error code (recommended), or by error type and a regex pattern matched
against the error message (more fragile):

```js
// by error type
"pattern": "^[a-zA-Z0-9_.-]+\\.dll$",
"@errorMessages": {
   "pattern": "Invalid value; must be a filename ending with .dll."
}
```
```js
// by error type + message pattern
"@errorMessages": {
   "oneOf:valid against no schemas": "Missing required field: EntryDll or ContentPackFor.",
   "oneOf:valid against more than one schema": "Can't specify both EntryDll or ContentPackFor, they're mutually exclusive."
}
```

Error messages may contain special tokens:

* The `@value` token is replaced with the error's value field. This is usually (but not always) the
  original field value.
* When an error has child errors, by default they're flattened into one message:
  ```
  line | field      | error
  ---- | ---------- | -------------------------------------------------------------------------
  4    | Changes[0] | JSON does not match schema from 'then'.
       |            |   ==> Changes[0].ToArea.Y: Invalid type. Expected Integer but got String.
       |            |   ==> Changes[0].ToArea: Missing required fields: Height.
  ```

  If you set the message for an error to `$transparent`, the parent error is omitted entirely and
  the child errors are shown instead:
  ```
  line | field               | error
  ---- | ------------------- | ----------------------------------------------
  8    | Changes[0].ToArea.Y | Invalid type. Expected Integer but got String.
  8    | Changes[0].ToArea   | Missing required fields: Height.
  ```

  The child errors themselves may be marked `$transparent`, etc. If an error has no child errors,
  this override is ignored.

  Validation errors for `then` blocks are transparent by default, unless overridden.

</dd>
</dl>

### Using a schema file directly
You can reference the validator schemas in your JSON file directly using the `$schema` field, for
text editors that support schema validation. For example:
```js
{
   "$schema": "https://smapi.io/schemas/manifest.json",
   "Name": "Some mod",
   ...
}
```

Available schemas:

format | schema URL
------ | ----------
[SMAPI: `manifest.json`](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest) | https://smapi.io/schemas/manifest.json
[SMAPI: translations (`i18n` folder)](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Translation) | https://smapi.io/schemas/i18n.json
[Content Patcher: `content.json`](https://github.com/Pathoschild/StardewMods/tree/develop/ContentPatcher#readme) | https://smapi.io/schemas/content-patcher.json

## Web API
### Overview
SMAPI provides a web API at `smapi.io/api` for use by SMAPI and external tools. The URL includes a
`{version}` token, which is the SMAPI version for backwards compatibility. This API is publicly
accessible but not officially released; it may change at any time.

### `/mods` endpoint
The API has one `/mods` endpoint. This crossreferences the mod against a variety of sources (e.g.
the wiki, Chucklefish, CurseForge, ModDrop, and Nexus) to provide metadata mainly intended for
update checks.

The API accepts a `POST` request with these fields:

<table>
<tr>
<th>field</th>
<th>summary</th>
</tr>

<tr>
<td><code>mods</code></td>
<td>

The mods for which to fetch metadata. Included fields:


field | summary
----- | -------
`id`  | The unique ID in the mod's `manifest.json`. This is used to crossreference with the wiki, and to index mods in the response. If it's unknown (e.g. you just have an update key), you can use a unique fake ID like `FAKE.Nexus.2400`.
`updateKeys` | _(optional)_ [Update keys](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest#Update_checks) which specify the mod pages to check, in addition to any mod pages linked to the `ID`.
`installedVersion` | _(optional)_ The installed version of the mod. If not specified, the API won't recommend an update.
`isBroken` | _(optional)_ Whether SMAPI failed to load the installed version of the mod, e.g. due to incompatibility. If true, the web API will be more permissive when recommending updates (e.g. allowing a stable → prerelease update).

</td>
</tr>

<tr>
<td><code>apiVersion</code></td>
<td>

_(optional)_ The installed version of SMAPI. If not specified, the API won't recommend an update.

</td>
</tr>

<tr>
<td><code>gameVersion</code></td>
<td>

_(optional)_ The installed version of Stardew Valley. This may be used to select updates.

</td>
</tr>

<tr>
<td><code>platform</code></td>
<td>

_(optional)_ The player's OS (`Android`, `Linux`, `Mac`, or `Windows`). This may be used to select updates.

</td>
</tr>

<tr>
<td><code>includeExtendedMetadata</code></td>
<td>

_(optional)_ Whether to include extra metadata that's not needed for SMAPI update checks, but which
may be useful to external tools.

</td>
</table>

Example request:
```js
POST https://smapi.io/api/v3.0/mods
{
   "mods": [
      {
         "id": "Pathoschild.ContentPatcher",
         "updateKeys": [ "nexus:1915" ],
         "installedVersion": "1.9.2",
         "isBroken": false
      }
   ],
   "apiVersion": "3.0.0",
   "gameVersion": "1.4.0",
   "platform": "Windows",
   "includeExtendedMetadata": true
}
```

Response fields:

<table>
<tr>
<th>field</th>
<th>summary</th>
</tr>

<tr>
<td><code>id</code></td>
<td>

The mod ID you specified in the request.

</td>
</tr>

<tr>
<td><code>suggestedUpdate</code></td>
<td>

The update version recommended by the web API, if any. This is based on some internal rules (e.g.
it won't recommend a prerelease update if the player has a working stable version) and context
(e.g. whether the player is in the game beta channel). Choosing an update version yourself isn't
recommended, but you can set `includeExtendedMetadata: true` and check the `metadata` field if you
really want to do that.

</td>
</tr>

<tr>
<td><code>errors</code></td>
<td>

Human-readable errors that occurred fetching the version info (e.g. if a mod page has an invalid
version).

</td>
</tr>

<tr>
<td><code>metadata</code></td>
<td>

Extra metadata that's not needed for SMAPI update checks but which may be useful to external tools,
if you set `includeExtendedMetadata: true` in the request. Included fields:

field | summary
----- | -------
`id`  | The known `manifest.json` unique IDs for this mod defined on the wiki, if any. That includes historical versions of the mod.
`name` | The normalised name for this mod based on the crossreferenced sites.
`nexusID` | The mod ID on [Nexus Mods](https://www.nexusmods.com/stardewvalley/), if any.
`chucklefishID` | The mod ID in the [Chucklefish mod repo](https://community.playstarbound.com/resources/categories/stardew-valley.22/), if any.
`curseForgeID` | The mod project ID on [CurseForge](https://www.curseforge.com/stardewvalley), if any.
`curseForgeKey` | The mod key on [CurseForge](https://www.curseforge.com/stardewvalley), if any. This is used in the mod page URL.
`modDropID` | The mod ID on [ModDrop](https://www.moddrop.com/stardew-valley), if any.
`gitHubRepo` | The GitHub repository containing the mod code, if any. Specified in the `Owner/Repo` form.
`customSourceUrl` | The custom URL to the mod code, if any. This is used for mods which aren't stored in a GitHub repo.
`customUrl` | The custom URL to the mod page, if any. This is used for mods which aren't stored on one of the standard mod sites covered by the ID fields.
`main` | The primary mod version, if any. This depends on the mod site, but it's typically either the version of the mod itself or of its latest non-optional download.
`optional` | The latest optional download version, if any.
`unofficial` | The version of the unofficial update defined on the wiki for this mod, if any.
`unofficialForBeta` | Equivalent to `unofficial`, but for beta versions of SMAPI or Stardew Valley.
`hasBetaInfo` | Whether there's an ongoing Stardew Valley or SMAPI beta which may affect update checks.
`compatibilityStatus` | The compatibility status for the mod for the stable version of the game, as defined on the wiki, if any. See [possible values](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI.Toolkit/Framework/Clients/Wiki/WikiCompatibilityStatus.cs).
`compatibilitySummary` | The human-readable summary of the mod's compatibility in HTML format, if any.
`brokeIn` | The SMAPI or Stardew Valley version that broke this mod, if any.
`betaCompatibilityStatus`<br />`betaCompatibilitySummary`<br />`betaBrokeIn` | Equivalent to the preceding fields, but for beta versions of SMAPI or Stardew Valley.


</td>
</tr>
</table>

Example response with `includeExtendedMetadata: false`:
```js
[
   {
      "id": "Pathoschild.ContentPatcher",
      "suggestedUpdate": {
         "version": "1.10.0",
         "url": "https://www.nexusmods.com/stardewvalley/mods/1915"
      },
      "errors": []
   }
]
```

Example response with `includeExtendedMetadata: true`:
```js
[
   {
      "id": "Pathoschild.ContentPatcher",
      "suggestedUpdate": {
         "version": "1.10.0",
         "url": "https://www.nexusmods.com/stardewvalley/mods/1915"
      },
      "metadata": {
         "id": [ "Pathoschild.ContentPatcher" ],
         "name": "Content Patcher",
         "nexusID": 1915,
         "curseForgeID": 309243,
         "curseForgeKey": "content-patcher",
         "modDropID": 470174,
         "gitHubRepo": "Pathoschild/StardewMods",
         "main": {
            "version": "1.10",
            "url": "https://www.nexusmods.com/stardewvalley/mods/1915"
         },
         "hasBetaInfo": true,
         "compatibilityStatus": "Ok",
         "compatibilitySummary": "✓ use latest version."
      },
      "errors": []
   }
]
```

## Short URLs
The SMAPI web services provides a few short URLs for convenience:

short url | → | target page
:-------- | - | :----------
[smapi.io/3.0](https://smapi.io/3.0) | → | [stardewvalleywiki.com/Modding:Migrate_to_SMAPI_3.0](https://stardewvalleywiki.com/Modding:Migrate_to_SMAPI_3.0)
[smapi.io/community](https://smapi.io/community) | → | [stardewvalleywiki.com/Modding:Community](https://stardewvalleywiki.com/Modding:Community)
[smapi.io/docs](https://smapi.io/docs) | → | [stardewvalleywiki.com/Modding:Index](https://stardewvalleywiki.com/Modding:Index)
[smapi.io/package](https://smapi.io/package) | → | [github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md)
[smapi.io/troubleshoot](https://smapi.io/troubleshoot) | → | [stardewvalleywiki.com/Modding:Player_Guide/Troubleshooting](https://stardewvalleywiki.com/Modding:Player_Guide/Troubleshooting)
[smapi.io/xnb](https://smapi.io/xnb) | → | [stardewvalleywiki.com/Modding:Using_XNB_mods](https://stardewvalleywiki.com/Modding:Using_XNB_mods)

## For SMAPI developers
### Local environment
A local environment lets you run a complete copy of the web project (including cache database) on
your machine, with no external dependencies aside from the actual mod sites.

1. Edit `appsettings.Development.json` and set these options:

   property name | description
   ------------- | -----------
   `NexusApiKey` | [Your Nexus API key](https://www.nexusmods.com/users/myaccount?tab=api#personal_key).

   Optional settings:

   property name               | description
   --------------------------- | -----------
   `AzureBlobConnectionString` | The connection string for the Azure Blob storage account. Defaults to using the system's temporary file folder if not specified.
   `GitHubUsername`<br />`GitHubPassword` | The GitHub credentials with which to query GitHub release info. Defaults to anonymous requests if not specified.

2. Launch `SMAPI.Web` from Visual Studio to run a local version of the site.

### Production environment
A production environment includes the web servers and cache database hosted online for public
access.

This section assumes you're creating a new environment on Azure, but the app isn't tied to any
Azure services. If you want to host it on a different site, you'll need to adjust the instructions
accordingly.

Initial setup:

1. Create an Azure Blob storage account for uploaded files.
2. Create an Azure App Services environment running the latest .NET on Linux or Windows.
3. Add these application settings in the new App Services environment:

   property name                   | description
   ------------------------------- | -----------------
   `ApiClients.AzureBlobConnectionString` | The connection string for the Azure Blob storage account created in step 2.
   `ApiClients.GitHubUsername`<br />`ApiClients.GitHubPassword` | The login credentials for the GitHub account with which to fetch release info. If these are omitted, GitHub will impose much stricter rate limits.
   `ApiClients:NexusApiKey`        | The [Nexus API authentication key](https://github.com/Pathoschild/FluentNexus#init-a-client).

   Optional settings:

   property name                   | description
   ------------------------------- | -----------------
   `BackgroundServices:Enabled`    | Set to `true` to enable background processes like fetching data from the wiki, or false to disable them.
   `Site:BetaEnabled`              | Set to `true` to show a separate download button if there's a beta version of SMAPI in its GitHub releases.
   `Site:BetaBlurb`                | If `Site:BetaEnabled` is true and there's a beta version of SMAPI in its GitHub releases, this is shown on the beta download button as explanatory subtext.
   `Site:SupporterList`            | A list of Patreon supports to credit on the download page.

To deploy updates, just [redeploy the web project from Visual Studio](https://docs.microsoft.com/en-us/visualstudio/deployment/quickstart-deploy-to-azure).

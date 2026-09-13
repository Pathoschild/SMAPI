← [README](README.md)

# Release notes
## Upcoming release
* For players:
  * Improved performance.
  * Improved error message when a mod is blocked by Windows Smart App Control.
  * Improved translations. Thanks to To2morrow (updated Korean)!
  * Fixed rare edge case where a mod blacklist update could fail.
  * Updated internal mod blacklist.

* For mod authors:
  * Added OS metrics to the [metrics API](technical/web.md#modsmetrics).

* For the web UI:
  * Added [mod stats page](https://smapi.io/stats) (see [announcement](https://www.patreon.com/pathoschild/posts/new-mod-dataset-161970558)).
  * Deprecated the [mod compatibility list](https://smapi.io/stats) (see [announcement](https://www.patreon.com/pathoschild/posts/162629586)).

* For tool maintainers:
  * Added the [open mod dataset](https://github.com/Pathoschild/StardewModDataset).

## 4.5.2
Released 14 March 2026 for Stardew Valley 1.6.14 or later. See [build attestation](https://github.com/Pathoschild/SMAPI/attestations/21366863).

* For players:
  * Improved performance a bit.
  * Fixed the Linux/macOS installer not saving the color scheme correctly in 4.5.0+.
  * Fixed typo in config UI text (thanks to QuentiumYT!).
  * Improved translations. Thanks to dekthaiinchina (updated Thai), dewanggatrustha (updated Indonesian), QuentiumYT (updated French), Timur13240
 (updated Russian), and vlcoo (updated Spanish)!

* For mod authors:
  * Fixed input API ignoring controller overrides when there's no controller plugged in (thanks to spacechase0!).
  * Fixed asset propagation for the farmhouse map not updating the farmhouse fridge position.

## 4.5.1
Released 25 January 2026 for Stardew Valley 1.6.14 or later. See [build attestation](https://github.com/Pathoschild/SMAPI/attestations/17385144).

* For players:
  * Fixed error installing SMAPI 4.5.0 on Linux/macOS.
  * Improved translations. Thanks to Maatsuki (updated Portuguese)!

## 4.5.0
Released 25 January 2026 for Stardew Valley 1.6.14 or later. See [release highlights](https://www.patreon.com/posts/149054246) and [build attestation](https://github.com/Pathoschild/SMAPI/attestations/17379361).

* For players:
  * Added in-game config UI for SMAPI via [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098).
  * SMAPI now uses [automated and attested builds](https://www.patreon.com/posts/automated-builds-148417912) (thanks to DecidedlyHuman)!  
    _This improves the security and transparency of SMAPI builds. Every step to build SMAPI from the public source code is now public and verifiable, with file signatures to let players and tools confirm the build hasn't been tampered with._
  * SMAPI can now detect known malicious loose files in the `Mods` folder.
  * Updated internal mod blacklist.

* For mod authors:
  * SMAPI no longer has a separate 'for developers' version.  
    _Instead, you can now use [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) to enable 'developer mode' in the console window options._

## 4.4.0
Released 10 January 2026 for Stardew Valley 1.6.14 or later. See [release highlights](https://www.patreon.com/posts/147916705).

* For players:
  * Added [`set_verbose` console command](https://stardewvalleywiki.com/Modding:Console_commands#set_verbose).
  * The SMAPI log now shows a friendly Windows name (like "Windows 11") instead of its internal identifier.
  * Fixed `player_add` and `list_items` console commands not including some newer juice items.
  * Fixed farmhouse map edits sometimes removing the spouse room (thanks to SinZ!).
  * Fixed installer error if Steam has an empty game path saved to the registry.

* For mod authors:
  * Added [input API to send button presses to the game](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Input#Send_input) (thanks to martiandweller!).
  * Added transparency masks via `PatchMode.Mask` when editing images (thanks to PinkSerenity!).
  * Added support for map tilesheets referencing an asset outside `Content/Maps` using a relative `../` path (thanks to Spiderbuttons!).
  * Added asset propagation for spouse room map edits.
  * Improved performance when propagating localized assets in some cases (thanks to SinZ!).
  * Improved error-handling during asset propagation.
  * Updated dependencies, including...
    * [Newtonsoft.Json](https://www.newtonsoft.com/json) 13.0.3 → 13.0.4 (see [changes](https://github.com/JamesNK/Newtonsoft.Json/releases/tag/13.0.4));
    * [Pintail](https://github.com/Nanoray-pl/Pintail) 2.8.1 → 2.9.1 (see [changes](https://github.com/Nanoray-pl/Pintail/blob/master/docs/release-notes.md#291)).
  * Fixed asset propagation for farmer sprites before a save is loaded.
  * Removed `System.Management.dll`, which SMAPI no longer uses.

* For the web UI:
  * Improved mod compatibility list:
    * Added support for mod links in warnings.
  * Improved Content Patcher [JSON schema](technical/web.md#using-a-schema-file-directly):
    * Updated for Content Patcher 2.8.0 and 2.9.0.
    * Fixed schema requiring `AddNPCWarps` instead of `AddNpcWarps`.
    * Fixed validation error if a warp field contains tokens or consecutive spaces (thanks to irocendar!).
    * Fixed validation error if a `Target` contains multiple targets (thanks to irocendar!).
    * Fixed `FromFile` errors like "_matches a schema that is not allowed_" (thanks to irocendar!).

## 4.3.2
Released 14 July 2025 for Stardew Valley 1.6.14 or later. See [4.3 release highlights](https://www.patreon.com/posts/133992196).

* For players:
  * Added a friendly error message when the game fails to launch with a `NoSuitableGraphicsDeviceException`.
  * Fixed crash when SMAPI tries to update the mod blacklist if ReShade is installed.

## 4.3.1
Released 13 July 2025 for Stardew Valley 1.6.14 or later.

* For players:
  * Improved performance when mods edit maps (thanks to SinZ!).
* For mod authors:
  * Fixed new `helper.ModRegistry.GetFromNamespacedId` not handling prefix IDs correctly.

## 4.3.0
Released 12 July 2025 for Stardew Valley 1.6.14 or later. See [release highlights](https://www.patreon.com/posts/133992196).

* For players:
  * Added 'malicious mod' blacklist.  
    _Once a malicious mod has been reported, this lets us quickly block it for all players. This helps mitigate damage in case of future attacks. This feature can be disabled in the SMAPI settings if needed._
  * Improved content load performance for non-English players.
  * Fixed some community shortcuts breaking if a mod edited the map which contains them.

* For mod authors:
  * Added `helper.ModRegistry.GetFromNamespacedId` method to get a mod given a [standard namespaced ID](https://stardewvalleywiki.com/Modding:Common_data_field_types#Unique_string_ID) (e.g. an item ID).
  * You can now have an `en.json` translation file which overrides `default.json`.
  * Updated dependencies, including...
    * [Mono.Cecil](https://github.com/jbevain/cecil) 0.11.5 → 0.11.6 (see [changes](https://github.com/jbevain/cecil/compare/0.11.5...0.11.6));
    * [FluentHttpClient](https://github.com/Pathoschild/FluentHttpClient#readme) 4.4.1 → 4.4.2 (see [changes](https://github.com/Pathoschild/FluentHttpClient/blob/develop/RELEASE-NOTES.md#442));
    * [Pintail](https://github.com/Nanoray-pl/Pintail) 2.6.1 → 2.8.1 (see [changes](https://github.com/Nanoray-pl/Pintail/blob/master/docs/release-notes.md#260)).

* For the web UI:
  * Increased default upload expiry from 30 to 60 days, to help avoid expired SMAPI logs when mod authors check messages monthly.
  * Improved JSON validator:
    * You can now hover/click braces to highlight matching pairs.
    * You can now hover and click 'Copy' on the top-right to copy the full code to the clipboard.
    * Updated to newer syntax highlighting library.
    * Fixed CurseForge update keys not recognized (thanks to Dunc4nNT!).
    * Fixed some JSON files breaking page layout.
  * Improved log parser:
    * Mods which failed to load are now shown in the mod list (with 'failed to load' in the error column).
    * Added suggested fix if there's a newer SMAPI version available.
    * Reduced response times with a new analysis cache and client-side fetch.
    * Removed support for very old SMAPI logs.
    * You can now download a JSON representation of the parsed log (see the download link at the bottom of the log page).
    * Fixed server error if a JSON file contains nested comment syntax.
  * Improved [JSON schemas](technical/web.md#using-a-schema-file-directly):
    * The Content Patcher JSON schema now allows decimal values in local tokens (thanks to rikai!).
    * The `$schema` value is no longer validated.
    * Updated Content Patcher schema for Content Patcher 2.7.0.
    * Updated manifest schema for the new `%ProjectVersion%` value in `Version`.
  * Improved mod compatibility list:
    * Reduced response times with a new cache and client-side fetch.
    * Fixed sort order for mods with non-Latin characters in the name.
  * Third-party libraries are now served from `smapi.io` instead of external CDNs.

## 4.2.1
Released 25 March 2025 for Stardew Valley 1.6.14 or later.

* For players:
  * Fixed crash when some mods' custom tiles are on-screen.

* For mod authors:
  * Reverted the fix for the game's `Data/ChairTiles` logic not handling unique string IDs like `Maps/Author.ModName` correctly.  
    _The fix caused crashes loading map tiles in some cases. This will be fixed in the next game update instead._

## 4.2.0
Released 24 March 2025 for Stardew Valley 1.6.14 or later. See [release highlights](https://www.patreon.com/posts/125017679).

* For players:
  * Fixed `log_context` command not disabling the extra logs when run again.
  * Fixed update alerts when using an unofficial port of SMAPI with a four-part version number.
  * Fixed installer on Linux not always opening a terminal as intended (thanks to HoodedDeath!).
  * Updated compatibility list.

* For mod authors:
  * Mod events are now raised on the shipping menu (except when it's actually saving).
  * Added translation API methods to query translation keys (`ContainsKey` and `GetKeys`).
  * ~~Fixed the game's `Data/ChairTiles` logic not handling unique string IDs like `Maps/Author.ModName` correctly.~~  
    _Reverted in 4.2.1._
  * Fixed exception thrown if `modRegistry.GetApi<T>` can't proxy the API to the given interface. It now logs an error and returns null as intended.

* For external tools:
  * Added toolkit method to read the compatibility list from a local copy of its Git repo.

* For the web UI:
  * You can now link to a mod in the compatibility list by its unique ID, like [smapi.io/mods#Pathoschild.ContentPatcher](https://smapi.io/mods#Pathoschild.ContentPatcher).
  * Fixed search engines able to index uploaded logs and JSON files via the raw download option.
  * Improved Content Patcher JSON schema:
    * Updated for Content Patcher 2.5.0.
    * Added format validation for token names.
    * Fixed incorrect error when setting a config default to a boolean or number.

## 4.1.10
Released 18 December 2024 for Stardew Valley 1.6.14 or later.

* For players:
  * Updated for the upcoming Stardew Valley 1.6.15.
  * Fixed errors when cross-playing between PC and Android.

* For mod authors:
  * Improved [Content Patcher JSON schema](technical/web.md#using-a-schema-file-directly) to allow boolean and numeric values in dynamic tokens.


> [!IMPORTANT]  
> **For players on macOS:**  
> There are recent security changes in macOS. Make sure to follow the updated [install guide for
> macOS](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Mac) when installing or updating SMAPI.
>
> Players on Linux or Windows can ignore this.

## 4.1.9
Released 08 December 2024 for Stardew Valley 1.6.14 or later.

* For players:
  * Fixed compatibility with new macOS security restrictions (again).
  * Fixed unable to override color schemes via `smapi-internal/config.user.json`.

## 4.1.8
Released 28 November 2024 for Stardew Valley 1.6.14 or later.

* For players:
  * Updated the mod compatibility blacklist.
  * Fixed compatibility with new macOS security restrictions.
  * Fixed crash with some rare combinations of mods involving Harmony and mod APIs.

* For mod authors:
  * Added `PathUtilities.CreateSlug` to get a safe Unicode string for use in special contexts like URLs and file paths.  
    _For example, `PathUtilities.CreateSlug("some 例子?!/\\~ text")` becomes `"some-例子-text"`._
  * `PathUtilities.IsSlug` now allows more Unicode characters.
  * Updated [Pintail](https://github.com/Nanoray-pl/Pintail) 2.6.0 → 2.6.1 (see [changes](https://github.com/Nanoray-pl/Pintail/blob/master/docs/release-notes.md#261)).

* For the web UI:
  * Fixed log parser not highlighting update alerts for mods which SMAPI couldn't load.
  * Fixed CurseForge links not shown for mods that have a CurseForge page.

* For external tools:
  * Revamped the mod compatibility list to simplify maintenance. It's now stored [in a Git repo](https://github.com/Pathoschild/SmapiCompatibilityList), which replaces the former [wiki page](https://stardewvalleywiki.com/Modding:Mod_compatibility).
  * Added toolkit method to get the URL from an update key's site and mod ID.

## 4.1.7
Released 12 November 2024 for Stardew Valley 1.6.14 or later.

* For players:
  * Updated for Stardew Valley 1.6.14.
  * Updated mod compatibility list.
  * Fixed crash if a mod has a missing or invalid DLL.

## 4.1.6
Released 07 November 2024 for Stardew Valley 1.6.10 or later.

* For players:
  * Revamped message shown after a game update to avoid confusion.
  * Added option to disable content integrity checks in `smapi-internal/config.json`. When disabled, SMAPI will log a warning for visibility when someone helps you troubleshoot game issues.

* For mod authors:
  * Fixed `translation.ApplyGenderSwitchBlocks(false)` not applied correctly.

## 4.1.5
Released 07 November 2024 for Stardew Valley 1.6.10 or later.

* For players:
  * Updated mod compatibility list.
  * Fixed translation issues in some mods with SMAPI 4.1._x_.

* For mod authors:
  * Fixed `translation.UsePlaceholder(false)` also disabling custom fallback text in recent builds, not just the "no translation" placeholder.

## 4.1.4
Released 05 November 2024 for Stardew Valley 1.6.10 or later.

* For players:
  * Fixed a wide variety of mod errors and crashes after SMAPI 4.1.0 in some specific cases (e.g. Content Patcher "unable to find constructor" errors).

* For mod authors:
  * Removed the new private assembly references feature. This may be revisited in a future update once the dust settles on 1.6.9.
  * Fixed error propagating edits to `Data/ChairTiles`.

## 4.1.3
Released 04 November 2024 for Stardew Valley 1.6.10 or later.

* For players:
  * Improved compatibility rewriters for Stardew Valley 1.6.9+.

## 4.1.2
Released 04 November 2024 for Stardew Valley 1.6.10 or later.

* For players:
  * Updated for Stardew Valley 1.6.10.
  * Fixed various issues with custom maps loaded from `.tmx` files in Stardew Valley 1.6.9.

## 4.1.1
Released 04 November 2024 for Stardew Valley 1.6.9 or later.

* For players:
  * Fixed crash when loading saves containing a custom spouse room loaded from a `.tmx` file.

## 4.1.0
Released 04 November 2024 for Stardew Valley 1.6.9 or later. See [release highlights](https://www.patreon.com/posts/115304143).

* For players:
  * Updated for Stardew Valley 1.6.9.
  * SMAPI now auto-detects missing or modified content files, and logs a warning if found.
  * SMAPI now uses iTerm2 on macOS if it's installed (thanks to yinxiangshi!).
  * SMAPI now enables GameMode on Linux if it's installed (thanks to noah1510!).
  * SMAPI now anonymizes paths containing your home path (thanks to AnotherPillow!).
  * Removed confusing "Found X mods with warnings:" log message.
  * The installer on Linux now tries to open a terminal if needed (thanks to HoodedDeath!).
  * Fixed installer not detecting Linux Flatpak install paths.
  * Fixed various content issues for non-English players (e.g. content packs not detecting the current festival correctly).
  * Fixed dependencies on obsolete redundant mods not ignored in some cases.
  * Fixed issues in Console Commands:
    * Fixed `list_items` & `player_add` not handling dried items, pickled forage, smoked fish, and specific bait correctly.
    * Fixed `list_items` & `player_add` listing some flooring & wallpaper items twice.
    * Fixed `show_data_files` & `show_game_files` no longer working correctly (thanks to jakerosado!).
  * Fixed some mod overlays mispositioned when your UI scale is non-100% and zoom level is 100%.
  * Fixed incorrect 'direct console access' warnings.
  * Updated mod compatibility list.

* For mod authors:
  * Added support for [private assembly references](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest#Private_assemblies) (thanks to Shockah!).
  * Added support for [i18n subfolders](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Translation#i18n_folder) (thanks to spacechase0!).
  * Added asset propagation for `Data/ChairTiles`.
  * Added new C# API methods:
    * Added `DoesAssetExist` methods to `helper.GameContent` and `helper.ModContent` (thanks to KhloeLeclair!).
    * Added scroll wheel suppression via `helper.Input.SuppressScrollWheel()` (thanks to MercuriusXeno!).
    * Added `PathUtilities.AnonymizePathForDisplay` to anonymize home paths (thanks to AnotherPillow!).
  * Added parameter docs to event interfaces. This lets you fully document your event handlers like `/// <inheritdoc cref="IGameLoopEvents.SaveLoaded" />`.
  * Translations now support [gender switch blocks](https://stardewvalleywiki.com/Modding:Dialogue#Gender_switch).
  * Translations now support tokens in their placeholder text.
  * SMAPI no longer blocks map edits which change the tilesheet order, since that no longer causes crashes in Stardew Valley 1.6.9.
  * The SMAPI log now includes the assembly version of each loaded mod (thanks to spacechase0!).
  * Updated dependencies, including...
    * [FluentHttpClient](https://github.com/Pathoschild/FluentHttpClient#readme) 4.3.0 → 4.4.1 (see [changes](https://github.com/Pathoschild/FluentHttpClient/blob/develop/RELEASE-NOTES.md#441));
    * [Pintail](https://github.com/Nanoray-pl/Pintail) 2.3.0 → 2.6.0 (see [changes](https://github.com/Nanoray-pl/Pintail/blob/master/docs/release-notes.md#260)).
  * Fixed `content.Load` ignoring language override in recent versions.
  * Fixed player sprites and building paint masks not always propagated on change.
  * Fixed `.tmx` map tile sizes being premultiplied, which is inconsistent with the game's `.tbin` maps.
  * Fixed various edge cases when chaining methods on `Translation` instances.

* For the update check server:
  * Rewrote update checks for mods on CurseForge and ModDrop to use new export API endpoints.  
    _This should result in much faster update checks for those sites, and less chance of update-check errors when their servers are under heavy load._
  * Added workaround for CurseForge auto-syncing prerelease versions with an invalid version number.

* For the log parser:
  * Clicking a checkbox in the mod list now always only changes that checkbox, to allow hiding a single mod.
  * Fixed the wrong game folder path shown if the `Mods` folder path was customized.

* For the JSON validator:
  * Updated for Content Patcher 2.1.0 &ndash; 2.4.0, and fixed validation for `Priority` fields.
  * Fixed incorrect errors shown for..
    * some valid `Entries`, `Fields`, `MapProperties`, `MapTiles`, and `When` field values;
    * `CustomLocations` entries which use the new [unique string ID](https://stardewvalleywiki.com/Modding:Common_data_field_types#Unique_string_ID) format;
    * `AddWarps` warps when a location name contains a dot.

* For the web API:
  * The [anonymized metrics for update check requests](technical/web.md#modsmetrics) now counts requests by SMAPI and game version.

## 4.0.8
Released 21 April 2024 for Stardew Valley 1.6.4 or later.

* For players:
  * Added option to disable Harmony fix for players with certain crashes.
  * Fixed crash for non-English players in split-screen mode when mods translate some vanilla assets.
  * SMAPI no longer rewrites mods which use Harmony 1.x, to help reduce Harmony crashes.  
    _This should affect very few mods that still work otherwise, and any Harmony mod updated after July 2021 should be unaffected._
  * Updated mod compatibility list to prevent common crashes.

* For the update check server:
  * Rewrote update checks for mods on Nexus Mods to use a new Nexus API endpoint.  
    _This should result in much faster update checks for Nexus, and less chance of update-check errors when the Nexus servers are under heavy load._

## 4.0.7
Released 18 April 2024 for Stardew Valley 1.6.4 or later.

* For players:
  * Updated for Stardew Valley 1.6.4.
  * The installer now lists detected game folders with an incompatible version to simplify troubleshooting.
  * When the installer asks for a game folder path, entering an incorrect path to a file inside it will now still select the folder.
  * Fixed installer not detecting 1.6 compatibility branch.

* For the web UI:
  * Updated `manifest.json` JSON schema for the new `MinimumGameVersion` field (thanks to KhloeLeclair!).

* For external tool authors:
  * In the SMAPI toolkit, added a new `GetGameFoldersIncludingInvalid()` method to get all detected game folders and their validity type.

## 4.0.6
Released 07 April 2024 for Stardew Valley 1.6.0 or later.

* For players:
  * The SMAPI log file now includes installed mod IDs, to help with troubleshooting (thanks to DecidedlyHuman!).

* For mod authors:
  * Added optional [`MinimumGameVersion` manifest field](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest#Minimum_SMAPI_or_game_version).

## 4.0.5
Released 06 April 2024 for Stardew Valley 1.6.0 or later.

* For players:
  * The installer now deletes obsolete files from very old SMAPI versions again. (This was removed in SMAPI 4.0, but many players still had very old versions.)
  * The installer now deletes Error Handler automatically if it's at the default path.
  * Fixed mods sometimes not applying logic inside new buildings.
  * Minor optimizations.
  * Updated mod compatibility list.

* For mod authors:
  * Fixed world-changed events (e.g. `ObjectListChanged`) not working correctly inside freshly-constructed buildings.

## 4.0.4
Released 29 March 2024 for Stardew Valley 1.6.0 or later.

* For players:
  * Added `log_context` console command, which replaces `test_input` and logs more info like menu changes.
  * Added [`--prefer-terminal-name` command-line argument](technical/smapi.md#command-line-arguments) to override which terminal SMAPI is launched with (thanks to test482!).
  * Fixed some mods compiled for Stardew Valley 1.6.3+ not working in 1.6.0–1.6.2.
  * Fixed SMAPI's "_Found warnings with X mods_" message counting hidden warnings.
  * Improved translations. Thanks to RezaHidayatM (added Indonesian)!

* For the web UI:
  * Improved smapi.io colors for accessibility, converted PNG images to SVG, and updated Patreon logo (thanks to ishan!).
  * Fixed JSON schema validation:
    * Manifest `UpdateKeys` field now allows dots in the GitHub repo name.
    * Fixed Content Patcher's `FromMapFile` and `FromFile` patterns.

## 4.0.3
Released 27 March 2024 for Stardew Valley 1.6.0 or later.

* For players:
  * Updated compatibility rewrites for Stardew Valley 1.6.3.
  * Updated mod compatibility list.
  * Tweaked `player_add` console command's error messages for clarity.

## 4.0.2
Released 24 March 2024 for Stardew Valley 1.6.0 or later.

* For players:
  * Updated mod compatibility list.
  * Improved status for obsolete mods to be clearer that they can be removed.
  * Disabled Extra Map Layers mod.
    _Extra Map Layers mod caused visual issues like dark shadows in all locations with extra map layers, since the game now handles them automatically. SMAPI now disables Extra Map Layers and ignores dependencies on it._
  * When using a custom `Mods` folder path, SMAPI now logs the game folder path to simplify troubleshooting.

## 4.0.1
Released 20 March 2024 for Stardew Valley 1.6.0 or later.

* For players:
  * Fixed error in some cases when rewritten mod code removes items from an inventory.

* For the web UI:
  * Added CurseForge download link to main page for cases where Nexus is unavailable.

## 4.0.0
Released 19 March 2024 for Stardew Valley 1.6.0 or later. See [release highlights](https://www.patreon.com/posts/100388693).

* For players:
  * Updated for Stardew Valley 1.6.
  * Added support for overriding SMAPI configuration per `Mods` folder (thanks to Shockah!).
  * Improved performance.
  * Improved compatibility rewriting to handle more cases (thanks to SinZ for his contributions!).
  * Removed the bundled `ErrorHandler` mod, which is now integrated into Stardew Valley 1.6.
  * Removed obsolete console commands: `list_item_types` (no longer needed) and `player_setimmunity` (broke in 1.6 and rarely used).
  * Removed support for seamlessly updating from SMAPI 2.11.3 and earlier (released in 2019).  
    _If needed, you can update to SMAPI 3.18.0 first and then install the latest version._

* For mod authors:
  * Updated to .NET 6.
  * Added [`RenderingStep` and `RenderedStep` events](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events#Display.RenderingStep), which let you handle a specific step in the game's render cycle.
  * Added support for [custom update manifests](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Update_checks#Custom_update_manifest) (thanks to Jamie Taylor!).
  * Removed all deprecated APIs.
  * SMAPI no longer intercepts output written to the console. Mods which directly access `Console` will be listed under mod warnings.
  * Calling `Monitor.VerboseLog` with an interpolated string no longer evaluates the string if verbose mode is disabled (thanks to atravita!). This only applies to mods compiled in SMAPI 4.0.0 or later.
  * Fixed redundant `TRACE` logs for a broken mod which references members with the wrong types.

* For the web UI:
  * Updated JSON validator for Content Patcher 2.0.0.
  * Added [anonymized metrics for update check requests](technical/web.md#modsmetrics).
  * Fixed uploaded log/JSON file expiry alway shown as renewed.
  * Fixed update check for mods with a prerelease version tag not recognized by the ModDrop API. SMAPI now parses the version itself if needed.

* For SMAPI developers:
  * Added `LogTechnicalDetailsForBrokenMods` option in `smapi-internal/config.json`, which adds more technical info to the SMAPI log when a mod is broken. This is mainly useful for creating compatibility rewriters.

## 3.18.6 and earlier
See [older release notes](release-notes-archived.md).

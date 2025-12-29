← [README](README.md)

# Release notes
## Upcoming release
* For players:
  * The SMAPI console/log now shows "Windows 10" or "Windows 11" in the log instead of its internal OS identifier.
  * Fixed `player_add` and `list_items` console commands not including some newer juice items.
  * Fixed farmhouse map edits sometimes removing the spouse room (thanks to SinZ!).
  * Fixed installer error if Steam has an empty game path saved to the registry.

* For mod authors:
  * Added [input API to send button presses to the game](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Input#Send_input) (thanks to martiandweller!).
  * Added `PatchMode.Mask` option when editing textures, which lets you apply a transparency mask to the target texture (thanks to PinkSerenity!).
  * Added support for map tilesheet paths starting with `../` to reference an asset under the `Content` folder that's not in `Content/Maps` (thanks to Spiderbuttons!).
  * Added asset propagation for spouse room map edits.
  * Improved performance when propagating localized assets in some cases (thanks to SinZ!).
  * Improved error-handling during asset propagation.
  * Updated dependencies, including...
    * [Harmony](https://harmony.pardeike.net) 2.2.2 → 2.4.2 (see [changes](https://github.com/pardeike/Harmony/releases)).
    * [Newtonsoft.Json](https://www.newtonsoft.com/json) 13.0.3 → 13.0.4 (see [changes](https://github.com/JamesNK/Newtonsoft.Json/releases/tag/13.0.4));
    * [Pintail](https://github.com/Nanoray-pl/Pintail) 2.8.1 → 2.9.1 (see [changes](https://github.com/Nanoray-pl/Pintail/blob/master/docs/release-notes.md#291)).
  * Removed `System.Management.dll`, which SMAPI no longer uses.

* For the web UI:
  * Improved mod compatibility list:
    * Added support for mod links in warnings.
  * Improved Content Patcher [JSON schemas](technical/web.md#using-a-schema-file-directly):
    * Updated for Content Patcher 2.8.0.
    * Fixed schema requiring `AddNPCWarps` instead of `AddNpcWarps`.
    * Fixed validation error if a warp field contains tokens or consecutive spaces (thanks to irocendar!).
    * Fixed validation error if a `Target` contains multiple targets (thanks to irocendar!).
    * Fixed `FromFile` errors like "_matches a schema that is not allowed_" (thanks to irocendar!).

**Breaking changes for mod authors:**
* Newer Harmony versions have stricter validation, which will break some patches which do unusual things (like
  selecting instructions past the end).

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

# 4.1.9
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

# 4.0.1
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

## 3.18.6
Released 05 October 2023 for Stardew Valley 1.5.6 or later.

* For players:
  * Fixed SpriteMaster compatibility in SMAPI 3.18.5+ with temporary workaround.
  * Fixed `player_add` and `list_items` console commands not including Pickled Ginger, and returning Honey instead of Wild Honey _(in Console Commands)_.

* For mod authors:
  * Added asset propagation for `LooseSprites/chatBox` and `LooseSprites/emojis`.

* For the web UI:
  * Fixed uploaded log/JSON file expiry alway shown as renewed.

## 3.18.5
Released 26 August 2023 for Stardew Valley 1.5.6 or later.

* For players:
  * Reduced startup time when many mods are rewritten for compatibility.
  * Fixed app icon on Linux/macOS, and for some players on Windows (thanks to Datrio!).
  * Fixed error if you copy a null field into `config.user.json`.
  * Fixed installer creating a "null" file in its folder.
  * Fixed installer moving bundled mods back to their default location on update. It now correctly updates their existing folder instead.

* For mod authors:
  * Updated dependencies, including [Mono.Cecil](https://github.com/jbevain/cecil) 0.11.4 → 0.11.5 (see [changes](https://github.com/jbevain/cecil/releases/tag/0.11.5)).
  * Fixed NPC warp cache not updated when a map edit changes door warps (thanks to atravita!).

* For the web UI:
  * Viewing an uploaded log/JSON file within 15 days of expiry now auto-renews it, to allow for discussions that last longer than the default 30-day expiry.
  * Fixed log parser's summary skipping mods if some have no description.
  * Fixed log parser no longer ignoring Error Handler in newer SMAPI-on-Android versions (thanks to AnotherPillow!).

* For SMAPI developers:
  * Overhauled compatibility rewriters.  
    _This allows simpler, more robust, and more flexible crossplatform rewrites (e.g. on Android) and prepares for the upcoming Stardew Valley 1.6. See remarks on the new `ReplaceReferencesRewriter` for more info._

## 3.18.4
Released 24 June 2023 for Stardew Valley 1.5.6 or later.

* For players:
  * In multiplayer, the game/SMAPI window titles now show whether you're the main player or a farmhand.
  * The `test_input` console command now logs input until the command is run again (instead of for 30 seconds).
  * Fixed logged SMAPI errors not having line numbers on Linux/macOS.
  * Fixed wezterm terminal support on Linux/macoS (thanks to romangraef!).
  * Fixed install error if a game folder has an invalid symlink.

* For mod authors:
  * Added `--no-prompt` installer command-line argument for automated tools (thanks to NyCodeGHG!).
  * Added clearer error message when a map tilesheet has no image source (thanks to atravita!).
  * Fixed `Context.HasRemotePlayers` being true when there's no farmhands connected.
  * Fixed error loading a mod if it explicitly sets `"MinimumApiVersion": null`.
  * Updated Newtonsoft.Json 13.0.2 → 13.0.3 (see [changes](https://github.com/JamesNK/Newtonsoft.Json/releases/tag/13.0.3)) and Pintail 2.2.2 → 2.3.0 (see [changes](https://github.com/Nanoray-pl/Pintail/blob/master/docs/release-notes.md#230)).

* For SMAPI toolkit users:
  * Fixed `ModFolder` not being JSON-serializable.

* For the web API:
  * Fixed manifest schema format for the `examples` field (thanks to boneskull!).

## 3.18.3
Released 09 April 2023 for Stardew Valley 1.5.6 or later.

* For players:
  * Fixed `findstr` installer error for some players.
  * Fixed installer error for some Linux players due to a non-portable shebang (thanks to freyacoded!).
  * Fixed error using load order overrides when there are broken mods installed (thanks to atravita!).
  * Removed `LargeAddressAware` flag on SMAPI (no longer needed since it's 64-bit now).
  * Improved translations. Thanks to stylemate (updated Korean)!

* For mod authors:
  * Added `IsActiveForScreen()` method to `PerScreen<T>`.
  * Updated to [FluentHttpClient](https://github.com/Pathoschild/FluentHttpClient#readme) 4.3.0 (see [changes](https://github.com/Pathoschild/FluentHttpClient/blob/develop/RELEASE-NOTES.md#430)).
  * Adjusted `ModContentManager.HandleUnknownFileType` to let mods patch it.
  * Fixed `Context.IsWorldReady` being editable by mods.

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.29.0.

## 3.18.2
Released 09 January 2023 for Stardew Valley 1.5.6 or later.

* For players:
  * Fixed empty save backups for some macOS players.
  * Fixed `player_add` console command not handling custom slingshots correctly (thanks to DaLion!).

* For mod authors:
  * Added `DelegatingModHooks` utility for mods which need to override SMAPI's mod hooks directly.
  * Updated to Newtonsoft.Json 13.0.2 (see [changes](https://github.com/JamesNK/Newtonsoft.Json/releases/tag/13.0.2)) and Pintail 2.2.2 (see [changes](https://github.com/Nanoray-pl/Pintail/blob/master/docs/release-notes.md#222)).

## 3.18.1
Released 01 December 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Fixed mod texture edits sometimes cut off (thanks to atravita!).

* For the web UI:
  * The log parser no longer warns about missing Error Handler on Android, where it doesn't exist yet (thanks to AnotherPillow!).

## 3.18.0
Released 12 November 2022 for Stardew Valley 1.5.6 or later. See [release highlights](https://www.patreon.com/posts/74565278).

* For players:
  * You can now override the mod load order in `smapi-internal/config.json` (thanks to Shockah!).
  * You can now disable console input in `smapi-internal/config.json`, which may reduce CPU usage on some Linux systems.
  * Fixed map edits not always applied for farmhands in multiplayer (thanks to SinZ163!).
  * Internal changes to prepare for the upcoming Stardew Valley 1.6 and SMAPI 4.0.

* For mod authors:
  * Optimized asset name comparisons (thanks to atravita!).
  * Raised all deprecation messages to the 'pending removal' level.
  * **This is the last major update before SMAPI 4.0.0, which will drop all deprecated APIs.** If you haven't [fixed deprecation warnings in your mod code](https://stardewvalleywiki.com/Modding:Migrate_to_SMAPI_4.0) (if any), you should do it soon. SMAPI 4.0.0 will release alongside the upcoming Stardew Valley 1.6.

* For the web UI:
  * The log parser now detects split-screen mode and shows which screen logged each message.

## 3.17.2
Released 21 October 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Fixed installer crash if Steam's library data is invalid or in an old format; it'll now be ignored instead.
* For mod authors:
  * Fixed image patches sometimes applied one pixel higher than expected after 3.17.0 (thanks to atravita!).

## 3.17.1
Released 10 October 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Fixed installer error on Windows if the Steam library folder exists but doesn't contain Steam's `.vdf` library data file.

## 3.17.0
Released 09 October 2022 for Stardew Valley 1.5.6 or later. See [release highlights](https://www.patreon.com/posts/73090322).

* For players:
  * You can now download SMAPI 'strict mode' from [Nexus files](https://www.nexusmods.com/stardewvalley/mods/2400/?tab=files), which removes all deprecated APIs. This may significantly improve performance, but mods which still show deprecation warnings won't work.
  * The SMAPI installer now also detects game folders in Steam's `.vdf` library data on Windows (thanks to pizzaoverhead!).
  * SMAPI now prevents mods from enabling Harmony debug mode, which impacts performance and creates a file on your desktop.  
    _You can allow debug mode by editing `smapi-internal/config.json` in your game folder._
  * Optimized performance and memory usage (thanks to atravita!).
  * Other internal optimizations.
  * Added more file extensions to ignore when searching for mod folders: `.7z`, `.tar`, `.tar.gz`, and `.xcf` (thanks to atravita!).
  * Removed transitional `UseRawImageLoading` option added in 3.15.0. This is now always enabled, except when PyTK is installed.
  * Fixed update alerts incorrectly shown for prerelease versions on GitHub that aren't marked as prerelease.

* For mod authors:
  * When [providing a mod API in a C# mod](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Integrations), you can now get the mod requesting it as an optional parameter (thanks to KhloeLeclair!).
  * SMAPI now treats square brackets in the manifest `Name` field as round ones to avoid breaking tools which parse log files.
  * Made deprecation message wording stronger for the upcoming SMAPI 4.0.0 release.
  * The `Texture2D.Name` field is now set earlier to support mods like SpriteMaster.
  * Updated dependencies: [Harmony](https://harmony.pardeike.net) 2.2.2 (see [changes](https://github.com/pardeike/Harmony/releases/tag/v2.2.2.0)) and [FluentHttpClient](https://github.com/Pathoschild/FluentHttpClient#readme) 4.2.0 (see [changes](https://github.com/Pathoschild/FluentHttpClient/blob/develop/RELEASE-NOTES.md#420)).
  * Fixed `LocationListChanged` event not raised & memory leak occurring when a generated mine/volcano is removed (thanks to tylergibbs2!).

## 3.16.2
Released 31 August 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Fixed `NoSuitableGraphicsDeviceException` launch error for some players with compatible GPUs since 3.16.0.

## 3.16.1
Released 29 August 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Updated PyTK compatibility mode for the latest PyTK version.
  * Fixed broken mods sometimes incorrectly listed as duplicate.

## 3.16.0
Released 22 August 2022 for Stardew Valley 1.5.6 or later. See [release highlights](https://www.patreon.com/posts/70797008).

* For players:
  * Added error message if mod files are detected directly under `Mods` (instead of each mod having its own subfolder).
  * SMAPI now sets a success/error code when the game exits.  
    _This is used by your OS (like Windows) to decide whether to keep the console window open when the game ends._
  * Fixed SMAPI on Windows applying different DPI awareness settings than the game (thanks to spacechase0!).
  * Fixed Linux/macOS installer's color scheme question partly unreadable if the terminal background is dark.
  * Fixed error message when a mod loads an invalid PNG file (thanks to atravita!).
  * Fixed error message when a mod is duplicated, but one of the copies is also missing the DLL file. This now shows the duplicate-mod message instead of the missing-DLL message.
  * Fixed macOS launcher using Terminal regardless of the system's default terminal (thanks to ishan!).
  * Fixed best practices in Linux/macOS launcher scripts (thanks to ishan!).
  * Improved translations. Thanks to KediDili (updated Turkish)!

* For mod authors:
  * While loading your mod, SMAPI now searches for indirect dependencies in your mod's folder (thanks to TehPers)! This mainly enables F# mods.
  * **Raised deprecation message levels.**  
    _Deprecation warnings are now player-visible in the SMAPI console as faded `DEBUG` messages._
  * Updated to Pintail 2.2.1 (see [changes](https://github.com/Nanoray-pl/Pintail/blob/master/docs/release-notes.md#221)).
  * Switched SMAPI's `.pdb` files to the newer 'portable' format. This has no effect on mods.

* For the web UI:
  * Added log parser warning about performance of PyTK 1.23.0 or earlier.
  * Converted images to SVG (thanks to ishan!).
  * Updated log parser for the new update alert format in SMAPI 3.15.1.
  * Updated the JSON validator/schema for Content Patcher 1.28.0.
  * Fixed log parsing for invalid content packs.
  * Fixed log parsing if a mod logged a null character.

## 3.15.1
Released 06 July 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Added current version to update alerts (thanks to ishan!).
  * Fixed lag for some players since Stardew Valley 1.5.5.
  * Fixed `smapi-internal/config.user.json` overrides not applied after SMAPI 3.14.0.
  * Fixed PyTK not rescaling images correctly in some cases.  
    _When PyTK 1.23.0 or earlier is installed, this will disable the main performance improvements in SMAPI 3.15.0._
  * Updated compatibility list.

* For mod authors:
  * The [FluentHttpClient package](https://github.com/Pathoschild/FluentHttpClient#readme) is now loaded by SMAPI.
  * Fixed `TRACE` logs not tracking reloaded map tilesheets as a propagated asset.

* For the web UI:
  * Added log parser suggested fix for missing/outdated Error Handler, and improved visual styles.
  * Updated the JSON validator/schema for Content Patcher 1.27.0.
  * Fixed the mod count in the log parser metadata.

## 3.15.0
Released 17 June 2022 for Stardew Valley 1.5.6 or later. See [release highlights](https://www.patreon.com/posts/67877219).

* For players:
  * Optimized mod image file loading.
  * Minor optimizations (thanks to Michael Kuklinski / Ameisen!).
  * Updated compatibility list.

* For mod authors:
  * Added an [`IRawTextureData` asset type](https://stardewvalleywiki.com/Modding:Migrate_to_SMAPI_4.0#Raw_texture_data), to avoid creating full `Texture2D` instances in many cases.
  * In `smapi-internal/config.json`, you can now enable verbose logging for specific mods (instead of all or nothing).
  * Updated dependencies:
    * Harmony 2.2.1 (see changes in [2.2.0](https://github.com/pardeike/Harmony/releases/tag/v2.2.0.0) and [2.2.1](https://github.com/pardeike/Harmony/releases/tag/v2.2.1.0));
    * Newtonsoft.Json 13.0.1 (see [changes](https://github.com/JamesNK/Newtonsoft.Json/releases/tag/13.0.1));
    * Pintail 2.2.0 (see [changes](https://github.com/Nanoray-pl/Pintail/blob/master/docs/release-notes.md#220)).
  * Removed transitional `UsePintail` option added in 3.14.0 (now always enabled).
  * Fixed `onBehalfOf` arguments in the new content API being case-sensitive.
  * Fixed map edits which change warps sometimes rebuilding the NPC pathfinding cache unnecessarily, which could cause a noticeable delay for players.

## 3.14.7
Released 01 June 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Optimized reflection cache to reduce frame skips for some players.

* For mod authors:
  * Removed `runtimeconfig.json` setting which impacted hot reload support.

## 3.14.6
Released 27 May 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Fixed error in split-screen mode when a mod provides a localized asset in one screen but not another.
  * Minor optimizations.

## 3.14.5
Released 22 May 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Improved performance when mods change some asset types (including NPC portraits/sprites).
  * Fixed _could not find file_ error if a mod provides a localized version of a normally unlocalized asset and then stops providing it.
  * Fixed CurseForge update checks for the new CurseForge API.

## 3.14.4
Released 15 May 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Improved performance for mods using deprecated APIs.

* For mod authors:
  * Removed warning for mods which use `dynamic`.  
    _This no longer causes errors on Linux/macOS after Stardew Valley 1.5.5._

## 3.14.3
Released 12 May 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Reduced in-game performance impact.

* For mod authors:
  * Refactored how event handling works under the hood, particularly the new content API. This should have no effect on mod usage.
  * Verbose mode now logs the in-game time.
  * Fixed error when loading a `.xnb` file through the old content API without the file extension.
  * Fixed asset propagation for player sprites not fully updating recolor masks in some cases.

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.26.0.

## 3.14.2
Released 08 May 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Enabled case-insensitive file paths by default for Android and Linux players.  
    _This was temporarily disabled in SMAPI 3.14.1, and will remain disabled by default on macOS and Windows since their filesystems are already case-insensitive._
  * Various performance improvements.
* For mod authors:
  * Dynamic content packs created via `helper.ContentPacks.CreateTemporary` or `CreateFake` are now listed in the log file.
  * Fixed assets loaded through a fake content pack not working correctly since 3.14.0.

## 3.14.1
Released 06 May 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Improved performance for mods still using the previous content API.
  * Disabled case-insensitive file paths (introduced in 3.14.0) by default.  
    _You can enable them by editing `smapi-internal/config.json` if needed. They'll be re-enabled in an upcoming version after they're reworked a bit._
  * Removed experimental 'aggressive memory optimizations' option.  
    _This was disabled by default and is no longer needed in most cases. Memory usage will be better reduced by reworked asset propagation in the upcoming SMAPI 4.0.0._
  * Fixed 'content file was not found' error when the game tries to load unlocalized text from a localizable mod data asset in 3.14.0.
  * Fixed error reading empty JSON files. These are now treated as if they didn't exist (matching pre-3.14.0 behavior).
  * Updated compatibility list.

## 3.14.0
Released 01 May 2022 for Stardew Valley 1.5.6 or later. See [release highlights](https://www.patreon.com/posts/65265507).

### For players
This is a big update, but existing mods should all work fine. If the latest version of a mod breaks in SMAPI 3.14, please report it [on the SMAPI mod page](https://www.nexusmods.com/stardewvalley/mods/2400?tab=posts).

* Improvements:
  * SMAPI now ignores dot-prefixed files when searching for mod folders (thanks to Nuztalgia!).
  * On Linux, SMAPI now fixes many case-sensitive mod path issues automatically.
  * On Linux/macOS, added `--use-current-shell` [command-line argument](technical/smapi.md#command-line-arguments) to avoid opening a separate terminal window.
  * Improved performance in some cases.
  * Improved translations. Thanks to ChulkyBow (updated Ukrainian)!
  * Dropped update checks for the unofficial 64-bit patcher (obsolete since SMAPI 3.12.6).
* Fixes:
  * Fixed some movie theater textures not translated when loaded through SMAPI (specifically assets with the `_international` suffix).
  * Fixed the warning text when a mod causes an asset load conflict with itself.
  * Fixed `--no-terminal` [command-line argument](technical/smapi.md#command-line-arguments) on Linux/macOS still opening a terminal window, even if nothing is logged to it (thanks to Ryhon0!).
  * Fixed `player_add` console command not handling journal scraps and secret notes correctly.
  * Fixed `set_farm_type` console command not updating warps.
* For the web UI:
  * Improved log parser UI (thanks to KhloeLeclair!):
    * Added pagination for big logs.
    * Added search box to filter the log.
    * Added option to show/hide content packs in the mod list.
    * Added jump links in the sidebar.
    * The filter options now stick to the top of the screen when scrolling.
    * Rewrote rendering to improve performance.

### For mod authors
This is a big release that includes the new features planned for SMAPI 4.0.0.

For C# mod authors: your mods should still work fine in SMAPI 3.14.0. However you should review the [migration to SMAPI 4.0](https://stardewvalleywiki.com/Modding:Migrate_to_SMAPI_4.0) guide and update your mods when possible. Deprecated code will be removed when SMAPI 4.0.0 releases later this year (no sooner than August 2022), and break any mods which haven't updated by that time. You can update affected mods now, there's no need to wait for 4.0.0.

For content pack authors: SMAPI 3.14.0 and 4.0.0 don't affect content packs. They should work fine as long as
the C# mod that loads them is updated.

* Major changes:
  * Added [content events](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events#Content), which will replace `IAssetEditor` and `IAssetLoader` in SMAPI 4.0.0.  
    _These include new features not supported by the old API like load conflict resolution, edit priority, and content pack labels. They also support new cases like easily detecting when an asset has changed, and avoid data corruption issues in some edge cases._
  * Added [nullable reference type annotations](https://stardewvalleywiki.com/Modding:Migrate_to_SMAPI_4.0#Nullable_reference_type_annotations) for all APIs.
  * Added [`helper.GameContent` and `helper.ModContent`](https://stardewvalleywiki.com/Modding:Migrate_to_SMAPI_4.0#Content_loading_API), which will replace `helper.Content` in SMAPI 4.0.0.
  * Improved [mod-provided API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Integrations#Mod-provided_APIs) proxying (thanks to Shockah!).  
    _This adds support for custom interfaces in return values or input arguments, custom enums if their values match, generic methods, and more. This is an internal change, you don't need to do anything different in your mod code._
  * Mod files loaded through SMAPI APIs (including `helper.Content.Load`) are now case-insensitive, even on Linux.
  * Enabled deprecation notices for all deprecated APIs. These will only be shown in `TRACE` logs for at least a month after SMAPI 3.14.0 releases.
* Other improvements:
  * Added `IAssetDataForImage.ExtendMap` to resize maps in asset editors.
  * Added `IContentPack.ModContent` property to manage content pack assets.
  * Added `Constants.ContentPath` to get the full path to the game's `Content` folder.
  * Added `IAssetName` fields to the info received by `IAssetEditor`, `IAssetLoader`, and content event methods.  
    _This adds methods for working with asset names, parsed locales, etc._
  * Added `helper.Content.ParseAssetName` to get an `IAssetName` for an arbitrary asset key.
  * Added [command-line arguments](technical/smapi.md#command-line-arguments) to toggle developer mode (thanks to Tondorian!).
  * If an asset is loaded multiple times in the same tick, `IAssetLoader.CanLoad` and `IAssetEditor.CanEdit` are now cached unless invalidated by `helper.Content.InvalidateCache`.
  * The `ISemanticVersion` comparison methods (`CompareTo`, `IsBetween`, `IsNewerThan`, and `IsOlderThan`) now allow null values. A null version is always considered older than any non-null version per [best practices](https://docs.microsoft.com/en-us/dotnet/api/system.icomparable-1.compareto#remarks).
  * Deprecation notices now show a shorter stack trace in most cases, so it's clearer where the deprecated code is in the mod.
* Fixes:
  * Fixed the `SDate` constructor being case-sensitive.
  * Fixed support for using locale codes from custom languages in asset names (e.g. `Data/Achievements.eo-EU`).
  * Fixed issue where suppressing `[Left|Right]Thumbstick[Down|Left]` keys would suppress the opposite direction instead.
  * Fixed null handling in various edge cases.
* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.25.0.
  * Added `data-*` attributes to the log parser page for external tools.
  * Fixed JSON validator showing incorrect error for update keys without a subkey.

### For SMAPI contributors
* You no longer need a Nexus API key to launch the `SMAPI.Web` project locally.

## 3.13.4
Released 16 January 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * Fixed Linux/macOS launch error in 3.13.3.

## 3.13.3
Released 16 January 2022 for Stardew Valley 1.5.6 or later.

* For players:
  * **SMAPI now needs Stardew Valley 1.5.6 or later.**
  * Added automatic fix for custom maps which are missing a required tilesheet.
  * Added automatic save recovery when a custom farm type isn't available anymore.
  * Added the game's new build number to the SMAPI console + log.
  * The installer now detects Xbox app game folders.
  * Reduced mod loading time a bit.
  * Fixed macOS launch issue when using some terminals (thanks to bruce2409!).
  * Fixed Linux/macOS terminal ignoring backspaces in Stardew Valley 1.5.5+.
  * Fixed extra newlines in the SMAPI console.
  * Fixed outdated instructions in Steam error message.
  * Fixed uninstaller not removing `StardewModdingAPI.deps.json` file.
  * Simplified [running without a terminal on Linux/macOS](https://stardewvalleywiki.com/Modding:Player_Guide/Troubleshooting#SMAPI_doesn.27t_recognize_controller_.28Steam_only.29) when needed.
  * Updated compatibility list.
  * Improved translations. Thanks to ChulkyBow (added Ukrainian)!

* For the web UI:
  * Added log instructions for Xbox app on Windows.
  * Added log download option.
  * Redesigned log instruction UI.
  * Fixed log parser not correctly handling multiple mods having the exact same name.
  * Fixed JSON validator not recognizing manifest [update subkeys](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Update_checks#Update_subkeys).

## 3.13.2
Released 05 December 2021 for Stardew Valley 1.5.5 or later.

* For players:
  * You no longer need .NET 5 to install or use SMAPI.
  * The installer now detects when the game folder contains an incompatible legacy game version.
  * Updated for the latest Stardew Valley 1.5.5 hotfix.
  * Updated compatibility list.

* For the web UI:
  * Fixed the JSON validator marking `.fnt` files invalid in Content Patcher files.

* For SMAPI maintainers:
  * Added [release package scripts](technical/smapi.md) to streamline preparing SMAPI releases.

## 3.13.1
Released 30 November 2021 for Stardew Valley 1.5.5 or later.

* For players:
  * Improved .NET 5 validation in Windows installer to better explain how to get the right version.
  * Fixed installer failing on Windows when run from the game folder.

## 3.13.0
Released 30 November 2021 for Stardew Valley 1.5.5 or later. See [release highlights](https://www.patreon.com/posts/59348226).

* For players:
  * Updated for Stardew Valley 1.5.5.
  * Added `set_farm_type` [console command](https://stardewvalleywiki.com/Modding:Console_commands#Console_commands) to change the current farm type.
  * Fixed installer window closing immediately if the installer crashed.
  * Updated compatibility list.

* For mod authors:
  * Migrated to 64-bit MonoGame and .NET 5 on all platforms (see [migration guide for mod authors](https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.5.5)).
  * Added support for [map overlays via `asset.AsMap().PatchMap`](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Content#Edit_a_map).
  * Added support for loading BmFont `.fnt` files for [custom languages](https://stardewvalleywiki.com/Modding:Custom_languages) through the [content API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Content).

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.24.0.

**Update note for players with older systems:**  
The game now has two branches: the _main branch_ which you'll get by default, and an optional
[_compatibility branch_ for older systems](https://www.stardewvalley.net/compatibility/). The two
branches have identical content, but use [different technologies](https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.5.5#Game_compatibility_branch).

Unfortunately **SMAPI only supports the main branch of the game**. There are formidable difficulties
across all mods in supporting all three variations, 32-bit imposes significant restrictions on what
mods can do, and the [Steam hardware stats](https://store.steampowered.com/hwsurvey) show that 99.69%
of players now have 64-bit.

## 3.12.8
Released 18 October 2021 for Stardew Valley 1.5.4.

* For players:
  * Fixed mod edits to the farmhouse shifting the player down one tile in some cases.
  * Fixed map tile rotations/flips not working for farmhands in split-screen mode.
  * Improved translations. Thanks to ellipszist (added Thai) and Zangorr (added Polish)!  
    _These are custom languages which require Stardew Valley 1.5.5 and the [Polish](https://www.nexusmods.com/stardewvalley/mods/3616) or [Thai](https://www.nexusmods.com/stardewvalley/mods/7052) mod._

* For mod authors:
  * SMAPI now intercepts dictionary duplicate-key errors and adds the key to the error message to simplify troubleshooting. (Due to Harmony limitations, this only works for the dictionary types used by the game.)
  * Fixed barn/coop exit warps being reset when you edit their interior map.

* For the web UI:
  * Added support for unified [mod data overrides](https://stardewvalleywiki.com/Modding:Mod_compatibility#Mod_data_overrides) defined on the wiki.
  * The mod compatibility list now shows separate beta stats when 'show advanced info' is enabled.

## 3.12.7
Released 18 September 2021 for Stardew Valley 1.5.4.

* For players:
  * Added more progress updates in the log during startup.
  * Simplified asset load error message.
  * Simplified exception logs.
  * Fixed crash loading mods with corrupted translation files.

* For mod authors:
  * Added asset propagation for `LooseSprites\Giftbox`.
  * Improved SMAPI's crossplatform read/writing of `Color`, `Point`, `Rectangle`, and `Vector2` in JSON to support nullable fields too.

* For the web UI:
  * The mod compatibility list now shows the beta status by default (if any).
  * Fixed JSON validator line numbers sometimes incorrect.

## 3.12.6
Released 03 September 2021 for Stardew Valley 1.5.4.

* For players:
  * Added friendly error when using SMAPI 3.2._x_ with Stardew Valley 1.5.5 or later.
  * Improved mod compatibility in 64-bit mode (thanks to spacechase0!).
  * Reduced load time when scanning/rewriting many mods for compatibility.
  * **Dropped support for unofficial 64-bit mode**. You can now use the [official 64-bit Stardew Valley 1.5.5 beta](https://stardewvalleywiki.com/Modding:Migrate_to_64-bit_on_Windows) instead.
  * Updated compatibility list.

* For mod authors:
  * Added `PathUtilities.NormalizeAssetName` and `PathUtilities.PreferredAssetSeparator` to prepare for the upcoming Stardew Valley 1.5.5.
  * **SMAPI no longer propagates changes to `Data/Bundles`.**  
    _You can still load/edit the asset like usual, but if bundles have already been loaded for a save, SMAPI will no longer dynamically update the in-game bundles to reflect the changes. Unfortunately this caused bundle corruption when playing in non-English._
  * Fixed content packs created via `helper.ContentPacks.CreateFake` or `CreateTemporary` not initializing translations correctly.

* For console commands:
  * Added `hurry_all` command which immediately warps all NPCs to their scheduled positions.

**Update note for mod authors:**  
Stardew Valley 1.5.5 will change how asset names are formatted. If you use `PathUtilities.NormalizePath`
to format asset names, you should switch to `PathUtilities.NormalizeAssetName` now so your code will
continue working in the next game update.

## 3.12.5
Released 26 August 2021 for Stardew Valley 1.5.4.

* Fixed some mods in unofficial 64-bit mode no longer loading after SMAPI 3.12.3.

## 3.12.4
Released 25 August 2021 for Stardew Valley 1.5.4.

* For players:
  * Fixed error loading some mods in SMAPI 3.12.3.

## 3.12.3
Released 25 August 2021 for Stardew Valley 1.5.4.

* For players:
  * Added friendly error in 64-bit mode when a mod is 32-bit only.
  * Fixed console encoding issues on Linux/macOS.
  * Fixed some installer errors not showing info header.

* For mod authors:
  * Added `helper.Translation.GetInAllLocales` to get a translation in every available locale.
  * Fixed Visual Studio debugger crash when any mods are rewritten for compatibility (thanks to spacechase0!).
  * Fixed `helper.Data.WriteJsonFile` not deleting the file if the model is null, unlike the other `Write*` methods.
  * Fixed error-handling for `StackOverflowException` thrown on Linux/macOS.
  * Internal changes to prepare for Stardew Valley 1.5.5.

* For the web API:
  * Fixed update checks not shown for prerelease mod versions when you have a SMAPI beta.
  * Fixed update checks shown for prerelease mod versions if you have a working non-prerelease version.

## 3.12.2
Released 05 August 2021 for Stardew Valley 1.5.4.

* For players:
  * Fixed error creating a new save or joining a multiplayer world in 3.12.1.

* For mod authors:
  * Reverted the `Constants.Save*` fix in SMAPI 3.12.1.  
    _The change caused a number of other issues, and is only needed for rare cases where the save folder was invalid. This may be revisited in a future version instead._
  * Fixed `NullReferenceException` in SMAPI's error-handling when trying to handle an invalid `ReflectionTypeLoadException`.

## 3.12.1
Released 03 August 2021 for Stardew Valley 1.5.4.

* For players:
  * The software conflict message is now shown as a warning to simplify troubleshooting.
  * Fixed error loading older Harmony mods for some Windows players using unofficial 64-bit Stardew Valley.
  * Updated compatibility list.

* For mod authors:
  * Fixed `Constants.Save*` fields incorrect if the save's folder name and ID don't match.

## 3.12.0
Released 01 August 2021 for Stardew Valley 1.5.4. See [release highlights](https://www.patreon.com/posts/54388616).

* For players:
  * Added save recovery when content mods leave null objects in the save (in _Error Handler_).
  * Added error if the wrong SMAPI bitness is installed (e.g. 32-bit SMAPI with 64-bit game).
  * Added error if some SMAPI files aren't updated correctly.
  * Added `removable` option to the `world_clear` console command (in _Console Commands_, thanks to bladeoflight16!).
  * Fixed handling of Unicode characters in console commands.
  * Fixed intermittent error if a mod gets mod-provided APIs asynchronously.
  * Fixed crash when creating a farm name containing characters that aren't allowed in a folder path.

* For mod authors:
  * **Updated Harmony 1.2.0.1 to 2.1.0 (see [_migrate to Harmony 2.0_](https://stardewvalleywiki.com/Modding:Migrate_to_Harmony_2.0) for more info).**
  * SMAPI now intercepts `KeyNotFoundException` errors and adds the key to the error message to simplify troubleshooting. (Due to Harmony limitations, this only works for the dictionary types used by the game.)
  * Fixed error loading `.xnb` files from the local mod folder.
  * Fixed reloading a map not correctly reapplying interior doors.

## 3.11.0
Released 09 July 2021 for Stardew Valley 1.5.4. See [release highlights](https://www.patreon.com/posts/53514295).

* For players:
  * Updated for Stardew Valley 1.4.5 multiplayer hotfix on Linux/macOS.
  * Fixed installer error on Windows when running as administrator (thanks to LostLogic!).
  * Fixed installer error on some Windows systems (thanks to eddyballs!).
  * Fixed error if SMAPI fails to dispose on game exit.
  * Fixed `player_add` and `list_items` console commands not including some shirts _(in Console Commands)_.

* For mod authors:
  * Added `World.FurnitureListChanged` event (thanks to DiscipleOfEris!).
  * Added asset propagation for building/house paint masks.
  * Added log message for troubleshooting if Windows software which often causes issues is installed (currently MSI Afterburner and RivaTuner).
  * Improved validation for the manifest `Dependencies` field.
  * Fixed validation for mods with invalid version `0.0.0`.
  * Fixed _loaded with custom settings_ trace log added when using default settings.
  * Fixed `Constants.SaveFolderName` and `Constants.CurrentSavePath` not set correctly in rare cases.

* For the web UI and JSON validator:
  * Updated the JSON validator/schema for Content Patcher 1.23.0.
  * Fixed [JSON schema](technical/web.md#using-a-schema-file-directly) in Visual Studio Code warning about comments and trailing commas.
  * Fixed JSON schema for `i18n` files requiring the wrong value for the `$schema` field.

## 3.10.1
Released 03 May 2021 for Stardew Valley 1.5.4.

* For players:
  * Fixed installer leaving an unneeded `StardewModdingAPI-x64.exe` file in 32-bit game folders.

## 3.10
Released 03 May 2021 for Stardew Valley 1.5.4. See [release highlights](https://www.patreon.com/posts/50764911).

* For players:
  * Added full support for the [unofficial 64-bit Stardew Valley patch](https://stardewvalleywiki.com/Modding:Migrate_to_64-bit_on_Windows), which removes memory limits. The installer detects which version of SMAPI you need, and SMAPI shows update alerts for Stardew64Installer if applicable.
  * Added smarter grouping for skipped mods, so it's easier to see root dependencies to update first.
  * Added crash recovery when the game can't update a map's seasonal tilesheets _(in Error Handler)_. SMAPI will log an error and keep the previous tilesheets in that case.
  * Added installer option to enter a custom game path even if it detected a game folder.
  * `*.ico` files are now ignored when scanning for mods.
  * Fixed error for non-English players after returning to title, reloading, and entering town with a completed movie theater.
  * Fixed `world_clear` console command not removing resource clumps outside the farm and secret woods.
  * Fixed error running SMAPI in a strict sandbox on Linux (thanks to kuesji!).
  * Fixed `StardewModdingAPI.bin.osx` on macOS overwritten with an identical file on launch which would reset file permissions (thanks to 007wayne!).
  * Fixed inconsistent spelling/style for 'macOS'.

* For modders:
  * Added support for [ignoring local map tilesheet files when loading a map](https://stardewvalleywiki.com/Modding:Maps#Local_copy_of_a_vanilla_tilesheet).
  * Added asset propagation for `Data\Concessions`.
  * Added SMAPI version and bitness to the console title before startup to simplify troubleshooting.
  * If a map loads a tilesheet path with no file extension, SMAPI now automatically links it to a `.png` version in the map folder if possible.
  * Improved error-handling during asset propagation.
  * Fixed `Context.IsMainPlayer` returning true for a farmhand in split-screen mode before the screen is initialized.
  * Fixed error when editing bundle data while a split-screen player is joining.
  * Fixed update subkeys not working in file descriptions for Nexus mods marked as adult content.

## 3.9.5
Released 21 March 2021 for Stardew Valley 1.5.4.

* For players:
  * Added console command to reset community center bundles _(in Console Commands)_.
  * Disabled aggressive memory optimization by default.  
    _The option was added in SMAPI 3.9.2 to reduce errors for some players, but it can cause multiplayer crashes with some mods. If you often see `OutOfMemoryException` errors, you can edit `smapi-internal/config.json` to re-enable it. We're experimenting with making Stardew Valley 64-bit to address memory issues more systematically._
  * Fixed bundles corrupted in non-English saves created after SMAPI 3.9.2.  
    _If you have an affected save, you can load your save and then enter the `regenerate_bundles confirm` command in the SMAPI console to fix it._
  * Internal changes to prepare for unofficial 64-bit.

* For mod authors:
  * Improved asset propagation:
    * Added for interior door sprites.
    * SMAPI now updates the NPC pathfinding cache when map warps are changed through the content API.
    * Reduced performance impact of invalidating cached assets before a save is loaded.
    * Fixed asset changes not reapplied in the edge case where you're playing in non-English, and the changes are only applied after the save is loaded, and the player returns to title and reloads a save, and the game reloads the target asset before the save is loaded.
  * Added a second `KeybindList` constructor to simplify single-key default bindings.
  * Added a `Constants.GameFramework` field which indicates whether the game is using XNA Framework or MonoGame.  
    _Note: mods don't need to handle the difference in most cases, but some players may use MonoGame on Windows in upcoming versions. Mods which check `Constants.TargetPlatform` should review usages as needed._

## 3.9.4
Released 07 March 2021 for Stardew Valley 1.5.4.

* For players:
  * Fixed installer error if the `Mods` folder doesn't exist in 3.9.3.

## 3.9.3
Released 07 March 2021 for Stardew Valley 1.5.4.

* For players:
  * Added descriptive error if possible when a `PathTooLongException` crashes SMAPI or the installer.
  * The installer window now tries to stay open if it crashed, so you can read the error and ask for help.
  * Fixed console showing _found 1 mod with warnings_ with no mods listed in some cases.

* For mod authors:
  * Added three stages to the specialised [`LoadStageChanged` event](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events#Specialised): `CreatedInitialLocations`, `SaveAddedLocations`, and `ReturningToTitle`.
  * Fixed `RewriteMods` option ignored when rewriting for OS compatibility.
  * Fixed edge case when playing as a farmhand in non-English where translatable assets loaded via `IAssetLoader` weren't reapplied immediately when the server disconnects.

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.21.

## 3.9.2
Released 21 February 2021 for Stardew Valley 1.5.4.

* For players:
  * Added more aggressive memory optimization to reduce `OutOfMemoryException` errors with some mods.
  * Improved error when `Stardew Valley.exe` exists but can't be loaded.
  * Fixed error running `install on Windows.bat` in very rare cases.
  * Fixed `world_settime` command not always updating outdoor ambient lighting _(in Console Commands)_.

* For mod authors:
  * Added early detection of disposed textures so the error details are more relevant _(in Error Handler)_.
  * Added error details when an event command fails _(in Error Handler)_.
  * Fixed asset propagation for `TileSheets/ChairTiles` not changing existing map seats.
  * Fixed edge case when playing in non-English where translatable assets loaded via `IAssetLoader` would no longer be applied after returning to the title screen unless manually invalidated from the cache.

* For the web UI:
  * Updated compatibility list for the new wiki.
  * Updated the JSON validator/schema for Content Patcher 1.20.
  * Fixed mod compatibility list error if a mod has no name.

* For SMAPI developers:
  * Fixed SMAPI toolkit defaulting the mod type incorrectly if a mod's `manifest.json` has neither `EntryDll` nor `ContentPackFor`. This only affects external tools, since SMAPI itself validates those fields separately.

## 3.9.1
Released 25 January 2021 for Stardew Valley 1.5.4.

* For players:
  * Fixed _tile contains an invalid TileSheet reference_ crash after mods change certain maps.
  * Fixed _patched game code_ issue shown for the bundled Error Handler mod.

## 3.9
Released 22 January 2021 for Stardew Valley 1.5.4. See [release highlights](https://www.patreon.com/posts/46553874).

* For players:
  * Updated for Stardew Valley 1.5.4.
  * Improved game detection in the installer:
    * The installer now prefers paths registered by Steam or GOG Galaxy.
    * The installer now detects default manual GOG installs.
  * Added clearer error text for empty mod folders created by Vortex.
  * Fixed the game's map changes not always reapplied correctly after mods change certain maps, which caused issues like the community center resetting to its non-repaired texture.
  * Fixed compatibility for very old content packs which still load maps from `.xnb` files. These were broken by map loading changes in Stardew Valley 1.5, but SMAPI now corrects them automatically.
  * Fixed some broken mods incorrectly listed as XNB mods under 'skipped mods'.

* For mod authors:
  * Added new input APIs:
    * Added an [API for multi-key bindings](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Input#KeybindList).
    * Added a new [`Input.ButtonsChanged` event](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events#Input.ButtonsChanged).
    * Added a `buttonState.IsDown()` extension.
    * Added a `helper.Input.SuppressActiveKeybinds` method to suppress the active buttons in a keybind list.
  * Improved multiplayer APIs:
    * `PerScreen<T>` now lets you get/set the value for any screen, get all active values, or clear all values.
    * Peer data from the multiplayer API/events now includes `IsSplitScreen` and `ScreenID` fields.
    * Fixed network messages through the multiplayer API being sent to players who don't have SMAPI installed in some cases.
  * Improved asset propagation:
    * Updated map propagation for the changes in Stardew Valley 1.5.4.
    * Added propagation for some `Strings\StringsFromCSFiles` keys (mainly short day names).
    * Fixed quarry bridge not fixed if the mountain map was reloaded.
  * Added an option to disable rewriting mods for compatibility (thanks to Bpendragon!). This prevents older mods from loading, but bypasses a Visual Studio crash when debugging.
  * Game errors shown in the chatbox are now logged.
  * Moved vanilla error-handling into a new Error Handler mod. This simplifies the core SMAPI logic, and lets users disable it if needed.

* For the Console Commands mod:
  * Removed the `inf` option for `player_sethealth`, `player_setmoney`, and `player_setstamina`. You can use mods like [CJB Cheats Menu](https://www.nexusmods.com/stardewvalley/mods/4) instead for that.

* For the Error Handler mod:
  * Added a detailed message for the _Input string was not in a correct format_ error when the game fails to parse an item text description.

* For the web UI:
  * Fixed JSON validator incorrectly marking some manifest update keys as invalid.

## 3.8.4
Released 15 January 2021 for Stardew Valley 1.5.3 or later.

* For players:
  * Updated for Stardew Valley 1.5.3.
  * Fixed issue where title screen music didn't stop after loading a save.

* For mod authors:
  * Fixed `SemanticVersion` comparisons returning wrong value in rare cases.

## 3.8.3
Released 08 January 2021 for Stardew Valley 1.5.2 or later.

* For players:
  * Updated for Stardew Valley 1.5.2.
  * Reduced memory usage.
  * You can now enter console commands for a specific screen in split-screen mode by adding `screen=ID` to the command.
  * Typing `help` in the SMAPI console is now more helpful.

* For mod authors:
  * Simplified tilesheet order warning added in SMAPI 3.8.2.

* For the Console Commands mod:
  * Removed experimental `performance` command. Unfortunately this impacted SMAPI's memory usage and performance, and the data was often misinterpreted. This may be replaced with more automatic performance alerts in a future version.

## 3.8.2
Released 03 January 2021 for Stardew Valley 1.5.1 or later.

* For players:
  * SMAPI now blocks farm map replacements that would crash the game in Stardew Valley 1.5.
  * On Linux, the SMAPI installer now auto-detects Flatpak Steam paths.
  * Updated compatibility list.
  * Fixed errors when multiple players join in split-screen mode.
  * Fixed 'skipped mods' section repeating mods in some cases.
  * Fixed out-of-date error text.

* For mod authors:
  * Added warning when a map replacement changes the order/IDs of the original tilesheets, which may cause errors and crashes. Doing so for a farm map is blocked outright since that causes a consistent crash in Stardew Valley 1.5.
  * Message data from the `ModMessageReceived` event now uses the same serializer settings as the rest of SMAPI. That mainly adds support for sending crossplatform `Color`, `Point`, `Vector2`, `Rectangle`, and `SemanticVersion` fields through network messages.
  * When a mod is blocked by SMAPI's compatibility override list, the `TRACE` messages while loading it now say so and indicate why.
  * Fixed how the input API handles UI scaling. This mainly affects `ICursorPosition` values returned by the API; see [the wiki docs](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Input#ICursorPosition) for how to account for UI scaling.

## 3.8.1
Released 26 December 2020 for Stardew Valley 1.5.1 or later.

* For players:
  * Fixed broken community center bundles for non-English saves created in Stardew Valley 1.5. Affected saves will be fixed automatically on load.

* For mod authors:
  * World events are now raised for volcano dungeon levels.
  * Added `apply_save_fix` command to reapply a save migration in exceptional cases. This should be used very carefully. Type `help apply_save_fix` for details.
  * **Deprecation notice:** the `Helper.ConsoleCommands.Trigger` method is now deprecated and should no longer be used. See [integration APIs](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Integrations) for better mod integration options. It will eventually be removed in SMAPI 4.0.

For the web UI:
  * Fixed edge cases in SMAPI log parsing.

## 3.8
Released 21 December 2020 for Stardew Valley 1.5 or later. See [release highlights](https://www.patreon.com/posts/45294737).

* For players:
  * Updated for Stardew Valley 1.5, including split-screen support.
  * You can now run the installer from a subfolder of your game folder to auto-detect it. That simplifies installation if you have multiple copies of the game or it can't otherwise auto-detect the game path.
  * Clarified error when the SMAPI installer is in the `Mods` folder.

* For mod authors:
  * Added `PerScreen<T>` utility and new `Context` fields to simplify split-screen support in mods.
  * Added screen ID to log when playing in split-screen mode.

* For the Console Commands mod:
  * Added `furniture` option to `world_clear`.

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.19.

## 3.7.6
Released 21 November 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Fixed error when heuristically rewriting an outdated mod in rare cases.
  * Fixed rare 'collection was modified' error when using `harmony summary` console command.

* For mod authors:
  * Updated TMXTile to 1.5.8 to fix exported `.tmx` files losing tile index properties.

* For the Console Commands mod:
  * `player_add` can now spawn shirts normally only available during character customization.
  * `player_add` now applies fish pond rules for roe items. (That mainly adds Clam Roe, Sea Urchin Roe, and custom roe from mods.)

## 3.7.5
Released 16 October 2020 for Stardew Valley 1.4.1 or later.

* For mod authors:
  * Fixed changes to the town map asset not reapplying the game's community center, JojaMart, and Pam house changes.

## 3.7.4
Released 03 October 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Improved performance on some older computers (thanks to millerscout!).
  * Fixed update alerts for Chucklefish forum mods broken by a recent site change.

* For mod authors:
  * Updated dependencies (including Mono.Cecil 0.11.2 → 0.11.3 and Platonymous.TMXTile 1.3.8 → 1.5.6).
  * Fixed asset propagation for `Data\MoviesReactions`.
  * Fixed error in content pack path handling when you pass a null path.

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.18.

* For SMAPI developers:
  * Simplified preparing a mod build config package release.

## 3.7.3
Released 16 September 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Fixed errors on Linux/macOS due to content packs with incorrect filename case.
  * Fixed map rendering crash due to conflict between SMAPI and PyTK.
  * Fixed error in heuristically-rewritten mods in rare cases (thanks to collaboration with ZaneYork!).

* For mod authors:
  * File paths accessed through `IContentPack` are now case-insensitive (even on Linux).

* For the web UI:
  * You can now renew the expiry for an uploaded JSON/log file if you need it longer.

## 3.7.2
Released 08 September 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Fixed mod recipe changes not always applied in 3.7.

* For mod authors:
  * Renamed `PathUtilities.NormalizePathSeparators` to `NormalizePath`, and added normalization for more cases.

## 3.7.1
Released 08 September 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Fixed input-handling bugs in 3.7.

## 3.7
Released 07 September 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/41341767).

* For players:
  * Added heuristic compatibility rewrites. (This improves mod compatibility with Android and future game updates.)
  * Tweaked the rules for showing update alerts (see _for SMAPI developers_ below for details).
  * Simplified the error shown for duplicate mods.
  * Fixed crossplatform compatibility for mods which use the `[HarmonyPatch(type)]` attribute (thanks to spacechase0!).
  * Fixed map tile rotation broken when you return to the title screen and reload a save.
  * Fixed broken URL in update alerts for unofficial versions.
  * Fixed rare error when a mod adds/removes event handlers asynchronously.
  * Fixed rare issue where the console showed incorrect colors when mods wrote to it asynchronously.
  * Fixed SMAPI not always detecting broken field references in mod code.
  * Removed the experimental `RewriteInParallel` option added in SMAPI 3.6 (it was already disabled by default). Unfortunately this caused intermittent and unpredictable errors when enabled.
  * Internal changes to prepare for upcoming game updates.

* For mod authors:
  * Added `PathUtilities` to simplify working with file/asset names.
  * You can now read/write `SDate` values to JSON (e.g. for `config.json`, network mod messages, etc).
  * Fixed asset propagation not updating title menu buttons immediately on Linux/macOS.

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.16 and 1.17.

* For SMAPI developers:
  * The web API now returns an update alert in two new cases: any newer unofficial update (previously only shown if the mod was incompatible), and a newer prerelease version if the installed non-prerelease version is broken (previously only shown if the installed version was prerelease).
  * Reorganised the SMAPI core to reduce coupling to game types like `Game1`, make it easier to navigate, and simplify future game updates.
  * SMAPI now automatically fixes code broken by these changes in game code, so manual rewriters are no longer needed:
    * reference to a method with new optional parameters;
    * reference to a field replaced by a property;
    * reference to a field replaced by a `const` field.
  * `FieldReplaceRewriter` now supports mapping to a different target type.

## 3.6.2
Released 02 August 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Improved compatibility with some Linux terminals (thanks to jlaw and Spatterjaaay!).
  * Fixed rare error when a mod adds/removes an event handler from an event handler.
  * Fixed string sorting/comparison for some special characters.

* For the Console Commands mod:
  * Fixed error opening menu when some item data is invalid.
  * Fixed spawned Floor TV not functional as a TV (thanks to Platonymous!).
  * Fixed spawned sturgeon roe having incorrect color.

* For mod authors:
  * Updated internal dependencies.
  * SMAPI now ignores more file types when scanning for mod folders (`.doc`, `.docx`, `.rar`, and `.zip`).
  * Added current GPU to trace logs to simplify troubleshooting.

## 3.6.1
Released 21 June 2020 for Stardew Valley 1.4.1 or later.

* Fixed event priority sorting.

## 3.6
Released 20 June 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/38441800).

* For players:
  * Added crossplatform compatibility for mods which use the `[HarmonyPatch(type)]` attribute.
  * Added experimental option to reduce startup time when loading mod DLLs (thanks to ZaneYork!). Enable `RewriteInParallel` in the `smapi-internal/config.json` to try it.
  * Reduced processing time when a mod loads many unpacked images (thanks to Entoarox!).
  * Mod load warnings are now listed alphabetically.
  * macOS files starting with `._` are now ignored and can no longer cause skipped mods.
  * Simplified paranoid warning logs and reduced their log level.
  * Fixed black maps on Android for mods which use `.tmx` files.
  * Fixed `BadImageFormatException` error detection.
  * Fixed `reload_i18n` command not reloading content pack translations.

* For the web UI:
  * Added GitHub licenses to mod compatibility list.
  * Improved JSON validator:
    * added SMAPI `i18n` schema;
    * editing an uploaded file now remembers the selected schema;
    * changed default schema to plain JSON.
  * Updated ModDrop URLs.
  * Internal changes to improve performance and reliability.

* For mod authors:
  * Added [event priorities](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events#Custom_priority) (thanks to spacechase0!).
  * Added [update subkeys](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Update_checks#Update_subkeys).
  * Added [a custom build of Harmony](https://github.com/Pathoschild/Harmony#readme) to provide more useful stack traces in error logs.
  * Added `harmony_summary` console command to list or search current Harmony patches.
  * Added `Multiplayer.PeerConnected` event.
  * Added support for overriding update keys from the wiki compatibility list.
  * Improved mod rewriting for compatibility to support more cases (e.g. custom attributes and generic types).
  * Fixed `helper.Reflection` blocking access to game methods/properties intercepted by SMAPI.
  * Fixed asset propagation for Gil's portraits.
  * Fixed `.pdb` files ignored for error stack traces when mods are rewritten by SMAPI.
  * Fixed `ModMessageReceived` event handlers not tracked for performance monitoring.

* For SMAPI developers:
  * Eliminated MongoDB storage in the web services, which complicated the code unnecessarily. The app still uses an abstract interface for storage, so we can wrap a distributed cache in the future if needed.
  * Overhauled update checks to simplify mod site integrations, centralize common logic, and enable upcoming features.
  * Merged the separate legacy redirects app on AWS into the main app on Azure.
  * Changed SMAPI's Harmony ID from `io.smapi` to `SMAPI` for readability in Harmony summaries.

## 3.5
Released 27 April 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/36471055).

* For players:
  * SMAPI now prevents more game errors due to broken items, so you no longer need save editing to remove them.
  * Added option to disable console colors.
  * Updated compatibility list.
  * Improved translations.¹

* For the Console Commands mod:
  * Commands like `world_setday` now also affect the 'days played' stat, so in-game events/randomization match what you'd get if you played to that date normally (thanks to kdau!).

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.13.
  * Fixed rare intermittent "CGI application encountered an error" errors.

* For mod authors:
  * Added map patching to the content API (via `asset.AsMap()`).
  * Added support for using patch helpers with arbitrary data (via `helper.Content.GetPatchHelper`).
  * Added `SDate` fields/methods: `SeasonIndex`, `FromDaysSinceStart`, `FromWorldDate`, `ToWorldDate`, and `ToLocaleString` (thanks to kdau!).
  * Added `SDate` translations taken from the Lookup Anything mod.¹
  * Fixed asset propagation for certain maps loaded through temporary content managers. This notably fixes unreliable patches to the farmhouse and town maps.
  * Fixed asset propagation on Linux/macOS for monster sprites, NPC dialogue, and NPC schedules.
  * Fixed asset propagation for NPC dialogue sometimes causing a spouse to skip marriage dialogue or not allow kisses.

¹ Date format translations were taken from the Lookup Anything mod; thanks to translators FixThisPlz (improved Russian), LeecanIt (added Italian), pomepome (added Japanese), S2SKY (added Korean), Sasara (added German), SteaNN (added Russian), ThomasGabrielDelavault (added Spanish), VincentRoth (added French), Yllelder (improved Spanish), and yuwenlan (added Chinese). Some translations for Korean, Hungarian, and Turkish were derived from the game translations.

## 3.4.1
Released 24 March 2020 for Stardew Valley 1.4.1 or later.

* For mod authors:
  * Asset changes now propagate to NPCs in an event (e.g. wedding sprites).
  * Fixed mouse input suppression not working in SMAPI 3.4.

## 3.4
Released 22 March 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/35161371).

* For players:
  * Fixed semi-transparency issues on Linux/macOS in recent versions of Mono (e.g. pink shadows).
  * Fixed `player_add` command error if you have broken XNB mods.
  * Removed invalid-location check now handled by the game.
  * Updated translations. Thanks to Annosz (added Hungarian)!

* For mod authors:
  * Added support for flipped and rotated map tiles (thanks to collaboration with Platonymous!).
  * Added support for `.tmx` maps using zlib compression (thanks to Platonymous!).
  * Added `this.Monitor.LogOnce` method.
  * Mods are no longer prevented from suppressing key presses in the chatbox.

* For the web UI:
  * Added option to upload files using a file picker.
  * Optimized log parser for very long multi-line log messages.
  * Fixed log parser not detecting folder path in recent versions of SMAPI.

* For SMAPI developers:
  * Added internal API to send custom input to the game/mods. This is mainly meant to support Virtual Keyboard on Android, but might be exposed as a public API in future versions.

## 3.3.2
Released 22 February 2020 for Stardew Valley 1.4.1 or later.

* Fixed mods receiving their own message broadcasts.

## 3.3.1
Released 22 February 2020 for Stardew Valley 1.4.1 or later.

* Fixed errors with custom spouse room mods in SMAPI 3.3.

## 3.3
Released 22 February 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/34248719).

* For players:
  * Improved performance for mods which load many images.
  * Reduced network traffic for mod broadcasts to players who can't process them.
  * Fixed update-check errors for recent versions of SMAPI on Android.
  * Updated draw logic to match recent game updates.
  * Updated compatibility list.
  * Updated SMAPI/game version map.
  * Updated translations. Thanks to xCarloC (added Italian)!

* For the Save Backup mod:
  * Fixed warning on macOS when you have no saves yet.
  * Reduced log messages.

* For the web UI:
  * Updated the JSON validator and Content Patcher schema for `.tmx` support.
  * The mod compatibility page now has a sticky table header.

* For mod authors:
  * Added support for [message sending](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Integrations#Message_sending) to mods on the current computer (in addition to remote computers).
  * Added `ExtendImage` method to content API when editing files to resize textures.
  * Added `helper.Input.GetState` to get the low-level state of a button.
  * **[Breaking change]** Map tilesheets are no loaded from `Content` if they can't be found in `Content/Maps`. This reflects an upcoming change in the game to delete duplicate map tilesheets under `Content`. Most mods should be unaffected.
  * Improved map tilesheet errors so they provide more info.
  * When mods load an asset using a more general type like `content.Load<object>`, SMAPI now calls `IAssetEditor` instances with the actual asset type instead of the specified one.
  * Updated dependencies (including Mono.Cecil 0.11.1 → 0.11.2).
  * Fixed dialogue propagation clearing marriage dialogue.

* For SMAPI/tool developers:
  * Improved support for four-part versions to support SMAPI on Android.
  * The SMAPI log now prefixes the OS name with `Android` on Android.

## 3.2
Released 01 February 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/33659728).

* For players:
  * SMAPI now prevents crashes due to invalid schedule data.
  * SMAPI now prevents crashes due to invalid building types.
  * Added support for persistent `smapi-internal/config.json` overrides (see info in the file).
  * Updated minimum game version (1.4 → 1.4.1).
  * Fixed 'collection was modified' error when returning to title in rare cases.
  * Fixed error when update-checking a mod with a Chucklefish page that has no version.
  * Fixed rare error when building/demolishing buildings.
  * Fixed SMAPI beta versions not showing update alert on next launch (thanks to danvolchek!).

* For the Console Commands mod:
  * Added `performance` command to track mod performance metrics. This is an advanced experimental feature. (Thanks to Drachenkätzchen!)
  * Added `test_input` command to view button codes in the console.

* For the Save Backup mod:
  * Fixed extra files under `Saves` (e.g. manual backups) not being ignored.
  * Fixed Android issue where game files were backed up.

* For mod authors:
  * Added support for `.tmx` map files. (Thanks to [Platonymous for the underlying library](https://github.com/Platonymous/TMXTile)!)
  * Added special handling for `Vector2` values in `.json` files, so they work consistently crossplatform.
  * Reworked the order that asset editors/loaders are called between multiple mods to support some framework mods like Content Patcher and Json Assets. Note that the order is undefined and should not be depended on.
  * Fixed incorrect warning about mods adding invalid schedules in some cases. The validation was unreliable, and has been removed.
  * Fixed asset propagation not updating other players' sprites.
  * Fixed asset propagation for player sprites not updating recolor maps (e.g. sleeves).
  * Fixed asset propagation for marriage dialogue.
  * Fixed dialogue asset changes not correctly propagated until the next day.
  * Fixed `helper.Data.Read`/`WriteGlobalData` using the `Saves` folder instead of the game's appdata folder. The installer will move existing folders automatically.
  * Fixed issue where a mod which implemented `IAssetEditor`/`IAssetLoader` on its entry class could then remove itself from the editor/loader list.

* For SMAPI/tool developers:
  * Added internal performance monitoring (thanks to Drachenkätzchen!). This is disabled by default in the current version, but can be enabled using the `performance` console command.
  * Added internal support for four-part versions to support SMAPI on Android.
  * Rewrote `SemanticVersion` parsing.
  * Updated links for the new r/SMAPI subreddit.
  * The `/mods` web API endpoint now includes version mappings from the wiki.
  * Dropped API support for the pre-3.0 update-check format.

## 3.1
Released 05 January 2019 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/32904041).

* For players:
  * Added separate group in 'skipped mods' list for broken dependencies, so it's easier to see what to fix first.
  * Added friendly log message for save file-not-found errors.
  * Updated for gamepad modes in Stardew Valley 1.4.1.
  * Improved performance in some cases.
  * Fixed compatibility with Linux Mint 18 (thanks to techge!), Arch Linux, and Linux systems with libhybris-utils installed.
  * Fixed memory leak when repeatedly loading a save and returning to title.
  * Fixed memory leak when mods reload assets.
  * Updated translations. Thanks to L30Bola (added Portuguese), PlussRolf (added Spanish), and shirutan (added Japanese)!

* For the Console Commands mod:
  * Added new clothing items.
  * Fixed spawning new flooring and rings (thanks to Mizzion!).
  * Fixed spawning custom rings added by mods.
  * Fixed errors when some item data is invalid.

* For the web UI:
  * Added option to edit & reupload in the JSON validator.
  * File uploads are now stored in Azure storage instead of Pastebin, due to ongoing Pastebin perfomance issues.
  * File uploads now expire after one month.
  * Updated the JSON validator for Content Patcher 1.10 and 1.11.
  * Fixed JSON validator no longer letting you change format when viewing a file.
  * Fixed JSON validator for Content Patcher not requiring `Default` if `AllowBlank` was omitted.
  * Fixed log parser not correctly handling content packs with no author (thanks to danvolchek!).
  * Fixed main sidebar link pointing to wiki instead of home page.

* For mod authors:
  * Added `World.ChestInventoryChanged` event (thanks to collaboration with wartech0!).
  * Added asset propagation for...
    * grass textures;
    * winter flooring textures;
    * `Data\Bundles` changes (for added bundles only);
    * `Characters\Farmer\farmer_girl_base_bald`.
  * Added paranoid-mode warning for direct `Console` access.
  * Improved error messages for `TargetParameterCountException` when using the reflection API.
  * `helper.Read/WriteSaveData` can now be used while a save is being loaded (e.g. within a `Specialized.LoadStageChanged` event).
  * Removed `DumpMetadata` option. It was only for specific debugging cases, but players would sometimes enable it incorrectly and then report crashes.
  * Fixed private textures loaded from content packs not having their `Name` field set.

* For SMAPI developers:
  * You can now run local environments without configuring Amazon, Azure, MongoDB, and Pastebin accounts.

## 3.0.1
Released 02 December 2019 for Stardew Valley 1.4 or later.

* For players:
  * Updated for Stardew Valley 1.4.0.1.
  * Improved compatibility with some Linux terminals (thanks to archification and DanielHeath!).
  * Updated translations. Thanks to berkayylmao (added Turkish), feathershine (added Chinese), and Osiris901 (added Russian)!

* For the web UI:
  * Rebuilt web infrastructure to handle higher traffic.
  * If a log can't be uploaded to Pastebin (e.g. due to rate limits), it's now uploaded to Amazon S3 instead. Logs uploaded to S3 expire after one month.
  * Fixed JSON validator not letting you drag & drop a file.

* For mod authors:
  * `SemanticVersion` now supports [semver 2.0](https://semver.org/) build metadata.

## 3.0
Released 26 November 2019 for Stardew Valley 1.4.

### Release highlights
For players:
* **Updated for Stardew Valley 1.4.**  
  SMAPI 3.0 adds compatibility with the latest game version, and improves mod APIs for changes in
  the game code.

* **Improved performance.**  
  SMAPI should have less impact on game performance and startup time for some players.

* **Automatic save fixing and more error recovery.**  
  SMAPI now detects and prevents more crashes due to game/mod bugs, and automatically fixes your
  save if you remove some custom-content mods.

* **Improved mod scanning.**  
  SMAPI now supports some non-standard mod structures automatically, improves compatibility with
  the Vortex mod manager, and improves various error/skip messages related to mod loading.

* **Overhauled update checks.**  
  SMAPI update checks are now handled entirely on the web server and support community-defined
  version mappings. In particular, false update alerts due to author mistakes can now be solved by
  the community for all players.

* **Fixed many bugs and edge cases.**

For mod authors:
* **New event system.**  
  SMAPI 3.0 removes the deprecated static events in favor of the new `helper.Events` API. The event
  engine is rewritten to make events more efficient, add events that weren't possible before, make
  existing events more useful, and make event usage and behavior more consistent. When a mod makes
  changes in an event handler, those changes are now also reflected in the next event raise.

* **Improved mod build package.**  
  The [mod build package](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig) now
  includes the `assets` folder by default if present, supports the new `.csproj` project format,
  enables mod `.pdb` files automatically (to provide line numbers in error messages), adds optional
  Harmony support, and fixes some bugs and edge cases. This also adds compatibility with SMAPI 3.0
  and Stardew Valley 1.4, and drops support for older versions.

* **Mods loaded earlier.**  
  SMAPI now loads mods much earlier, before the game is initialised. That lets mods do things that
  were difficult before, like intercepting some core assets.

* **Improved Android support.**  
  SMAPI now automatically detects when it's running on Android, and updates `Constants.TargetPlatform`
  so mods can adjust their logic if needed. The Save Backup mod is also now Android-compatible.

* **Improved asset propagation.**  
  SMAPI now automatically propagates asset changes for farm animal data, NPC default location data,
  critter textures, and `DayTimeMoneyBox` buttons. Every loaded texture now also has a `Name` field
  so mods can check which asset a texture was loaded for.

* **Breaking changes:**  
  See _[migrate to SMAPI 3.0](https://stardewvalleywiki.com/Modding:Migrate_to_SMAPI_3.0)_ and
  _[migrate to Stardew Valley 1.4](https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.4)_
  for more info.

### For players
* Changes:
  * Updated for Stardew Valley 1.4.
  * Improved performance.
  * Reworked update checks and added community-defined version mapping, to reduce false update alerts due to author mistakes.
  * SMAPI now removes invalid locations/NPCs when loading a save to prevent crashes. A warning is shown in-game when this happens.
  * Added update checks for CurseForge mods.
  * Added support for editing console colors via `smapi-internal/config.json` (for players with unusual consoles).
  * Added support for setting SMAPI CLI arguments as environment variables for Linux/macOS compatibility.
  * Improved mod scanning:
    * Now ignores metadata files/folders (like `__MACOSX` and `__folder_managed_by_vortex`) and content files (like `.txt` or `.png`), which avoids missing-manifest errors in some cases.
    * Now detects XNB mods more accurately, and consolidates multi-folder XNB mods in logged messages.
  * Improved launch script compatibility on Linux (thanks to kurumushi and toastal!).
  * Made error messages more user-friendly in some cases.
  * Save Backup now works in the background, to avoid affecting startup time for players with a large number of saves.
  * The installer now recognises custom game paths stored in [`stardewvalley.targets`](http://smapi.io/package/custom-game-path).
  * Duplicate-mod errors now show the mod version in each folder.
  * Update checks are now faster in some cases.
  * Updated mod compatibility list.
  * Updated SMAPI/game version map.
  * Updated translations. Thanks to eren-kemer (added German)!
* Fixes:
  * Fixed some assets not updated when you switch language to English.
  * Fixed lag in some cases due to incorrect asset caching when playing in non-English.
  * Fixed lag when a mod invalidates many NPC portraits/sprites at once.
  * Fixed Console Commands not including upgraded tools in item commands.
  * Fixed Console Commands' item commands failing if a mod adds invalid item data.
  * Fixed Save Backup not pruning old backups if they're uncompressed.
  * Fixed issues when a farmhand reconnects before the game notices they're disconnected.
  * Fixed 'received message' logs shown in non-developer mode.
  * Fixed various error messages and inconsistent spelling.
  * Fixed update-check error if a Nexus mod is marked as adult content.
  * Fixed update-check error if the Chucklefish page for an update key doesn't exist.

### For the web UI
* Mod compatibility list:
  * Added support for CurseForge mods.
  * Added metadata links and dev notes (if any) to advanced info.
  * Now loads faster (since data is fetched in a background service).
  * Now continues working with cached data when the wiki is offline.
  * Clicking a mod link now automatically adds it to the visible mods if the list is filtered.

* JSON validator:
  * Added JSON validator at [smapi.io/json](https://smapi.io/json), which lets you validate a JSON file against predefined mod formats.
  * Added support for the `manifest.json` format.
  * Added support for the Content Patcher format (thanks to TehPers!).
  * Added support for referencing a schema in a JSON Schema-compatible text editor.

* For the log parser:
  * Added instructions for Android.
  * The page now detects your OS and preselects the right instructions (thanks to danvolchek!).

### For mod authors
* Breaking changes:
  * Mods are now loaded much earlier in the game launch. This lets mods intercept any content asset, but the game is not fully initialized when `Entry` is called; use the `GameLaunched` event if you need to run code when the game is initialized.
  * Removed all deprecated APIs.
  * Removed unused APIs: `Monitor.ExitGameImmediately`, `Translation.ModName`, and `Translation.Assert`.
  * Fixed `ICursorPosition.AbsolutePixels` not adjusted for zoom.
  * `SemanticVersion` no longer omits `.0` patch numbers when formatting versions, for better [semver](https://semver.org/) conformity (e.g. `3.0` is now formatted as `3.0.0`).
* Changes:
  * Added support for content pack translations.
  * Added `IContentPack.HasFile`, `Context.IsGameLaunched`, and `SemanticVersion.TryParse`.
  * Added separate `LogNetworkTraffic` option to make verbose logging less overwhelmingly verbose.
  * Added asset propagation for `Data\FarmAnimals`, critter textures, and `DayTimeMoneyBox` buttons.
  * Added `Texture2D.Name` values set to the asset key.
  * Added trace logs for skipped loose files in the `Mods` folder and custom SMAPI settings so it's easier to troubleshoot player logs.
  * `Constants.TargetPlatform` now returns `Android` when playing on an Android device.
  * Trace logs for a broken mod now list all detected issues (instead of the first one).
  * Trace logs when loading mods are now more clear.
  * Clarified update-check errors for mods with multiple update keys.
  * Updated dependencies (including Json.NET 11.0.2 → 12.0.3 and Mono.Cecil 0.10.1 → 0.11.1).
* Fixes:
  * Fixed map reloads resetting tilesheet seasons.
  * Fixed map reloads not updating door warps.
  * Fixed outdoor tilesheets being seasonalised when added to an indoor location.
  * Fixed mods needing to load custom `Map` assets before the game accesses them. SMAPI now does so automatically.
  * Fixed custom maps loaded from `.xnb` files not having their tilesheet paths automatically adjusted.
  * Fixed custom maps loaded from the mod folder with tilesheets in a subfolder not working crossplatform. All tilesheet paths are now normalized for the OS automatically.
  * Fixed issue where mod changes weren't tracked correctly for raising events in some cases. Events now reflect a frozen snapshot of the game state, and any mod changes are reflected in the next event tick.
  * Fixed issue where, when a mod's `IAssetEditor` uses `asset.ReplaceWith` on a texture asset while playing in non-English, any changes from that point forth wouldn't affect subsequent cached asset loads.
  * Fixed asset propagation for NPC portraits resetting any unique portraits (e.g. Maru's hospital portrait) to the default.
  * Fixed changes to `Data\NPCDispositions` not always propagated correctly to existing NPCs.
  * Fixed `Rendering`/`Rendered` events not raised during minigames.
  * Fixed `LoadStageChanged` event not raising correct flags in some cases when creating a new save.
  * Fixed `GetApi` without an interface not checking if all mods are loaded.

### For SMAPI maintainers
* Added support for core translation files.
* Migrated to new `.csproj` format.
* Internal refactoring.

## 2.11.3 and earlier
See [older release notes](release-notes-archived.md).

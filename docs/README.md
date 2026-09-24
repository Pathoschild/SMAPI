**SMAPI** is an open-source modding framework and API for [Stardew Valley](https://stardewvalley.net/)
that lets you play the game with mods. It's safely installed alongside the game's executable, and
doesn't change any of your game files. It serves seven main purposes:

1. **Load mods into the game.**  
   _SMAPI loads mods when the game is starting up so they can interact with it. (Code mods aren't
   possible without SMAPI to load them.)_

2. **Provide APIs and events for mods.**  
   _SMAPI provides APIs and events which let mods interact with the game in ways they otherwise
   couldn't._

3. **Rewrite mods for compatibility.**  
   _SMAPI rewrites mods' compiled code before loading them so they work on Linux/macOS/Windows
   without the mods needing to handle differences between the Linux/macOS and Windows versions of
   the game. In some cases it also rewrites code broken by a game update so the mod doesn't break._

5. **Intercept errors and automatically fix saves.**  
   _SMAPI intercepts errors, shows the error info in the SMAPI console, and in most cases
   automatically recovers the game. That prevents mods from crashing the game, and makes it
   possible to troubleshoot errors in the game itself that would otherwise show a generic 'program
   has stopped working' type of message._

   _SMAPI also automatically fixes save data in some cases when a load would crash, e.g. due to a
   custom location or NPC mod that was removed._

6. **Provide update checks.**  
   _SMAPI automatically checks for new versions of your installed mods, and notifies you when any
   are available._

7. **Provide compatibility checks.**  
   _SMAPI automatically detects outdated or broken code in mods, and safely disables them before
   they cause problems._

8. **Back up your save files.**  
   _SMAPI automatically creates a daily backup of your saves and keeps ten backups (via the bundled
   Save Backup mod), in case something goes wrong._

## Documentation
Have questions? Come [ask the community](https://smapi.io/community) to get help from SMAPI
developers and other modders!

### For players
* [Player guide](https://stardewvalleywiki.com/Modding:Player_Guide)

### For modders
* [Modding documentation](https://smapi.io/docs)
* [Mod build configuration](technical/mod-package.md)
* [Release notes](release-notes.md)

### For SMAPI developers
* [Technical docs](technical/smapi.md)

## Translating SMAPI
Contributions are welcome! See [Modding:Translations](https://stardewvalleywiki.com/Modding:Translations)
on the wiki for help contributing translations.

locale      | status
----------- | :----------------
default     | ✓ [original text](../src/SMAPI/i18n/default.json)
Chinese     | ✓ [fully translated](../src/SMAPI/i18n/zh.json)
French      | ✓ [fully translated](../src/SMAPI/i18n/fr.json)
German      | ↻ [partly translated](../src/SMAPI/i18n/de.json)
Hungarian   | ↻ [partly translated](../src/SMAPI/i18n/hu.json)
Indonesian  | ✓ [fully translated](../src/SMAPI/i18n/id.json)
Italian     | ↻ [partly translated](../src/SMAPI/i18n/it.json)
Japanese    | ↻ [partly translated](../src/SMAPI/i18n/ja.json)
Korean      | ✓ [fully translated](../src/SMAPI/i18n/ko.json)
[Polish]    | ↻ [partly translated](../src/SMAPI/i18n/pl.json)
Portuguese  | ✓ [fully translated](../src/SMAPI/i18n/pt.json)
Russian     | ✓ [fully translated](../src/SMAPI/i18n/ru.json)
Spanish     | ✓ [partly translated](../src/SMAPI/i18n/es.json)
[Thai]      | ✓ [partly translated](../src/SMAPI/i18n/th.json)
Turkish     | ↻ [partly translated](../src/SMAPI/i18n/tr.json)
[Ukrainian] | ↻ [partly translated](../src/SMAPI/i18n/uk.json)

[Polish]: https://www.nexusmods.com/stardewvalley/mods/3616
[Thai]: https://www.nexusmods.com/stardewvalley/mods/7052
[Ukrainian]: https://www.nexusmods.com/stardewvalley/mods/8427

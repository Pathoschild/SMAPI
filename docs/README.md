**SMAPI**: An Open-Source Modding Framework and API for [Stardew Valley](https://stardewvalley.net/)
---

SMAPI allows you to play Stardew Valley with mods. It's safely installed alongside the game's executable without altering any game files. SMAPI serves seven main purposes:

1. **Load mods into the game**
   - SMAPI loads mods during the game startup, enabling them to interact with the game. (Code mods require SMAPI for loading.)

2. **Provide APIs and events for mods**
   - SMAPI offers APIs and events that allow mods to interact with the game in ways otherwise not possible.

3. **Rewrite mods for compatibility**
   - SMAPI rewrites mods' compiled code before loading, ensuring compatibility with Linux, macOS, and Windows without mods needing to handle platform-specific differences. It also rewrites code broken by game updates, preventing mod breakage.

4. **Intercept errors and automatically fix saves**
   - SMAPI intercepts errors, displays error info in the SMAPI console, and recovers the game in most cases. This prevents mods from crashing the game and enables troubleshooting for errors in the game itself that would typically show a generic 'program has stopped working' message.
   - SMAPI also automatically fixes save data in certain cases when a load would crash, e.g., due to a removed custom location or NPC mod.

5. **Provide update checks**
   - SMAPI checks for new versions of your installed mods and notifies you when updates are available.

6. **Provide compatibility checks**
   - SMAPI detects outdated or broken code in mods and safely disables them before causing problems.

7. **Back up your save files**
   - SMAPI creates daily save backups and keeps ten backups (through the bundled Save Backup mod) in case of issues.

Documentation
---
Need help? Join the [SMAPI community](https://smapi.io/community) to get assistance from SMAPI developers and fellow modders!

### For Players
- [Player Guide](https://stardewvalleywiki.com/Modding:Player_Guide)

### For Modders
- [Modding Documentation](https://smapi.io/docs)
- [Mod Build Configuration](technical/mod-package.md)
- [Release Notes](release-notes.md)

### For SMAPI Developers
- [Technical Docs](technical/smapi.md)

Translating SMAPI
---
SMAPI rarely displays in-game text, so it only requires a few translations. Contributions are welcome! Visit [Modding:Translations](https://stardewvalleywiki.com/Modding:Translations) on the wiki for help contributing translations.

| Locale     | Status                                         |
| ---------- | ---------------------------------------------- |
| Default    | ✓ [Fully Translated](../src/SMAPI/i18n/default.json) |
| Chinese    | ✓ [Fully Translated](../src/SMAPI/i18n/zh.json)     |
| French     | ✓ [Fully Translated](../src/SMAPI/i18n/fr.json)     |
| German     | ✓ [Fully Translated](../src/SMAPI/i18n/de.json)     |
| Hungarian  | ✓ [Fully Translated](../src/SMAPI/i18n/hu.json)     |
| Italian    | ✓ [Fully Translated](../src/SMAPI/i18n/it.json)     |
| Japanese   | ✓ [Fully Translated](../src/SMAPI/i18n/ja.json)     |
| Korean     | ✓ [Fully Translated](../src/SMAPI/i18n/ko.json)     |
| [Polish]   | ✓ [Fully Translated](../src/SMAPI/i18n/pl.json)     |
| Portuguese | ✓ [Fully Translated](../src/SMAPI/i18n/pt.json)     |
| Russian    | ✓ [Fully Translated](../src/SMAPI/i18n/ru.json)     |
| Spanish    | ✓ [Fully Translated](../src/SMAPI/i18n/es.json)     |
| [Thai]     | ✓ [Fully Translated](../src/SMAPI/i18n/th.json)     |
| Turkish    | ✓ [Fully Translated](../src/SMAPI/i18n/tr.json)     |
| [Ukrainian]| ✓ [Fully Translated](../src/SMAPI/i18n/uk.json)     |

[Polish]: https://www.nexusmods.com/stardewvalley/mods/3616
[Thai]: https://www.nexusmods.com/stardewvalley/mods/7052
[Ukrainian]: https://www.nexusmods.com/stardewvalley/mods/8427

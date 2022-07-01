using System.Collections.Generic;
using StardewValley;

namespace StardewModdingAPI;

/// <summary>Provides translations stored in the mod's <c>i18n</c> folder, with one file per locale (like <c>en.json</c>) containing a flat key => value structure. Translations are fetched with locale fallback, so missing translations are filled in from broader locales (like <c>pt-BR.json</c> &lt; <c>pt.json</c> &lt; <c>default.json</c>).</summary>
public interface ITranslationHelper : IModLinked
{
    /*********
    ** Accessors
    *********/
    /// <summary>The current locale code like <c>fr-FR</c>, or an empty string for English.</summary>
    string Locale { get; }

    /// <summary>The game's current language code.</summary>
    LocalizedContentManager.LanguageCode LocaleEnum { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Get all translations for the current locale.</summary>
    IEnumerable<Translation> GetTranslations();

    /// <summary>Get a translation for the current locale.</summary>
    /// <param name="key">The translation key.</param>
    Translation Get(string key);

    /// <summary>Get a translation for the current locale.</summary>
    /// <param name="key">The translation key.</param>
    /// <param name="tokens">An object containing token key/value pairs. This can be an anonymous object (like <c>new { value = 42, name = "Cranberries" }</c>), a dictionary, or a class instance.</param>
    Translation Get(string key, object? tokens);

    /// <summary>Get a translation in every locale for which it's defined.</summary>
    /// <param name="key">The translation key.</param>
    /// <param name="withFallback">Whether to add duplicate translations for locale fallback. For example, if a translation is defined in <c>default.json</c> but not <c>fr.json</c>, setting this to true will add a <c>fr</c> entry which duplicates the default text.</param>
    IDictionary<string, Translation> GetInAllLocales(string key, bool withFallback = false);
}

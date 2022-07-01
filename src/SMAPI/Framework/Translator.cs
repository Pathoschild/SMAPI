using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Framework;

/// <summary>Encapsulates access to arbitrary translations. Translations are fetched with locale fallback, so missing translations are filled in from broader locales (like <c>pt-BR.json</c> &lt; <c>pt.json</c> &lt; <c>default.json</c>).</summary>
internal class Translator
{
    /*********
    ** Fields
    *********/
    /// <summary>The translations for each locale.</summary>
    private readonly IDictionary<string, IDictionary<string, string>> All = new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The translations for the current locale, with locale fallback taken into account.</summary>
    private IDictionary<string, Translation> ForLocale;


    /*********
    ** Accessors
    *********/
    /// <summary>The current locale code like <c>fr-FR</c>, or an empty string for English.</summary>
    public string Locale { get; private set; }

    /// <summary>The game's current language code.</summary>
    public LocalizedContentManager.LanguageCode LocaleEnum { get; private set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public Translator()
    {
        this.SetLocale(string.Empty, LocalizedContentManager.LanguageCode.en);
    }

    /// <summary>Set the current locale and pre-cache translations.</summary>
    /// <param name="locale">The current locale.</param>
    /// <param name="localeEnum">The game's current language code.</param>
    [MemberNotNull(nameof(Translator.ForLocale), nameof(Translator.Locale))]
    public void SetLocale(string locale, LocalizedContentManager.LanguageCode localeEnum)
    {
        this.Locale = locale.ToLower().Trim();
        this.LocaleEnum = localeEnum;

        this.ForLocale = new Dictionary<string, Translation>(StringComparer.OrdinalIgnoreCase);
        foreach (string key in this.GetAllKeysRaw())
        {
            string? text = this.GetRaw(key, locale, withFallback: true);
            if (text != null)
                this.ForLocale.Add(key, new Translation(this.Locale, key, text));
        }
    }

    /// <summary>Get all translations for the current locale.</summary>
    public IEnumerable<Translation> GetTranslations()
    {
        return this.ForLocale.Values.ToArray();
    }

    /// <summary>Get a translation for the current locale.</summary>
    /// <param name="key">The translation key.</param>
    public Translation Get(string key)
    {
        this.ForLocale.TryGetValue(key, out Translation? translation);
        return translation ?? new Translation(this.Locale, key, null);
    }

    /// <summary>Get a translation for the current locale.</summary>
    /// <param name="key">The translation key.</param>
    /// <param name="tokens">An object containing token key/value pairs. This can be an anonymous object (like <c>new { value = 42, name = "Cranberries" }</c>), a dictionary, or a class instance.</param>
    public Translation Get(string key, object? tokens)
    {
        return this.Get(key).Tokens(tokens);
    }

    /// <summary>Get a translation in every locale for which it's defined.</summary>
    /// <param name="key">The translation key.</param>
    /// <param name="withFallback">Whether to add duplicate translations for locale fallback. For example, if a translation is defined in <c>default.json</c> but not <c>fr.json</c>, setting this to true will add a <c>fr</c> entry which duplicates the default text.</param>
    public IDictionary<string, Translation> GetInAllLocales(string key, bool withFallback)
    {
        IDictionary<string, Translation> translations = new Dictionary<string, Translation>();

        foreach (var localeSet in this.All)
        {
            string locale = localeSet.Key;
            string? text = this.GetRaw(key, locale, withFallback);

            if (text != null)
                translations[locale] = new Translation(locale, key, text);
        }

        return translations;
    }

    /// <summary>Set the translations to use.</summary>
    /// <param name="translations">The translations to use.</param>
    internal Translator SetTranslations(IDictionary<string, IDictionary<string, string>> translations)
    {
        // reset translations
        this.All.Clear();
        foreach (var pair in translations)
            this.All[pair.Key] = new Dictionary<string, string>(pair.Value, StringComparer.OrdinalIgnoreCase);

        // rebuild cache
        this.SetLocale(this.Locale, this.LocaleEnum);

        return this;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get all translation keys in the underlying translation data, ignoring the <see cref="ForLocale"/> cache.</summary>
    private IEnumerable<string> GetAllKeysRaw()
    {
        return new HashSet<string>(
            this.All.SelectMany(p => p.Value.Keys),
            StringComparer.OrdinalIgnoreCase
        );
    }

    /// <summary>Get a translation from the underlying translation data, ignoring the <see cref="ForLocale"/> cache.</summary>
    /// <param name="key">The translation key.</param>
    /// <param name="locale">The locale to get.</param>
    /// <param name="withFallback">Whether to add duplicate translations for locale fallback. For example, if a translation is defined in <c>default.json</c> but not <c>fr.json</c>, setting this to true will add a <c>fr</c> entry which duplicates the default text.</param>
    private string? GetRaw(string key, string locale, bool withFallback)
    {
        foreach (string next in this.GetRelevantLocales(locale))
        {
            string? translation = null;
            bool hasTranslation =
                this.All.TryGetValue(next, out IDictionary<string, string>? translations)
                && translations.TryGetValue(key, out translation);

            if (hasTranslation)
                return translation;

            if (!withFallback)
                break;
        }

        return null;
    }

    /// <summary>Get the locales which can provide translations for the given locale, in precedence order.</summary>
    /// <param name="locale">The locale for which to find valid locales.</param>
    private IEnumerable<string> GetRelevantLocales(string locale)
    {
        // given locale
        yield return locale;

        // broader locales (like pt-BR => pt)
        while (true)
        {
            int dashIndex = locale.LastIndexOf('-');
            if (dashIndex <= 0)
                break;

            locale = locale.Substring(0, dashIndex);
            yield return locale;
        }

        // default
        if (locale != "default")
            yield return "default";
    }
}

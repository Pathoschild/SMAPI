using System.Collections.Generic;
using StardewValley;

namespace StardewModdingAPI.Framework.ModHelpers;

/// <summary>Provides translations stored in the mod's <c>i18n</c> folder, with one file per locale (like <c>en.json</c>) containing a flat key => value structure. Translations are fetched with locale fallback, so missing translations are filled in from broader locales (like <c>pt-BR.json</c> &lt; <c>pt.json</c> &lt; <c>default.json</c>).</summary>
internal class TranslationHelper : BaseHelper, ITranslationHelper
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying translation manager.</summary>
    private readonly Translator Translator;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string Locale => this.Translator.Locale;

    /// <inheritdoc />
    public LocalizedContentManager.LanguageCode LocaleEnum => this.Translator.LocaleEnum;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod using this instance.</param>
    /// <param name="locale">The initial locale.</param>
    /// <param name="languageCode">The game's current language code.</param>
    public TranslationHelper(IModMetadata mod, string locale, LocalizedContentManager.LanguageCode languageCode)
        : base(mod)
    {
        this.Translator = new Translator();
        this.Translator.SetLocale(locale, languageCode);
    }

    /// <inheritdoc />
    public IEnumerable<Translation> GetTranslations()
    {
        return this.Translator.GetTranslations();
    }

    /// <inheritdoc />
    public Translation Get(string key)
    {
        return this.Translator.Get(key);
    }

    /// <inheritdoc />
    public Translation Get(string key, object? tokens)
    {
        return this.Translator.Get(key, tokens);
    }

    /// <inheritdoc />
    public IDictionary<string, Translation> GetInAllLocales(string key, bool withFallback = false)
    {
        return this.Translator.GetInAllLocales(key, withFallback);
    }

    /// <summary>Set the translations to use.</summary>
    /// <param name="translations">The translations to use.</param>
    internal TranslationHelper SetTranslations(IDictionary<string, IDictionary<string, string>> translations)
    {
        this.Translator.SetTranslations(translations);
        return this;
    }

    /// <summary>Set the current locale and pre-cache translations.</summary>
    /// <param name="locale">The current locale.</param>
    /// <param name="localeEnum">The game's current language code.</param>
    internal void SetLocale(string locale, LocalizedContentManager.LanguageCode localeEnum)
    {
        this.Translator.SetLocale(locale, localeEnum);
    }
}

using System;
using LanguageCode = StardewValley.LocalizedContentManager.LanguageCode;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IContentEvents.LocaleChanged"/> event.</summary>
public class LocaleChangedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The previous language enum value.</summary>
    /// <remarks>For a custom language, this is always <see cref="LanguageCode.mod"/>.</remarks>
    public LanguageCode OldLanguage { get; }

    /// <summary>The previous locale code.</summary>
    /// <remarks>This is the locale code as it appears in asset names, like <c>fr-FR</c> in <c>Maps/springobjects.fr-FR</c>. The locale code for English is an empty string.</remarks>
    public string OldLocale { get; }

    /// <summary>The new language enum value.</summary>
    /// <remarks><inheritdoc cref="OldLanguage" select="remarks" /></remarks>
    public LanguageCode NewLanguage { get; }

    /// <summary>The new locale code.</summary>
    /// <remarks><inheritdoc cref="OldLocale" select="remarks" /></remarks>
    public string NewLocale { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="oldLanguage">The previous language enum value.</param>
    /// <param name="oldLocale">The previous locale code.</param>
    /// <param name="newLanguage">The new language enum value.</param>
    /// <param name="newLocale">The new locale code.</param>
    internal LocaleChangedEventArgs(LanguageCode oldLanguage, string oldLocale, LanguageCode newLanguage, string newLocale)
    {
        this.OldLanguage = oldLanguage;
        this.OldLocale = oldLocale;
        this.NewLanguage = newLanguage;
        this.NewLocale = newLocale;
    }
}

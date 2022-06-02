using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace StardewModdingAPI;

/// <summary>A translation string with a fluent API to customise it.</summary>
public class Translation
{
    /*********
    ** Fields
    *********/
    /// <summary>The placeholder text when the translation is <c>null</c> or empty, where <c>{0}</c> is the translation key.</summary>
    internal const string PlaceholderText = "(no translation:{0})";

    /// <summary>The locale for which the translation was fetched like <c>fr-FR</c>, or an empty string for English.</summary>
    private readonly string Locale;

    /// <summary>The underlying translation text.</summary>
    private readonly string? Text;

    /// <summary>The value to return if the translations is undefined.</summary>
    private readonly string? Placeholder;


    /*********
    ** Accessors
    *********/
    /// <summary>The original translation key.</summary>
    public string Key { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="locale">The locale for which the translation was fetched.</param>
    /// <param name="key">The translation key.</param>
    /// <param name="text">The underlying translation text.</param>
    internal Translation(string locale, string key, string? text)
        : this(locale, key, text, string.Format(Translation.PlaceholderText, key)) { }

    /// <summary>Replace the text if it's <c>null</c> or empty. If you set a <c>null</c> or empty value, the translation will show the fallback "no translation" placeholder (see <see cref="UsePlaceholder"/> if you want to disable that). Returns a new instance if changed.</summary>
    /// <param name="default">The default value.</param>
    public Translation Default(string? @default)
    {
        return this.HasValue()
            ? this
            : new Translation(this.Locale, this.Key, @default);
    }

    /// <summary>Whether to return a "no translation" placeholder if the translation is <c>null</c> or empty. Returns a new instance.</summary>
    /// <param name="use">Whether to return a placeholder. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the text non-nullable.</strong></param>
    public Translation UsePlaceholder(bool use)
    {
        return new Translation(this.Locale, this.Key, this.Text, use ? string.Format(Translation.PlaceholderText, this.Key) : null);
    }

    /// <summary>Replace tokens in the text like <c>{{value}}</c> with the given values. Returns a new instance.</summary>
    /// <param name="tokens">An object containing token key/value pairs. This can be an anonymous object (like <c>new { value = 42, name = "Cranberries" }</c>), a dictionary, or a class instance.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="tokens"/> argument is <c>null</c>.</exception>
    public Translation Tokens(object? tokens)
    {
        if (string.IsNullOrWhiteSpace(this.Text) || tokens == null)
            return this;

        // get dictionary of tokens
        Dictionary<string, string?> tokenLookup = new(StringComparer.OrdinalIgnoreCase);
        {
            // from dictionary
            if (tokens is IDictionary inputLookup)
            {
                foreach (DictionaryEntry entry in inputLookup)
                {
                    string? key = entry.Key.ToString()?.Trim();
                    if (key != null)
                        tokenLookup[key] = entry.Value?.ToString();
                }
            }

            // from object properties
            else
            {
                Type type = tokens.GetType();
                foreach (PropertyInfo prop in type.GetProperties())
                    tokenLookup[prop.Name] = prop.GetValue(tokens)?.ToString();
                foreach (FieldInfo field in type.GetFields())
                    tokenLookup[field.Name] = field.GetValue(tokens)?.ToString();
            }
        }

        // format translation
        string text = Regex.Replace(this.Text, @"{{([ \w\.\-]+)}}", match =>
        {
            string key = match.Groups[1].Value.Trim();
            return tokenLookup.TryGetValue(key, out string? value)
                ? (value ?? "")
                : match.Value;
        });
        return new Translation(this.Locale, this.Key, text);
    }

    /// <summary>Get whether the translation has a defined value.</summary>
    public bool HasValue()
    {
        return !string.IsNullOrEmpty(this.Text);
    }

    /// <summary>Get the translation text. Calling this method isn't strictly necessary, since you can assign a <see cref="Translation"/> value directly to a string.</summary>
    /// <remarks><strong>Limitation with nullable reference types: if there's no text and you disabled the fallback via <see cref="UsePlaceholder"/>, this will return null but the return value will still be marked non-nullable.</strong></remarks>
    public override string ToString()
    {
        return this.Placeholder != null && !this.HasValue()
            ? this.Placeholder
            : this.Text!;
    }

    /// <summary>Get a string representation of the given translation.</summary>
    /// <param name="translation">The translation key.</param>
    /// <remarks><strong>Limitation with nullable reference types: if there's no text and you disabled the fallback via <see cref="UsePlaceholder"/>, this will return null but the return value will still be marked non-nullable.</strong></remarks>
    [SuppressMessage("ReSharper", "ConstantConditionalAccessQualifier", Justification = "The null check is required due to limitations in nullable type annotations (see remarks).")]
    public static implicit operator string(Translation translation)
    {
        return translation?.ToString()!;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="locale">The locale for which the translation was fetched.</param>
    /// <param name="key">The translation key.</param>
    /// <param name="text">The underlying translation text.</param>
    /// <param name="placeholder">The value to return if the translations is undefined.</param>
    private Translation(string locale, string key, string? text, string? placeholder)
    {
        this.Locale = locale;
        this.Key = key;
        this.Text = text;
        this.Placeholder = placeholder;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using StardewValley;

namespace StardewModdingAPI;

/// <summary>A translation string with a fluent API to customise it.</summary>
public class Translation
{
    /*********
    ** Fields
    *********/
    /// <summary>The placeholder text when the translation is <c>null</c> or empty, where <c>{0}</c> is the translation key.</summary>
    internal const string PlaceholderText = "(no translation:{0})";

    /// <summary>The gender to use for gender switch blocks.</summary>
    /// <remarks>This enables formatting translations in unit tests; most code should leave this null to get it from <see cref="Game1.player"/> instead.</remarks>
    internal Func<Gender>? ForceGender { get; init; }

    /// <summary>The locale for which the translation was fetched like <c>fr-FR</c>, or an empty string for English.</summary>
    private readonly string Locale;

    /// <summary>The underlying translation text.</summary>
    private readonly string? Text;

    /// <summary>The custom value to return if the translations is undefined.</summary>
    private string? CustomDefault { get; init; }

    /// <summary>Whether to show a placeholder if the <see cref="Text"/> and <see cref="CustomDefault"/> are both is <c>null</c> or empty.</summary>
    private bool ShouldUsePlaceholder { get; init; }

    /// <summary>Whether to process switch blocks in translation text, if any.</summary>
    private bool ShouldApplySwitchBlocks { get; init; }

    /// <summary>The tokens to apply to the translation text, if any.</summary>
    private Dictionary<string, string?>? TokenValues { get; init; }

    /// <summary>Whether the <see cref="CachedResult"/> has been prepared.</summary>
    private bool Cached;

    /// <summary>The final output text, it's already been prepared.</summary>
    private string? CachedResult;


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
    {
        this.Locale = locale;
        this.Key = key;
        this.Text = text;
        this.ShouldUsePlaceholder = true;
        this.ShouldApplySwitchBlocks = true;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="other">The other translation to copy.</param>
    internal Translation(Translation other)
        : this(other.Locale, other.Key, other.Text)
    {
        this.CustomDefault = other.CustomDefault;
        this.ShouldUsePlaceholder = other.ShouldUsePlaceholder;
        this.ShouldApplySwitchBlocks = other.ShouldApplySwitchBlocks;
        this.TokenValues = other.TokenValues;
        this.ForceGender = other.ForceGender;
    }

    /// <summary>The custom text to use if the text is <c>null</c> or empty. If set to <c>null</c> or empty value, defaults to a fallback "no translation" placeholder unless you disable that via <see cref="UsePlaceholder"/>.</summary>
    /// <param name="default">The default value.</param>
    /// <remarks>Returns a new instance if this would change the result, else the current instance.</remarks>
    public Translation Default(string? @default)
    {
        if (string.IsNullOrEmpty(@default))
            @default = null;

        if (this.HasValue() || @default == this.CustomDefault)
            return this;

        return new Translation(this)
        {
            CustomDefault = @default
        };
    }

    /// <summary>Whether to return a "no translation" placeholder if the translation is <c>null</c> or empty.</summary>
    /// <param name="use">Whether to return a placeholder. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the text non-nullable.</strong></param>
    /// <remarks>Returns a new instance if this would change the result, else the current instance.</remarks>
    public Translation UsePlaceholder(bool use)
    {
        if (this.HasValue() || this.ShouldUsePlaceholder == use)
            return this;

        return new Translation(this)
        {
            ShouldUsePlaceholder = use
        };
    }

    /// <summary>Whether to automatically process switch blocks (i.e. <see cref="Dialogue.ApplySwitchBlocks(string,string)"/>) before the text is returned.</summary>
    /// <param name="apply">Whether to process switch blocks.</param>
    /// <remarks>Returns a new instance if this would change the result, else the current instance.</remarks>
    public Translation ApplySwitchBlocks(bool apply)
    {
        if (this.ShouldApplySwitchBlocks == apply)
            return this;

        return new Translation(this)
        {
            ShouldApplySwitchBlocks = apply
        };
    }

    /// <summary>Replace tokens in the text like <c>{{value}}</c> with the given values.</summary>
    /// <param name="tokens">An object containing token key/value pairs. This can be an anonymous object (like <c>new { value = 42, name = "Cranberries" }</c>), a dictionary, or a class instance.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="tokens"/> argument is <c>null</c>.</exception>
    /// <remarks>Returns a new instance if this would change the result, else the current instance.</remarks>
    public Translation Tokens(object? tokens)
    {
        // get token values
        Dictionary<string, string?>? tokenValues = this.GetTokenValues(tokens);
        if (tokenValues is null)
            return this;

        // include any previous values
        if (this.TokenValues != null)
        {
            foreach ((string key, string? value) in this.TokenValues)
                tokenValues.TryAdd(key, value);
        }

        // build result
        return new Translation(this)
        {
            TokenValues = tokenValues
        };
    }

    /// <summary>Get whether the translation has a defined value, before tokens are replaced.</summary>
    public bool HasValue()
    {
        return !string.IsNullOrEmpty(this.Text);
    }

    /// <summary>Get the translation text. Calling this method isn't strictly necessary, since you can assign a <see cref="Translation"/> value directly to a string.</summary>
    /// <remarks><strong>Limitation with nullable reference types: if there's no text, no <see cref="Default"/>, and you disabled the fallback via <see cref="UsePlaceholder"/>, this will return null but the return value will still be marked non-nullable.</strong></remarks>
    public override string ToString()
    {
        if (!this.Cached)
        {
            string? rawText;
            if (this.HasValue())
                rawText = this.Text;
            else if (this.CustomDefault != null)
                rawText = this.CustomDefault;
            else if (this.ShouldUsePlaceholder)
                rawText = string.Format(Translation.PlaceholderText, this.Key);
            else
                rawText = this.Text;

            this.CachedResult = this.FormatText(rawText);
            this.Cached = true;
        }

        return this.CachedResult!;
    }

    /// <summary>Get a string representation of the given translation.</summary>
    /// <param name="translation">The translation key.</param>
    /// <remarks><strong>Limitation with nullable reference types: if there's no text and you disabled the fallback via <see cref="UsePlaceholder"/>, this will return null but the return value will still be marked non-nullable.</strong></remarks>
    [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "The null check is required due to limitations in nullable type annotations (see remarks).")]
    public static implicit operator string(Translation translation)
    {
        return translation?.ToString()!;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Apply any formatting or tokens to the given text.</summary>
    /// <param name="text">The text to format.</param>
    [return: NotNullIfNotNull("text")]
    private string? FormatText(string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            // apply tokens
            if (this.TokenValues?.Count > 0)
            {
                text = Regex.Replace(text, @"{{([ \w\.\-]+)}}", match =>
                {
                    string key = match.Groups[1].Value.Trim();
                    return this.TokenValues.TryGetValue(key, out string? value)
                        ? (value ?? "")
                        : match.Value;
                });
            }

            // apply switch blocks
            if (this.ShouldApplySwitchBlocks)
            {
                Gender gender = this.ForceGender?.Invoke() ?? Game1.player?.Gender ?? default;
                text = Dialogue.ApplySwitchBlocks(gender, text, this.Key);
            }
        }

        return text;
    }

    /// <summary>Get token values to apply from a given token source, if any.</summary>
    /// <param name="source">An object containing token key/value pairs. This can be an anonymous object (like <c>new { value = 42, name = "Cranberries" }</c>), a dictionary, or a class instance.</param>
    /// <returns>Returns the token values, or <c>null</c> if there were none.</returns>
    private Dictionary<string, string?>? GetTokenValues(object? source)
    {
        if (source != null)
        {
            Dictionary<string, string?> tokenValues = new(StringComparer.OrdinalIgnoreCase);

            // from dictionary
            if (source is IDictionary inputLookup)
            {
                foreach (DictionaryEntry entry in inputLookup)
                {
                    string? key = entry.Key.ToString()?.Trim();
                    if (key != null)
                        tokenValues[key] = entry.Value?.ToString();
                }
            }

            // from object properties
            else
            {
                Type type = source.GetType();
                foreach (PropertyInfo prop in type.GetProperties())
                    tokenValues[prop.Name] = prop.GetValue(source)?.ToString();
                foreach (FieldInfo field in type.GetFields())
                    tokenValues[field.Name] = field.GetValue(source)?.ToString();
            }

            if (tokenValues.Count > 0)
                return tokenValues;
        }

        return null;
    }
}

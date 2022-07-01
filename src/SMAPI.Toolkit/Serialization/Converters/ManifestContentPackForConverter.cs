using System;
using Newtonsoft.Json;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace StardewModdingAPI.Toolkit.Serialization.Converters;

/// <summary>Handles deserialization of <see cref="ManifestContentPackFor"/> arrays.</summary>
public class ManifestContentPackForConverter : JsonConverter
{
    /*********
    ** Accessors
    *********/
    /// <summary>Whether this converter can write JSON.</summary>
    public override bool CanWrite => false;


    /*********
    ** Public methods
    *********/
    /// <summary>Get whether this instance can convert the specified object type.</summary>
    /// <param name="objectType">The object type.</param>
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ManifestContentPackFor[]);
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Read the JSON representation of the object.</summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="objectType">The object type.</param>
    /// <param name="existingValue">The object being read.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        return serializer.Deserialize<ManifestContentPackFor>(reader);
    }

    /// <summary>Writes the JSON representation of the object.</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new InvalidOperationException("This converter does not write JSON.");
    }
}

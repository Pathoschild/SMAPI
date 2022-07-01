using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace StardewModdingAPI.Toolkit.Serialization.Converters;

/// <summary>Handles deserialization of <see cref="ManifestDependency"/> arrays.</summary>
internal class ManifestDependencyArrayConverter : JsonConverter
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
        return objectType == typeof(ManifestDependency[]);
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Read the JSON representation of the object.</summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="objectType">The object type.</param>
    /// <param name="existingValue">The object being read.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        List<ManifestDependency> result = new List<ManifestDependency>();
        foreach (JObject obj in JArray.Load(reader).Children<JObject>())
        {
            string uniqueID = obj.ValueIgnoreCase<string>(nameof(ManifestDependency.UniqueID))!; // will be validated separately if null
            string? minVersion = obj.ValueIgnoreCase<string>(nameof(ManifestDependency.MinimumVersion));
            bool required = obj.ValueIgnoreCase<bool?>(nameof(ManifestDependency.IsRequired)) ?? true;
            result.Add(new ManifestDependency(uniqueID, minVersion, required));
        }
        return result.ToArray();
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

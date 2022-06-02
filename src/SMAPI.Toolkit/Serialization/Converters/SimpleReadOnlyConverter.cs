using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StardewModdingAPI.Toolkit.Serialization.Converters;

/// <summary>The base implementation for simplified converters which deserialize <typeparamref name="T"/> without overriding serialization.</summary>
/// <typeparam name="T">The type to deserialize.</typeparam>
internal abstract class SimpleReadOnlyConverter<T> : JsonConverter
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
        return objectType == typeof(T) || Nullable.GetUnderlyingType(objectType) == typeof(T);
    }

    /// <summary>Reads the JSON representation of the object.</summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="objectType">The object type.</param>
    /// <param name="existingValue">The object being read.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        string path = reader.Path;
        switch (reader.TokenType)
        {
            case JsonToken.Null when Nullable.GetUnderlyingType(objectType) != null:
                return null;

            case JsonToken.StartObject:
                return this.ReadObject(JObject.Load(reader), path);

            case JsonToken.String:
                return this.ReadString(JToken.Load(reader).Value<string>(), path);

            default:
                throw new SParseException($"Can't parse {typeof(T).Name} from {reader.TokenType} node (path: {reader.Path}).");
        }
    }

    /// <summary>Writes the JSON representation of the object.</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new InvalidOperationException("This converter does not write JSON.");
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Read a JSON object.</summary>
    /// <param name="obj">The JSON object to read.</param>
    /// <param name="path">The path to the current JSON node.</param>
    protected virtual T ReadObject(JObject obj, string path)
    {
        throw new SParseException($"Can't parse {typeof(T).Name} from object node (path: {path}).");
    }

    /// <summary>Read a JSON string.</summary>
    /// <param name="str">The JSON string value.</param>
    /// <param name="path">The path to the current JSON node.</param>
    protected virtual T ReadString(string str, string path)
    {
        throw new SParseException($"Can't parse {typeof(T).Name} from string node (path: {path}).");
    }
}

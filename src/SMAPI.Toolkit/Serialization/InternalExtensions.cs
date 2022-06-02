using System;
using Newtonsoft.Json.Linq;

namespace StardewModdingAPI.Toolkit.Serialization;

/// <summary>Provides extension methods for parsing JSON.</summary>
public static class JsonExtensions
{
    /// <summary>Get a JSON field value from a case-insensitive field name. This will check for an exact match first, then search without case sensitivity.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="obj">The JSON object to search.</param>
    /// <param name="fieldName">The field name.</param>
    public static T? ValueIgnoreCase<T>(this JObject obj, string fieldName)
    {
        JToken? token = obj.GetValue(fieldName, StringComparison.OrdinalIgnoreCase);
        return token != null
            ? token.Value<T>()
            : default;
    }
}

using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Serialization.Converters;

namespace StardewModdingAPI.Framework.Serialization;

/// <summary>Handles deserialization of <see cref="Vector2"/> for crossplatform compatibility.</summary>
/// <remarks>
/// - Linux/macOS format: { "X": 1, "Y": 2 }
/// - Windows format:   "1, 2"
/// </remarks>
internal class Vector2Converter : SimpleReadOnlyConverter<Vector2>
{
    /*********
    ** Protected methods
    *********/
    /// <summary>Read a JSON object.</summary>
    /// <param name="obj">The JSON object to read.</param>
    /// <param name="path">The path to the current JSON node.</param>
    protected override Vector2 ReadObject(JObject obj, string path)
    {
        float x = obj.ValueIgnoreCase<float>(nameof(Vector2.X));
        float y = obj.ValueIgnoreCase<float>(nameof(Vector2.Y));
        return new Vector2(x, y);
    }

    /// <summary>Read a JSON string.</summary>
    /// <param name="str">The JSON string value.</param>
    /// <param name="path">The path to the current JSON node.</param>
    protected override Vector2 ReadString(string str, string path)
    {
        string[] parts = str.Split(',');
        if (parts.Length != 2)
            throw new SParseException($"Can't parse {nameof(Vector2)} from invalid value '{str}' (path: {path}).");

        float x = Convert.ToSingle(parts[0]);
        float y = Convert.ToSingle(parts[1]);
        return new Vector2(x, y);
    }
}

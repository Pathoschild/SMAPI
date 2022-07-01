using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Serialization.Converters;

namespace StardewModdingAPI.Framework.Serialization;

/// <summary>Handles deserialization of <see cref="Color"/> for crossplatform compatibility.</summary>
/// <remarks>
/// - Linux/macOS format: { "B": 76, "G": 51, "R": 25, "A": 102 }
/// - Windows format:   "26, 51, 76, 102"
/// </remarks>
internal class ColorConverter : SimpleReadOnlyConverter<Color>
{
    /*********
    ** Protected methods
    *********/
    /// <summary>Read a JSON object.</summary>
    /// <param name="obj">The JSON object to read.</param>
    /// <param name="path">The path to the current JSON node.</param>
    protected override Color ReadObject(JObject obj, string path)
    {
        int r = obj.ValueIgnoreCase<int>(nameof(Color.R));
        int g = obj.ValueIgnoreCase<int>(nameof(Color.G));
        int b = obj.ValueIgnoreCase<int>(nameof(Color.B));
        int a = obj.ValueIgnoreCase<int>(nameof(Color.A));
        return new Color(r, g, b, a);
    }

    /// <summary>Read a JSON string.</summary>
    /// <param name="str">The JSON string value.</param>
    /// <param name="path">The path to the current JSON node.</param>
    protected override Color ReadString(string str, string path)
    {
        string[] parts = str.Split(',');
        if (parts.Length != 4)
            throw new SParseException($"Can't parse {nameof(Color)} from invalid value '{str}' (path: {path}).");

        int r = Convert.ToInt32(parts[0]);
        int g = Convert.ToInt32(parts[1]);
        int b = Convert.ToInt32(parts[2]);
        int a = Convert.ToInt32(parts[3]);
        return new Color(r, g, b, a);
    }
}

using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Serialization.Converters;

namespace StardewModdingAPI.Framework.Serialization;

/// <summary>Handles deserialization of <see cref="Rectangle"/> for crossplatform compatibility.</summary>
/// <remarks>
/// - Linux/macOS format: { "X": 1, "Y": 2, "Width": 3, "Height": 4 }
/// - Windows format:   "{X:1 Y:2 Width:3 Height:4}"
/// </remarks>
internal class RectangleConverter : SimpleReadOnlyConverter<Rectangle>
{
    /*********
    ** Protected methods
    *********/
    /// <summary>Read a JSON object.</summary>
    /// <param name="obj">The JSON object to read.</param>
    /// <param name="path">The path to the current JSON node.</param>
    protected override Rectangle ReadObject(JObject obj, string path)
    {
        int x = obj.ValueIgnoreCase<int>(nameof(Rectangle.X));
        int y = obj.ValueIgnoreCase<int>(nameof(Rectangle.Y));
        int width = obj.ValueIgnoreCase<int>(nameof(Rectangle.Width));
        int height = obj.ValueIgnoreCase<int>(nameof(Rectangle.Height));
        return new Rectangle(x, y, width, height);
    }

    /// <summary>Read a JSON string.</summary>
    /// <param name="str">The JSON string value.</param>
    /// <param name="path">The path to the current JSON node.</param>
    protected override Rectangle ReadString(string str, string path)
    {
        if (string.IsNullOrWhiteSpace(str))
            return Rectangle.Empty;

        var match = Regex.Match(str, @"^\{X:(?<x>-?\d+) Y:(?<y>-?\d+) Width:(?<width>-?\d+) Height:(?<height>-?\d+)\}$", RegexOptions.IgnoreCase);
        if (!match.Success)
            throw new SParseException($"Can't parse {nameof(Rectangle)} from invalid value '{str}' (path: {path}).");

        int x = Convert.ToInt32(match.Groups["x"].Value);
        int y = Convert.ToInt32(match.Groups["y"].Value);
        int width = Convert.ToInt32(match.Groups["width"].Value);
        int height = Convert.ToInt32(match.Groups["height"].Value);

        return new Rectangle(x, y, width, height);
    }
}

using System.Collections.Generic;

namespace StardewModdingAPI.Toolkit;

/// <summary>A comparer for semantic versions based on the <see cref="SemanticVersion.CompareTo(ISemanticVersion)"/> field.</summary>
public class SemanticVersionComparer : IComparer<ISemanticVersion?>
{
    /*********
    ** Accessors
    *********/
    /// <summary>A singleton instance of the comparer.</summary>
    public static SemanticVersionComparer Instance { get; } = new();


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public int Compare(ISemanticVersion? x, ISemanticVersion? y)
    {
        if (object.ReferenceEquals(x, y))
            return 0;

        if (x is null)
            return -1;
        if (y is null)
            return 1;

        return x.CompareTo(y);
    }
}

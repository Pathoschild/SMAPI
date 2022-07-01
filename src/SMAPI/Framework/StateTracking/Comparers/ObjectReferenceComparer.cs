using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace StardewModdingAPI.Framework.StateTracking.Comparers;

/// <summary>A comparer which considers two references equal if they point to the same instance.</summary>
/// <typeparam name="T">The value type.</typeparam>
internal class ObjectReferenceComparer<T> : IEqualityComparer<T>
{
    /*********
    ** Public methods
    *********/
    /// <summary>Determines whether the specified objects are equal.</summary>
    /// <returns>true if the specified objects are equal; otherwise, false.</returns>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    public bool Equals(T? x, T? y)
    {
        return object.ReferenceEquals(x, y);
    }

    /// <summary>Get a hash code for the specified object.</summary>
    /// <param name="obj">The value.</param>
    public int GetHashCode(T obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}

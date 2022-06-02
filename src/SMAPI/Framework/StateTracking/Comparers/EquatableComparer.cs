using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace StardewModdingAPI.Framework.StateTracking.Comparers;

/// <summary>Compares instances using <see cref="IEqualityComparer{T}.Equals(T,T)"/>.</summary>
/// <typeparam name="T">The value type.</typeparam>
internal class EquatableComparer<T> : IEqualityComparer<T> where T : IEquatable<T>
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
        if (x == null)
            return y == null;
        return x.Equals(y);
    }

    /// <summary>Get a hash code for the specified object.</summary>
    /// <param name="obj">The value.</param>
    public int GetHashCode(T obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}

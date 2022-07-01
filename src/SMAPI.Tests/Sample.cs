using System;

namespace SMAPI.Tests;

/// <summary>Provides sample values for unit testing.</summary>
internal static class Sample
{
    /*********
    ** Fields
    *********/
    /// <summary>A random number generator.</summary>
    private static readonly Random Random = new();


    /*********
    ** Accessors
    *********/
    /// <summary>Get a sample string.</summary>
    public static string String()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>Get a sample integer.</summary>
    public static int Int()
    {
        return Sample.Random.Next();
    }
}

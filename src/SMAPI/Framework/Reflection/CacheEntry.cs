using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace StardewModdingAPI.Framework.Reflection;

/// <summary>A cached member reflection result.</summary>
internal readonly struct CacheEntry
{
    /*********
    ** Accessors
    *********/
    /// <summary>Whether the lookup found a valid match.</summary>
    [MemberNotNullWhen(true, nameof(CacheEntry.MemberInfo))]
    public bool IsValid => this.MemberInfo != null;

    /// <summary>The reflection data for this member (or <c>null</c> if invalid).</summary>
    public MemberInfo? MemberInfo { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="memberInfo">The reflection data for this member (or <c>null</c> if invalid).</param>
    public CacheEntry(MemberInfo? memberInfo)
    {
        this.MemberInfo = memberInfo;
    }
}

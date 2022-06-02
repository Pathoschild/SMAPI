namespace StardewModdingAPI.Toolkit.Framework.UpdateData;

/// <summary>A mod site which SMAPI can check for updates.</summary>
public enum ModSiteKey
{
    /// <summary>An unknown or invalid mod repository.</summary>
    Unknown,

    /// <summary>The Chucklefish mod repository.</summary>
    Chucklefish,

    /// <summary>The CurseForge mod repository.</summary>
    CurseForge,

    /// <summary>A GitHub project containing releases.</summary>
    GitHub,

    /// <summary>The ModDrop mod repository.</summary>
    ModDrop,

    /// <summary>The Nexus Mods mod repository.</summary>
    Nexus
}
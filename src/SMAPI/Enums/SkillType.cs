using StardewValley;

namespace StardewModdingAPI.Enums;

/// <summary>The player skill types.</summary>
public enum SkillType
{
    /// <summary>The combat skill.</summary>
    Combat = Farmer.combatSkill,

    /// <summary>The farming skill.</summary>
    Farming = Farmer.farmingSkill,

    /// <summary>The fishing skill.</summary>
    Fishing = Farmer.fishingSkill,

    /// <summary>The foraging skill.</summary>
    Foraging = Farmer.foragingSkill,

    /// <summary>The mining skill.</summary>
    Mining = Farmer.miningSkill,

    /// <summary>The luck skill.</summary>
    Luck = Farmer.luckSkill
}

using System;
using StardewModdingAPI.Enums;
using StardewValley;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for a <see cref="IPlayerEvents.LevelChanged"/> event.</summary>
public class LevelChangedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The player whose skill level changed.</summary>
    public Farmer Player { get; }

    /// <summary>The skill whose level changed.</summary>
    public SkillType Skill { get; }

    /// <summary>The previous skill level.</summary>
    public int OldLevel { get; }

    /// <summary>The new skill level.</summary>
    public int NewLevel { get; }

    /// <summary>Whether the affected player is the local one.</summary>
    public bool IsLocalPlayer => this.Player.IsLocalPlayer;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="player">The player whose skill level changed.</param>
    /// <param name="skill">The skill whose level changed.</param>
    /// <param name="oldLevel">The previous skill level.</param>
    /// <param name="newLevel">The new skill level.</param>
    internal LevelChangedEventArgs(Farmer player, SkillType skill, int oldLevel, int newLevel)
    {
        this.Player = player;
        this.Skill = skill;
        this.OldLevel = oldLevel;
        this.NewLevel = newLevel;
    }
}

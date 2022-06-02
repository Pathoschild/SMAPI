using System;

namespace StardewModdingAPI.Events;

/// <summary>An attribute which specifies the priority for an event handler.</summary>
[AttributeUsage(AttributeTargets.Method)]
public class EventPriorityAttribute : Attribute
{
    /*********
    ** Accessors
    *********/
    /// <summary>The event handler priority, relative to other handlers across all mods registered for this event.</summary>
    internal EventPriority Priority { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="priority">The event handler priority, relative to other handlers across all mods registered for this event. Higher-priority handlers are notified before lower-priority handlers.</param>
    public EventPriorityAttribute(EventPriority priority)
    {
        this.Priority = priority;
    }
}

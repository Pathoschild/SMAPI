using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewModdingAPI.Events;
using StardewModdingAPI.Internal;

namespace StardewModdingAPI.Framework.Events;

/// <summary>An event wrapper which intercepts and logs errors in handler code.</summary>
/// <typeparam name="TEventArgs">The event arguments type.</typeparam>
internal class ManagedEvent<TEventArgs> : IManagedEvent
{
    /*********
    ** Fields
    *********/
    /// <summary>The mod registry with which to identify mods.</summary>
    protected readonly ModRegistry ModRegistry;

    /// <summary>The underlying event handlers.</summary>
    private readonly List<ManagedEventHandler<TEventArgs>> Handlers = new();

    /// <summary>A cached snapshot of the <see cref="Handlers"/> sorted by event priority, or <c>null</c> to rebuild it next raise.</summary>
    private ManagedEventHandler<TEventArgs>[]? CachedHandlers = Array.Empty<ManagedEventHandler<TEventArgs>>();

    /// <summary>The total number of event handlers registered for this events, regardless of whether they're still registered.</summary>
    private int RegistrationIndex;

    /// <summary>Whether handlers were removed since the last raise.</summary>
    private bool HasRemovedHandlers;

    /// <summary>Whether any of the handlers have a custom priority.</summary>
    private bool HasPriorities;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string EventName { get; }

    /// <inheritdoc />
    public bool HasListeners { get; private set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="eventName">A human-readable name for the event.</param>
    /// <param name="modRegistry">The mod registry with which to identify mods.</param>
    public ManagedEvent(string eventName, ModRegistry modRegistry)
    {
        this.EventName = eventName;
        this.ModRegistry = modRegistry;
    }

    /// <summary>Add an event handler.</summary>
    /// <param name="handler">The event handler.</param>
    /// <param name="mod">The mod which added the event handler.</param>
    public void Add(EventHandler<TEventArgs> handler, IModMetadata mod)
    {
        lock (this.Handlers)
        {
            EventPriority priority = handler.Method.GetCustomAttribute<EventPriorityAttribute>()?.Priority ?? EventPriority.Normal;
            var managedHandler = new ManagedEventHandler<TEventArgs>(handler, this.RegistrationIndex++, priority, mod);

            this.Handlers.Add(managedHandler);
            this.CachedHandlers = null;
            this.HasListeners = true;
            this.HasPriorities |= priority != EventPriority.Normal;
        }
    }

    /// <summary>Remove an event handler.</summary>
    /// <param name="handler">The event handler.</param>
    public void Remove(EventHandler<TEventArgs> handler)
    {
        lock (this.Handlers)
        {
            // match C# events: if a handler is listed multiple times, remove the last one added
            for (int i = this.Handlers.Count - 1; i >= 0; i--)
            {
                if (this.Handlers[i].Handler != handler)
                    continue;

                this.Handlers.RemoveAt(i);
                this.CachedHandlers = null;
                this.HasListeners = this.Handlers.Count != 0;
                this.HasRemovedHandlers = true;
                break;
            }
        }
    }

    /// <summary>Raise the event and notify all handlers.</summary>
    /// <param name="args">The event arguments to pass.</param>
    public void Raise(TEventArgs args)
    {
        // skip if no handlers
        if (this.Handlers.Count == 0)
            return;

        // raise event
        foreach (ManagedEventHandler<TEventArgs> handler in this.GetHandlers())
        {
            Context.HeuristicModsRunningCode.Push(handler.SourceMod);

            try
            {
                handler.Handler(null, args);
            }
            catch (Exception ex)
            {
                this.LogError(handler, ex);
            }
            finally
            {
                Context.HeuristicModsRunningCode.TryPop(out _);
            }
        }
    }

    /// <summary>Raise the event and notify all handlers.</summary>
    /// <param name="invoke">Invoke an event handler. This receives the mod which registered the handler, and should invoke the callback with the event arguments to pass it.</param>
    public void Raise(Action<IModMetadata, Action<TEventArgs>> invoke)
    {
        // skip if no handlers
        if (this.Handlers.Count == 0)
            return;

        // raise event
        foreach (ManagedEventHandler<TEventArgs> handler in this.GetHandlers())
        {
            Context.HeuristicModsRunningCode.Push(handler.SourceMod);

            try
            {
                invoke(handler.SourceMod, args => handler.Handler(null, args));
            }
            catch (Exception ex)
            {
                this.LogError(handler, ex);
            }
            finally
            {
                Context.HeuristicModsRunningCode.TryPop(out _);
            }
        }
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Log an exception from an event handler.</summary>
    /// <param name="handler">The event handler instance.</param>
    /// <param name="ex">The exception that was raised.</param>
    private void LogError(ManagedEventHandler<TEventArgs> handler, Exception ex)
    {
        handler.SourceMod.LogAsMod($"This mod failed in the {this.EventName} event. Technical details: \n{ex.GetLogSummary()}", LogLevel.Error);
    }

    /// <summary>Get cached copy of the sorted handlers to invoke.</summary>
    /// <remarks>This returns the handlers sorted by priority, and allows iterating the list even if a mod adds/removes handlers while handling it. This is debounced when requested to avoid repeatedly sorting when handlers are added/removed.</remarks>
    private ManagedEventHandler<TEventArgs>[] GetHandlers()
    {
        ManagedEventHandler<TEventArgs>[]? handlers = this.CachedHandlers;

        if (handlers == null)
        {
            lock (this.Handlers)
            {
                // recheck priorities
                if (this.HasRemovedHandlers)
                    this.HasPriorities = this.Handlers.Any(p => p.Priority != EventPriority.Normal);

                // sort by priority if needed
                if (this.HasPriorities)
                    this.Handlers.Sort();

                // update cache
                this.CachedHandlers = handlers = this.Handlers.ToArray();
                this.HasRemovedHandlers = false;
            }
        }

        return handlers;
    }
}

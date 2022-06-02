using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Deprecations;

namespace StardewModdingAPI.Utilities;

/// <summary>Manages a separate value for each player in split-screen mode. This can safely be used in non-split-screen mode too, it'll just have a single state in that case.</summary>
/// <typeparam name="T">The state class.</typeparam>
public class PerScreen<T>
{
    /*********
    ** Fields
    *********/
    /// <summary>Create the initial value for a screen.</summary>
    private readonly Func<T> CreateNewState;

    /// <summary>The tracked values for each screen.</summary>
    private readonly IDictionary<int, T> States = new Dictionary<int, T>();

    /// <summary>The last <see cref="Context.LastRemovedScreenId"/> value for which this instance was updated.</summary>
    private int LastRemovedScreenId;


    /*********
    ** Accessors
    *********/
    /// <summary>The value for the current screen.</summary>
    /// <remarks>The value is initialized the first time it's requested for that screen, unless it's set manually first.</remarks>
    public T Value
    {
        get => this.GetValueForScreen(Context.ScreenId);
        set => this.SetValueForScreen(Context.ScreenId, value);
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <remarks><strong>Limitation with nullable reference types:</strong> when the underlying type <typeparamref name="T"/> is nullable, this sets the default value to null regardless of whether you marked the type parameter nullable. To avoid that, set the default value with the 'createNewState' argument instead.</remarks>
    public PerScreen()
        : this(null, nullExpected: true) { }

    /// <summary>Construct an instance.</summary>
    /// <param name="createNewState">Create the initial state for a screen.</param>
    public PerScreen(Func<T> createNewState)
        : this(createNewState, nullExpected: false) { }

    /// <summary>Get all active values by screen ID. This doesn't initialize the value for a screen ID if it's not created yet.</summary>
    public IEnumerable<KeyValuePair<int, T>> GetActiveValues()
    {
        this.RemoveDeadScreens();
        return this.States.ToArray();
    }

    /// <summary>Get the value for a given screen ID, creating it if needed.</summary>
    /// <param name="screenId">The screen ID to check.</param>
    public T GetValueForScreen(int screenId)
    {
        this.RemoveDeadScreens();
        return this.States.TryGetValue(screenId, out T? state)
            ? state
            : this.States[screenId] = this.CreateNewState();
    }

    /// <summary>Set the value for a given screen ID.</summary>
    /// <param name="screenId">The screen ID whose value set.</param>
    /// <param name="value">The value to set.</param>
    public void SetValueForScreen(int screenId, T value)
    {
        this.RemoveDeadScreens();
        this.States[screenId] = value;
    }

    /// <summary>Remove all active values.</summary>
    public void ResetAllScreens()
    {
        this.RemoveScreens(_ => true);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="createNewState">Create the initial state for a screen.</param>
    /// <param name="nullExpected">Whether a null <paramref name="createNewState"/> value is expected.</param>
    /// <remarks>This constructor only exists to maintain backwards compatibility. In SMAPI 4.0.0, the overload that passes <c>nullExpected: false</c> should throw an exception instead.</remarks>
    private PerScreen(Func<T>? createNewState, bool nullExpected)
    {
        if (createNewState is null)
        {
            createNewState = (() => default!);

            if (!nullExpected)
            {
                SCore.DeprecationManager.Warn(
                    null,
                    $"calling the {nameof(PerScreen<T>)} constructor with null",
                    "3.14.0",
                    DeprecationLevel.Notice
                );
            }
        }

        this.CreateNewState = createNewState;
    }

    /// <summary>Remove screens which are no longer active.</summary>
    private void RemoveDeadScreens()
    {
        if (this.LastRemovedScreenId == Context.LastRemovedScreenId)
            return;
        this.LastRemovedScreenId = Context.LastRemovedScreenId;

        this.RemoveScreens(id => !Context.HasScreenId(id));
    }

    /// <summary>Remove screens matching a condition.</summary>
    /// <param name="shouldRemove">Returns whether a screen ID should be removed.</param>
    private void RemoveScreens(Func<int, bool> shouldRemove)
    {
        foreach (var pair in this.States.ToArray())
        {
            if (shouldRemove(pair.Key))
                this.States.Remove(pair.Key);
        }
    }
}
using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events;

/// <inheritdoc cref="IDisplayEvents" />
internal class ModDisplayEvents : ModEventsBase, IDisplayEvents
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public event EventHandler<MenuChangedEventArgs> MenuChanged
    {
        add => this.EventManager.MenuChanged.Add(value, this.Mod);
        remove => this.EventManager.MenuChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<RenderingEventArgs> Rendering
    {
        add => this.EventManager.Rendering.Add(value, this.Mod);
        remove => this.EventManager.Rendering.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<RenderedEventArgs> Rendered
    {
        add => this.EventManager.Rendered.Add(value, this.Mod);
        remove => this.EventManager.Rendered.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<RenderingWorldEventArgs> RenderingWorld
    {
        add => this.EventManager.RenderingWorld.Add(value, this.Mod);
        remove => this.EventManager.RenderingWorld.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<RenderedWorldEventArgs> RenderedWorld
    {
        add => this.EventManager.RenderedWorld.Add(value, this.Mod);
        remove => this.EventManager.RenderedWorld.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<RenderingActiveMenuEventArgs> RenderingActiveMenu
    {
        add => this.EventManager.RenderingActiveMenu.Add(value, this.Mod);
        remove => this.EventManager.RenderingActiveMenu.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<RenderedActiveMenuEventArgs> RenderedActiveMenu
    {
        add => this.EventManager.RenderedActiveMenu.Add(value, this.Mod);
        remove => this.EventManager.RenderedActiveMenu.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<RenderingHudEventArgs> RenderingHud
    {
        add => this.EventManager.RenderingHud.Add(value, this.Mod);
        remove => this.EventManager.RenderingHud.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<RenderedHudEventArgs> RenderedHud
    {
        add => this.EventManager.RenderedHud.Add(value, this.Mod);
        remove => this.EventManager.RenderedHud.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<WindowResizedEventArgs> WindowResized
    {
        add => this.EventManager.WindowResized.Add(value, this.Mod);
        remove => this.EventManager.WindowResized.Remove(value);
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod which uses this instance.</param>
    /// <param name="eventManager">The underlying event manager.</param>
    internal ModDisplayEvents(IModMetadata mod, EventManager eventManager)
        : base(mod, eventManager) { }
}
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Framework.Extensions;
using StardewModdingAPI.Internal;
#if SMAPI_FOR_ANDROID
using StardewModdingAPI.Mobile;
#endif
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Mods;

namespace StardewModdingAPI.Framework;

/// <summary>Invokes callbacks for mod hooks provided by the game.</summary>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Inherited from the game code.")]
internal class SModHooks : DelegatingModHooks
{
    /*********
    ** Fields
    *********/
    /// <summary>A callback to invoke before <see cref="Game1.newDayAfterFade"/> runs.</summary>
    private readonly Action BeforeNewDayAfterFade;

    /// <summary>Writes messages to the console.</summary>
    private readonly IMonitor Monitor;

    /// <summary>A callback to invoke when the load stage changes.</summary>
    private readonly Action<LoadStage> OnStageChanged;

    /// <summary>A callback to invoke when the game starts a render step in the draw loop.</summary>
    private readonly Action<RenderSteps, SpriteBatch, RenderTarget2D?> OnRenderingStep;

    /// <summary>A callback to invoke when the game finishes a render step in the draw loop.</summary>
    private readonly Action<RenderSteps, SpriteBatch, RenderTarget2D?> OnRenderedStep;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="parent">The underlying hooks to call by default.</param>
    /// <param name="beforeNewDayAfterFade">A callback to invoke before <see cref="Game1.newDayAfterFade"/> runs.</param>
    /// <param name="onStageChanged">A callback to invoke when the load stage changes.</param>
    /// <param name="onRenderingStep">A callback to invoke when the game starts a render step in the draw loop.</param>
    /// <param name="onRenderedStep">A callback to invoke when the game finishes a render step in the draw loop.</param>
    /// <param name="monitor">Writes messages to the console.</param>
    public SModHooks(ModHooks parent, Action beforeNewDayAfterFade, Action<LoadStage> onStageChanged, Action<RenderSteps, SpriteBatch, RenderTarget2D?> onRenderingStep, Action<RenderSteps, SpriteBatch, RenderTarget2D?> onRenderedStep, IMonitor monitor)
        : base(parent)
    {
        this.Monitor = monitor;
        this.BeforeNewDayAfterFade = beforeNewDayAfterFade;
        this.OnStageChanged = onStageChanged;
        this.OnRenderingStep = onRenderingStep;
        this.OnRenderedStep = onRenderedStep;

#if SMAPI_FOR_ANDROID
        AndroidSModHooks.Init();
#endif
    }

    /// <inheritdoc />
    public override void OnGame1_NewDayAfterFade(Action action)
    {
        this.BeforeNewDayAfterFade();
        action();
    }

    /// <inheritdoc />
    public override Task StartTask(Task task, string id)
    {
#if SMAPI_FOR_ANDROID
        return AndroidSModHooks.StartTaskBackground(task, id);
#else
        this.Monitor.Log($"Synchronizing '{id}' task...");
        task.RunSynchronously();
        this.Monitor.Log("   task complete.");
#endif
        return task;
    }

    /// <inheritdoc />
    public override Task<T> StartTask<T>(Task<T> task, string id)
    {
        this.Monitor.Log($"Synchronizing '{id}' task...");
        task.RunSynchronously();
        this.Monitor.Log("   task complete.");
        return task;
    }

    /// <inheritdoc />
    public override void CreatedInitialLocations()
    {
        this.OnStageChanged(LoadStage.CreatedInitialLocations);
    }

    /// <inheritdoc />
    public override void SaveAddedLocations()
    {
        this.OnStageChanged(LoadStage.SaveAddedLocations);
    }

    /// <inheritdoc />
    public override bool OnRendering(RenderSteps step, SpriteBatch sb, GameTime time, RenderTarget2D? target_screen)
    {
        this.OnRenderingStep(step, sb, target_screen);

        return true;
    }

    /// <inheritdoc />
#if SMAPI_FOR_ANDROID
    /// <summary>The render step currently being drawn.</summary>
    internal static RenderSteps CurrentRenderedStep = RenderSteps.FullScene;
#endif
    public override void OnRendered(RenderSteps step, SpriteBatch sb, GameTime time, RenderTarget2D? target_screen)
    {
#if SMAPI_FOR_ANDROID
        CurrentRenderedStep = step;
#endif
        this.OnRenderedStep(step, sb, target_screen);
    }

    /// <inheritdoc />
    public override bool TryDrawMenu(IClickableMenu menu, Action draw_menu_action)
    {
        try
        {
            draw_menu_action();
            return true;
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"The {menu.GetMenuChainLabel()} menu crashed while drawing itself. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
            Game1.activeClickableMenu.exitThisMenu();
            return false;
        }
    }
}

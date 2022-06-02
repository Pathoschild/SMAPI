using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Internal.Patching;
using StardewValley;

namespace StardewModdingAPI.Mods.ErrorHandler.Patches;

/// <summary>Harmony patches for <see cref="Dialogue"/> which intercept invalid dialogue lines and logs an error instead of crashing.</summary>
/// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
internal class DialoguePatcher : BasePatcher
{
    /*********
    ** Fields
    *********/
    /// <summary>Writes messages to the console and log file on behalf of the game.</summary>
    private static IMonitor MonitorForGame = null!;

    /// <summary>Simplifies access to private code.</summary>
    private static IReflectionHelper Reflection = null!;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
    /// <param name="reflector">Simplifies access to private code.</param>
    public DialoguePatcher(IMonitor monitorForGame, IReflectionHelper reflector)
    {
        DialoguePatcher.MonitorForGame = monitorForGame;
        DialoguePatcher.Reflection = reflector;
    }

    /// <inheritdoc />
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireConstructor<Dialogue>(typeof(string), typeof(NPC)),
            finalizer: this.GetHarmonyMethod(nameof(DialoguePatcher.Finalize_Constructor))
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>The method to call when the Dialogue constructor throws an exception.</summary>
    /// <param name="__instance">The instance being patched.</param>
    /// <param name="masterDialogue">The dialogue being parsed.</param>
    /// <param name="speaker">The NPC for which the dialogue is being parsed.</param>
    /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
    /// <returns>Returns the exception to throw, if any.</returns>
    private static Exception? Finalize_Constructor(Dialogue __instance, string masterDialogue, NPC? speaker, Exception? __exception)
    {
        if (__exception != null)
        {
            // log message
            string? name = !string.IsNullOrWhiteSpace(speaker?.Name) ? speaker.Name : null;
            DialoguePatcher.MonitorForGame.Log($"Failed parsing dialogue string{(name != null ? $" for {name}" : "")}:\n{masterDialogue}\n{__exception.GetLogSummary()}", LogLevel.Error);

            // set default dialogue
            IReflectedMethod parseDialogueString = DialoguePatcher.Reflection.GetMethod(__instance, "parseDialogueString");
            IReflectedMethod checkForSpecialDialogueAttributes = DialoguePatcher.Reflection.GetMethod(__instance, "checkForSpecialDialogueAttributes");
            parseDialogueString.Invoke("...");
            checkForSpecialDialogueAttributes.Invoke();
        }

        return null;
    }
}
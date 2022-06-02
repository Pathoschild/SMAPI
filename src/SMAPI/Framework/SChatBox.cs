using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework;

/// <summary>SMAPI's implementation of the chat box which intercepts errors for logging.</summary>
internal class SChatBox : ChatBox
{
    /*********
    ** Fields
    *********/
    /// <summary>Encapsulates monitoring and logging.</summary>
    private readonly IMonitor Monitor;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="monitor">Encapsulates monitoring and logging.</param>
    public SChatBox(IMonitor monitor)
    {
        this.Monitor = monitor;
    }

    /// <inheritdoc />
    protected override void runCommand(string command)
    {
        this.Monitor.Log($"> chat command: {command}");
        base.runCommand(command);
    }

    /// <inheritdoc />
    public override void receiveChatMessage(long sourceFarmer, int chatKind, LocalizedContentManager.LanguageCode language, string message)
    {
        if (chatKind == ChatBox.errorMessage)
        {
            // log error
            this.Monitor.Log(message, LogLevel.Error);

            // add event details if applicable
            if (Game1.CurrentEvent != null && message.StartsWith("Event script error:"))
                this.Monitor.Log($"In event #{Game1.CurrentEvent.id} for location {Game1.currentLocation?.NameOrUniqueName}", LogLevel.Error);
        }

        base.receiveChatMessage(sourceFarmer, chatKind, language, message);
    }
}

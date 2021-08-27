using System;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.World
{
    /// <summary>A command which immediately warps all NPCs to their scheduled positions. To hurry a single NPC, see <c>debug hurry npc-name</c> instead.</summary>
    internal class HurryAllCommand : ConsoleCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public HurryAllCommand()
            : base(
                name: "hurry_all",
                description: "Immediately warps all NPCs to their scheduled positions. (To hurry a single NPC, use `debug hurry npc-name` instead.)\n\n"
                    + "Usage: hurry_all"
            )
        { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // check context
            if (!Context.IsWorldReady)
            {
                monitor.Log("You need to load a save to use this command.", LogLevel.Error);
                return;
            }

            // hurry all NPCs
            foreach (NPC npc in Utility.getAllCharacters())
            {
                if (!npc.isVillager())
                    continue;

                monitor.Log($"Hurrying {npc.Name}...");
                try
                {
                    npc.warpToPathControllerDestination();
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed hurrying {npc.Name}. Technical details:\n{ex}", LogLevel.Error);
                }
            }

            monitor.Log("Done!", LogLevel.Info);
        }
    }
}

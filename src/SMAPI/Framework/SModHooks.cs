using System;
using System.Threading.Tasks;
using StardewValley;

namespace StardewModdingAPI.Framework
{
    /// <summary>Invokes callbacks for mod hooks provided by the game.</summary>
    internal class SModHooks : ModHooks
    {
        /*********
        ** Fields
        *********/
        /// <summary>A callback to invoke before <see cref="Game1.newDayAfterFade"/> runs.</summary>
        private readonly Action BeforeNewDayAfterFade;

        /// <summary>Writes messages to the console.</summary>
        private readonly IMonitor Monitor;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="beforeNewDayAfterFade">A callback to invoke before <see cref="Game1.newDayAfterFade"/> runs.</param>
        /// <param name="monitor">Writes messages to the console.</param>
        public SModHooks(Action beforeNewDayAfterFade, IMonitor monitor)
        {
            this.BeforeNewDayAfterFade = beforeNewDayAfterFade;
            this.Monitor = monitor;
        }

        /// <summary>A hook invoked when <see cref="Game1.newDayAfterFade"/> is called.</summary>
        /// <param name="action">The vanilla <see cref="Game1.newDayAfterFade"/> logic.</param>
        public override void OnGame1_NewDayAfterFade(Action action)
        {
            this.BeforeNewDayAfterFade?.Invoke();
            action();
        }

        /// <summary>Start an asynchronous task for the game.</summary>
        /// <param name="task">The task to start.</param>
        /// <param name="id">A unique key which identifies the task.</param>
        public override Task StartTask(Task task, string id)
        {
            this.Monitor.Log($"Synchronizing '{id}' task...");
            task.RunSynchronously();
            this.Monitor.Log("   task complete.");
            return task;
        }

        /// <summary>Start an asynchronous task for the game.</summary>
        /// <param name="task">The task to start.</param>
        /// <param name="id">A unique key which identifies the task.</param>
        public override Task<T> StartTask<T>(Task<T> task, string id)
        {
            this.Monitor.Log($"Synchronizing '{id}' task...");
            task.RunSynchronously();
            this.Monitor.Log("   task complete.");
            return task;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StardewModdingAPI.Mobile;

internal static class AndroidMainThread
{
    internal static Thread MainThread { get; private set; }
    internal static int MainThreadID => MainThread.ManagedThreadId;
    internal static bool IsOnMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadID;
    internal static bool IsOnBackgroundThread => IsOnMainThread is false;

    //init at Program entry point
    public static void Init(string[] args)
    {
        MainThread = Thread.CurrentThread;
    }
    public static void InvokeOnMainThread(Action callback, string? taskName = null)
    {
        var task = new Task(() => { callback(); });
        AndroidSModHooks.AddTaskRunOnMainThread(task, taskName);
        task.Wait();
    }
}

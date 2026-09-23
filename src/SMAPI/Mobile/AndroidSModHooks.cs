using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Internal;
using StardewValley.Extensions;

namespace StardewModdingAPI.Mobile;

[HarmonyPatch]
internal static class AndroidSModHooks
{
    static IMonitor Monitor => SCore.Instance.SMAPIMonitor;

    internal static void Init()
    {
        AndroidGameLoopManager.RegisterOnGameUpdating(OnGameUpdating_TaskUpdate);
    }

    internal static bool OnGameUpdating_TaskUpdate(GameTime time)
    {

#if false
        //debug only
        if (SCore.ProcessTicksElapsed % 30 == 0 && queueTaskNeedToStartOnMainThread.IsEmpty is false)
        {
            Console.WriteLine();
            Console.WriteLine("SMod Hook Updating..");
            Console.WriteLine("task: " + queueTaskNeedToStartOnMainThread.Count);
            //foreach (var task in tasks)
            //{
            //    Console.WriteLine($"status task ID: {task.Id}");
            //    Console.WriteLine($"  status: {task.Status}");
            //    Console.WriteLine($"  IsCanceled: {task.IsCanceled}");
            //    Console.WriteLine($"  IsCompleted: {task.IsCompleted}");
            //    Console.WriteLine($"  IsCompletedSuccessfully: {task.IsCompletedSuccessfully}");
            //    Console.WriteLine($"  IsFaulted: {task.IsFaulted}");
            //}
        }
#endif

        bool markSkipGameUpdating = false;

        //process task on main thread
        //update main thread task

        // millisecond 1000.0 == 1 sec
        double runTaskOnMainThreadTotalTime = 0;
        int runTaskOnMainThreadCount = 0;
        bool isInGame = false;
        while (queueTaskNeedToStartOnMainThread.TryDequeue(out var task))
        {
            bool shouldShowLogTask = task.name is not null;
            markSkipGameUpdating = true;
            var stopwatch = Stopwatch.StartNew();
            //if (shouldShowLogTask)
            //    Monitor.Log($"Start taskOnMainThread: '{task.name}'");

            task.task.RunSynchronously();
            stopwatch.Stop();
            runTaskOnMainThreadCount++;
            runTaskOnMainThreadTotalTime += stopwatch.Elapsed.TotalMilliseconds;
            if (shouldShowLogTask)
            {
                Monitor.Log($"Done taskOnMainThread: '{task.name}' in {stopwatch.Elapsed.TotalMilliseconds}ms");
            }

            //debug
            if (runTaskOnMainThreadTotalTime > 2000)
            {
                Monitor.Log($"Warn!!, current task MainThread '{task.name}' " +
                    $"it's very long time in {runTaskOnMainThreadTotalTime:F3}ms", LogLevel.Warn);
            }

            //limit run task
            //maybe 1-2 frame, or 16ms or 32ms
            if (runTaskOnMainThreadTotalTime > 32)
            {
                break;
            }
        }

        //process task background thread
        lock (listTaskOnThreadBackground)
        {
            if (listTaskOnThreadBackground.Count > 0)
            {
                int removeCount = listTaskOnThreadBackground.RemoveAll(task => task.IsCompleted);
            }
            if (listTaskOnThreadBackground.Count > 0)
            {
                markSkipGameUpdating = true;
            }
        }

        return markSkipGameUpdating;
    }
    internal class TaskOnMainThread
    {
        public readonly string? name;
        public readonly Task task;
        public TaskOnMainThread(Task task, string? name)
        {
            this.task = task;
            this.name = name;
        }
    }
    static List<Task> listTaskOnThreadBackground = new();
    static ConcurrentQueue<TaskOnMainThread> queueTaskNeedToStartOnMainThread = new();

    internal static Task AddTaskRunOnMainThread(Action callback, string name)
        => AddTaskRunOnMainThread(new Task(callback), name);

    internal static Task AddTaskRunOnMainThread(Task yourTask, string? taskName)
    {
        var taskOnMainThread = new TaskOnMainThread(yourTask, taskName);
        queueTaskNeedToStartOnMainThread.Enqueue(taskOnMainThread);
        return yourTask;
    }
    internal static Task StartTaskBackground(Action callback, string nameID)
    {
        return StartTaskBackground(new Task(callback), nameID);
    }
    internal static Task StartTaskBackground(Task gameTask, string nameID)
    {
        Monitor.Log($"Try StartTask name: '{nameID}' on Android SModHook");
#if false
        //debug only
        Console.WriteLine("Debug try start task background in main thread");
        var st = Stopwatch.StartNew();
        gameTask.RunSynchronously();
        st.Stop();
        Console.WriteLine($"done task: {nameID} in {st.Elapsed.TotalMilliseconds}ms");
        return gameTask;
#endif

        //setup
        var currentModHookTask = new Task(() =>
        {
            try
            {
                var st = Stopwatch.StartNew();
                Monitor.Log($"Starting Task On Background id: '{nameID}'");
                gameTask.RunSynchronously();
                st.Stop();
                Monitor.Log($"Completed Task On Background id: {nameID} in {st.Elapsed.TotalMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Monitor.Log($"Exception on task id: {nameID}");
                Monitor.Log($"{ex.GetLogSummary()}");
            }
        });

        //Console.WriteLine("try add new task, current task count: " + listTaskOnThreadBackground.Count);
        lock (listTaskOnThreadBackground)
        {
            listTaskOnThreadBackground.Add(currentModHookTask);
        }

        //ready
        currentModHookTask.Start();

        //Console.WriteLine($"End & return StartTask name: '{nameID}', taskIDNumber: {currentModHookTask.Id}");
        return currentModHookTask;
    }

}

---
name: android-smapi-runtime
description: >-
  Threading, Activity lifetime, and main-thread rules for SMAPI Mobile code.
  Use when editing src/SMAPI/Mobile, AndroidMainThread, SMAPIActivityTool, or Android crashes, ANRs, and Activity leaks.
---

# Android SMAPI runtime

`src/SMAPI/Mobile/` runs inside the game activity on .NET 9 Mono (`arm64-v8a`). The loader owns the activity lifecycle.

## Threading

- Do not spin (`while (!flag) {}`) or poll with `Thread.Sleep`.
- From a background thread, hop to the game thread with `AndroidMainThread.InvokeOnMainThread`. That helper blocks the caller on `Task.Wait()`; do not call it from the main thread.
- Do not use `.Result` or `.Wait()` on the Android UI thread.

## Activities

- Do not store `Activity` in a static field or a long-lived event handler.
- Hold `WeakReference<Activity>`, and clear it when the activity finishes.
- `SMAPIActivityTool.MainActivity` currently caches a strong static `Activity` looked up from `MainActivity.instance`. Do not add more caches like that.

## Other

- Log exceptions. No empty `catch`.
- Game assemblies are read-only references. Do not patch the installed APK.
- If you add zip extraction, reject entry paths whose full path is outside the destination directory.

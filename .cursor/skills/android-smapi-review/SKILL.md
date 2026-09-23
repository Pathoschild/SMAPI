---
name: android-smapi-review
description: >-
  Review Android SMAPI diffs for Harmony, threading, Activity leaks, and loader boundary mistakes.
  Use when reviewing a branch, pull request, or uncommitted Android changes in this SMAPI repo.
---

# Android SMAPI review

Read `AGENTS.md` first. Review only this repo. Loader UI, assembly-store, and APK changes belong in AndroidSMAPI.

## Check

1. `git diff` for `src/SMAPI/Mobile/`, `src/SMAPI/SMAPI.csproj`, `src/Directory.Packages.props`, `src/DependenciesDll/`, `src/PackSMAPIZip/`, `build/android/`.
2. Harmony: Android configuration references `DependenciesDll/0Harmony.dll` and modular MonoMod. Flag `Lib.Harmony` and `MonoMod.RuntimeDetour`.
3. Threading: spin-waits, `.Wait()` / `.Result` on the UI thread, static `Activity` fields.
4. Empty `catch`, zip paths that skip a destination-prefix check, any write to the game APK.
5. `dependencies.txt` must not gain Harmony or MonoMod copies.
6. Desktop `net6.0` and `build/0Harmony.dll` stay unchanged unless the diff is explicitly a desktop change.

## Report

- **Summary** and a merge call: Approve, Approve with comments, or Request changes.
- **Findings** with severity (Critical, High, Medium, Low), file, and a concrete fix. Blockers are invariant breaks, a broken Android build, or a boot crash.
- **Gaps** that need a device: loader install of the new zip, arm64 logcat, mod load.

State when no issues are found.

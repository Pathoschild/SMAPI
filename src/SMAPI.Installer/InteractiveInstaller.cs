using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using StardewModdingApi.Installer.Enums;
using StardewModdingAPI.Installer.Framework;
using StardewModdingAPI.Internal.ConsoleWriting;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework;
using StardewModdingAPI.Toolkit.Framework.ModScanning;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingApi.Installer
{
    /// <summary>Interactively performs the install and uninstall logic.</summary>
    internal class InteractiveInstaller
    {
        /*********
        ** Fields
        *********/
        /// <summary>The absolute path to the directory containing the files to copy into the game folder.</summary>
        private readonly string BundlePath;

        /// <summary>The mod IDs which the installer should allow as bundled mods.</summary>
        private readonly string[] BundledModIDs = {
            "SMAPI.SaveBackup",
            "SMAPI.ConsoleCommands",
            "SMAPI.ErrorHandler"
        };

        /// <summary>Get the absolute file or folder paths to remove when uninstalling SMAPI.</summary>
        /// <param name="installDir">The folder for Stardew Valley and SMAPI.</param>
        /// <param name="modsDir">The folder for SMAPI mods.</param>
        private IEnumerable<string> GetUninstallPaths(DirectoryInfo installDir, DirectoryInfo modsDir)
        {
            string GetInstallPath(string path) => Path.Combine(installDir.FullName, path);

            // current files
            yield return GetInstallPath("StardewModdingAPI");          // Linux/macOS only
            yield return GetInstallPath("StardewModdingAPI.dll");
            yield return GetInstallPath("StardewModdingAPI.exe");
            yield return GetInstallPath("StardewModdingAPI.exe.config");
            yield return GetInstallPath("StardewModdingAPI.exe.mdb");  // Linux/macOS only
            yield return GetInstallPath("StardewModdingAPI.pdb");      // Windows only
            yield return GetInstallPath("StardewModdingAPI.runtimeconfig.json");
            yield return GetInstallPath("StardewModdingAPI.xml");
            yield return GetInstallPath("smapi-internal");
            yield return GetInstallPath("steam_appid.txt");

            // obsolete
            yield return GetInstallPath("libgdiplus.dylib");                 // before 3.13 (macOS only)
            yield return GetInstallPath(Path.Combine("Mods", ".cache"));     // 1.3-1.4
            yield return GetInstallPath(Path.Combine("Mods", "TrainerMod")); // *–2.0 (renamed to ConsoleCommands)
            yield return GetInstallPath("Mono.Cecil.Rocks.dll");             // 1.3–1.8
            yield return GetInstallPath("StardewModdingAPI-settings.json");  // 1.0-1.4
            yield return GetInstallPath("StardewModdingAPI.AssemblyRewriters.dll"); // 1.3-2.5.5
            yield return GetInstallPath("0Harmony.dll");                    // moved in 2.8
            yield return GetInstallPath("0Harmony.pdb");                    // moved in 2.8
            yield return GetInstallPath("Mono.Cecil.dll");                  // moved in 2.8
            yield return GetInstallPath("Newtonsoft.Json.dll");             // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.config.json");   // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.crash.marker");  // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.metadata.json"); // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.update.marker"); // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.dll");   // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.pdb");   // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.xml");   // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.CoreInterfaces.dll"); // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.CoreInterfaces.pdb"); // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.CoreInterfaces.xml"); // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI-x64.exe");         // before 3.13

            if (modsDir.Exists)
            {
                foreach (DirectoryInfo modDir in modsDir.EnumerateDirectories())
                    yield return Path.Combine(modDir.FullName, ".cache"); // 1.4–1.7
            }

            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "ErrorLogs"); // remove old log files
        }

        /// <summary>Handles writing text to the console.</summary>
        private IConsoleWriter ConsoleWriter;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="bundlePath">The absolute path to the directory containing the files to copy into the game folder.</param>
        public InteractiveInstaller(string bundlePath)
        {
            this.BundlePath = bundlePath;
            this.ConsoleWriter = new ColorfulConsoleWriter(EnvironmentUtility.DetectPlatform());
        }

        /// <summary>Run the install or uninstall script.</summary>
        /// <param name="args">The command line arguments.</param>
        /// <remarks>
        /// Initialization flow:
        ///     1. Collect information (mainly OS and install path) and validate it.
        ///     2. Ask the user whether to install or uninstall.
        ///
        /// Uninstall logic:
        ///     1. On Linux/macOS: if a backup of the launcher exists, delete the launcher and restore the backup.
        ///     2. Delete all files and folders in the game directory matching one of the values returned by <see cref="GetUninstallPaths"/>.
        ///
        /// Install flow:
        ///     1. Run the uninstall flow.
        ///     2. Copy the SMAPI files from package/Windows or package/Mono into the game directory.
        ///     3. On Linux/macOS: back up the game launcher and replace it with the SMAPI launcher. (This isn't possible on Windows, so the user needs to configure it manually.)
        ///     4. Create the 'Mods' directory.
        ///     5. Copy the bundled mods into the 'Mods' directory (deleting any existing versions).
        ///     6. Move any mods from app data into game's mods directory.
        /// </remarks>
        public void Run(string[] args)
        {
            /*********
            ** Step 1: initial setup
            *********/
            /****
            ** Get basic info & set window title
            ****/
            ModToolkit toolkit = new ModToolkit();
            var context = new InstallerContext();
            Console.Title = $"SMAPI {context.GetInstallerVersion()} installer on {context.Platform} {context.PlatformName}";
            Console.WriteLine();

            /****
            ** Check if correct installer
            ****/
#if SMAPI_FOR_WINDOWS
            if (context.IsUnix)
            {
                this.PrintError($"This is the installer for Windows. Run the 'install on {context.Platform}.{(context.Platform == Platform.Mac ? "command" : "sh")}' file instead.");
                Console.ReadLine();
                return;
            }
#else
            if (context.IsWindows)
            {
                this.PrintError($"This is the installer for Linux/macOS. Run the 'install on Windows.exe' file instead.");
                Console.ReadLine();
                return;
            }
#endif

            /****
            ** read command-line arguments
            ****/
            // get action from CLI
            bool installArg = args.Contains("--install");
            bool uninstallArg = args.Contains("--uninstall");
            if (installArg && uninstallArg)
            {
                this.PrintError("You can't specify both --install and --uninstall command-line flags.");
                Console.ReadLine();
                return;
            }

            // get game path from CLI
            string gamePathArg = null;
            {
                int pathIndex = Array.LastIndexOf(args, "--game-path") + 1;
                if (pathIndex >= 1 && args.Length >= pathIndex)
                    gamePathArg = args[pathIndex];
            }


            /*********
            ** Step 2: choose a theme (can't auto-detect on Linux/macOS)
            *********/
            MonitorColorScheme scheme = MonitorColorScheme.AutoDetect;
            if (context.IsUnix)
            {
                /****
                ** print header
                ****/
                this.PrintPlain("Hi there! I'll help you install or remove SMAPI. Just a few questions first.");
                this.PrintPlain("----------------------------------------------------------------------------");
                Console.WriteLine();

                /****
                ** show theme selector
                ****/
                // get theme writers
                var lightBackgroundWriter = new ColorfulConsoleWriter(context.Platform, ColorfulConsoleWriter.GetDefaultColorSchemeConfig(MonitorColorScheme.LightBackground));
                var darkBackgroundWriter = new ColorfulConsoleWriter(context.Platform, ColorfulConsoleWriter.GetDefaultColorSchemeConfig(MonitorColorScheme.DarkBackground));

                // print question
                this.PrintPlain("Which text looks more readable?");
                Console.WriteLine();
                Console.Write("   [1] ");
                lightBackgroundWriter.WriteLine("Dark text on light background", ConsoleLogLevel.Info);
                Console.Write("   [2] ");
                darkBackgroundWriter.WriteLine("Light text on dark background", ConsoleLogLevel.Info);
                Console.WriteLine();

                // handle choice
                string choice = this.InteractivelyChoose("Type 1 or 2, then press enter.", new[] { "1", "2" });
                switch (choice)
                {
                    case "1":
                        scheme = MonitorColorScheme.LightBackground;
                        this.ConsoleWriter = lightBackgroundWriter;
                        break;
                    case "2":
                        scheme = MonitorColorScheme.DarkBackground;
                        this.ConsoleWriter = darkBackgroundWriter;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected action key '{choice}'.");
                }
            }
            Console.Clear();


            /*********
            ** Step 3: find game folder
            *********/
            InstallerPaths paths;
            {
                /****
                ** print header
                ****/
                this.PrintInfo("Hi there! I'll help you install or remove SMAPI. Just a few questions first.");
                this.PrintDebug($"Color scheme: {this.GetDisplayText(scheme)}");
                this.PrintDebug("----------------------------------------------------------------------------");
                Console.WriteLine();

                /****
                ** collect details
                ****/
                // get game path
                DirectoryInfo installDir = this.InteractivelyGetInstallPath(toolkit, context, gamePathArg);
                if (installDir == null)
                {
                    this.PrintError("Failed finding your game path.");
                    Console.ReadLine();
                    return;
                }

                // get folders
                DirectoryInfo bundleDir = new DirectoryInfo(this.BundlePath);
                paths = new InstallerPaths(bundleDir, installDir);
            }


            /*********
            ** Step 4: validate assumptions
            *********/
            // executable exists
            if (!File.Exists(paths.GameDllPath))
            {
                this.PrintError("The detected game install path doesn't contain a Stardew Valley executable.");
                Console.ReadLine();
                return;
            }
            Console.Clear();


            /*********
            ** Step 5: ask what to do
            *********/
            ScriptAction action;
            {
                /****
                ** print header
                ****/
                this.PrintInfo("Hi there! I'll help you install or remove SMAPI. Just one question first.");
                this.PrintDebug($"Game path: {paths.GamePath}");
                this.PrintDebug($"Color scheme: {this.GetDisplayText(scheme)}");
                this.PrintDebug("----------------------------------------------------------------------------");
                Console.WriteLine();

                /****
                ** ask what to do
                ****/
                if (installArg)
                    action = ScriptAction.Install;
                else if (uninstallArg)
                    action = ScriptAction.Uninstall;
                else
                {
                    this.PrintInfo("What do you want to do?");
                    Console.WriteLine();
                    this.PrintInfo("[1] Install SMAPI.");
                    this.PrintInfo("[2] Uninstall SMAPI.");
                    Console.WriteLine();

                    string choice = this.InteractivelyChoose("Type 1 or 2, then press enter.", new[] { "1", "2" });
                    switch (choice)
                    {
                        case "1":
                            action = ScriptAction.Install;
                            break;
                        case "2":
                            action = ScriptAction.Uninstall;
                            break;
                        default:
                            throw new InvalidOperationException($"Unexpected action key '{choice}'.");
                    }
                }
            }
            Console.Clear();


            /*********
            ** Step 6: apply
            *********/
            {
                /****
                ** print header
                ****/
                this.PrintInfo($"That's all I need! I'll {action.ToString().ToLower()} SMAPI now.");
                this.PrintDebug($"Game path: {paths.GamePath}");
                this.PrintDebug($"Color scheme: {this.GetDisplayText(scheme)}");
                this.PrintDebug("----------------------------------------------------------------------------");
                Console.WriteLine();

                /****
                ** Back up user settings
                ****/
                if (File.Exists(paths.ApiUserConfigPath))
                    File.Copy(paths.ApiUserConfigPath, paths.BundleApiUserConfigPath);

                /****
                ** Always uninstall old files
                ****/
                // restore game launcher
                if (context.IsUnix && File.Exists(paths.BackupLaunchScriptPath))
                {
                    this.PrintDebug("Removing SMAPI launcher...");
                    this.InteractivelyDelete(paths.VanillaLaunchScriptPath);
                    File.Move(paths.BackupLaunchScriptPath, paths.VanillaLaunchScriptPath);
                }

                // remove old files
                string[] removePaths = this.GetUninstallPaths(paths.GameDir, paths.ModsDir)
                    .Where(path => Directory.Exists(path) || File.Exists(path))
                    .ToArray();
                if (removePaths.Any())
                {
                    this.PrintDebug(action == ScriptAction.Install ? "Removing previous SMAPI files..." : "Removing SMAPI files...");
                    foreach (string path in removePaths)
                        this.InteractivelyDelete(path);
                }

                // move global save data folder (changed in 3.2)
                {
                    string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley");
                    DirectoryInfo oldDir = new DirectoryInfo(Path.Combine(dataPath, "Saves", ".smapi"));
                    DirectoryInfo newDir = new DirectoryInfo(Path.Combine(dataPath, ".smapi"));

                    if (oldDir.Exists)
                    {
                        if (newDir.Exists)
                            this.InteractivelyDelete(oldDir.FullName);
                        else
                            oldDir.MoveTo(newDir.FullName);
                    }
                }

                /****
                ** Install new files
                ****/
                if (action == ScriptAction.Install)
                {
                    // copy SMAPI files to game dir
                    this.PrintDebug("Adding SMAPI files...");
                    foreach (FileSystemInfo sourceEntry in paths.BundleDir.EnumerateFileSystemInfos().Where(this.ShouldCopy))
                    {
                        this.InteractivelyDelete(Path.Combine(paths.GameDir.FullName, sourceEntry.Name));
                        this.RecursiveCopy(sourceEntry, paths.GameDir);
                    }

                    // replace mod launcher (if possible)
                    if (context.IsUnix)
                    {
                        this.PrintDebug("Safely replacing game launcher...");

                        // back up & remove current launcher
                        if (File.Exists(paths.VanillaLaunchScriptPath))
                        {
                            if (!File.Exists(paths.BackupLaunchScriptPath))
                                File.Move(paths.VanillaLaunchScriptPath, paths.BackupLaunchScriptPath);
                            else
                                this.InteractivelyDelete(paths.VanillaLaunchScriptPath);
                        }

                        // add new launcher
                        File.Move(paths.NewLaunchScriptPath, paths.VanillaLaunchScriptPath);

                        // mark files executable
                        // (MSBuild doesn't keep permission flags for files zipped in a build task.)
                        foreach (string path in new[] { paths.VanillaLaunchScriptPath, paths.UnixSmapiExecutablePath })
                        {
                            new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = "chmod",
                                    Arguments = $"755 \"{path}\"",
                                    CreateNoWindow = true
                                }
                            }.Start();
                        }
                    }

                    // create mods directory (if needed)
                    if (!paths.ModsDir.Exists)
                    {
                        this.PrintDebug("Creating mods directory...");
                        paths.ModsDir.Create();
                    }

                    // add or replace bundled mods
                    DirectoryInfo bundledModsDir = new DirectoryInfo(Path.Combine(paths.BundlePath, "Mods"));
                    if (bundledModsDir.Exists && bundledModsDir.EnumerateDirectories().Any())
                    {
                        this.PrintDebug("Adding bundled mods...");

                        ModFolder[] targetMods = toolkit.GetModFolders(paths.ModsPath).ToArray();
                        foreach (ModFolder sourceMod in toolkit.GetModFolders(bundledModsDir.FullName))
                        {
                            // validate source mod
                            if (sourceMod.Manifest == null)
                            {
                                this.PrintWarning($"   ignored invalid bundled mod {sourceMod.DisplayName}: {sourceMod.ManifestParseError}");
                                continue;
                            }
                            if (!this.BundledModIDs.Contains(sourceMod.Manifest.UniqueID))
                            {
                                this.PrintWarning($"   ignored unknown '{sourceMod.DisplayName}' mod in the installer folder. To add mods, put them here instead: {paths.ModsPath}");
                                continue;
                            }

                            // find target folder
                            ModFolder targetMod = targetMods.FirstOrDefault(p => p.Manifest?.UniqueID?.Equals(sourceMod.Manifest.UniqueID, StringComparison.OrdinalIgnoreCase) == true);
                            DirectoryInfo defaultTargetFolder = new DirectoryInfo(Path.Combine(paths.ModsPath, sourceMod.Directory.Name));
                            DirectoryInfo targetFolder = targetMod?.Directory ?? defaultTargetFolder;
                            this.PrintDebug(targetFolder.FullName == defaultTargetFolder.FullName
                                ? $"   adding {sourceMod.Manifest.Name}..."
                                : $"   adding {sourceMod.Manifest.Name} to {Path.Combine(paths.ModsDir.Name, PathUtilities.GetRelativePath(paths.ModsPath, targetFolder.FullName))}..."
                            );

                            // remove existing folder
                            if (targetFolder.Exists)
                                this.InteractivelyDelete(targetFolder.FullName);

                            // copy files
                            this.RecursiveCopy(sourceMod.Directory, paths.ModsDir, filter: this.ShouldCopy);
                        }
                    }

                    // set SMAPI's color scheme if defined
                    if (scheme != MonitorColorScheme.AutoDetect)
                    {
                        string text = File
                            .ReadAllText(paths.ApiConfigPath)
                            .Replace(@"""UseScheme"": ""AutoDetect""", $@"""UseScheme"": ""{scheme}""");
                        File.WriteAllText(paths.ApiConfigPath, text);
                    }

                    // remove obsolete appdata mods
                    this.InteractivelyRemoveAppDataMods(paths.ModsDir, bundledModsDir);
                }
            }
            Console.WriteLine();
            Console.WriteLine();


            /*********
            ** Step 7: final instructions
            *********/
            if (context.IsWindows)
            {
                if (action == ScriptAction.Install)
                {
                    this.PrintSuccess("SMAPI is installed! If you use Steam, set your launch options to enable achievements (see smapi.io/install):");
                    this.PrintSuccess($"    \"{Path.Combine(paths.GamePath, "StardewModdingAPI.exe")}\" %command%");
                    Console.WriteLine();
                    this.PrintSuccess("If you don't use Steam, launch StardewModdingAPI.exe in your game folder to play with mods.");
                }
                else
                    this.PrintSuccess("SMAPI is removed! If you configured Steam to launch SMAPI, don't forget to clear your launch options.");
            }
            else
            {
                this.PrintSuccess(action == ScriptAction.Install
                    ? "SMAPI is installed! Launch the game the same way as before to play with mods."
                    : "SMAPI is removed! Launch the game the same way as before to play without mods."
                );
            }

            Console.ReadKey();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the display text for a color scheme.</summary>
        /// <param name="scheme">The color scheme.</param>
        private string GetDisplayText(MonitorColorScheme scheme)
        {
            switch (scheme)
            {
                case MonitorColorScheme.AutoDetect:
                    return "auto-detect";
                case MonitorColorScheme.DarkBackground:
                    return "light text on dark background";
                case MonitorColorScheme.LightBackground:
                    return "dark text on light background";
                default:
                    return scheme.ToString();
            }
        }

        /// <summary>Print a message without formatting.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintPlain(string text) => Console.WriteLine(text);

        /// <summary>Print a debug message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintDebug(string text) => this.ConsoleWriter.WriteLine(text, ConsoleLogLevel.Debug);

        /// <summary>Print a debug message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintInfo(string text) => this.ConsoleWriter.WriteLine(text, ConsoleLogLevel.Info);

        /// <summary>Print a warning message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintWarning(string text) => this.ConsoleWriter.WriteLine(text, ConsoleLogLevel.Warn);

        /// <summary>Print a warning message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintError(string text) => this.ConsoleWriter.WriteLine(text, ConsoleLogLevel.Error);

        /// <summary>Print a success message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintSuccess(string text) => this.ConsoleWriter.WriteLine(text, ConsoleLogLevel.Success);

        /// <summary>Interactively delete a file or folder path, and block until deletion completes.</summary>
        /// <param name="path">The file or folder path.</param>
        private void InteractivelyDelete(string path)
        {
            while (true)
            {
                try
                {
                    FileUtilities.ForceDelete(Directory.Exists(path) ? new DirectoryInfo(path) : (FileSystemInfo)new FileInfo(path));
                    break;
                }
                catch (Exception ex)
                {
                    this.PrintError($"Oops! The installer couldn't delete {path}: [{ex.GetType().Name}] {ex.Message}.");
                    this.PrintError("Try rebooting your computer and then run the installer again. If that doesn't work, try deleting it yourself then press any key to retry.");
                    Console.ReadKey();
                }
            }
        }

        /// <summary>Recursively copy a directory or file.</summary>
        /// <param name="source">The file or folder to copy.</param>
        /// <param name="targetFolder">The folder to copy into.</param>
        /// <param name="filter">A filter which matches directories and files to copy, or <c>null</c> to match all.</param>
        private void RecursiveCopy(FileSystemInfo source, DirectoryInfo targetFolder, Func<FileSystemInfo, bool> filter = null)
        {
            if (filter != null && !filter(source))
                return;

            if (!targetFolder.Exists)
                targetFolder.Create();

            switch (source)
            {
                case FileInfo sourceFile:
                    sourceFile.CopyTo(Path.Combine(targetFolder.FullName, sourceFile.Name));
                    break;

                case DirectoryInfo sourceDir:
                    DirectoryInfo targetSubfolder = new DirectoryInfo(Path.Combine(targetFolder.FullName, sourceDir.Name));
                    foreach (var entry in sourceDir.EnumerateFileSystemInfos())
                        this.RecursiveCopy(entry, targetSubfolder, filter);
                    break;

                default:
                    throw new NotSupportedException($"Unknown filesystem info type '{source.GetType().FullName}'.");
            }
        }

        /// <summary>Interactively ask the user to choose a value.</summary>
        /// <param name="print">A callback which prints a message to the console.</param>
        /// <param name="message">The message to print.</param>
        /// <param name="options">The allowed options (not case sensitive).</param>
        /// <param name="indent">The indentation to prefix to output.</param>
        private string InteractivelyChoose(string message, string[] options, string indent = "", Action<string> print = null)
        {
            print ??= this.PrintInfo;

            while (true)
            {
                print(indent + message);
                Console.Write(indent);
                string input = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (!options.Contains(input))
                {
                    print($"{indent}That's not a valid option.");
                    continue;
                }
                return input;
            }
        }

        /// <summary>Interactively locate the game install path to update.</summary>
        /// <param name="toolkit">The mod toolkit.</param>
        /// <param name="context">The installer context.</param>
        /// <param name="specifiedPath">The path specified as a command-line argument (if any), which should override automatic path detection.</param>
        private DirectoryInfo InteractivelyGetInstallPath(ModToolkit toolkit, InstallerContext context, string specifiedPath)
        {
            // use specified path
            if (specifiedPath != null)
            {
                var dir = new DirectoryInfo(specifiedPath);
                if (!dir.Exists)
                {
                    this.PrintError($"You specified --game-path \"{specifiedPath}\", but that folder doesn't exist.");
                    return null;
                }
                if (!context.LooksLikeGameFolder(dir))
                {
                    this.PrintError($"You specified --game-path \"{specifiedPath}\", but that folder doesn't contain the Stardew Valley executable.");
                    return null;
                }
                return dir;
            }

            // let user choose detected path
            DirectoryInfo[] defaultPaths = this.DetectGameFolders(toolkit, context).ToArray();
            if (defaultPaths.Any())
            {
                this.PrintInfo("Where do you want to add or remove SMAPI?");
                Console.WriteLine();
                for (int i = 0; i < defaultPaths.Length; i++)
                    this.PrintInfo($"[{i + 1}] {defaultPaths[i].FullName}");
                this.PrintInfo($"[{defaultPaths.Length + 1}] Enter a custom game path.");
                Console.WriteLine();

                string[] validOptions = Enumerable.Range(1, defaultPaths.Length + 1).Select(p => p.ToString(CultureInfo.InvariantCulture)).ToArray();
                string choice = this.InteractivelyChoose("Type the number next to your choice, then press enter.", validOptions);
                int index = int.Parse(choice, CultureInfo.InvariantCulture) - 1;

                if (index < defaultPaths.Length)
                    return defaultPaths[index];
            }
            else
                this.PrintInfo("Oops, couldn't find the game automatically.");

            // let user enter manual path
            while (true)
            {
                // get path from user
                Console.WriteLine();
                this.PrintInfo($"Type the file path to the game directory (the one containing '{Constants.GameDllName}'), then press enter.");
                string path = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(path))
                {
                    this.PrintWarning("You must specify a directory path to continue.");
                    continue;
                }

                // normalize path
                path = context.IsWindows
                    ? path.Replace("\"", "") // in Windows, quotes are used to escape spaces and aren't part of the file path
                    : path.Replace("\\ ", " "); // in Linux/macOS, spaces in paths may be escaped if copied from the command line
                if (path.StartsWith("~/"))
                {
                    string home = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("USERPROFILE");
                    path = Path.Combine(home, path.Substring(2));
                }

                // get directory
                if (File.Exists(path))
                    path = Path.GetDirectoryName(path);
                DirectoryInfo directory = new DirectoryInfo(path);

                // validate path
                if (!directory.Exists)
                {
                    this.PrintWarning("That directory doesn't seem to exist.");
                    continue;
                }
                if (!context.LooksLikeGameFolder(directory))
                {
                    this.PrintWarning("That directory doesn't contain a Stardew Valley executable.");
                    continue;
                }

                // looks OK
                this.PrintInfo("   OK!");
                return directory;
            }
        }

        /// <summary>Get the possible game paths to update.</summary>
        /// <param name="toolkit">The mod toolkit.</param>
        /// <param name="context">The installer context.</param>
        private IEnumerable<DirectoryInfo> DetectGameFolders(ModToolkit toolkit, InstallerContext context)
        {
            HashSet<string> foundPaths = new HashSet<string>();

            // game folder which contains the installer, if any
            {
                DirectoryInfo curPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
                while (curPath?.Parent != null) // must be in a folder (not at the root)
                {
                    if (context.LooksLikeGameFolder(curPath))
                    {
                        foundPaths.Add(curPath.FullName);
                        yield return curPath;
                        break;
                    }

                    curPath = curPath.Parent;
                }
            }

            // game paths detected by toolkit
            foreach (DirectoryInfo dir in toolkit.GetGameFolders())
            {
                if (foundPaths.Add(dir.FullName))
                    yield return dir;
            }
        }

        /// <summary>Interactively move mods out of the appdata directory.</summary>
        /// <param name="properModsDir">The directory which should contain all mods.</param>
        /// <param name="packagedModsDir">The installer directory containing packaged mods.</param>
        private void InteractivelyRemoveAppDataMods(DirectoryInfo properModsDir, DirectoryInfo packagedModsDir)
        {
            // get packaged mods to delete
            string[] packagedModNames = packagedModsDir.GetDirectories().Select(p => p.Name).ToArray();

            // get path
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley");
            DirectoryInfo modDir = new DirectoryInfo(Path.Combine(appDataPath, "Mods"));

            // check if migration needed
            if (!modDir.Exists)
                return;
            this.PrintDebug($"Found an obsolete mod path: {modDir.FullName}");
            this.PrintDebug("   Support for mods here was dropped in SMAPI 1.0 (it was never officially supported).");

            // move mods if no conflicts (else warn)
            foreach (FileSystemInfo entry in modDir.EnumerateFileSystemInfos().Where(this.ShouldCopy))
            {
                // get type
                bool isDir = entry is DirectoryInfo;
                if (!isDir && !(entry is FileInfo))
                    continue; // should never happen

                // delete packaged mods (newer version bundled into SMAPI)
                if (isDir && packagedModNames.Contains(entry.Name, StringComparer.OrdinalIgnoreCase))
                {
                    this.PrintDebug($"   Deleting {entry.Name} because it's bundled into SMAPI...");
                    this.InteractivelyDelete(entry.FullName);
                    continue;
                }

                // check paths
                string newPath = Path.Combine(properModsDir.FullName, entry.Name);
                if (isDir ? Directory.Exists(newPath) : File.Exists(newPath))
                {
                    this.PrintWarning($"   Can't move {entry.Name} because it already exists in your game's mod directory.");
                    continue;
                }

                // move into mods
                this.PrintDebug($"   Moving {entry.Name} into the game's mod directory...");
                this.Move(entry, newPath);
            }

            // delete if empty
            if (modDir.EnumerateFileSystemInfos().Any())
                this.PrintWarning("   You have files in this folder which couldn't be moved automatically. These will be ignored by SMAPI.");
            else
            {
                this.PrintDebug("   Deleted empty directory.");
                modDir.Delete(recursive: true);
            }
        }

        /// <summary>Move a filesystem entry to a new parent directory.</summary>
        /// <param name="entry">The filesystem entry to move.</param>
        /// <param name="newPath">The destination path.</param>
        /// <remarks>We can't use <see cref="FileInfo.MoveTo"/> or <see cref="DirectoryInfo.MoveTo"/>, because those don't work across partitions.</remarks>
        private void Move(FileSystemInfo entry, string newPath)
        {
            // file
            if (entry is FileInfo file)
            {
                file.CopyTo(newPath);
                file.Delete();
            }

            // directory
            else
            {
                Directory.CreateDirectory(newPath);

                DirectoryInfo directory = (DirectoryInfo)entry;
                foreach (FileSystemInfo child in directory.EnumerateFileSystemInfos().Where(this.ShouldCopy))
                    this.Move(child, Path.Combine(newPath, child.Name));

                directory.Delete(recursive: true);
            }
        }

        /// <summary>Get whether a file or folder should be copied from the installer files.</summary>
        /// <param name="entry">The file or folder info.</param>
        private bool ShouldCopy(FileSystemInfo entry)
        {
            switch (entry.Name)
            {
                case "mcs":
                    return false; // ignore macOS symlink
                case "Mods":
                    return false; // Mods folder handled separately
                default:
                    return true;
            }
        }
    }
}

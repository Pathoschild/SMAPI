using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StardewValley;

namespace StardewModdingAPI.Mods.SaveBackup;

/// <summary>The main entry point for the mod.</summary>
public class ModEntry : Mod
{
    /*********
    ** Fields
    *********/
    /// <summary>The number of backups to keep.</summary>
    private readonly int BackupsToKeep = 10;

    /// <summary>The absolute path to the folder in which to store save backups.</summary>
    private readonly string BackupFolder = Path.Combine(Constants.GamePath, "save-backups");

    /// <summary>A unique label for the save backup to create.</summary>
    private readonly string BackupLabel = $"{DateTime.UtcNow:yyyy-MM-dd} - SMAPI {Constants.ApiVersion} with Stardew Valley {Game1.version}";

    /// <summary>The name of the save archive to create.</summary>
    private string FileName => $"{this.BackupLabel}.zip";


    /*********
    ** Public methods
    *********/
    /// <summary>The mod entry point, called after the mod is first loaded.</summary>
    /// <param name="helper">Provides simplified APIs for writing mods.</param>
    public override void Entry(IModHelper helper)
    {
        try
        {
            // init backup folder
            DirectoryInfo backupFolder = new(this.BackupFolder);
            backupFolder.Create();

            // back up & prune saves
            Task
                .Run(() => this.CreateBackup(backupFolder))
                .ContinueWith(_ => this.PruneBackups(backupFolder, this.BackupsToKeep));
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Error backing up saves: {ex}", LogLevel.Error);
        }
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Back up the current saves.</summary>
    /// <param name="backupFolder">The folder containing save backups.</param>
    private void CreateBackup(DirectoryInfo backupFolder)
    {
        try
        {
            // get target path
            FileInfo targetFile = new(Path.Combine(backupFolder.FullName, this.FileName));
            DirectoryInfo fallbackDir = new(Path.Combine(backupFolder.FullName, this.BackupLabel));
            if (targetFile.Exists || fallbackDir.Exists)
            {
                this.Monitor.Log("Already backed up today.");
                return;
            }

            // copy saves to fallback directory (ignore non-save files/folders)
            DirectoryInfo savesDir = new(Constants.SavesPath);
            if (!this.RecursiveCopy(savesDir, fallbackDir, entry => this.MatchSaveFolders(savesDir, entry), copyRoot: false))
            {
                this.Monitor.Log("No saves found.");
                return;
            }

            // compress backup if possible
            if (!this.TryCompress(fallbackDir.FullName, targetFile, out Exception? compressError))
            {
                this.Monitor.Log(Constants.TargetPlatform != GamePlatform.Android
                    ? $"Backed up to {fallbackDir.FullName}." // expected to fail on Android
                    : $"Backed up to {fallbackDir.FullName}. Couldn't compress backup:\n{compressError}"
                );
            }
            else
            {
                this.Monitor.Log($"Backed up to {targetFile.FullName}.");
                fallbackDir.Delete(recursive: true);
            }
        }
        catch (Exception ex)
        {
            this.Monitor.Log("Couldn't back up saves (see log file for details).", LogLevel.Warn);
            this.Monitor.Log(ex.ToString());
        }
    }

    /// <summary>Remove old backups if we've exceeded the limit.</summary>
    /// <param name="backupFolder">The folder containing save backups.</param>
    /// <param name="backupsToKeep">The number of backups to keep.</param>
    private void PruneBackups(DirectoryInfo backupFolder, int backupsToKeep)
    {
        try
        {
            var oldBackups = backupFolder
                .GetFileSystemInfos()
                .OrderByDescending(p => p.CreationTimeUtc)
                .Skip(backupsToKeep);

            foreach (FileSystemInfo entry in oldBackups)
            {
                try
                {
                    this.Monitor.Log($"Deleting {entry.Name}...");
                    if (entry is DirectoryInfo folder)
                        folder.Delete(recursive: true);
                    else
                        entry.Delete();
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Error deleting old save backup '{entry.Name}': {ex}", LogLevel.Error);
                }
            }
        }
        catch (Exception ex)
        {
            this.Monitor.Log("Couldn't remove old backups (see log file for details).", LogLevel.Warn);
            this.Monitor.Log(ex.ToString());
        }
    }

    /// <summary>Create a zip using the best available method.</summary>
    /// <param name="sourcePath">The file or directory path to zip.</param>
    /// <param name="destination">The destination file to create.</param>
    /// <param name="error">The error which occurred trying to compress, if applicable. This is <see cref="NotSupportedException"/> if compression isn't supported on this platform.</param>
    /// <returns>Returns whether compression succeeded.</returns>
    private bool TryCompress(string sourcePath, FileInfo destination, [NotNullWhen(false)] out Exception? error)
    {
        try
        {
            if (Constants.TargetPlatform == GamePlatform.Mac)
                this.CompressUsingMacProcess(sourcePath, destination); // due to limitations with the bundled Mono on macOS, we can't reference System.IO.Compression
            else
                this.CompressUsingNetFramework(sourcePath, destination);

            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex;
            return false;
        }
    }

    /// <summary>Create a zip using the .NET compression library.</summary>
    /// <param name="sourcePath">The file or directory path to zip.</param>
    /// <param name="destination">The destination file to create.</param>
    /// <exception cref="NotSupportedException">The compression libraries aren't available on this system.</exception>
    private void CompressUsingNetFramework(string sourcePath, FileInfo destination)
    {
        // get compress method
        MethodInfo createFromDirectory;
        try
        {
            // create compressed backup
            Assembly coreAssembly = Assembly.Load("System.IO.Compression, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            Assembly fsAssembly = Assembly.Load("System.IO.Compression.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            Type compressionLevelType = coreAssembly.GetType("System.IO.Compression.CompressionLevel") ?? throw new InvalidOperationException("Can't load CompressionLevel type.");
            Type zipFileType = fsAssembly.GetType("System.IO.Compression.ZipFile") ?? throw new InvalidOperationException("Can't load ZipFile type.");
            createFromDirectory = zipFileType.GetMethod("CreateFromDirectory", new[] { typeof(string), typeof(string), compressionLevelType, typeof(bool) }) ?? throw new InvalidOperationException("Can't load ZipFile.CreateFromDirectory method.");
        }
        catch (Exception ex)
        {
            throw new NotSupportedException("Couldn't load the .NET compression libraries on this system.", ex);
        }

        // compress file
        createFromDirectory.Invoke(null, new object[] { sourcePath, destination.FullName, CompressionLevel.Fastest, false });
    }

    /// <summary>Create a zip using a process command on macOS.</summary>
    /// <param name="sourcePath">The file or directory path to zip.</param>
    /// <param name="destination">The destination file to create.</param>
    private void CompressUsingMacProcess(string sourcePath, FileInfo destination)
    {
        DirectoryInfo saveFolder = new(sourcePath);
        ProcessStartInfo startInfo = new()
        {
            FileName = "zip",
            Arguments = $"-rq \"{destination.FullName}\" \"{saveFolder.Name}\" -x \"*.DS_Store\" -x \"__MACOSX\"",
            WorkingDirectory = $"{saveFolder.FullName}/../",
            CreateNoWindow = true
        };
        new Process { StartInfo = startInfo }.Start();
    }

    /// <summary>Recursively copy a directory or file.</summary>
    /// <param name="source">The file or folder to copy.</param>
    /// <param name="targetFolder">The folder to copy into.</param>
    /// <param name="copyRoot">Whether to copy the root folder itself, or <c>false</c> to only copy its contents.</param>
    /// <param name="filter">A filter which matches the files or directories to copy, or <c>null</c> to copy everything.</param>
    /// <remarks>Derived from the SMAPI installer code.</remarks>
    /// <returns>Returns whether any files were copied.</returns>
    private bool RecursiveCopy(FileSystemInfo source, DirectoryInfo targetFolder, Func<FileSystemInfo, bool>? filter, bool copyRoot = true)
    {
        if (!source.Exists || filter?.Invoke(source) == false)
            return false;

        bool anyCopied = false;

        switch (source)
        {
            case FileInfo sourceFile:
                targetFolder.Create();
                sourceFile.CopyTo(Path.Combine(targetFolder.FullName, sourceFile.Name));
                anyCopied = true;
                break;

            case DirectoryInfo sourceDir:
                DirectoryInfo targetSubfolder = copyRoot ? new DirectoryInfo(Path.Combine(targetFolder.FullName, sourceDir.Name)) : targetFolder;
                foreach (var entry in sourceDir.EnumerateFileSystemInfos())
                    anyCopied = this.RecursiveCopy(entry, targetSubfolder, filter) || anyCopied;
                break;

            default:
                throw new NotSupportedException($"Unknown filesystem info type '{source.GetType().FullName}'.");
        }

        return anyCopied;
    }

    /// <summary>A copy filter which matches save folders.</summary>
    /// <param name="savesFolder">The folder containing save folders.</param>
    /// <param name="entry">The current entry to check under <paramref name="savesFolder"/>.</param>
    private bool MatchSaveFolders(DirectoryInfo savesFolder, FileSystemInfo entry)
    {
        // only need to filter top-level entries
        string? parentPath = (entry as FileInfo)?.DirectoryName ?? (entry as DirectoryInfo)?.Parent?.FullName;
        if (parentPath != savesFolder.FullName)
            return true;


        // match folders with Name_ID format
        return
            entry is DirectoryInfo
            && ulong.TryParse(entry.Name.Split('_').Last(), out _);
    }
}

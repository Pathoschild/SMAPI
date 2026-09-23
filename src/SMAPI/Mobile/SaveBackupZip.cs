using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI.Framework;
using StardewValley;

namespace StardewModdingAPI.Mobile;

// Original source code by SMAPI.SaveBackup
internal class SaveBackupZip
{
    IMonitor Monitor => SCore.Instance.SMAPIMonitor;
    readonly int BackupsToKeep = 20;
    readonly string BackupFolder = Path.Combine(EarlyConstants.ExternalFilesDir, "save-backups");

    readonly string BackupLabel =
       $"{DateTime.UtcNow:yyyy-MM-dd}" +
        $" - SMAPI {Constants.ApiVersionForAndroid} - {SMAPIAndroidBuild.BuildCode}" +
        $" with Stardew Valley {Game1.version}";

    string FileName => $"{this.BackupLabel}.zip";

    public void Start()
    {
        this.Monitor.Log("Starting saves backup...");
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
            if (!this.TryCompressDir(fallbackDir.FullName, targetFile, out Exception? compressError))
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

    /// <summary>Try to create a compressed zip file for a directory.</summary>
    /// <param name="sourcePath">The directory path to zip.</param>
    /// <param name="destination">The destination file to create.</param>
    /// <param name="error">The error which occurred trying to compress, if applicable. This is <see cref="NotSupportedException"/> if compression isn't supported on this platform.</param>
    /// <returns>Returns whether compression succeeded.</returns>
    private bool TryCompressDir(string sourcePath, FileInfo destination, [NotNullWhen(false)] out Exception? error)
    {
        try
        {
            ZipFile.CreateFromDirectory(sourcePath, destination.FullName, CompressionLevel.Fastest, false);

            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex;
            return false;
        }
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

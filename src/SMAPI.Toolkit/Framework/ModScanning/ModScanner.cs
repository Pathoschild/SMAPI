using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Serialization.Models;
using StardewModdingAPI.Toolkit.Utilities.PathLookups;

namespace StardewModdingAPI.Toolkit.Framework.ModScanning;

/// <summary>Scans folders for mod data.</summary>
public class ModScanner
{
    /*********
    ** Fields
    *********/
    /// <summary>The JSON helper with which to read manifests.</summary>
    private readonly JsonHelper JsonHelper;

    /// <summary>A list of filesystem entry names to ignore when checking whether a folder should be treated as a mod.</summary>
    private readonly HashSet<Regex> IgnoreFilesystemNames = new()
    {
        new Regex(@"^__folder_managed_by_vortex$", RegexOptions.Compiled | RegexOptions.IgnoreCase), // Vortex mod manager
        new Regex(@"(?:^\._|^\.DS_Store$|^__MACOSX$|^mcs$)", RegexOptions.Compiled | RegexOptions.IgnoreCase), // macOS
        new Regex(@"^(?:desktop\.ini|Thumbs\.db)$", RegexOptions.Compiled | RegexOptions.IgnoreCase) // Windows
    };

    /// <summary>A list of file extensions to ignore when searching for mod files.</summary>
    private readonly HashSet<string> IgnoreFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // text
        ".doc",
        ".docx",
        ".md",
        ".rtf",
        ".txt",

        // images
        ".bmp",
        ".gif",
        ".ico",
        ".jpeg",
        ".jpg",
        ".png",
        ".psd",
        ".tif",

        // archives
        ".rar",
        ".zip",

        // backup files
        ".backup",
        ".bak",
        ".old",

        // Windows shortcut files
        ".url",
        ".lnk"
    };

    /// <summary>The extensions for packed content files.</summary>
    private readonly HashSet<string> StrictXnbModExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".xgs",
        ".xnb",
        ".xsb",
        ".xwb"
    };

    /// <summary>The extensions for files which an XNB mod may contain, in addition to <see cref="StrictXnbModExtensions"/>.</summary>
    private readonly HashSet<string> PotentialXnbModExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".json",
        ".yaml"
    };

    /// <summary>The name of the marker file added by Vortex to indicate it's managing the folder.</summary>
    private readonly string VortexMarkerFileName = "__folder_managed_by_vortex";

    /// <summary>The name for a mod's configuration JSON file.</summary>
    private readonly string ConfigFileName = "config.json";


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="jsonHelper">The JSON helper with which to read manifests.</param>
    public ModScanner(JsonHelper jsonHelper)
    {
        this.JsonHelper = jsonHelper;
    }

    /// <summary>Extract information about all mods in the given folder.</summary>
    /// <param name="rootPath">The root folder containing mods.</param>
    /// <param name="useCaseInsensitiveFilePaths">Whether to match file paths case-insensitively, even on Linux.</param>
    public IEnumerable<ModFolder> GetModFolders(string rootPath, bool useCaseInsensitiveFilePaths)
    {
        DirectoryInfo root = new(rootPath);
        return this.GetModFolders(root, root, useCaseInsensitiveFilePaths);
    }

    /// <summary>Extract information about all mods in the given folder.</summary>
    /// <param name="rootPath">The root folder containing mods. Only the <paramref name="modPath"/> will be searched, but this field allows it to be treated as a potential mod folder of its own.</param>
    /// <param name="modPath">The mod path to search.</param>
    /// <param name="useCaseInsensitiveFilePaths">Whether to match file paths case-insensitively, even on Linux.</param>
    public IEnumerable<ModFolder> GetModFolders(string rootPath, string modPath, bool useCaseInsensitiveFilePaths)
    {
        return this.GetModFolders(root: new DirectoryInfo(rootPath), folder: new DirectoryInfo(modPath), useCaseInsensitiveFilePaths: useCaseInsensitiveFilePaths);
    }

    /// <summary>Extract information from a mod folder.</summary>
    /// <param name="root">The root folder containing mods.</param>
    /// <param name="searchFolder">The folder to search for a mod.</param>
    /// <param name="useCaseInsensitiveFilePaths">Whether to match file paths case-insensitively, even on Linux.</param>
    public ModFolder ReadFolder(DirectoryInfo root, DirectoryInfo searchFolder, bool useCaseInsensitiveFilePaths)
    {
        // find manifest.json
        FileInfo? manifestFile = this.FindManifest(searchFolder, useCaseInsensitiveFilePaths);

        // set appropriate invalid-mod error
        if (manifestFile == null)
        {
            FileInfo[] files = this.RecursivelyGetFiles(searchFolder).ToArray();
            FileInfo[] relevantFiles = files.Where(this.IsRelevant).ToArray();

            // empty Vortex folder
            // (this filters relevant files internally so it can check for the normally-ignored Vortex marker file)
            if (this.IsEmptyVortexFolder(files))
                return new ModFolder(root, searchFolder, ModType.Invalid, null, ModParseError.EmptyVortexFolder, "it's an empty Vortex folder (is the mod disabled in Vortex?).");

            // empty folder
            if (!relevantFiles.Any())
                return new ModFolder(root, searchFolder, ModType.Invalid, null, ModParseError.EmptyFolder, "it's an empty folder.");

            // XNB mod
            if (this.IsXnbMod(relevantFiles))
                return new ModFolder(root, searchFolder, ModType.Xnb, null, ModParseError.XnbMod, "it's not a SMAPI mod (see https://smapi.io/xnb for info).");

            // SMAPI installer
            if (relevantFiles.Any(p => p.Name is "install on Linux.sh" or "install on macOS.command" or "install on Windows.bat"))
                return new ModFolder(root, searchFolder, ModType.Invalid, null, ModParseError.ManifestMissing, "the SMAPI installer isn't a mod (you can delete this folder after running the installer file).");

            // not a mod?
            return new ModFolder(root, searchFolder, ModType.Invalid, null, ModParseError.ManifestMissing, "it contains files, but none of them are manifest.json.");
        }

        // read mod info
        Manifest? manifest = null;
        ModParseError error = ModParseError.None;
        string? errorText = null;
        {
            try
            {
                if (!this.JsonHelper.ReadJsonFileIfExists<Manifest>(manifestFile.FullName, out manifest))
                {
                    error = ModParseError.ManifestInvalid;
                    errorText = "its manifest is invalid.";
                }
            }
            catch (SParseException ex)
            {
                error = ModParseError.ManifestInvalid;
                errorText = $"parsing its manifest failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                error = ModParseError.ManifestInvalid;
                errorText = $"parsing its manifest failed:\n{ex}";
            }
        }

        // get mod type
        ModType type;
        {
            bool isContentPack = !string.IsNullOrWhiteSpace(manifest?.ContentPackFor?.UniqueID);
            bool isSmapi = !string.IsNullOrWhiteSpace(manifest?.EntryDll);

            if (isContentPack == isSmapi)
                type = ModType.Invalid;
            else if (isContentPack)
                type = ModType.ContentPack;
            else
                type = ModType.Smapi;
        }

        // build result
        return new ModFolder(root, manifestFile.Directory!, type, manifest, error, errorText);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Recursively extract information about all mods in the given folder.</summary>
    /// <param name="root">The root mod folder.</param>
    /// <param name="folder">The folder to search for mods.</param>
    /// <param name="useCaseInsensitiveFilePaths">Whether to match file paths case-insensitively, even on Linux.</param>
    private IEnumerable<ModFolder> GetModFolders(DirectoryInfo root, DirectoryInfo folder, bool useCaseInsensitiveFilePaths)
    {
        bool isRoot = folder.FullName == root.FullName;

        // skip
        if (!isRoot)
        {
            if (folder.Name.StartsWith("."))
            {
                yield return new ModFolder(root, folder, ModType.Ignored, null, ModParseError.IgnoredFolder, "ignored folder because its name starts with a dot.");
                yield break;
            }
            if (!this.IsRelevant(folder))
                yield break;
        }

        // find mods in subfolders
        if (this.IsModSearchFolder(root, folder))
        {
            IEnumerable<ModFolder> subfolders = folder.EnumerateDirectories().SelectMany(sub => this.GetModFolders(root, sub, useCaseInsensitiveFilePaths));
            if (!isRoot)
                subfolders = this.TryConsolidate(root, folder, subfolders.ToArray());
            foreach (ModFolder subfolder in subfolders)
                yield return subfolder;
        }

        // treat as mod folder
        else
            yield return this.ReadFolder(root, folder, useCaseInsensitiveFilePaths);
    }

    /// <summary>Consolidate adjacent folders into one mod folder, if possible.</summary>
    /// <param name="root">The folder containing both parent and subfolders.</param>
    /// <param name="parentFolder">The parent folder to consolidate, if possible.</param>
    /// <param name="subfolders">The subfolders to consolidate, if possible.</param>
    private IEnumerable<ModFolder> TryConsolidate(DirectoryInfo root, DirectoryInfo parentFolder, ModFolder[] subfolders)
    {
        if (subfolders.Length > 1)
        {
            // a collection of empty folders
            if (subfolders.All(p => p.ManifestParseError == ModParseError.EmptyFolder))
                return new[] { new ModFolder(root, parentFolder, ModType.Invalid, null, ModParseError.EmptyFolder, subfolders[0].ManifestParseErrorText) };

            // an XNB mod
            if (subfolders.All(p => p.Type == ModType.Xnb || p.ManifestParseError == ModParseError.EmptyFolder))
                return new[] { new ModFolder(root, parentFolder, ModType.Xnb, null, ModParseError.XnbMod, subfolders[0].ManifestParseErrorText) };
        }

        return subfolders;
    }

    /// <summary>Find the manifest for a mod folder.</summary>
    /// <param name="folder">The folder to search.</param>
    /// <param name="useCaseInsensitiveFilePaths">Whether to match file paths case-insensitively, even on Linux.</param>
    private FileInfo? FindManifest(DirectoryInfo folder, bool useCaseInsensitiveFilePaths)
    {
        // check for conventional manifest in current folder
        const string defaultName = "manifest.json";
        FileInfo file = new(Path.Combine(folder.FullName, defaultName));
        if (file.Exists)
            return file;

        // check for manifest with incorrect capitalization
        if (useCaseInsensitiveFilePaths)
        {
            CaseInsensitiveFileLookup fileLookup = new(folder.FullName, SearchOption.TopDirectoryOnly); // don't use GetCachedFor, since we only need it temporarily
            file = fileLookup.GetFile(defaultName);
            return file.Exists
                ? file
                : null;
        }

        // not found
        return null;
    }

    /// <summary>Get whether a given folder should be treated as a search folder (i.e. look for subfolders containing mods).</summary>
    /// <param name="root">The root mod folder.</param>
    /// <param name="folder">The folder to search for mods.</param>
    private bool IsModSearchFolder(DirectoryInfo root, DirectoryInfo folder)
    {
        if (root.FullName == folder.FullName)
            return true;

        DirectoryInfo[] subfolders = folder.GetDirectories().Where(this.IsRelevant).ToArray();
        FileInfo[] files = folder.GetFiles().Where(this.IsRelevant).ToArray();
        return subfolders.Any() && !files.Any();
    }

    /// <summary>Recursively get all files in a folder.</summary>
    /// <param name="folder">The root folder to search.</param>
    private IEnumerable<FileInfo> RecursivelyGetFiles(DirectoryInfo folder)
    {
        foreach (FileSystemInfo entry in folder.GetFileSystemInfos())
        {
            if (entry is DirectoryInfo && !this.IsRelevant(entry))
                continue;

            if (entry is FileInfo file)
                yield return file;

            if (entry is DirectoryInfo subfolder)
            {
                foreach (FileInfo subfolderFile in this.RecursivelyGetFiles(subfolder))
                    yield return subfolderFile;
            }
        }
    }

    /// <summary>Get whether a file or folder is relevant when deciding how to process a mod folder.</summary>
    /// <param name="entry">The file or folder.</param>
    private bool IsRelevant(FileSystemInfo entry)
    {
        // ignored file extensions and any files starting with "."
        if ((entry is FileInfo file) && (this.IgnoreFileExtensions.Contains(file.Extension) || file.Name.StartsWith(".")))
            return false;

        // ignored entry name
        return !this.IgnoreFilesystemNames.Any(p => p.IsMatch(entry.Name));
    }

    /// <summary>Get whether a set of files looks like an XNB mod.</summary>
    /// <param name="files">The files in the mod.</param>
    private bool IsXnbMod(IEnumerable<FileInfo> files)
    {
        bool hasXnbFile = false;

        foreach (FileInfo file in files.Where(this.IsRelevant))
        {
            if (this.StrictXnbModExtensions.Contains(file.Extension))
            {
                hasXnbFile = true;
                continue;
            }

            if (!this.PotentialXnbModExtensions.Contains(file.Extension))
                return false;
        }

        return hasXnbFile;
    }

    /// <summary>Get whether a set of files looks like an XNB mod.</summary>
    /// <param name="files">The files in the mod.</param>
    private bool IsEmptyVortexFolder(IEnumerable<FileInfo> files)
    {
        bool hasVortexMarker = false;

        foreach (FileInfo file in files)
        {
            if (file.Name == this.VortexMarkerFileName)
            {
                hasVortexMarker = true;
                continue;
            }

            if (this.IsRelevant(file) && file.Name != this.ConfigFileName)
                return false;
        }

        return hasVortexMarker;
    }
}

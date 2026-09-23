using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using Mono.Cecil;
using PackSMAPIZip;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

internal class Program
{
    const string StardewModdingAPIFileName = "StardewModdingAPI.dll";

    static string GetSMAPIVersion(string dllFilePath)
    {
        var assembly = AssemblyDefinition.ReadAssembly(dllFilePath);
        var constantsType = assembly.MainModule.Types.Single(t => t.FullName == "StardewModdingAPI.EarlyConstants");
        var RawApiVersionForAndroidField = constantsType.Fields.Single(p => p.Name == "RawApiVersionForAndroid");
        string version = RawApiVersionForAndroidField.Constant as string;
        return version;
    }
    static async Task Main(string[] args)
    {
        try
        {
            await StartRunPack(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.Read();
        }
    }

    static string GetOutputSMAPIZipFileName(string smapiBinDir)
    {
        string SMAPIVersionName = GetSMAPIVersion(Path.Combine(smapiBinDir, StardewModdingAPIFileName)).ToString();
        string finalName = $"SMAPI-Android-{SMAPIVersionName}";
        return finalName;
    }

    static async Task StartRunPack(string[] args)
    {

        //Create Folder SMAPI-x.x.x.x
        string SMAPIBinDir = args.Length > 0 && Directory.Exists(args[0])
            ? args[0]
            : FindSmapiBinDir();
        var outputSMAPIZipFileName = GetOutputSMAPIZipFileName(SMAPIBinDir);
        Console.WriteLine("Start Pack: " + outputSMAPIZipFileName);
        string smapiOutputDir = Path.Combine(Directory.GetCurrentDirectory(), outputSMAPIZipFileName);
        if (Directory.Exists(smapiOutputDir))
            Directory.Delete(smapiOutputDir, true);
        Directory.CreateDirectory(smapiOutputDir);

        //clone dll files
        string[] dependencies = File.ReadAllLines("dependencies.txt");
        foreach (string dllFileName in dependencies)
        {
            string srcPath = Path.Combine(SMAPIBinDir, dllFileName);
            string destPath = Path.Combine(smapiOutputDir, dllFileName);
            Console.WriteLine("try add: " + srcPath);
            File.Copy(srcPath, destPath, true);
        }

        //build smapi-internal folder
        Console.WriteLine("try build smapi-internal folder");
        string smapiInternalDir = Path.Combine(smapiOutputDir, "smapi-internal");
        Directory.CreateDirectory(smapiInternalDir);
        CloneDirectory(Path.Combine(SMAPIBinDir, "i18n"), Path.Combine(smapiInternalDir, "i18n"));
        File.Copy(Path.Combine(SMAPIBinDir, "SMAPI.config.json"), Path.Combine(smapiInternalDir, "config.json"));
        File.Copy(Path.Combine(SMAPIBinDir, "SMAPI.blacklist.json"), Path.Combine(smapiInternalDir, "blacklist.json"));

        await DownloadSMAPIMetadataJson();
        File.Copy("SMAPI.metadata.json", Path.Combine(smapiInternalDir, "metadata.json"));

        Console.WriteLine("done added smapi-internal");


        //Pack SMAPI-x.x.x.x.zip from directory SMAPI-x.x.x.x
        string outputZipFilePath = Path.Combine(Directory.GetCurrentDirectory(), outputSMAPIZipFileName + ".zip");
        string stardewModdingAPIFilePath = Path.Combine(SMAPIBinDir, StardewModdingAPIFileName);
        var buildTool = new SMAPIAndroidBuildTool(stardewModdingAPIFilePath);
        string buildCode = $"{buildTool.GetBuildCode()}";
        outputZipFilePath = outputZipFilePath.Replace(".zip", $"-({buildCode}).zip");

        Console.WriteLine("try pack SMPAI.zip output at " + outputZipFilePath);
        using var zipStream = File.Open(outputZipFilePath, FileMode.Create, FileAccess.ReadWrite);
        ZipFile.CreateFromDirectory(smapiOutputDir, zipStream, CompressionLevel.Fastest, true);
        Console.WriteLine("done pack & file size: " + zipStream.Length);
        zipStream.Close();

        //clean up
        Directory.Delete(smapiOutputDir, true);
        Console.WriteLine("done delete folder: " + smapiOutputDir);

        Console.WriteLine("Successfully Pack SMAPI Zip");
        Console.WriteLine("result file: " + new FileInfo(outputZipFilePath).Name);
    }

    private static async Task DownloadSMAPIMetadataJson()
    {
        var url = "https://raw.githubusercontent.com/Pathoschild/SMAPI/develop/src/SMAPI.Web/wwwroot/SMAPI.metadata.json";
        var outputPath = "SMAPI.metadata.json";

        using HttpClient client = new HttpClient();
        var json = await client.GetStringAsync(url);
        await File.WriteAllTextAsync(outputPath, json);
        Console.WriteLine("downloaded: " + outputPath);
    }

    static string FindSmapiBinDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string repoBin = Path.Combine(dir.FullName, "src", "SMAPI", "bin", "ARM64");
            if (Directory.Exists(repoBin))
            {
                foreach (string name in new[] { "Android Release", "Android Debug" })
                {
                    string candidate = Path.Combine(repoBin, name);
                    if (File.Exists(Path.Combine(candidate, StardewModdingAPIFileName)))
                        return candidate;
                }
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Couldn't find src/SMAPI/bin/ARM64/Android Release. Build SMAPI with configuration \"Android Release\" first.");
    }

    static string GetParentDirectory(string currentDir, int levelsUp)
    {
        string targetDir = currentDir;

        for (int i = 0; i < levelsUp; i++)
        {
            targetDir = Directory.GetParent(targetDir)?.FullName;
            if (targetDir == null)
                throw new InvalidOperationException("limit directory levels up");
        }

        return targetDir;
    }
    static void CloneDirectory(string sourceDir, string destDir)
    {
        // Ensure destination directory exists
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // Copy all files in the source directory to the destination directory
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true); // 'true' allows overwriting existing files
        }

        // Recursively copy subdirectories
        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CloneDirectory(subDir, destSubDir);
        }
    }
}

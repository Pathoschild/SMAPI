using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace StardewModdingAPI.Web;

/// <summary>The main app entry point.</summary>
public class Program
{
    /*********
    ** Fields
    *********/
    /// <summary>The backing field for <see cref="CacheBustValue"/>.</summary>
    private static string? CacheBustValueImpl;


    /*********
    ** Accessors
    *********/
    /// <summary>A value which can be appended to URLs to force browsers to fetch new data when the server is redeployed.</summary>
    public static string CacheBustValue
    {
        get
        {
            if (Program.CacheBustValueImpl is null)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileInfo fileInfo = new(assembly.Location);
                Program.CacheBustValueImpl = new DateTimeOffset(fileInfo.LastWriteTime).ToUnixTimeSeconds().ToString();
            }

            return Program.CacheBustValueImpl;
        }
    }

    /// <summary>The cache-bust value for the latest fetched mod dataset, if available.</summary>
    public static string? ModDatasetCacheBustValue { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>The main app entry point.</summary>
    /// <param name="args">The command-line arguments.</param>
    public static void Main(string[] args)
    {
        Host
            .CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(builder => builder
                .CaptureStartupErrors(true)
                .UseSetting("detailedErrors", "true")
                .UseStartup<Startup>()
            )
            .Build()
            .Run();
    }
}

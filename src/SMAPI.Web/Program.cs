using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace StardewModdingAPI.Web;

/// <summary>The main app entry point.</summary>
public class Program
{
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

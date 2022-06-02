using System.Collections.Generic;
using System.Net;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Web.Framework;
using StardewModdingAPI.Web.Framework.Caching.Mods;
using StardewModdingAPI.Web.Framework.Caching.Wiki;
using StardewModdingAPI.Web.Framework.Clients.Chucklefish;
using StardewModdingAPI.Web.Framework.Clients.CurseForge;
using StardewModdingAPI.Web.Framework.Clients.GitHub;
using StardewModdingAPI.Web.Framework.Clients.ModDrop;
using StardewModdingAPI.Web.Framework.Clients.Nexus;
using StardewModdingAPI.Web.Framework.Clients.Pastebin;
using StardewModdingAPI.Web.Framework.Compression;
using StardewModdingAPI.Web.Framework.ConfigModels;
using StardewModdingAPI.Web.Framework.RedirectRules;
using StardewModdingAPI.Web.Framework.Storage;

namespace StardewModdingAPI.Web;

/// <summary>The web app startup configuration.</summary>
internal class Startup
{
    /*********
    ** Accessors
    *********/
    /// <summary>The web app configuration.</summary>
    public IConfigurationRoot Configuration { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="env">The hosting environment.</param>
    public Startup(IWebHostEnvironment env)
    {
        this.Configuration = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>The method called by the runtime to add services to the container.</summary>
    /// <param name="services">The service injection container.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // init basic services
        services
            .Configure<ApiClientsConfig>(this.Configuration.GetSection("ApiClients"))
            .Configure<BackgroundServicesConfig>(this.Configuration.GetSection("BackgroundServices"))
            .Configure<ModCompatibilityListConfig>(this.Configuration.GetSection("ModCompatibilityList"))
            .Configure<ModUpdateCheckConfig>(this.Configuration.GetSection("ModUpdateCheck"))
            .Configure<SiteConfig>(this.Configuration.GetSection("Site"))
            .Configure<RouteOptions>(options => options.ConstraintMap.Add("semanticVersion", typeof(VersionConstraint)))
            .AddLogging()
            .AddMemoryCache();

        // init MVC
        services
            .AddControllers()
            .AddNewtonsoftJson(options => this.ConfigureJsonNet(options.SerializerSettings))
            .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new InternalControllerFeatureProvider()));
        services
            .AddRazorPages();

        // init storage
        services.AddSingleton<IModCacheRepository>(new ModCacheMemoryRepository());
        services.AddSingleton<IWikiCacheRepository>(new WikiCacheMemoryRepository());

        // init Hangfire
        services
            .AddHangfire((_, config) =>
            {
                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseMemoryStorage();
            });

        // init background service
        {
            BackgroundServicesConfig config = this.Configuration.GetSection("BackgroundServices").Get<BackgroundServicesConfig>();
            if (config.Enabled)
                services.AddHostedService<BackgroundService>();
        }

        // init API clients
        {
            ApiClientsConfig api = this.Configuration.GetSection("ApiClients").Get<ApiClientsConfig>();
            string version = this.GetType().Assembly.GetName().Version!.ToString(3);
            string userAgent = string.Format(api.UserAgent, version);

            services.AddSingleton<IChucklefishClient>(new ChucklefishClient(
                userAgent: userAgent,
                baseUrl: api.ChucklefishBaseUrl,
                modPageUrlFormat: api.ChucklefishModPageUrlFormat
            ));

            services.AddSingleton<ICurseForgeClient>(new CurseForgeClient(
                userAgent: userAgent,
                apiUrl: api.CurseForgeBaseUrl,
                apiKey: api.CurseForgeApiKey
            ));

            services.AddSingleton<IGitHubClient>(new GitHubClient(
                baseUrl: api.GitHubBaseUrl,
                userAgent: userAgent,
                acceptHeader: api.GitHubAcceptHeader,
                username: api.GitHubUsername,
                password: api.GitHubPassword
            ));

            services.AddSingleton<IModDropClient>(new ModDropClient(
                userAgent: userAgent,
                apiUrl: api.ModDropApiUrl,
                modUrlFormat: api.ModDropModPageUrl
            ));

            if (!string.IsNullOrWhiteSpace(api.NexusApiKey))
            {
                services.AddSingleton<INexusClient>(new NexusClient(
                    webUserAgent: userAgent,
                    webBaseUrl: api.NexusBaseUrl,
                    webModUrlFormat: api.NexusModUrlFormat,
                    webModScrapeUrlFormat: api.NexusModScrapeUrlFormat,
                    apiAppVersion: version,
                    apiKey: api.NexusApiKey
                ));
            }
            else
            {
                services.AddSingleton<INexusClient>(new DisabledNexusClient());
            }

            services.AddSingleton<IPastebinClient>(new PastebinClient(
                baseUrl: api.PastebinBaseUrl,
                userAgent: userAgent
            ));
        }

        // init helpers
        services
            .AddSingleton<IGzipHelper>(new GzipHelper())
            .AddSingleton<IStorageProvider>(serv => new StorageProvider(
                serv.GetRequiredService<IOptions<ApiClientsConfig>>(),
                serv.GetRequiredService<IPastebinClient>(),
                serv.GetRequiredService<IGzipHelper>()
            ));
    }

    /// <summary>The method called by the runtime to configure the HTTP request pipeline.</summary>
    /// <param name="app">The application builder.</param>
    public void Configure(IApplicationBuilder app)
    {
        // basic config
        app.UseDeveloperExceptionPage();
        app
            .UseCors(policy => policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithOrigins("https://smapi.io")
            )
            .UseRewriter(this.GetRedirectRules())
            .UseStaticFiles() // wwwroot folder
            .UseRouting()
            .UseAuthorization()
            .UseEndpoints(p =>
            {
                p.MapControllers();
                p.MapRazorPages();
            });

        // enable Hangfire dashboard
        app.UseHangfireDashboard("/tasks", new DashboardOptions
        {
            IsReadOnlyFunc = context => !JobDashboardAuthorizationFilter.IsLocalRequest(context),
            Authorization = new[] { new JobDashboardAuthorizationFilter() }
        });
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Configure a Json.NET serializer.</summary>
    /// <param name="settings">The serializer settings to edit.</param>
    private void ConfigureJsonNet(JsonSerializerSettings settings)
    {
        foreach (JsonConverter converter in new JsonHelper().JsonSettings.Converters)
            settings.Converters.Add(converter);

        settings.Formatting = Formatting.Indented;
        settings.NullValueHandling = NullValueHandling.Ignore;
    }

    /// <summary>Get the redirect rules to apply.</summary>
    private RewriteOptions GetRedirectRules()
    {
        var redirects = new RewriteOptions()
            // shortcut paths
            .Add(new RedirectPathsToUrlsRule(new Dictionary<string, string>
            {
                // wiki pages
                [@"^/3\.0\.?$"] = "https://stardewvalleywiki.com/Modding:Migrate_to_SMAPI_3.0",
                [@"^/community\.?$"] = "https://stardewvalleywiki.com/Modding:Community",
                [@"^/docs\.?$"] = "https://stardewvalleywiki.com/Modding:Index",
                [@"^/help\.?$"] = "https://stardewvalleywiki.com/Modding:Help",
                [@"^/install\.?$"] = "https://stardewvalleywiki.com/Modding:Player_Guide/Getting_Started#Install_SMAPI",
                [@"^/troubleshoot(.*)$"] = "https://stardewvalleywiki.com/Modding:Player_Guide/Troubleshooting$1",
                [@"^/xnb\.?$"] = "https://stardewvalleywiki.com/Modding:Using_XNB_mods",

                // GitHub docs
                [@"^/package(?:/?(.*))$"] = "https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md#$1",
                [@"^/release(?:/?(.*))$"] = "https://github.com/Pathoschild/SMAPI/blob/develop/docs/release-notes.md#$1",

                // legacy redirects
                [@"^/compat\.?$"] = "https://smapi.io/mods"
            }))

            // legacy paths
            .Add(new RedirectPathsToUrlsRule(this.GetLegacyPathRedirects()))

            // subdomains
            .Add(new RedirectHostsToUrlsRule(HttpStatusCode.PermanentRedirect, host => host switch
            {
                "api.smapi.io" => "smapi.io/api",
                "json.smapi.io" => "smapi.io/json",
                "log.smapi.io" => "smapi.io/log",
                "mods.smapi.io" => "smapi.io/mods",
                _ => host.EndsWith(".smapi.io")
                    ? "smapi.io"
                    : null
            }))

            // redirect to HTTPS (except API for Linux/macOS Mono compatibility)
            .Add(
                new RedirectToHttpsRule(except: req => req.Host.Host == "localhost" || req.Path.StartsWithSegments("/api"))
            );

        return redirects;
    }

    /// <summary>Get the redirects for legacy paths that have been moved elsewhere.</summary>
    private IDictionary<string, string> GetLegacyPathRedirects()
    {
        var redirects = new Dictionary<string, string>();

        // canimod.com => wiki
        var wikiRedirects = new Dictionary<string, string[]>
        {
            ["Modding:Index#Migration_guides"] = new[] { "^/for-devs/updating-a-smapi-mod", "^/guides/updating-a-smapi-mod" },
            ["Modding:Modder_Guide"] = new[] { "^/for-devs/creating-a-smapi-mod", "^/guides/creating-a-smapi-mod", "^/for-devs/creating-a-smapi-mod-advanced-config" },
            ["Modding:Player_Guide"] = new[] { "^/for-players/install-smapi", "^/guides/using-mods", "^/for-players/faqs", "^/for-players/intro", "^/for-players/use-mods", "^/guides/asking-for-help", "^/guides/smapi-faq" },

            ["Modding:Editing_XNB_files"] = new[] { "^/for-devs/creating-an-xnb-mod", "^/guides/creating-an-xnb-mod" },
            ["Modding:Event_data"] = new[] { "^/for-devs/events", "^/guides/events" },
            ["Modding:Gift_taste_data"] = new[] { "^/for-devs/npc-gift-tastes", "^/guides/npc-gift-tastes" },
            ["Modding:IDE_reference"] = new[] { "^/for-devs/creating-a-smapi-mod-ide-primer" },
            ["Modding:Object_data"] = new[] { "^/for-devs/object-data", "^/guides/object-data" },
            ["Modding:Weather_data"] = new[] { "^/for-devs/weather", "^/guides/weather" }
        };
        foreach ((string page, string[] patterns) in wikiRedirects)
        {
            foreach (string pattern in patterns)
                redirects.Add(pattern, "https://stardewvalleywiki.com/" + page);
        }

        return redirects;
    }
}

using Hangfire.Dashboard;

namespace StardewModdingAPI.Web.Framework;

/// <summary>Authorizes requests to access the Hangfire job dashboard.</summary>
internal class JobDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    /*********
    ** Fields
    *********/
    /// <summary>An authorization filter that allows local requests.</summary>
    private static readonly LocalRequestsOnlyAuthorizationFilter LocalRequestsOnlyFilter = new();


    /*********
    ** Public methods
    *********/
    /// <summary>Authorize a request.</summary>
    /// <param name="context">The dashboard context.</param>
    public bool Authorize(DashboardContext context)
    {
        return
            context.IsReadOnly // always allow readonly access
            || JobDashboardAuthorizationFilter.IsLocalRequest(context); // else allow access from localhost
    }

    /// <summary>Get whether a request originated from a user on the server machine.</summary>
    /// <param name="context">The dashboard context.</param>
    public static bool IsLocalRequest(DashboardContext context)
    {
        return JobDashboardAuthorizationFilter.LocalRequestsOnlyFilter.Authorize(context);
    }
}

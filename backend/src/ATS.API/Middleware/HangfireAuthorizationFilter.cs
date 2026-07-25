using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace ATS.API.Middleware;

/// <summary>
/// Restricts Hangfire dashboard to authenticated SuperAdmin users.
/// In development mode, allows all access for convenience.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IWebHostEnvironment _env;

    public HangfireAuthorizationFilter(IWebHostEnvironment env)
    {
        _env = env;
    }

    public bool Authorize(DashboardContext context)
    {
        // Allow unrestricted access in development
        if (_env.IsDevelopment()) return true;

        var httpContext = context.GetHttpContext();

        // Must be authenticated
        if (httpContext.User?.Identity?.IsAuthenticated != true) return false;

        // Must be SuperAdmin role
        return httpContext.User.IsInRole("SuperAdmin");
    }
}

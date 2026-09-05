using Hangfire.Dashboard;

namespace Buy2.Api.Services.PointsAutomation;

public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    private static readonly string[] AllowedRoles = ["Admin", "HRAdmin", "SuperAdmin"];

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return AllowedRoles.Any(user.IsInRole);
    }
}

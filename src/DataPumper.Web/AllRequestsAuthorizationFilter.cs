using Hangfire.Dashboard;

namespace DataPumper.Web
{
    public class AllRequestsAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }
}

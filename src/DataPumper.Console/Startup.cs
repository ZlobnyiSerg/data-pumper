using Hangfire;
using Owin;

namespace DataPumper.Console
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseHangfireDashboard("");
        }
    }
}
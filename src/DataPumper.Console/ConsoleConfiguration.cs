using Microsoft.Extensions.Configuration;

namespace DataPumper.Console
{
    public class ConsoleConfiguration
    {
        public readonly IConfiguration ConfigurationSource;

        public ConsoleConfiguration(IConfiguration configurationSource)
        {
            ConfigurationSource = configurationSource;
        }

        public string ConnectionString => ConfigurationSource.Get<string>("Core:ConnectionString");

        public string TargetConnectionString => ConfigurationSource.Get<string>("Core:TargetConnectionString");

        public string HangFireDashboardUrl => ConfigurationSource.Get<string>("Core:HangFireDashboardUrl", "http://localhost:9019");
        
        public string ScheduleCron => ConfigurationSource.Get<string>("Core:ScheduleCron");

        public string[] TenantCodes
        {
            get
            {
                var tenantCodes = ConfigurationSource.Get<string>("Core:TenantCodes");
                if (string.IsNullOrEmpty(tenantCodes)) return null;

                return tenantCodes.Split(',');
            }
        }
    }
}
